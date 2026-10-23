using System;
using System.IO;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Xunit;
using CryptoResources = UiPath.Cryptography.Properties.Resources;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Signature authentication through the public helper API: the signature must be the verifier key's, cover the
    /// returned content and agree with its header. Fixtures are built from BouncyCastle.
    /// </summary>
    public class PgpSignatureAuthenticityTests
    {
        private static readonly SecureRandom Random = new SecureRandom();
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("payload covered by the signature");

        private static PgpBcFixture NewUser(string passphrase = "fixturepass")
            => PgpBcFixture.Create(new PgpBcFixture.Options { WithEncryptionSubkey = true }, passphrase);

        private static PgpPublicKey EncryptionKey(PgpBcFixture user)
            => user.PublicRing.GetPublicKeys().Cast<PgpPublicKey>().Where(k => k.IsEncryptionKey).OrderBy(k => k.IsMasterKey).First();

        private static PgpPrivateKey SigningPrivateKey(PgpBcFixture user)
        {
            var secretRing = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(new MemoryStream(user.PrivateKeyRing)))
                .GetKeyRings().Cast<PgpSecretKeyRing>().First();
            return secretRing.GetSecretKey(user.SigningPublicKey.KeyId).ExtractPrivateKeyRaw(Encoding.UTF8.GetBytes(user.Passphrase));
        }

        // A private key from an unrelated key pair that carries the given key id.
        private static PgpPrivateKey OtherKeyWithId(long keyId)
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), Random, 2048, 12));
            var pair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, generator.GenerateKeyPair(), DateTime.UtcNow);
            return new PgpPrivateKey(keyId, pair.PublicKey.PublicKeyPacket, pair.PrivateKey.Key);
        }

        // Writes OPS(ops...) literal(content) SIG(signatures...), optionally compressed, into a stream.
        private static void WriteSigned(Stream target, bool compress, PgpOnePassSignature[] ops, byte[] content, PgpSignature[] signatures)
        {
            Stream compressed = compress ? new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip).Open(target, new byte[1 << 16]) : null;
            var bcpg = new BcpgOutputStream(compressed ?? target);
            foreach (var header in ops)
            {
                header.Encode(bcpg);
            }

            using (var literal = new PgpLiteralDataGenerator().Open(bcpg, PgpLiteralData.Binary, "data", content.Length, DateTime.UtcNow))
            {
                literal.Write(content, 0, content.Length);
            }

            foreach (var signature in signatures)
            {
                signature.Encode(bcpg);
            }

            bcpg.Flush();
            compressed?.Dispose();
        }

        private static byte[] SignedOnly(bool compress, PgpOnePassSignature ops, byte[] content, PgpSignature signature, bool armor = true)
            => SignedOnly(compress, new[] { ops }, content, new[] { signature }, armor);

        private static byte[] SignedOnly(bool compress, PgpOnePassSignature[] ops, byte[] content, PgpSignature[] signatures, bool armor = true)
        {
            using var buffer = new MemoryStream();
            if (armor)
            {
                using var armored = new ArmoredOutputStream(buffer);
                WriteSigned(armored, compress, ops, content, signatures);
            }
            else
            {
                WriteSigned(buffer, compress, ops, content, signatures);
            }

            return buffer.ToArray();
        }

        private static byte[] SignedAndEncrypted(PgpPublicKey recipientKey, bool compress, PgpOnePassSignature ops, byte[] content, PgpSignature signature, bool armor = true)
            => SignedAndEncrypted(recipientKey, compress, new[] { ops }, content, new[] { signature }, armor);

        private static byte[] SignedAndEncrypted(PgpPublicKey recipientKey, bool compress, PgpOnePassSignature[] ops, byte[] content, PgpSignature[] signatures, bool armor = true)
        {
            var encryption = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, Random);
            encryption.AddMethod(recipientKey);
            using var buffer = new MemoryStream();
            if (armor)
            {
                using var armored = new ArmoredOutputStream(buffer);
                using var encrypted = encryption.Open(armored, new byte[1 << 16]);
                WriteSigned(encrypted, compress, ops, content, signatures);
            }
            else
            {
                using var encrypted = encryption.Open(buffer, new byte[1 << 16]);
                WriteSigned(encrypted, compress, ops, content, signatures);
            }

            return buffer.ToArray();
        }

        // The header and the signature for 'content' as made by 'signer' with the given signature type.
        private static (PgpOnePassSignature Ops, PgpSignature Signature) Sign(PgpPrivateKey signer, PgpPublicKey signerPublic, int type, byte[] signedBytes)
        {
            var generator = new PgpSignatureGenerator(signerPublic.Algorithm, HashAlgorithmTag.Sha256);
            generator.InitSign(type, signer);
            generator.Update(signedBytes);
            return (generator.GenerateOnePassVersion(false), generator.Generate());
        }

        private static byte[] Decrypt(byte[] message, PgpBcFixture recipient, byte[] verifierRing, string passphrase = null)
        {
            using var privateKey = new MemoryStream(recipient.PrivateKeyRing);
            using var publicKey = new MemoryStream(verifierRing);
            return CryptographyHelper.PgpDecrypt(message, privateKey, passphrase ?? recipient.Passphrase, publicKey, verifySignature: true);
        }

        private static bool Verify(byte[] message, byte[] verifierRing)
        {
            using var publicKey = new MemoryStream(verifierRing);
            return CryptographyHelper.PgpVerify(message, publicKey);
        }

        private static void AssertSignatureRejected(Action action)
        {
            var exception = Assert.Throws<InvalidOperationException>(action);
            Assert.Equal(CryptoResources.PgpSignatureVerificationFailed, exception.Message);
        }

        // ---------------------------------------------------------------- the signature must be the verifier key's

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Decrypt_SignatureNotMadeByTheVerifierKey_IsRejected(bool compress)
        {
            var user = NewUser();
            var otherKey = OtherKeyWithId(user.SigningPublicKey.KeyId);
            var (ops, signature) = Sign(otherKey, user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignedAndEncrypted(EncryptionKey(user), compress, ops, Payload, signature);

            AssertSignatureRejected(() => Decrypt(message, user, user.PublicKeyRing));
            AssertSignatureRejected(() =>
            {
                using var privateKey = new MemoryStream(user.PrivateKeyRing);
                using var publicKey = new MemoryStream(user.PublicKeyRing);
                CryptographyHelper.PgpDecryptText(Encoding.ASCII.GetString(message), privateKey, user.Passphrase, publicKey, verifySignature: true);
            });
        }

        [Fact]
        public void Decrypt_GenuineSignature_IsAccepted()
        {
            var user = NewUser();
            var (ops, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignedAndEncrypted(EncryptionKey(user), compress: true, ops, Payload, signature);

            Assert.Equal(Payload, Decrypt(message, user, user.PublicKeyRing));
        }

        [Fact]
        public void Decrypt_InputWithoutEncryptionLayer_IsRejected()
        {
            var user = NewUser();
            var otherKey = OtherKeyWithId(user.SigningPublicKey.KeyId);
            var (ops, signature) = Sign(otherKey, user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignedOnly(compress: true, ops, Payload, signature);

            Assert.ThrowsAny<Exception>(() => Decrypt(message, user, user.PublicKeyRing));
        }

        // ---------------------------------------------------------------- a message needs a signature

        [Fact]
        public void Verify_MessageWithoutSignature_IsFalse()
        {
            var user = NewUser();
            var message = PgpBcFixture.CreateEncryptedUnsignedMessage(Payload, user, armor: true);

            Assert.False(Verify(message, user.PublicKeyRing));
            using var publicKey = new MemoryStream(user.PublicKeyRing);
            Assert.False(CryptographyHelper.PgpVerifyText(Encoding.ASCII.GetString(message), publicKey));
        }

        // ---------------------------------------------------------------- header and signature must agree

        private static readonly byte[] CrLfContent = Encoding.ASCII.GetBytes("first line\r\nsecond line\r\n");
        private static readonly byte[] LfContent = Encoding.ASCII.GetBytes("first line\nsecond line\n");

        // A signature of one kind under a header of another kind, over content that differs in its line endings.
        private static (PgpOnePassSignature Ops, PgpSignature Signature) SignatureUnderHeaderOfOtherKind(PgpBcFixture user, int headerType)
        {
            var key = SigningPrivateKey(user);
            var header = new PgpSignatureGenerator(user.SigningPublicKey.Algorithm, HashAlgorithmTag.Sha256);
            header.InitSign(headerType, key);
            var signature = new PgpSignatureGenerator(user.SigningPublicKey.Algorithm, HashAlgorithmTag.Sha256);
            signature.InitSign(PgpSignature.BinaryDocument, key);
            signature.Update(CrLfContent);
            return (header.GenerateOnePassVersion(false), signature.Generate());
        }

        [Fact]
        public void Decrypt_HeaderAndSignatureKindDiffer_IsRejected()
        {
            var user = NewUser();
            var (ops, signature) = SignatureUnderHeaderOfOtherKind(user, PgpSignature.CanonicalTextDocument);
            var message = SignedAndEncrypted(EncryptionKey(user), compress: true, ops, LfContent, signature);

            AssertSignatureRejected(() => Decrypt(message, user, user.PublicKeyRing));
        }

        [Fact]
        public void Verify_HeaderAndSignatureKindDiffer_IsFalse()
        {
            var user = NewUser();
            var (ops, signature) = SignatureUnderHeaderOfOtherKind(user, PgpSignature.CanonicalTextDocument);
            var message = SignedOnly(compress: true, ops, LfContent, signature);

            Assert.False(Verify(message, user.PublicKeyRing));
        }

        [Fact]
        public void Decrypt_BinaryHeaderOverBinarySignature_IsAccepted()
        {
            var user = NewUser();
            var (ops, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, CrLfContent);
            var message = SignedAndEncrypted(EncryptionKey(user), compress: true, ops, CrLfContent, signature);

            Assert.Equal(CrLfContent, Decrypt(message, user, user.PublicKeyRing));
        }

        // A text-mode signature is canonicalized on both sides, so line endings in the literal may differ from the signed ones.
        [Fact]
        public void Decrypt_TextHeaderOverTextSignature_IsAccepted()
        {
            var user = NewUser();
            var (ops, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.CanonicalTextDocument, CrLfContent);
            var message = SignedAndEncrypted(EncryptionKey(user), compress: true, ops, LfContent, signature);

            Assert.Equal(LfContent, Decrypt(message, user, user.PublicKeyRing));
        }

        // ---------------------------------------------------------------- the message must be exactly one signed message

        // Two headers for one signature, from a signer whose key is revoked: the signature must not carry the message.
        private static (byte[] Encrypted, byte[] SignedOnly) DuplicatedHeaderMessages(PgpBcFixture user)
        {
            var generator = new PgpSignatureGenerator(user.SigningPublicKey.Algorithm, HashAlgorithmTag.Sha256);
            generator.InitSign(PgpSignature.BinaryDocument, SigningPrivateKey(user));
            generator.Update(Payload);
            var headers = new[] { generator.GenerateOnePassVersion(true), generator.GenerateOnePassVersion(false) };
            var signatures = new[] { generator.Generate() };
            return (SignedAndEncrypted(EncryptionKey(user), compress: true, headers, Payload, signatures),
                    SignedOnly(compress: true, headers, Payload, signatures));
        }

        private static PgpBcFixture NewRevokedUser()
            => PgpBcFixture.Create(new PgpBcFixture.Options { WithEncryptionSubkey = true, RevokeSubkey = true });

        [Fact]
        public void Verify_MoreHeadersThanSignatures_IsFalse()
        {
            var user = NewRevokedUser();
            var (_, signedOnly) = DuplicatedHeaderMessages(user);

            Assert.False(Verify(signedOnly, user.PublicKeyRing));
        }

        [Fact]
        public void Decrypt_MoreHeadersThanSignatures_IsRejected()
        {
            var user = NewRevokedUser();
            var (encrypted, _) = DuplicatedHeaderMessages(user);

            Assert.ThrowsAny<Exception>(() => Decrypt(encrypted, user, user.PublicKeyRing));
            Assert.ThrowsAny<Exception>(() => DecryptText(encrypted, user));
        }

        private static string DecryptText(byte[] armoredMessage, PgpBcFixture user)
        {
            using var privateKey = new MemoryStream(user.PrivateKeyRing);
            using var publicKey = new MemoryStream(user.PublicKeyRing);
            return CryptographyHelper.PgpDecryptText(Encoding.ASCII.GetString(armoredMessage), privateKey, user.Passphrase, publicKey, verifySignature: true);
        }

        private static byte[] GenuineSignedAndEncrypted(PgpBcFixture user, bool armor)
        {
            var (ops, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            return SignedAndEncrypted(EncryptionKey(user), compress: true, ops, Payload, signature, armor);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Decrypt_SecondMessageAfterTheFirst_IsRejected(bool armor)
        {
            var user = NewUser();
            var message = GenuineSignedAndEncrypted(user, armor).Concat(GenuineSignedAndEncrypted(user, armor)).ToArray();

            Assert.ThrowsAny<Exception>(() => Decrypt(message, user, user.PublicKeyRing));
            if (armor)
            {
                Assert.ThrowsAny<Exception>(() => DecryptText(message, user));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Verify_SecondMessageAfterTheFirst_IsFalse(bool armor)
        {
            var user = NewUser();
            var first = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var second = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignedOnly(compress: true, first.Ops, Payload, first.Signature, armor)
                .Concat(SignedOnly(compress: true, second.Ops, Payload, second.Signature, armor)).ToArray();

            Assert.False(Verify(message, user.PublicKeyRing));
        }

        // ---------------------------------------------------------------- signature-first messages

        // Writes SIG(signatures...) literal(content), optionally compressed: the layout without one-pass headers.
        private static void WriteSignatureFirst(Stream target, bool compress, PgpSignature[] signatures, byte[] content)
        {
            Stream compressed = compress ? new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip).Open(target, new byte[1 << 16]) : null;
            var bcpg = new BcpgOutputStream(compressed ?? target);
            foreach (var signature in signatures)
            {
                signature.Encode(bcpg);
            }

            using (var literal = new PgpLiteralDataGenerator().Open(bcpg, PgpLiteralData.Binary, "data", content.Length, DateTime.UtcNow))
            {
                literal.Write(content, 0, content.Length);
            }

            bcpg.Flush();
            compressed?.Dispose();
        }

        private static byte[] SignatureFirstSigned(bool compress, PgpSignature signature, byte[] content, bool armor)
        {
            using var buffer = new MemoryStream();
            if (armor)
            {
                using var armored = new ArmoredOutputStream(buffer);
                WriteSignatureFirst(armored, compress, new[] { signature }, content);
            }
            else
            {
                WriteSignatureFirst(buffer, compress, new[] { signature }, content);
            }

            return buffer.ToArray();
        }

        private static byte[] SignatureFirstSignedAndEncrypted(PgpBcFixture user, PgpSignature signature, byte[] content, bool armor)
        {
            var encryption = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, Random);
            encryption.AddMethod(EncryptionKey(user));
            using var buffer = new MemoryStream();
            if (armor)
            {
                using var armored = new ArmoredOutputStream(buffer);
                using var encrypted = encryption.Open(armored, new byte[1 << 16]);
                WriteSignatureFirst(encrypted, compress: true, new[] { signature }, content);
            }
            else
            {
                using var encrypted = encryption.Open(buffer, new byte[1 << 16]);
                WriteSignatureFirst(encrypted, compress: true, new[] { signature }, content);
            }

            return buffer.ToArray();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Verify_SignatureFirstMessage_IsTrue(bool compress, bool armor)
        {
            var user = NewUser();
            var (_, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);

            Assert.True(Verify(SignatureFirstSigned(compress, signature, Payload, armor), user.PublicKeyRing));
        }

        [Fact]
        public void VerifyText_SignatureFirstMessage_IsTrue()
        {
            var user = NewUser();
            var (_, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignatureFirstSigned(compress: true, signature, Payload, armor: true);

            using var publicKey = new MemoryStream(user.PublicKeyRing);
            Assert.True(CryptographyHelper.PgpVerifyText(Encoding.ASCII.GetString(message), publicKey));
        }

        [Fact]
        public void Verify_SignatureFirstMessageWithAnotherSignersSignature_IsFalse()
        {
            var user = NewUser();
            var (_, signature) = Sign(OtherKeyWithId(user.SigningPublicKey.KeyId), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);

            Assert.False(Verify(SignatureFirstSigned(compress: true, signature, Payload, armor: true), user.PublicKeyRing));
        }

        [Fact]
        public void Verify_SignatureFirstMessageWithAlteredContent_IsFalse()
        {
            var user = NewUser();
            var (_, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var altered = Encoding.UTF8.GetBytes("payload NOT covered by the signature");

            Assert.False(Verify(SignatureFirstSigned(compress: true, signature, altered, armor: true), user.PublicKeyRing));
        }

        [Fact]
        public void Verify_SignatureFirstMessageFromARevokedKey_IsFalse()
        {
            var user = NewRevokedUser();
            var (_, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);

            Assert.False(Verify(SignatureFirstSigned(compress: true, signature, Payload, armor: true), user.PublicKeyRing));
        }

        [Fact]
        public void Verify_SecondSignatureFirstMessageAfterTheFirst_IsFalse()
        {
            var user = NewUser();
            var (_, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignatureFirstSigned(compress: true, signature, Payload, armor: false)
                .Concat(SignatureFirstSigned(compress: true, signature, Payload, armor: false)).ToArray();

            Assert.False(Verify(message, user.PublicKeyRing));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Decrypt_SignatureFirstMessage_IsAccepted(bool armor)
        {
            var user = NewUser();
            var (_, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignatureFirstSignedAndEncrypted(user, signature, Payload, armor);

            Assert.Equal(Payload, Decrypt(message, user, user.PublicKeyRing));
            if (armor)
            {
                Assert.Equal(Encoding.UTF8.GetString(Payload), DecryptText(message, user));
            }
        }

        [Fact]
        public void Decrypt_SignatureFirstMessageWithAnotherSignersSignature_IsRejected()
        {
            var user = NewUser();
            var (_, signature) = Sign(OtherKeyWithId(user.SigningPublicKey.KeyId), user.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignatureFirstSignedAndEncrypted(user, signature, Payload, armor: true);

            AssertSignatureRejected(() => Decrypt(message, user, user.PublicKeyRing));
        }

        // ---------------------------------------------------------------- armor-looking content inside a binary message

        private static readonly byte[] ContentWithArmorMarkers = Encoding.ASCII.GetBytes(
            "-----BEGIN PGP PUBLIC KEY BLOCK-----\r\nAAAA\r\n-----END PGP PUBLIC KEY BLOCK-----\r\n" +
            "-----BEGIN PGP PUBLIC KEY BLOCK-----\r\nBBBB\r\n-----END PGP PUBLIC KEY BLOCK-----\r\n");

        [Fact]
        public void Verify_BinaryMessageWhoseContentHoldsTwoArmorBlocks_IsTrue()
        {
            var user = NewUser();
            var (ops, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, ContentWithArmorMarkers);
            var message = SignedOnly(compress: false, ops, ContentWithArmorMarkers, signature, armor: false);

            Assert.True(Verify(message, user.PublicKeyRing));
        }

        [Fact]
        public void Decrypt_BinaryMessageWhoseContentHoldsTwoArmorBlocks_IsAccepted()
        {
            var user = NewUser();
            var (ops, signature) = Sign(SigningPrivateKey(user), user.SigningPublicKey, PgpSignature.BinaryDocument, ContentWithArmorMarkers);
            var message = SignedAndEncrypted(EncryptionKey(user), compress: false, ops, ContentWithArmorMarkers, signature, armor: false);

            Assert.Equal(ContentWithArmorMarkers, Decrypt(message, user, user.PublicKeyRing));
        }

        // ---------------------------------------------------------------- the recipient key's passphrase is UTF-8

        // The secret key ring of a recipient whose key is protected with the UTF-8 bytes of the passphrase, as GnuPG does.
        private static (byte[] PrivateRing, PgpPublicKey EncryptionKey) NewRecipientWithUtf8Passphrase(string passphrase)
        {
            PgpKeyPair NewPair()
            {
                var generator = new RsaKeyPairGenerator();
                generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), Random, 2048, 12));
                return new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, generator.GenerateKeyPair(), DateTime.UtcNow);
            }

            var masterFlags = new PgpSignatureSubpacketGenerator();
            masterFlags.SetKeyFlags(false, PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign);
            var ringGenerator = new PgpKeyRingGenerator(PgpSignature.PositiveCertification, NewPair(), "recipient@example.com",
                SymmetricKeyAlgorithmTag.Aes256, HashAlgorithmTag.Sha256, true, passphrase.ToCharArray(), true,
                masterFlags.Generate(), null, Random);
            var subkeyFlags = new PgpSignatureSubpacketGenerator();
            subkeyFlags.SetKeyFlags(false, PgpKeyFlags.CanEncryptCommunications | PgpKeyFlags.CanEncryptStorage);
            ringGenerator.AddSubKey(NewPair(), subkeyFlags.Generate(), null);

            using var buffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(buffer))
            {
                ringGenerator.GenerateSecretKeyRing().Encode(armored);
            }

            var encryptionKey = ringGenerator.GeneratePublicKeyRing().GetPublicKeys().Cast<PgpPublicKey>()
                .Where(k => k.IsEncryptionKey).OrderBy(k => k.IsMasterKey).First();
            return (buffer.ToArray(), encryptionKey);
        }

        [Fact]
        public void Decrypt_RecipientKeyProtectedWithNonAsciiPassphrase_Verifies()
        {
            const string passphrase = "pässwörd-ț";
            var signer = NewUser();
            var (privateRing, encryptionKey) = NewRecipientWithUtf8Passphrase(passphrase);
            var (ops, signature) = Sign(SigningPrivateKey(signer), signer.SigningPublicKey, PgpSignature.BinaryDocument, Payload);
            var message = SignedAndEncrypted(encryptionKey, compress: true, ops, Payload, signature);

            using var privateKey = new MemoryStream(privateRing);
            using var publicKey = new MemoryStream(signer.PublicKeyRing);
            Assert.Equal(Payload, CryptographyHelper.PgpDecrypt(message, privateKey, passphrase, publicKey, verifySignature: true));
        }
    }
}
