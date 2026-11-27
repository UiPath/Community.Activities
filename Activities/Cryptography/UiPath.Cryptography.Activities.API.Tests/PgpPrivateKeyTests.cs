using System;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Activities.API;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.API.Tests
{
    /// <summary>
    /// Coverage for <see cref="PgpPrivateKey"/> and <see cref="PgpKeyPair"/> behaviours
    /// not exercised by the round-trip tests in <see cref="CryptographyServiceTests"/>:
    /// load-from-file, save-overwrite contract, and wrong-passphrase translation.
    /// </summary>
    public class PgpPrivateKeyTests : IClassFixture<PgpKeyFixture>
    {
        private readonly CryptographyService _service = new CryptographyService();
        private readonly PgpKeyFixture _keys;

        public PgpPrivateKeyTests(PgpKeyFixture keys) => _keys = keys;

        // Save → FromFilePath round-trip. Today CryptographyServiceTests only exercises
        // PgpPublicKey.FromFilePath; pin the private-key path the same way.
        [Fact]
        public void PrivateKey_SaveThenLoadFromFilePath_RoundTrips()
        {
            string privatePath = Path.Combine(Path.GetTempPath(), $"pgp_priv_{Guid.NewGuid():N}.asc");
            try
            {
                _keys.PrivateKey.Save(privatePath, overwrite: true);
                File.Exists(privatePath).ShouldBeTrue();

                using PgpPrivateKey reloaded = PgpPrivateKey.FromFilePath(privatePath, PgpKeyFixture.Passphrase);

                byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("save-reload"), _keys.PublicKey);
                byte[] plain = _service.PgpDecryptBytes(cipher, reloaded);
                Encoding.UTF8.GetString(plain).ShouldBe("save-reload");
            }
            finally
            {
                if (File.Exists(privatePath)) File.Delete(privatePath);
            }
        }

        [Fact]
        public void PrivateKey_SaveOverwriteFalse_ExistingFile_Throws()
        {
            string path = Path.Combine(Path.GetTempPath(), $"pgp_priv_{Guid.NewGuid():N}.asc");
            File.WriteAllText(path, "stub-existing-content");
            try
            {
                Should.Throw<InvalidOperationException>(() => _keys.PrivateKey.Save(path, overwrite: false));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void PrivateKey_SaveOverwriteTrue_ExistingFile_Overwrites()
        {
            string path = Path.Combine(Path.GetTempPath(), $"pgp_priv_{Guid.NewGuid():N}.asc");
            File.WriteAllText(path, "stub-existing-content");
            try
            {
                _keys.PrivateKey.Save(path, overwrite: true);

                // The file should now contain the actual private-key bytes.
                byte[] onDisk = File.ReadAllBytes(path);
                onDisk.ShouldBe(_keys.PrivateKeyBytes);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Wrong passphrase → BouncyCastle's "Checksum mismatch" gets translated by
        // CryptographyHelper.TranslatePgpException into an InvalidOperationException with a
        // clear hint. Pin the message substring so the hint doesn't regress to a generic exception.
        [Fact]
        public void PrivateKey_WrongPassphrase_Throws_TranslatedException()
        {
            using PgpPrivateKey wrong = PgpPrivateKey.FromBytes(_keys.PrivateKeyBytes, "definitely-not-the-passphrase");
            byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("ok"), _keys.PublicKey);

            // The translated exception should be InvalidOperationException with a passphrase
            // hint, not a raw BouncyCastle "Checksum mismatch".
            InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
                _service.PgpDecryptBytes(cipher, wrong));

            // The hint should mention "passphrase" so the user knows what to fix.
            ex.Message.ShouldContain("passphrase", Case.Insensitive);
        }

        // FromFilePath validation — empty/whitespace path is caught at the factory.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void PrivateKey_FromFilePath_EmptyPath_Throws(string path)
        {
            Should.Throw<ArgumentException>(() => PgpPrivateKey.FromFilePath(path, "pwd"));
        }

        // FromFilePath with SecureString passphrase — overload coverage.
        [Fact]
        public void PrivateKey_FromFilePath_WithSecureStringPassphrase_RoundTrips()
        {
            string path = Path.Combine(Path.GetTempPath(), $"pgp_priv_{Guid.NewGuid():N}.asc");
            try
            {
                _keys.PrivateKey.Save(path, overwrite: true);
                SecureString securePass = new NetworkCredential(string.Empty, PgpKeyFixture.Passphrase).SecurePassword;

                using PgpPrivateKey loaded = PgpPrivateKey.FromFilePath(path, securePass);

                byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("secure-load"), _keys.PublicKey);
                byte[] plain = _service.PgpDecryptBytes(cipher, loaded);
                Encoding.UTF8.GetString(plain).ShouldBe("secure-load");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ToBytes returns a defensive copy — pin that mutating the returned array
        // doesn't corrupt subsequent encryption with the live PgpPrivateKey instance.
        [Fact]
        public void PrivateKey_ToBytes_ReturnsDefensiveCopy()
        {
            byte[] bytes1 = _keys.PrivateKey.ToBytes();
            byte[] bytes2 = _keys.PrivateKey.ToBytes();
            bytes1.ShouldBe(bytes2);

            // Mutate the first copy — second call must still return the canonical bytes.
            Array.Clear(bytes1, 0, bytes1.Length);
            byte[] bytes3 = _keys.PrivateKey.ToBytes();
            bytes3.ShouldBe(bytes2);
            bytes3.ShouldNotBe(bytes1);
        }
    }
}
