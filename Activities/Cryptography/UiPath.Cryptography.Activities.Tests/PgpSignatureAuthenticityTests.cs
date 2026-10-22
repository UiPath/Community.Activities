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

        // Writes OPS(ops) literal(content) SIG(signature), optionally compressed, into a stream.
        private static void WriteSigned(Stream target, bool compress, PgpOnePassSignature ops, byte[] content, PgpSignature signature)
        {
            Stream compressed = compress ? new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip).Open(target) : null;
            var bcpg = new BcpgOutputStream(compressed ?? target);
            ops.Encode(bcpg);
            using (var literal = new PgpLiteralDataGenerator().Open(bcpg, PgpLiteralData.Binary, "data", content.Length, DateTime.UtcNow))
            {
                literal.Write(content, 0, content.Length);
            }

            signature.Encode(bcpg);
            bcpg.Flush();
            compressed?.Dispose();
        }

        private static byte[] SignedOnly(bool compress, PgpOnePassSignature ops, byte[] content, PgpSignature signature)
        {
            using var buffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(buffer))
            {
                WriteSigned(armored, compress, ops, content, signature);
            }

            return buffer.ToArray();
        }

        private static byte[] SignedAndEncrypted(PgpPublicKey recipientKey, bool compress, PgpOnePassSignature ops, byte[] content, PgpSignature signature)
        {
            var encryption = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, Random);
            encryption.AddMethod(recipientKey);
            using var buffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(buffer))
            using (var encrypted = encryption.Open(armored, new byte[1 << 16]))
            {
                WriteSigned(encrypted, compress, ops, content, signature);
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
