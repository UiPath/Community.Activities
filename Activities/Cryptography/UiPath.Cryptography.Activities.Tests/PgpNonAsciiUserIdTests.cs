using System;
using System.IO;
using System.Text;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// PgpCore 8.1.0 threw an ArgumentNullException from its key validation on every operation for a key whose
    /// user ID contains a non-ASCII character (mattosaurus/PgpCore#345, fixed in 8.1.1). These tests run the
    /// public helper operations with such keys so a PgpCore version with that defect cannot be shipped.
    /// </summary>
    public class PgpNonAsciiUserIdTests : IDisposable
    {
        private const string Passphrase = "testpassphrase";
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("payload for a key with a non-ASCII user id");

        private readonly string _publicKeyPath;
        private readonly string _privateKeyPath;

        public PgpNonAsciiUserIdTests()
        {
            _publicKeyPath = Path.Combine(Path.GetTempPath(), $"pgp_nonascii_public_{Guid.NewGuid()}.asc");
            _privateKeyPath = Path.Combine(Path.GetTempPath(), $"pgp_nonascii_private_{Guid.NewGuid()}.asc");
        }

        public void Dispose()
        {
            if (File.Exists(_publicKeyPath)) File.Delete(_publicKeyPath);
            if (File.Exists(_privateKeyPath)) File.Delete(_privateKeyPath);
            GC.SuppressFinalize(this);
        }

        private void GenerateKeys(string userId)
            => CryptographyHelper.PgpGenerateKeys(_publicKeyPath, _privateKeyPath, userId, Passphrase, RsaKeySize.Rsa2048);

        private Stream PublicKey() => File.OpenRead(_publicKeyPath);

        private Stream PrivateKey() => File.OpenRead(_privateKeyPath);

        [Theory]
        [InlineData("Wirsén <wirsen@test.local>")]
        [InlineData("李雷 <lilei@test.local>")]
        public void Sign_Verify_WithNonAsciiUserId(string userId)
        {
            GenerateKeys(userId);

            byte[] signed;
            using (var privateKey = PrivateKey())
            {
                signed = CryptographyHelper.PgpSign(Payload, privateKey, Passphrase);
            }

            using var publicKey = PublicKey();
            Assert.True(CryptographyHelper.PgpVerify(signed, publicKey));
        }

        [Theory]
        [InlineData("Wirsén <wirsen@test.local>")]
        [InlineData("李雷 <lilei@test.local>")]
        public void ClearSign_VerifyClear_WithNonAsciiUserId(string userId)
        {
            GenerateKeys(userId);

            string signed;
            using (var privateKey = PrivateKey())
            {
                signed = CryptographyHelper.PgpClearSignText("clear text to sign", privateKey, Passphrase);
            }

            using var publicKey = PublicKey();
            Assert.True(CryptographyHelper.PgpVerifyClearText(signed, publicKey));
        }

        [Theory]
        [InlineData("Wirsén <wirsen@test.local>")]
        [InlineData("李雷 <lilei@test.local>")]
        public void Encrypt_Decrypt_WithNonAsciiUserId(string userId)
        {
            GenerateKeys(userId);

            byte[] cipher;
            using (var publicKey = PublicKey())
            {
                cipher = CryptographyHelper.PgpEncrypt(Payload, publicKey);
            }

            using var privateKey = PrivateKey();
            Assert.Equal(Payload, CryptographyHelper.PgpDecrypt(cipher, privateKey, Passphrase));
        }

        [Theory]
        [InlineData("Wirsén <wirsen@test.local>")]
        [InlineData("李雷 <lilei@test.local>")]
        public void EncryptAndSign_DecryptAndVerify_WithNonAsciiUserId(string userId)
        {
            GenerateKeys(userId);

            byte[] cipher;
            using (var publicKey = PublicKey())
            using (var privateKey = PrivateKey())
            {
                cipher = CryptographyHelper.PgpEncrypt(Payload, publicKey, privateKey, Passphrase, sign: true);
            }

            using var decryptKey = PrivateKey();
            using var verifyKey = PublicKey();
            Assert.Equal(Payload, CryptographyHelper.PgpDecrypt(cipher, decryptKey, Passphrase, verifyKey, verifySignature: true));
        }
    }
}
