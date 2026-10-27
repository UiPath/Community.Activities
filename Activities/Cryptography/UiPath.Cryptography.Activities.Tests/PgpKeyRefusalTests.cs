using System;
using System.IO;
using System.Text;
using Xunit;
using CryptoResources = UiPath.Cryptography.Properties.Resources;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Expired and revoked keys are refused alike: neither a revoked nor an expired private key signs, and neither an
    /// expired nor a revoked public key is encrypted to. Fixtures are built from BouncyCastle.
    /// </summary>
    public class PgpKeyRefusalTests
    {
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("payload");

        private static PgpBcFixture Create(Action<PgpBcFixture.Options> configure = null)
        {
            var options = new PgpBcFixture.Options { WithEncryptionSubkey = true };
            configure?.Invoke(options);
            return PgpBcFixture.Create(options);
        }

        private static byte[] Sign(PgpBcFixture signer)
        {
            using var privateKey = new MemoryStream(signer.PrivateKeyRing);
            return CryptographyHelper.PgpSign(Payload, privateKey, signer.Passphrase);
        }

        private static byte[] Encrypt(PgpBcFixture recipient)
        {
            using var publicKey = new MemoryStream(recipient.PublicKeyRing);
            return CryptographyHelper.PgpEncrypt(Payload, publicKey);
        }

        [Fact]
        public void Sign_WithAValidKey_Works()
            => Assert.NotEmpty(Sign(Create()));

        [Fact]
        public void Sign_WithARevokedKey_IsRefused()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => Sign(Create(o => o.RevokePrimary = true)));
            Assert.Equal(CryptoResources.PgpSigningKeyNotUsable, exception.Message);
        }

        // PgpCore signs with the primary key when a subkey is revoked, so the revoked subkey is simply not used.
        [Fact]
        public void Sign_WithARevokedSubkey_UsesTheUnrevokedPrimaryKey()
            => Assert.NotEmpty(Sign(Create(o => o.RevokeSubkey = true)));

        [Fact]
        public void SignText_WithAValidKey_Works()
        {
            var signer = Create();
            using var privateKey = new MemoryStream(signer.PrivateKeyRing);
            Assert.NotEmpty(CryptographyHelper.PgpSignText("payload", privateKey, signer.Passphrase));
        }

        [Fact]
        public void SignText_WithARevokedKey_IsRefused()
        {
            var signer = Create(o => o.RevokePrimary = true);
            using var privateKey = new MemoryStream(signer.PrivateKeyRing);

            var exception = Assert.Throws<InvalidOperationException>(() => CryptographyHelper.PgpSignText("payload", privateKey, signer.Passphrase));
            Assert.Equal(CryptoResources.PgpSigningKeyNotUsable, exception.Message);
        }

        [Fact]
        public void ClearSignText_WithARevokedKey_IsRefused()
        {
            var signer = Create(o => o.RevokePrimary = true);
            using var privateKey = new MemoryStream(signer.PrivateKeyRing);

            var exception = Assert.Throws<InvalidOperationException>(() => CryptographyHelper.PgpClearSignText("payload", privateKey, signer.Passphrase));
            Assert.Equal(CryptoResources.PgpSigningKeyNotUsable, exception.Message);
        }

        [Fact]
        public void EncryptAndSign_WithARevokedSigningKey_IsRefused()
        {
            var signer = Create(o => o.RevokePrimary = true);
            using var privateKey = new MemoryStream(signer.PrivateKeyRing);
            using var publicKey = new MemoryStream(signer.PublicKeyRing);

            var exception = Assert.Throws<InvalidOperationException>(() => CryptographyHelper.PgpEncrypt(
                Payload, publicKey, privateKey, signer.Passphrase, sign: true));
            Assert.Equal(CryptoResources.PgpSigningKeyNotUsable, exception.Message);
        }

        [Fact]
        public void Encrypt_ToAValidKey_Works()
            => Assert.NotEmpty(Encrypt(Create()));

        [Fact]
        public void Encrypt_ToARevokedKey_IsRefused()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => Encrypt(Create(o => o.RevokePrimary = true)));
            Assert.Equal(CryptoResources.PgpEncryptionKeyNotUsable, exception.Message);
        }

        [Fact]
        public void Encrypt_ToAnExpiredKey_IsRefused()
        {
            var recipient = Create(o =>
            {
                o.MasterCreatedUtc = DateTime.UtcNow.AddDays(-30);
                o.SubkeyCreatedUtc = DateTime.UtcNow.AddDays(-30);
                o.MasterExpirySeconds = 24 * 3600;
            });

            var exception = Assert.Throws<InvalidOperationException>(() => Encrypt(recipient));
            Assert.Equal(CryptoResources.PgpEncryptionKeyNotUsable, exception.Message);
        }
    }
}
