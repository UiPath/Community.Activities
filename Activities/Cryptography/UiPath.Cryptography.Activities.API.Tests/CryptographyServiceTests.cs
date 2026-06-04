using System;
using System.IO;
using System.Security;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Activities.API;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.API.Tests
{
    /// <summary>
    /// xUnit class fixture: generates a single PGP key pair (RSA-2048 for test speed) once
    /// per test class and exposes the bytes + paths so PGP roundtrip tests don't pay the
    /// multi-second key-generation cost per case.
    /// </summary>
    public sealed class PgpKeyFixture : IDisposable
    {
        public const string Passphrase = "fixture-pass";

        public byte[] PublicKey { get; }
        public byte[] PrivateKey { get; }
        public string PublicKeyArmored { get; }
        public string PrivateKeyArmored { get; }
        public string PublicKeyPath { get; }
        public string PrivateKeyPath { get; }

        public PgpKeyFixture()
        {
            var service = new CryptographyService();
            PublicKeyPath = Path.Combine(Path.GetTempPath(), $"cryptosvc_pub_{Guid.NewGuid()}.asc");
            PrivateKeyPath = Path.Combine(Path.GetTempPath(), $"cryptosvc_priv_{Guid.NewGuid()}.asc");
            service.PgpGenerateKeys(PublicKeyPath, PrivateKeyPath, "Service Tester <svc@test.com>", Passphrase, RsaKeySize.Rsa2048);
            PublicKey = File.ReadAllBytes(PublicKeyPath);
            PrivateKey = File.ReadAllBytes(PrivateKeyPath);
            PublicKeyArmored = Encoding.UTF8.GetString(PublicKey);
            PrivateKeyArmored = Encoding.UTF8.GetString(PrivateKey);
        }

        public void Dispose()
        {
            if (File.Exists(PublicKeyPath)) File.Delete(PublicKeyPath);
            if (File.Exists(PrivateKeyPath)) File.Delete(PrivateKeyPath);
        }
    }

#pragma warning disable CS0618 // Tests intentionally exercise the obsolete AES enum because that's the algorithm the service ships.
    public class CryptographyServiceTests : IClassFixture<PgpKeyFixture>
    {
        private readonly CryptographyService _service = new CryptographyService();
        private readonly PgpKeyFixture _keys;

        public CryptographyServiceTests(PgpKeyFixture keys)
        {
            _keys = keys;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Symmetric Encrypt / Decrypt — Bytes form
        // ═══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        public void Encrypt_Decrypt_StringKey_RoundTrip(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Hello bytes form!");
            byte[] cipher = _service.EncryptBytes(plain, algorithm, "myKey", Encoding.UTF8);
            byte[] decrypted = _service.DecryptBytes(cipher, algorithm, "myKey", Encoding.UTF8);
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        public void Encrypt_Decrypt_SecureStringKey_RoundTrip(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Hello secure bytes!");
            SecureString key = ToSecureString("mySecureKey");
            byte[] cipher = _service.EncryptBytes(plain, algorithm, key, Encoding.UTF8);
            byte[] decrypted = _service.DecryptBytes(cipher, algorithm, key, Encoding.UTF8);
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        public void Encrypt_Decrypt_ByteArrayKey_RoundTrip(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Hello raw key bytes!");
            byte[] key = Encoding.UTF8.GetBytes("rawKeyBytes!");
            byte[] cipher = _service.EncryptBytes(plain, algorithm, key);
            byte[] decrypted = _service.DecryptBytes(cipher, algorithm, key);
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Encrypt_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.EncryptBytes(null, EncryptionAlgorithm.AES, "k", Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.EncryptBytes(null, EncryptionAlgorithm.AES, ToSecureString("k"), Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.EncryptBytes(null, EncryptionAlgorithm.AES, Encoding.UTF8.GetBytes("k")));
        }

        [Fact]
        public void Decrypt_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.DecryptBytes(null, EncryptionAlgorithm.AES, "k", Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.DecryptBytes(null, EncryptionAlgorithm.AES, ToSecureString("k"), Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.DecryptBytes(null, EncryptionAlgorithm.AES, Encoding.UTF8.GetBytes("k")));
        }

        [Fact]
        public void Encrypt_EmptyKey_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.EncryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, string.Empty, Encoding.UTF8));
            Should.Throw<ArgumentException>(() => _service.EncryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, Array.Empty<byte>()));
        }

        [Fact]
        public void Encrypt_NullSecureStringKey_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.EncryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, (SecureString)null, Encoding.UTF8));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Symmetric Encrypt / Decrypt — Text form
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void EncryptText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.EncryptText(null, EncryptionAlgorithm.AES, "key", Encoding.UTF8));
        }

        [Fact]
        public void EncryptText_NullEncoding_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, "key", null));
        }

        [Fact]
        public void EncryptText_EmptyKey_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, string.Empty, Encoding.UTF8));
        }

        [Fact]
        public void DecryptText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.DecryptText(null, EncryptionAlgorithm.AES, "key", Encoding.UTF8));
        }

        [Fact]
        public void DecryptText_NullEncoding_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.DecryptText("aGVsbG8=", EncryptionAlgorithm.AES, "key", null));
        }

        [Fact]
        public void DecryptText_EmptyKey_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.DecryptText("aGVsbG8=", EncryptionAlgorithm.AES, string.Empty, Encoding.UTF8));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_StringKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, coded workflows!";
            string encrypted = _service.EncryptText(original, algorithm, "mySecretKey", Encoding.UTF8);
            string decrypted = _service.DecryptText(encrypted, algorithm, "mySecretKey", Encoding.UTF8);
            decrypted.ShouldBe(original);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_SecureStringKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, SecureString!";
            SecureString key = ToSecureString("mySecretKey");
            string encrypted = _service.EncryptText(original, algorithm, key, Encoding.UTF8);
            string decrypted = _service.DecryptText(encrypted, algorithm, key, Encoding.UTF8);
            decrypted.ShouldBe(original);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_ByteArrayKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, byte[] key!";
            byte[] keyBytes = Encoding.UTF8.GetBytes("myRawKeyBytes!!");
            string encrypted = _service.EncryptText(original, algorithm, keyBytes, Encoding.UTF8);
            string decrypted = _service.DecryptText(encrypted, algorithm, keyBytes, Encoding.UTF8);
            decrypted.ShouldBe(original);
        }

        [Fact]
        public void EncryptText_NullSecureStringKey_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, (SecureString)null, Encoding.UTF8));
        }

        [Fact]
        public void EncryptText_NullOrEmptyByteArrayKey_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, (byte[])null, Encoding.UTF8));
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, Array.Empty<byte>(), Encoding.UTF8));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Symmetric Encrypt / Decrypt — File form
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void EncryptFile_NullInputPath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile(null, "out.bin", EncryptionAlgorithm.AES, "key", Encoding.UTF8, true));
        }

        [Fact]
        public void EncryptFile_NullOutputPath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile("in.txt", null, EncryptionAlgorithm.AES, "key", Encoding.UTF8, true));
        }

        [Fact]
        public void DecryptFile_NullInputPath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.DecryptFile(null, "out.txt", EncryptionAlgorithm.AES, "key", Encoding.UTF8, true));
        }

        [Fact]
        public void EncryptFile_NullOrEmptyByteArrayKey_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile("in.txt", "out.bin", EncryptionAlgorithm.AES, (byte[])null, overwrite: true));
        }

        [Fact]
        public void EncryptFile_ExistingOutputWithoutOverwrite_Throws()
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, "test content");
                File.WriteAllText(outputPath, "existing output");
                Should.Throw<InvalidOperationException>(() =>
                    _service.EncryptFile(inputPath, outputPath, EncryptionAlgorithm.AES, "key", Encoding.UTF8, overwrite: false));
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(outputPath);
            }
        }

        [Theory]
        [InlineData("string")]
        [InlineData("secure")]
        [InlineData("bytes")]
        public void EncryptFile_ThenDecryptFile_AllKeyForms_RoundTrip(string keyForm)
        {
            string original = "File round-trip — " + keyForm;
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);
                switch (keyForm)
                {
                    case "string":
                        _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, "testKey", Encoding.UTF8, overwrite: true);
                        _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, "testKey", Encoding.UTF8, overwrite: true);
                        break;
                    case "secure":
                        var ss = ToSecureString("testKey");
                        _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, ss, Encoding.UTF8, overwrite: true);
                        _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, ss, Encoding.UTF8, overwrite: true);
                        break;
                    case "bytes":
                        byte[] kb = Encoding.UTF8.GetBytes("testKey");
                        _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, kb, overwrite: true);
                        _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, kb, overwrite: true);
                        break;
                }
                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
                if (File.Exists(decryptedPath)) File.Delete(decryptedPath);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Keyed Hash — Bytes / Text / File
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void KeyedHashBytes_StringKey_DeterministicAndHex()
        {
            byte[] input = Encoding.UTF8.GetBytes("hello");
            string hash1 = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);
            string hash2 = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);
            hash1.ShouldBe(hash2);
            hash1.ShouldMatch("^[0-9A-F]+$");
        }

        [Fact]
        public void KeyedHashBytes_SecureStringKey_MatchesStringKey()
        {
            byte[] input = Encoding.UTF8.GetBytes("hello");
            string fromString = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, "k", Encoding.UTF8);
            string fromSecure = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, ToSecureString("k"), Encoding.UTF8);
            fromSecure.ShouldBe(fromString);
        }

        [Fact]
        public void KeyedHashBytes_ByteArrayKey_Works()
        {
            byte[] input = Encoding.UTF8.GetBytes("hello");
            byte[] key = Encoding.UTF8.GetBytes("k");
            string hash = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, key);
            hash.ShouldMatch("^[0-9A-F]+$");
        }

        [Fact]
        public void KeyedHashBytes_Guards()
        {
            byte[] input = Encoding.UTF8.GetBytes("x");
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashBytes(null, KeyedHashAlgorithms.HMACSHA256, "k", Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashBytes(null, KeyedHashAlgorithms.HMACSHA256, ToSecureString("k"), Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashBytes(null, KeyedHashAlgorithms.HMACSHA256, Encoding.UTF8.GetBytes("k")));
            Should.Throw<ArgumentException>(() => _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, string.Empty, Encoding.UTF8));
            Should.Throw<ArgumentException>(() => _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, (byte[])null));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, (SecureString)null, Encoding.UTF8));
        }

        [Fact]
        public void KeyedHashText_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashText(null, KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, "key", null));
            Should.Throw<ArgumentException>(() => _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, string.Empty, Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, (SecureString)null, Encoding.UTF8));
            Should.Throw<ArgumentException>(() => _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, (byte[])null, Encoding.UTF8));
        }

        [Fact]
        public void KeyedHashText_StringKey_DeterministicHex()
        {
            string h1 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);
            string h2 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);
            h1.ShouldBe(h2);
            h1.ShouldMatch("^[0-9A-F]+$");
        }

        [Fact]
        public void KeyedHashText_DifferentKeys_DifferentHash()
        {
            string h1 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "k1", Encoding.UTF8);
            string h2 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "k2", Encoding.UTF8);
            h1.ShouldNotBe(h2);
        }

        [Fact]
        public void KeyedHashText_ByteArrayKey_MatchesStringKey()
        {
            byte[] kb = Encoding.UTF8.GetBytes("hmacKey");
            string fromBytes = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, kb, Encoding.UTF8);
            string fromString = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "hmacKey", Encoding.UTF8);
            fromBytes.ShouldBe(fromString);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("secure")]
        [InlineData("bytes")]
        public void KeyedHashFile_AllKeyForms_RoundTrip(string keyForm)
        {
            string filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                // File.WriteAllBytes (not WriteAllText) — WriteAllText with Encoding.UTF8 prepends a BOM,
                // which would make the file content differ from Encoding.UTF8.GetBytes("hello") and break the equality assertion.
                File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes("hello"));
                string hashFromFile = keyForm switch
                {
                    "string" => _service.KeyedHashFile(filePath, KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8),
                    "secure" => _service.KeyedHashFile(filePath, KeyedHashAlgorithms.HMACSHA256, ToSecureString("key"), Encoding.UTF8),
                    "bytes"  => _service.KeyedHashFile(filePath, KeyedHashAlgorithms.HMACSHA256, Encoding.UTF8.GetBytes("key")),
                    _ => throw new InvalidOperationException(),
                };
                string hashFromText = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);
                hashFromFile.ShouldBe(hashFromText);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public void KeyedHashFile_Guards()
        {
            Should.Throw<ArgumentException>(() => _service.KeyedHashFile(null, KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8));
            Should.Throw<ArgumentException>(() => _service.KeyedHashFile("file.txt", KeyedHashAlgorithms.HMACSHA256, (byte[])null));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Encrypt / Decrypt — Bytes / Text / File
        // ═══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(false)] // string passphrase
        [InlineData(true)]  // SecureString passphrase
        public void PgpEncrypt_Decrypt_Bytes_RoundTrip(bool useSecure)
        {
            byte[] plain = Encoding.UTF8.GetBytes("PGP bytes round-trip");
            byte[] cipher;
            byte[] decrypted;
            if (useSecure)
            {
                var ss = ToSecureString(PgpKeyFixture.Passphrase);
                cipher = _service.PgpEncryptBytes(plain, _keys.PublicKey, _keys.PrivateKey, ss);
                decrypted = _service.PgpDecryptBytes(cipher, _keys.PrivateKey, ss);
            }
            else
            {
                cipher = _service.PgpEncryptBytes(plain, _keys.PublicKey);
                decrypted = _service.PgpDecryptBytes(cipher, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            }
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpEncrypt_Decrypt_Text_RoundTrip(bool useSecure)
        {
            const string plain = "PGP text round-trip";
            string cipher;
            string decrypted;
            if (useSecure)
            {
                var ss = ToSecureString(PgpKeyFixture.Passphrase);
                cipher = _service.PgpEncryptText(plain, _keys.PublicKey, _keys.PrivateKey, ss);
                decrypted = _service.PgpDecryptText(cipher, _keys.PrivateKey, ss);
            }
            else
            {
                cipher = _service.PgpEncryptText(plain, _keys.PublicKey);
                decrypted = _service.PgpDecryptText(cipher, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            }
            cipher.ShouldStartWith("-----BEGIN PGP MESSAGE-----");
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpEncryptFile_DecryptFile_RoundTrip(bool useSecure)
        {
            const string original = "PGP file round-trip";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);
                if (useSecure)
                {
                    var ss = ToSecureString(PgpKeyFixture.Passphrase);
                    _service.PgpEncryptFile(inputPath, encryptedPath, _keys.PublicKey, null, ss, sign: false, overwrite: true);
                    _service.PgpDecryptFile(encryptedPath, decryptedPath, _keys.PrivateKey, ss, null, verifySignature: false, overwrite: true);
                }
                else
                {
                    _service.PgpEncryptFile(inputPath, encryptedPath, _keys.PublicKey, null, (string)null, sign: false, overwrite: true);
                    _service.PgpDecryptFile(encryptedPath, decryptedPath, _keys.PrivateKey, PgpKeyFixture.Passphrase, null, verifySignature: false, overwrite: true);
                }
                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
                if (File.Exists(decryptedPath)) File.Delete(decryptedPath);
            }
        }

        [Fact]
        public void PgpEncrypt_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpEncryptBytes(null, Array.Empty<byte>()));
            Should.Throw<ArgumentException>(() => _service.PgpEncryptBytes(new byte[] { 1 }, (byte[])null));
            Should.Throw<ArgumentNullException>(() => _service.PgpEncryptBytes(new byte[] { 1 }, Array.Empty<byte>(), Array.Empty<byte>(), (SecureString)null));
        }

        [Fact]
        public void PgpDecrypt_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpDecryptBytes(null, Array.Empty<byte>(), "pass"));
            Should.Throw<ArgumentException>(() => _service.PgpDecryptBytes(new byte[] { 1 }, (byte[])null, "pass"));
            Should.Throw<ArgumentNullException>(() => _service.PgpDecryptBytes(new byte[] { 1 }, Array.Empty<byte>(), (SecureString)null));
        }

        [Fact]
        public void PgpEncryptText_DecryptText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpEncryptText(null, Array.Empty<byte>()));
            Should.Throw<ArgumentNullException>(() => _service.PgpDecryptText(null, Array.Empty<byte>(), "pass"));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Sign / Clearsign + Verify — Bytes / Text / File
        // ═══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpSign_Bytes_Then_PgpVerify_RoundTrip(bool useSecure)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Sign me");
            byte[] signed = useSecure
                ? _service.PgpSignBytes(plain, _keys.PrivateKey, ToSecureString(PgpKeyFixture.Passphrase))
                : _service.PgpSignBytes(plain, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            _service.PgpVerifyBytes(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpSignText_Then_PgpVerifyText_RoundTrip(bool useSecure)
        {
            const string plain = "Sign-me-text";
            string signed = useSecure
                ? _service.PgpSignText(plain, _keys.PrivateKey, ToSecureString(PgpKeyFixture.Passphrase))
                : _service.PgpSignText(plain, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            signed.ShouldStartWith("-----BEGIN PGP MESSAGE-----");
            _service.PgpVerifyText(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpSignFile_Then_PgpVerifyFile_RoundTrip(bool useSecure)
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string signedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, "Sign-me-file", Encoding.UTF8);
                if (useSecure)
                {
                    _service.PgpSignFile(inputPath, signedPath, _keys.PrivateKey, ToSecureString(PgpKeyFixture.Passphrase), overwrite: true);
                }
                else
                {
                    _service.PgpSignFile(inputPath, signedPath, _keys.PrivateKey, PgpKeyFixture.Passphrase, overwrite: true);
                }
                _service.PgpVerifyFile(signedPath, _keys.PublicKey).ShouldBeTrue();
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpClearsign_Bytes_Then_PgpVerifyClear_RoundTrip(bool useSecure)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Clearsign me");
            byte[] signed = useSecure
                ? _service.PgpClearsignBytes(plain, _keys.PrivateKey, ToSecureString(PgpKeyFixture.Passphrase))
                : _service.PgpClearsignBytes(plain, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            _service.PgpVerifyClearBytes(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpClearsignText_Then_PgpVerifyClearText_RoundTrip(bool useSecure)
        {
            const string plain = "Clearsign-me-text";
            string signed = useSecure
                ? _service.PgpClearsignText(plain, _keys.PrivateKey, ToSecureString(PgpKeyFixture.Passphrase))
                : _service.PgpClearsignText(plain, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            signed.ShouldStartWith("-----BEGIN PGP SIGNED MESSAGE-----");
            _service.PgpVerifyClearText(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpClearsignFile_Then_PgpVerifyClearFile_RoundTrip(bool useSecure)
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string signedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, "Clearsign-me-file", Encoding.UTF8);
                if (useSecure)
                {
                    _service.PgpClearsignFile(inputPath, signedPath, _keys.PrivateKey, ToSecureString(PgpKeyFixture.Passphrase), overwrite: true);
                }
                else
                {
                    _service.PgpClearsignFile(inputPath, signedPath, _keys.PrivateKey, PgpKeyFixture.Passphrase, overwrite: true);
                }
                _service.PgpVerifyClearFile(signedPath, _keys.PublicKey).ShouldBeTrue();
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Fact]
        public void PgpSign_Clearsign_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpSignBytes(null, Array.Empty<byte>(), "pass"));
            Should.Throw<ArgumentNullException>(() => _service.PgpClearsignBytes(null, Array.Empty<byte>(), "pass"));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Verify — guards + tamper detection
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpVerify_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyBytes(null, Array.Empty<byte>()));
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyClearBytes(null, Array.Empty<byte>()));
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyText(null, Array.Empty<byte>()));
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyClearText(null, Array.Empty<byte>()));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyFile(null, _keys.PublicKey));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyClearFile(null, _keys.PublicKey));
        }

        [Fact]
        public void PgpVerify_TamperedBytes_ReturnsFalse()
        {
            byte[] plain = Encoding.UTF8.GetBytes("tamper test");
            byte[] signed = _service.PgpSignBytes(plain, _keys.PrivateKey, PgpKeyFixture.Passphrase);
            signed[signed.Length / 2] ^= 0x01; // flip a bit
            _service.PgpVerifyBytes(signed, _keys.PublicKey).ShouldBeFalse();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP VerifyPublicKey — Bytes / Text / File
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpVerifyPublicKey_Bytes_ValidKey_ReturnsTrue()
        {
            _service.PgpVerifyPublicKeyBytes(_keys.PublicKey).ShouldBeTrue();
        }

        [Fact]
        public void PgpVerifyPublicKey_Text_ValidKey_ReturnsTrue()
        {
            _service.PgpVerifyPublicKeyText(_keys.PublicKeyArmored).ShouldBeTrue();
        }

        [Fact]
        public void PgpVerifyPublicKey_File_ValidKey_ReturnsTrue()
        {
            _service.PgpVerifyPublicKeyFile(_keys.PublicKeyPath).ShouldBeTrue();
        }

        [Fact]
        public void PgpVerifyPublicKey_GarbageBytes_ReturnsFalse()
        {
            _service.PgpVerifyPublicKeyBytes(Encoding.UTF8.GetBytes("not a key")).ShouldBeFalse();
        }

        [Fact]
        public void PgpVerifyPublicKey_GarbageText_ReturnsFalse()
        {
            _service.PgpVerifyPublicKeyText("not a key").ShouldBeFalse();
        }

        [Fact]
        public void PgpVerifyPublicKey_Guards()
        {
            Should.Throw<ArgumentException>(() => _service.PgpVerifyPublicKeyBytes((byte[])null));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyPublicKeyText(null));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyPublicKeyText(string.Empty));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyPublicKeyFile(null));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Generate Keys — 4-arg + 5-arg roundtrips
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpGenerateKeys_Guards()
        {
            Should.Throw<ArgumentException>(() => _service.PgpGenerateKeys(null, "private.asc", "user", "pass"));
            Should.Throw<ArgumentException>(() => _service.PgpGenerateKeys("public.asc", null, "user", "pass"));
            Should.Throw<ArgumentException>(() => _service.PgpGenerateKeys(null, "private.asc", "user", "pass", RsaKeySize.Rsa2048));
        }

        [Fact]
        public void PgpGenerateKeys_4Arg_ProducesUsableKeyPair()
        {
            string pubPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string privPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                _service.PgpGenerateKeys(pubPath, privPath, "Gen Test <gen@test.com>", "gen-pass");
                byte[] pub = File.ReadAllBytes(pubPath);
                byte[] priv = File.ReadAllBytes(privPath);
                _service.PgpVerifyPublicKeyBytes(pub).ShouldBeTrue();
                byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("ok"), pub);
                byte[] plain = _service.PgpDecryptBytes(cipher, priv, "gen-pass");
                Encoding.UTF8.GetString(plain).ShouldBe("ok");
            }
            finally
            {
                if (File.Exists(pubPath)) File.Delete(pubPath);
                if (File.Exists(privPath)) File.Delete(privPath);
            }
        }

        [Theory]
        [InlineData(RsaKeySize.Rsa2048)]
        [InlineData(RsaKeySize.Rsa3072)]
        public void PgpGenerateKeys_5Arg_RespectsKeySize(RsaKeySize size)
        {
            string pubPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string privPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                _service.PgpGenerateKeys(pubPath, privPath, "Sized <s@t.com>", "sized-pass", size);
                _service.PgpVerifyPublicKeyFile(pubPath).ShouldBeTrue();
                // Cross-check by completing an encrypt/decrypt round-trip.
                byte[] pub = File.ReadAllBytes(pubPath);
                byte[] priv = File.ReadAllBytes(privPath);
                byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("rt"), pub);
                byte[] plain = _service.PgpDecryptBytes(cipher, priv, "sized-pass");
                Encoding.UTF8.GetString(plain).ShouldBe("rt");
            }
            finally
            {
                if (File.Exists(pubPath)) File.Delete(pubPath);
                if (File.Exists(privPath)) File.Delete(privPath);
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // helpers
        // ───────────────────────────────────────────────────────────────────────

        private static SecureString ToSecureString(string value)
        {
            var ss = new SecureString();
            foreach (char c in value)
                ss.AppendChar(c);
            ss.MakeReadOnly();
            return ss;
        }
    }
#pragma warning restore CS0618
}
