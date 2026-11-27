using System;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Builds PGP test fixtures with BouncyCastle that reproduce the packet structures GnuPG 2.4+
    /// emits by default: signatures issued by a signing-capable SUBKEY (STUD-80429) and one-pass
    /// signatures wrapped in a COMPRESSED packet (STUD-80430). gpg is not available in the build
    /// environment, so these fixtures are generated in-process to exercise the verify fixes.
    /// </summary>
    internal sealed class PgpBcFixture
    {
        /// <summary>Knobs for building keys that exercise the verify fallback's key-validity checks.</summary>
        internal sealed class Options
        {
            public DateTime? MasterCreatedUtc { get; set; }
            public DateTime? SubkeyCreatedUtc { get; set; }
            public long? MasterExpirySeconds { get; set; }
            public long? SubkeyExpirySeconds { get; set; }

            /// <summary>Key flags written on the subkey binding signature; null omits the subpacket.</summary>
            public int? SubkeyKeyFlags { get; set; } = PgpKeyFlags.CanSign;

            /// <summary>Key flags written on the primary key's self-signature; null omits the subpacket.</summary>
            public int? PrimaryKeyFlags { get; set; }

            public bool RevokeSubkey { get; set; }
            public bool RevokePrimary { get; set; }

            /// <summary>Remove the subkey's binding signature from the published ring.</summary>
            public bool StripSubkeyBinding { get; set; }

            /// <summary>Sign with the primary key instead of the subkey.</summary>
            public bool SignWithPrimary { get; set; }

            /// <summary>Add a third key: an encryption-capable subkey (so messages can be encrypted to this fixture).</summary>
            public bool WithEncryptionSubkey { get; set; }

            /// <summary>What the signing subkey's binding carries as its embedded back-signature (0x19).</summary>
            public BackSignatureMode BackSignature { get; set; } = BackSignatureMode.Valid;

            /// <summary>Put the embedded back-signature in the unhashed subpacket area instead of the hashed one.</summary>
            public bool BackSignatureInUnhashedArea { get; set; }

            /// <summary>Makes the (first) subkey binding signature itself lapse after this many seconds.</summary>
            public long? SubkeyBindingExpirySeconds { get; set; }

            /// <summary>
            /// Extra binding signatures added after the first one (e.g. a later restriction or a
            /// later authorization), each with its own creation time and key flags (null omits them).
            /// The back-signature of a later binding follows <see cref="BackSignature"/> unless given here.
            /// </summary>
            public System.Collections.Generic.List<(DateTime CreatedUtc, int? KeyFlags, BackSignatureMode? BackSignature)> LaterBindings { get; }
                = new System.Collections.Generic.List<(DateTime, int?, BackSignatureMode?)>();

            /// <summary>Extra self-certifications made by the primary key itself (creation time, key flags; null omits them).</summary>
            public System.Collections.Generic.List<(DateTime CreatedUtc, int? KeyFlags)> LaterPrimaryCertifications { get; }
                = new System.Collections.Generic.List<(DateTime, int?)>();

            /// <summary>
            /// Self-certifications that name the primary key as issuer but were made by another key (the subkey), so
            /// they do not verify against the primary key.
            /// </summary>
            public System.Collections.Generic.List<(DateTime CreatedUtc, int? KeyFlags)> ForgedPrimaryCertifications { get; }
                = new System.Collections.Generic.List<(DateTime, int?)>();

            /// <summary>The user id of the primary key.</summary>
            public string UserId { get; set; } = "fixture@test.com";

            /// <summary>Direct-key signatures made by the primary key itself (creation time, key flags; null omits them).</summary>
            public System.Collections.Generic.List<(DateTime CreatedUtc, int? KeyFlags)> DirectKeySignatures { get; }
                = new System.Collections.Generic.List<(DateTime, int?)>();

            /// <summary>Revocations of the user-id certifications (type 0x30) made by the primary key; they carry no key flags.</summary>
            public System.Collections.Generic.List<DateTime> UserIdRevocations { get; }
                = new System.Collections.Generic.List<DateTime>();

            /// <summary>
            /// Self-certifications that name a signature algorithm BouncyCastle does not know (so they cannot be
            /// verified): creation time, key flags.
            /// </summary>
            public System.Collections.Generic.List<(DateTime CreatedUtc, int? KeyFlags)> UnverifiableSelfCertifications { get; }
                = new System.Collections.Generic.List<(DateTime, int?)>();
        }

        public enum BackSignatureMode
        {
            /// <summary>Made by the subkey over (primary, subkey): what GnuPG writes.</summary>
            Valid,

            /// <summary>Valid, but without the Issuer Key ID subpacket (the signed data is unchanged).</summary>
            ValidWithoutIssuer,

            /// <summary>No embedded signature at all.</summary>
            Missing,

            /// <summary>Made by an unrelated key instead of the subkey.</summary>
            SignedByWrongKey,

            /// <summary>Made by the subkey, but over a different primary key.</summary>
            WrongPrimary,
        }

        /// <summary>Where to put an ignorable Marker packet (RFC 4880 section 5.8) in a signed message.</summary>
        public enum MarkerPlacement
        {
            None,

            /// <summary>Before the compressed packet (or before the one-pass header when uncompressed).</summary>
            Outside,

            /// <summary>Inside the compressed packet, before the one-pass header.</summary>
            Inside,
        }

        private static readonly byte[] MarkerPacket = { 0xCA, 0x03, (byte)'P', (byte)'G', (byte)'P' };

        private readonly PgpSecretKey _signingSecret;
        private readonly char[] _passphrase;
        private readonly long? _encryptionKeyId;

        public PgpPublicKeyRing PublicRing { get; }

        public byte[] PublicKeyRing { get; }

        /// <summary>The same public key ring (binary) as it was before any revocation was added; equals the ring when none was.</summary>
        public byte[] PublicKeyRingBeforeRevocation { get; }

        /// <summary>Armored secret key ring (null for fixtures that have none, e.g. a grafted ring).</summary>
        public byte[] PrivateKeyRing { get; }

        public string Passphrase => new string(_passphrase);

        private PgpBcFixture(PgpPublicKeyRing publicRing, PgpSecretKey signingSecret, char[] passphrase,
            PgpSecretKeyRing secretRing = null, long? encryptionKeyId = null, PgpPublicKeyRing ringBeforeRevocation = null)
        {
            PublicKeyRingBeforeRevocation = (ringBeforeRevocation ?? publicRing).GetEncoded();
            PublicRing = publicRing;
            _signingSecret = signingSecret;
            _passphrase = passphrase;
            _encryptionKeyId = encryptionKeyId;
            PublicKeyRing = EncodeArmored(publicRing);
            if (secretRing is not null)
            {
                using var buffer = new MemoryStream();
                using (var armored = new ArmoredOutputStream(buffer))
                {
                    secretRing.Encode(armored);
                }

                PrivateKeyRing = buffer.ToArray();
            }
        }

        /// <summary>The (public) signing key, for grafting into another ring.</summary>
        internal PgpPublicKey SigningPublicKey => PublicRing.GetPublicKey(_signingSecret.KeyId);

        public static PgpBcFixture Create(string passphrase = "fixturepass")
            => Create(new Options(), passphrase);

        public static PgpBcFixture Create(Options options, string passphrase = "fixturepass")
        {
            var passChars = passphrase.ToCharArray();
            var random = new SecureRandom();
            var now = DateTime.UtcNow;

            var masterPair = GenerateRsaKeyPair(random);
            var subPair = GenerateRsaKeyPair(random);

            // Master key: certification only. Subkey: signing-capable (mirrors gpg defaults).
            var masterPgpPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, masterPair, options.MasterCreatedUtc ?? now);
            var subPgpPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaSign, subPair, options.SubkeyCreatedUtc ?? now);

            PgpSignatureSubpacketVector masterHashed = null;
            if (options.MasterExpirySeconds.HasValue || options.PrimaryKeyFlags.HasValue)
            {
                var gen = new PgpSignatureSubpacketGenerator();
                if (options.MasterExpirySeconds.HasValue)
                {
                    gen.SetKeyExpirationTime(false, options.MasterExpirySeconds.Value);
                }

                if (options.PrimaryKeyFlags.HasValue)
                {
                    gen.SetKeyFlags(false, options.PrimaryKeyFlags.Value);
                }

                masterHashed = gen.Generate();
            }

            var keyRingGenerator = new PgpKeyRingGenerator(
                PgpSignature.PositiveCertification,
                masterPgpPair,
                options.UserId,
                SymmetricKeyAlgorithmTag.Aes256,
                passChars,
                true,
                masterHashed,
                null,
                random);

            // The binding signature is created when the subkey is: a binding dated after the signing
            // time would (correctly) not be in force for messages signed earlier.
            var subGen = new PgpSignatureSubpacketGenerator();
            var subUnhashedGen = new PgpSignatureSubpacketGenerator();
            var backSignature = CreateBackSignature(options.BackSignature, masterPgpPair, subPgpPair, random, now);
            if (backSignature is not null)
            {
                (options.BackSignatureInUnhashedArea ? subUnhashedGen : subGen).AddEmbeddedSignature(false, backSignature);
            }

            subGen.SetSignatureCreationTime(false, options.SubkeyCreatedUtc ?? now);
            if (options.SubkeyKeyFlags.HasValue)
            {
                subGen.SetKeyFlags(false, options.SubkeyKeyFlags.Value);
            }

            if (options.SubkeyExpirySeconds.HasValue)
            {
                subGen.SetKeyExpirationTime(false, options.SubkeyExpirySeconds.Value);
            }

            if (options.SubkeyBindingExpirySeconds.HasValue)
            {
                subGen.SetSignatureExpirationTime(false, options.SubkeyBindingExpirySeconds.Value);
            }

            keyRingGenerator.AddSubKey(
                subPgpPair, subGen.Generate(), options.BackSignatureInUnhashedArea && backSignature is not null ? subUnhashedGen.Generate() : null);

            if (options.WithEncryptionSubkey)
            {
                var encryptionPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, GenerateRsaKeyPair(random), options.SubkeyCreatedUtc ?? now);
                var encryptionSubpackets = new PgpSignatureSubpacketGenerator();
                encryptionSubpackets.SetSignatureCreationTime(false, options.SubkeyCreatedUtc ?? now);
                encryptionSubpackets.SetKeyFlags(false, PgpKeyFlags.CanEncryptCommunications | PgpKeyFlags.CanEncryptStorage);
                keyRingGenerator.AddSubKey(encryptionPair, encryptionSubpackets.Generate(), null);
            }

            var secretRing = keyRingGenerator.GenerateSecretKeyRing();
            var publicRing = keyRingGenerator.GeneratePublicKeyRing();

            // The first key in the ring is the master; the second is the signing subkey.
            PgpSecretKey masterSecret = null;
            PgpSecretKey subkeySecret = null;
            long? encryptionKeyId = null;
            var secretKeyIndex = 0;
            foreach (PgpSecretKey secretKey in secretRing.GetSecretKeys())
            {
                if (secretKeyIndex == 0)
                {
                    masterSecret = secretKey;
                }
                else if (secretKeyIndex == 1)
                {
                    subkeySecret = secretKey;
                }
                else if (secretKeyIndex == 2)
                {
                    encryptionKeyId = secretKey.KeyId;
                }

                secretKeyIndex++;
            }

            if (masterSecret is null || subkeySecret is null)
            {
                throw new InvalidOperationException("Failed to generate a master key and signing subkey for the fixture.");
            }

            var masterPrivate = masterSecret.ExtractPrivateKey(passChars);

            // Certifications must be added to the keys as published in the PUBLIC ring: the public
            // half carried by a secret key does not know whether it is a master key or a subkey.
            var masterPublic = publicRing.GetPublicKey(masterSecret.KeyId);
            var subkeyPublic = publicRing.GetPublicKey(subkeySecret.KeyId);

            foreach (var (createdUtc, keyFlags, laterBackSignatureMode) in options.LaterBindings)
            {
                var later = new PgpSignatureSubpacketGenerator();
                var laterUnhashed = new PgpSignatureSubpacketGenerator();
                var laterBackSignature = laterBackSignatureMode.HasValue
                    ? CreateBackSignature(laterBackSignatureMode.Value, masterPgpPair, subPgpPair, random, now)
                    : backSignature;
                if (laterBackSignature is not null)
                {
                    (options.BackSignatureInUnhashedArea ? laterUnhashed : later).AddEmbeddedSignature(false, laterBackSignature);
                }

                later.SetSignatureCreationTime(false, createdUtc);
                if (keyFlags.HasValue)
                {
                    later.SetKeyFlags(false, keyFlags.Value);
                }

                var bindingGenerator = new PgpSignatureGenerator(masterSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
                bindingGenerator.InitSign(PgpSignature.SubkeyBinding, masterPrivate);
                bindingGenerator.SetHashedSubpackets(later.Generate());
                if (options.BackSignatureInUnhashedArea && laterBackSignature is not null)
                {
                    bindingGenerator.SetUnhashedSubpackets(laterUnhashed.Generate());
                }

                subkeyPublic = PgpPublicKey.AddCertification(
                    subkeyPublic, bindingGenerator.GenerateCertification(masterPublic, subkeyPublic));
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, subkeyPublic);
            }

            foreach (var (createdUtc, keyFlags) in options.LaterPrimaryCertifications)
            {
                masterPublic = AddPrimaryCertification(masterPublic, options.UserId, masterSecret.PublicKey.Algorithm, masterPrivate, createdUtc, keyFlags, null);
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, masterPublic);
            }

            foreach (var (createdUtc, keyFlags) in options.ForgedPrimaryCertifications)
            {
                masterPublic = AddPrimaryCertification(
                    masterPublic, options.UserId, subkeySecret.PublicKey.Algorithm, subkeySecret.ExtractPrivateKey(passChars), createdUtc, keyFlags, masterSecret.KeyId);
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, masterPublic);
            }

            foreach (var (createdUtc, keyFlags) in options.DirectKeySignatures)
            {
                var subpackets = new PgpSignatureSubpacketGenerator();
                subpackets.SetSignatureCreationTime(false, createdUtc);
                if (keyFlags.HasValue)
                {
                    subpackets.SetKeyFlags(false, keyFlags.Value);
                }

                var direct = new PgpSignatureGenerator(masterSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
                direct.InitSign(PgpSignature.DirectKey, masterPrivate);
                direct.SetHashedSubpackets(subpackets.Generate());
                masterPublic = PgpPublicKey.AddCertification(masterPublic, direct.GenerateCertification(masterPublic));
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, masterPublic);
            }

            foreach (var createdUtc in options.UserIdRevocations)
            {
                var subpackets = new PgpSignatureSubpacketGenerator();
                subpackets.SetSignatureCreationTime(false, createdUtc);
                var userIdRevocation = new PgpSignatureGenerator(masterSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
                userIdRevocation.InitSign(PgpSignature.CertificationRevocation, masterPrivate);
                userIdRevocation.SetHashedSubpackets(subpackets.Generate());
                masterPublic = PgpPublicKey.AddCertification(
                    masterPublic, options.UserId, userIdRevocation.GenerateCertification(options.UserId, masterPublic));
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, masterPublic);
            }

            foreach (var (createdUtc, keyFlags) in options.UnverifiableSelfCertifications)
            {
                masterPublic = AddPrimaryCertification(
                    masterPublic, options.UserId, masterSecret.PublicKey.Algorithm, masterPrivate, createdUtc, keyFlags, null, unknownAlgorithm: true);
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, masterPublic);
            }

            var ringBeforeRevocation = publicRing;

            if (options.RevokeSubkey)
            {
                var revocation = new PgpSignatureGenerator(masterSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
                revocation.InitSign(PgpSignature.SubkeyRevocation, masterPrivate);
                var revokedSub = PgpPublicKey.AddCertification(
                    subkeyPublic, revocation.GenerateCertification(masterPublic, subkeyPublic));
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, revokedSub);
            }

            if (options.RevokePrimary)
            {
                var revocation = new PgpSignatureGenerator(masterSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
                revocation.InitSign(PgpSignature.KeyRevocation, masterPrivate);
                var revokedMaster = PgpPublicKey.AddCertification(
                    masterPublic, revocation.GenerateCertification(masterPublic));
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, revokedMaster);
            }

            if (options.StripSubkeyBinding)
            {
                var sub = publicRing.GetPublicKey(subkeySecret.KeyId);
                foreach (PgpSignature binding in sub.GetSignaturesOfType(PgpSignature.SubkeyBinding))
                {
                    sub = PgpPublicKey.RemoveCertification(sub, binding);
                }

                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, sub);
            }

            if (options.RevokePrimary || options.RevokeSubkey)
            {
                // A revocation is part of the key as published; the secret ring carries it too once the owner revokes.
                secretRing = PgpSecretKeyRing.ReplacePublicKeys(secretRing, publicRing);
            }

            return new PgpBcFixture(publicRing, options.SignWithPrimary ? masterSecret : subkeySecret, passChars, secretRing, encryptionKeyId, ringBeforeRevocation);
        }

        // A user-id certification of the primary key; with a different signer it names the primary as issuer but does
        // not verify against it.
        private static PgpPublicKey AddPrimaryCertification(
            PgpPublicKey primary, string userId, PublicKeyAlgorithmTag algorithm, PgpPrivateKey signer, DateTime createdUtc, int? keyFlags,
            long? claimedIssuer, bool unknownAlgorithm = false)
        {
            var subpackets = new PgpSignatureSubpacketGenerator();
            subpackets.SetSignatureCreationTime(false, createdUtc);
            if (keyFlags.HasValue)
            {
                subpackets.SetKeyFlags(false, keyFlags.Value);
            }

            var generator = new PgpSignatureGenerator(algorithm, HashAlgorithmTag.Sha256);
            generator.InitSign(PgpSignature.PositiveCertification, signer);
            generator.SetHashedSubpackets(subpackets.Generate());
            var certification = generator.GenerateCertification(userId, primary);
            if (claimedIssuer.HasValue)
            {
                certification = RewriteUnhashedIssuer(certification, claimedIssuer.Value);
            }

            if (unknownAlgorithm)
            {
                // ElGamal signatures carry two integers: the RSA one plus a dummy second one keeps the packet parseable.
                certification = RewriteSignatureBody(certification, body =>
                {
                    body[2] = (byte)PublicKeyAlgorithmTag.ElGamalGeneral;
                    return body.Concat(new byte[] { 0x00, 0x01, 0x01 }).ToArray();
                });
            }

            return PgpPublicKey.AddCertification(primary, userId, certification);
        }

        // A Primary Key Binding signature (0x19): made by the SUBKEY over (primary key, subkey), embedded in
        // the primary's binding so the subkey proves it consents to being bound.
        private static PgpSignature CreateBackSignature(BackSignatureMode mode, PgpKeyPair master, PgpKeyPair subkey, SecureRandom random, DateTime now)
        {
            if (mode == BackSignatureMode.Missing)
            {
                return null;
            }

            var signer = mode == BackSignatureMode.SignedByWrongKey
                ? new PgpKeyPair(PublicKeyAlgorithmTag.RsaSign, GenerateRsaKeyPair(random), now)
                : subkey;
            var primary = mode == BackSignatureMode.WrongPrimary
                ? new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, GenerateRsaKeyPair(random), now).PublicKey
                : master.PublicKey;

            var generator = new PgpSignatureGenerator(signer.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
            generator.InitSign(PgpSignature.PrimaryKeyBinding, signer.PrivateKey);
            var signature = generator.GenerateCertification(primary, subkey.PublicKey);
            return mode == BackSignatureMode.ValidWithoutIssuer ? RewriteUnhashedIssuer(signature, null) : signature;
        }

        // Re-encodes a version 4 signature with a changed packet body (the signed data is not touched).
        private static PgpSignature RewriteSignatureBody(PgpSignature signature, Func<byte[], byte[]> change)
        {
            var encoded = signature.GetEncoded();
            var header = (encoded[0] & 0x40) != 0 ? 2 : new[] { 2, 3, 5 }[encoded[0] & 0x03];
            var body = encoded.Skip(header).ToArray();
            body = change(body);
            var packet = new MemoryStream();
            packet.WriteByte(0x89); // old format, tag 2, two length octets
            packet.WriteByte((byte)(body.Length >> 8));
            packet.WriteByte((byte)body.Length);
            packet.Write(body, 0, body.Length);
            return (PgpSignature)((PgpSignatureList)new PgpObjectFactory(packet.ToArray()).NextPgpObject())[0];
        }

        // The unhashed subpackets are not covered by the signature, so dropping the Issuer Key ID (type 16) from
        // them leaves a valid signature that no longer names its issuer; a replacement id makes it name another key
        // (the signature stays unverifiable against that key). Rebuilds a version 4 signature packet.
        private static PgpSignature RewriteUnhashedIssuer(PgpSignature signature, long? replacementKeyId)
        {
            const int IssuerKeyIdSubpacket = 16;
            var encoded = signature.GetEncoded();
            var header = (encoded[0] & 0x40) != 0 ? 2 : new[] { 2, 3, 5 }[encoded[0] & 0x03]; // new format: one length octet
            var body = encoded.Skip(header).ToArray();
            var hashedLength = (body[4] << 8) | body[5];
            var unhashedStart = 6 + hashedLength;
            var unhashedLength = (body[unhashedStart] << 8) | body[unhashedStart + 1];
            var kept = new MemoryStream();
            for (var offset = unhashedStart + 2; offset < unhashedStart + 2 + unhashedLength;)
            {
                var length = body[offset]; // subpackets used here are shorter than 192 octets
                if ((body[offset + 1] & 0x7F) != IssuerKeyIdSubpacket)
                {
                    kept.Write(body, offset, length + 1);
                }

                offset += length + 1;
            }

            if (replacementKeyId.HasValue)
            {
                kept.WriteByte(9);
                kept.WriteByte(IssuerKeyIdSubpacket);
                for (var shift = 56; shift >= 0; shift -= 8)
                {
                    kept.WriteByte((byte)(replacementKeyId.Value >> shift));
                }
            }

            var rebuilt = new MemoryStream();
            rebuilt.Write(body, 0, unhashedStart);
            rebuilt.WriteByte((byte)(kept.Length >> 8));
            rebuilt.WriteByte((byte)kept.Length);
            kept.WriteTo(rebuilt);
            rebuilt.Write(body, unhashedStart + 2 + unhashedLength, body.Length - (unhashedStart + 2 + unhashedLength));

            var packet = new MemoryStream();
            packet.WriteByte(0x89); // old format, tag 2, two length octets
            packet.WriteByte((byte)(rebuilt.Length >> 8));
            packet.WriteByte((byte)rebuilt.Length);
            rebuilt.WriteTo(packet);
            return (PgpSignature)((PgpSignatureList)new PgpObjectFactory(packet.ToArray()).NextPgpObject())[0];
        }

        /// <summary>
        /// A ring whose published subkey was GRAFTED ON from an unrelated key: the subkey's binding
        /// signature was made by the attacker's primary key, not this ring's primary key. Messages
        /// are signed with the grafted subkey's private half.
        /// </summary>
        public static PgpBcFixture CreateWithGraftedSubkey(string passphrase = "fixturepass")
        {
            var victim = Create(passphrase);
            var attacker = Create(passphrase);
            var grafted = PgpPublicKeyRing.InsertPublicKey(victim.PublicRing, attacker.SigningPublicKey);
            return new PgpBcFixture(grafted, attacker._signingSecret, passphrase.ToCharArray());
        }

        /// <summary>Armored concatenation of several fixtures' public rings (one armor block).</summary>
        public static byte[] CombinePublicKeyRings(params PgpBcFixture[] fixtures)
        {
            using var buffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(buffer))
            {
                foreach (var fixture in fixtures)
                {
                    fixture.PublicRing.Encode(armored);
                }
            }

            return buffer.ToArray();
        }

        /// <summary>
        /// Produce a one-pass signature over <paramref name="data"/> signed by the SUBKEY,
        /// optionally wrapped in a compressed packet (STUD-80430 + STUD-80429 combined).
        /// <paramref name="signedAtUtc"/> overrides the signature creation time.
        /// </summary>
        public byte[] CreateSignedMessage(byte[] data, bool compress, DateTime? signedAtUtc = null)
            => CreateMultiSignerMessage(data, compress, signedAtUtc, this);

        /// <summary>
        /// One-pass signatures from several signers, nested per RFC 4880 section 5.4:
        /// OPS(first) OPS(second) literal SIG(second) SIG(first).
        /// </summary>
        public static byte[] CreateMultiSignerMessage(byte[] data, bool compress, DateTime? signedAtUtc, params PgpBcFixture[] signers)
            => CreateMultiSignerMessage(data, compress, signedAtUtc, MarkerPlacement.None, signers);

        public static byte[] CreateMultiSignerMessage(
            byte[] data, bool compress, DateTime? signedAtUtc, MarkerPlacement marker, params PgpBcFixture[] signers)
        {
            var generators = new PgpSignatureGenerator[signers.Length];
            for (var i = 0; i < signers.Length; i++)
            {
                generators[i] = signers[i].NewGenerator(PgpSignature.BinaryDocument, signedAtUtc);
            }

            using var outerBuffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(outerBuffer))
            {
                WriteSignedPayload(armored, data, compress, generators, marker);
            }

            return outerBuffer.ToArray();
        }

        // Writes [marker] [compressed(] OPS... literal SIG... [)] to target: the signed payload of a message.
        private static void WriteSignedPayload(
            Stream armored, byte[] data, bool compress, PgpSignatureGenerator[] generators, MarkerPlacement marker)
        {
            if (marker == MarkerPlacement.Outside)
            {
                armored.Write(MarkerPacket, 0, MarkerPacket.Length);
            }

            // Disposing the stream returned by Open() (rather than calling the obsolete
            // PgpCompressedDataGenerator.Close()) flushes the compression trailer.
            Stream compressedStream = compress
                ? new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip).Open(armored)
                : null;
            Stream signingTarget = compressedStream ?? armored;

            if (marker == MarkerPlacement.Inside)
            {
                signingTarget.Write(MarkerPacket, 0, MarkerPacket.Length);
            }

            var bcpgOut = new BcpgOutputStream(signingTarget);
            for (var i = 0; i < generators.Length; i++)
            {
                // isNested == true writes "another one-pass packet follows"; only the last is false.
                generators[i].GenerateOnePassVersion(i < generators.Length - 1).Encode(bcpgOut);
            }

            var literalGenerator = new PgpLiteralDataGenerator();
            using (var literalOut = literalGenerator.Open(bcpgOut, PgpLiteralData.Binary, "fixture.dat", data.Length, DateTime.UtcNow))
            {
                literalOut.Write(data, 0, data.Length);
                foreach (var generator in generators)
                {
                    generator.Update(data);
                }
            }

            for (var i = generators.Length - 1; i >= 0; i--)
            {
                generators[i].Generate().Encode(bcpgOut);
            }

            bcpgOut.Flush();
            compressedStream?.Dispose();
        }

        /// <summary>
        /// Encrypt (AES-256, with integrity packet) to <paramref name="recipient"/>'s encryption subkey a
        /// payload signed by <paramref name="signers"/>, i.e. Encrypted(Compressed(OPS literal SIG)),
        /// which is what gpg --encrypt --sign produces. <paramref name="armor"/> false yields raw binary.
        /// </summary>
        public static byte[] CreateEncryptedSignedMessage(
            byte[] data, PgpBcFixture recipient, bool compress, DateTime? signedAtUtc, bool armor, params PgpBcFixture[] signers)
        {
            var generators = new PgpSignatureGenerator[signers.Length];
            for (var i = 0; i < signers.Length; i++)
            {
                generators[i] = signers[i].NewGenerator(PgpSignature.BinaryDocument, signedAtUtc);
            }

            return Encrypt(recipient, armor, encrypted => WriteSignedPayload(encrypted, data, compress, generators, MarkerPlacement.None));
        }

        /// <summary>Encrypt without any signature (verification must then fail).</summary>
        public static byte[] CreateEncryptedUnsignedMessage(byte[] data, PgpBcFixture recipient, bool armor)
            => Encrypt(recipient, armor, encrypted =>
            {
                var literal = new PgpLiteralDataGenerator();
                using var literalOut = literal.Open(encrypted, PgpLiteralData.Binary, "fixture.dat", data.Length, DateTime.UtcNow);
                literalOut.Write(data, 0, data.Length);
            });

        private static byte[] Encrypt(PgpBcFixture recipient, bool armor, Action<Stream> writePayload)
        {
            if (recipient._encryptionKeyId is null)
            {
                throw new InvalidOperationException("The recipient fixture was created without an encryption subkey.");
            }

            var encryptionKey = recipient.PublicRing.GetPublicKey(recipient._encryptionKeyId.Value);
            var generator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, new SecureRandom());
            generator.AddMethod(encryptionKey);

            using var outerBuffer = new MemoryStream();
            Stream sink = armor ? new ArmoredOutputStream(outerBuffer) : outerBuffer;
            try
            {
                using (var encrypted = generator.Open(sink, new byte[1 << 16]))
                {
                    writePayload(encrypted);
                }
            }
            finally
            {
                if (armor)
                {
                    sink.Dispose();
                }
            }

            return outerBuffer.ToArray();
        }

        /// <summary>
        /// Produce a clear-text signature over <paramref name="text"/> signed by the SUBKEY (STUD-80429).
        /// Mirrors BouncyCastle's canonical ClearSignedFileProcessor line handling so the produced
        /// armor matches what a verifier expects.
        /// </summary>
        public byte[] CreateClearSignedMessage(string text, DateTime? signedAtUtc = null)
            => CreateMultiSignerClearSignedMessage(text, signedAtUtc, this);

        /// <summary>One clear-text block carrying one signature packet per signer, in the given order.</summary>
        public static byte[] CreateMultiSignerClearSignedMessage(string text, DateTime? signedAtUtc, params PgpBcFixture[] signers)
            => CreateMultiSignerClearSignedMessage(text, signedAtUtc, markerBeforeSignature: false, signers);

        public static byte[] CreateMultiSignerClearSignedMessage(
            string text, DateTime? signedAtUtc, bool markerBeforeSignature, params PgpBcFixture[] signers)
        {
            var generators = new PgpSignatureGenerator[signers.Length];
            for (var i = 0; i < signers.Length; i++)
            {
                generators[i] = signers[i].NewGenerator(PgpSignature.CanonicalTextDocument, signedAtUtc);
            }

            var inputBytes = System.Text.Encoding.ASCII.GetBytes(text.Replace("\r\n", "\n"));

            using var outerBuffer = new MemoryStream();
            using (var fIn = new MemoryStream(inputBytes))
            using (var armored = new ArmoredOutputStream(outerBuffer))
            {
                armored.BeginClearText(HashAlgorithmTag.Sha256);

                var lineOut = new MemoryStream();
                int lookAhead = ReadInputLine(lineOut, fIn);
                byte[] lastLine = lineOut.ToArray();
                ProcessLine(armored, generators, lastLine);

                if (lookAhead != -1)
                {
                    do
                    {
                        lookAhead = ReadInputLine(lineOut, lookAhead, fIn);
                        foreach (var generator in generators)
                        {
                            generator.Update((byte)'\r');
                            generator.Update((byte)'\n');
                        }

                        lastLine = lineOut.ToArray();
                        ProcessLine(armored, generators, lastLine);
                    }
                    while (lookAhead != -1);
                }

                // The armored clear-text section must end with a line break before the
                // "-----BEGIN PGP SIGNATURE-----" boundary. Add one if the source text didn't
                // already end with one - it is purely a structural separator, not part of the
                // hashed content, so it must not go through signatureGenerator.Update.
                bool lastLineHasTerminator = lastLine.Length > 0 &&
                    (lastLine[lastLine.Length - 1] == '\n' || lastLine[lastLine.Length - 1] == '\r');
                if (!lastLineHasTerminator)
                {
                    armored.Write(new byte[] { (byte)'\r', (byte)'\n' }, 0, 2);
                }

                armored.EndClearText();

                if (markerBeforeSignature)
                {
                    armored.Write(MarkerPacket, 0, MarkerPacket.Length);
                }

                var bcpgOut = new BcpgOutputStream(armored);
                foreach (var generator in generators)
                {
                    generator.Generate().Encode(bcpgOut);
                }
            }

            return outerBuffer.ToArray();
        }

        private PgpSignatureGenerator NewGenerator(int signatureType, DateTime? signedAtUtc)
        {
            var generator = new PgpSignatureGenerator(_signingSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
            generator.InitSign(signatureType, _signingSecret.ExtractPrivateKey(_passphrase));

            if (signedAtUtc.HasValue)
            {
                var subpackets = new PgpSignatureSubpacketGenerator();
                subpackets.SetSignatureCreationTime(false, signedAtUtc.Value);
                generator.SetHashedSubpackets(subpackets.Generate());
            }

            return generator;
        }

        private static byte[] EncodeArmored(PgpPublicKeyRing ring)
        {
            using var pubBuffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(pubBuffer))
            {
                ring.Encode(armored);
            }

            return pubBuffer.ToArray();
        }

        private static void ProcessLine(Stream aOut, PgpSignatureGenerator[] sGens, byte[] line)
        {
            int length = GetLengthWithoutWhiteSpace(line);
            if (length > 0)
            {
                foreach (var sGen in sGens)
                {
                    sGen.Update(line, 0, length);
                }
            }

            aOut.Write(line, 0, line.Length);
        }

        private static int GetLengthWithoutWhiteSpace(byte[] line)
        {
            int end = line.Length - 1;
            while (end >= 0 && IsWhiteSpace(line[end]))
            {
                end--;
            }

            return end + 1;
        }

        private static bool IsWhiteSpace(byte b)
            => b == '\r' || b == '\n' || b == '\t' || b == ' ';

        private static int ReadInputLine(MemoryStream lineOut, Stream fIn)
        {
            lineOut.SetLength(0);

            int lookAhead = -1;
            int ch;
            while ((ch = fIn.ReadByte()) >= 0)
            {
                lineOut.WriteByte((byte)ch);
                if (ch == '\r' || ch == '\n')
                {
                    lookAhead = ReadPastEol(lineOut, ch, fIn);
                    break;
                }
            }

            return lookAhead;
        }

        private static int ReadInputLine(MemoryStream lineOut, int lookAhead, Stream fIn)
        {
            lineOut.SetLength(0);

            // Unlike the no-lookAhead overload above, this one must write ch to lineOut
            // unconditionally (including the terminator itself) — this fixture's ProcessLine
            // writes lineOut straight back to the armor, so dropping the terminator here would
            // concatenate this line onto the next one in the produced armor text.
            int ch = lookAhead;
            do
            {
                lineOut.WriteByte((byte)ch);
                if (ch == '\r' || ch == '\n')
                {
                    return ReadPastEol(lineOut, ch, fIn);
                }
            }
            while ((ch = fIn.ReadByte()) >= 0);

            return -1;
        }

        private static int ReadPastEol(MemoryStream lineOut, int lastCh, Stream fIn)
        {
            int lookAhead = fIn.ReadByte();

            if (lastCh == '\r' && lookAhead == '\n')
            {
                lineOut.WriteByte((byte)lookAhead);
                lookAhead = fIn.ReadByte();
            }

            return lookAhead;
        }

        private static AsymmetricCipherKeyPair GenerateRsaKeyPair(SecureRandom random)
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), random, 2048, 25));
            return generator.GenerateKeyPair();
        }
    }
}
