using System;
using System.IO;
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

            public bool RevokeSubkey { get; set; }
            public bool RevokePrimary { get; set; }

            /// <summary>Remove the subkey's binding signature from the published ring.</summary>
            public bool StripSubkeyBinding { get; set; }

            /// <summary>Sign with the primary key instead of the subkey.</summary>
            public bool SignWithPrimary { get; set; }

            /// <summary>Makes the (first) subkey binding signature itself lapse after this many seconds.</summary>
            public long? SubkeyBindingExpirySeconds { get; set; }

            /// <summary>
            /// Extra binding signatures added after the first one (e.g. a later restriction or a
            /// later authorization), each with its own creation time and key flags (null omits them).
            /// </summary>
            public System.Collections.Generic.List<(DateTime CreatedUtc, int? KeyFlags)> LaterBindings { get; }
                = new System.Collections.Generic.List<(DateTime, int?)>();
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

        public PgpPublicKeyRing PublicRing { get; }

        public byte[] PublicKeyRing { get; }

        private PgpBcFixture(PgpPublicKeyRing publicRing, PgpSecretKey signingSecret, char[] passphrase)
        {
            PublicRing = publicRing;
            _signingSecret = signingSecret;
            _passphrase = passphrase;
            PublicKeyRing = EncodeArmored(publicRing);
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
            if (options.MasterExpirySeconds.HasValue)
            {
                var gen = new PgpSignatureSubpacketGenerator();
                gen.SetKeyExpirationTime(false, options.MasterExpirySeconds.Value);
                masterHashed = gen.Generate();
            }

            var keyRingGenerator = new PgpKeyRingGenerator(
                PgpSignature.PositiveCertification,
                masterPgpPair,
                "fixture@test.com",
                SymmetricKeyAlgorithmTag.Aes256,
                passChars,
                true,
                masterHashed,
                null,
                random);

            // The binding signature is created when the subkey is: a binding dated after the signing
            // time would (correctly) not be in force for messages signed earlier.
            var subGen = new PgpSignatureSubpacketGenerator();
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

            keyRingGenerator.AddSubKey(subPgpPair, subGen.Generate(), null);

            var secretRing = keyRingGenerator.GenerateSecretKeyRing();
            var publicRing = keyRingGenerator.GeneratePublicKeyRing();

            // The first key in the ring is the master; the second is the signing subkey.
            PgpSecretKey masterSecret = null;
            PgpSecretKey subkeySecret = null;
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

            foreach (var (createdUtc, keyFlags) in options.LaterBindings)
            {
                var later = new PgpSignatureSubpacketGenerator();
                later.SetSignatureCreationTime(false, createdUtc);
                if (keyFlags.HasValue)
                {
                    later.SetKeyFlags(false, keyFlags.Value);
                }

                var bindingGenerator = new PgpSignatureGenerator(masterSecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
                bindingGenerator.InitSign(PgpSignature.SubkeyBinding, masterPrivate);
                bindingGenerator.SetHashedSubpackets(later.Generate());
                subkeyPublic = PgpPublicKey.AddCertification(
                    subkeyPublic, bindingGenerator.GenerateCertification(masterPublic, subkeyPublic));
                publicRing = PgpPublicKeyRing.InsertPublicKey(publicRing, subkeyPublic);
            }

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

            return new PgpBcFixture(publicRing, options.SignWithPrimary ? masterSecret : subkeySecret, passChars);
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
