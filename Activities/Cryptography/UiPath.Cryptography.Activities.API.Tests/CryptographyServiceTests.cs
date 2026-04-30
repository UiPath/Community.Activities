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
    public class CryptographyServiceTests
    {
        private readonly CryptographyService _service = new CryptographyService();

        #region EncryptText / DecryptText

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
        public void EncryptText_ThenDecryptText_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, coded workflows!";
            string key = "mySecretKey";

            string encrypted = _service.EncryptText(original, algorithm, key, Encoding.UTF8);
            string decrypted = _service.DecryptText(encrypted, algorithm, key, Encoding.UTF8);

            decrypted.ShouldBe(original);
        }

        #endregion

        #region KeyedHashText

        [Fact]
        public void KeyedHashText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.KeyedHashText(null, KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8));
        }

        [Fact]
        public void KeyedHashText_NullEncoding_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, "key", null));
        }

        [Fact]
        public void KeyedHashText_EmptyKey_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, string.Empty, Encoding.UTF8));
        }

        [Fact]
        public void KeyedHashText_ReturnsHexString()
        {
            string result = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);

            result.ShouldNotBeNull();
            result.ShouldMatch("^[0-9A-F]+$");
        }

        [Fact]
        public void KeyedHashText_SameInputAndKey_ReturnsSameHash()
        {
            string hash1 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);
            string hash2 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key", Encoding.UTF8);

            hash1.ShouldBe(hash2);
        }

        [Fact]
        public void KeyedHashText_DifferentKeys_ReturnsDifferentHash()
        {
            string hash1 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key1", Encoding.UTF8);
            string hash2 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, "key2", Encoding.UTF8);

            hash1.ShouldNotBe(hash2);
        }

        #endregion

        #region EncryptFile / DecryptFile

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
        public void EncryptFile_ExistingOutputWithoutOverwrite_Throws()
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                File.WriteAllText(inputPath, "test content");
                File.WriteAllText(outputPath, "existing output"); // output must exist to trigger the guard

                Should.Throw<InvalidOperationException>(() =>
                    _service.EncryptFile(inputPath, outputPath, EncryptionAlgorithm.AES, "key", Encoding.UTF8, overwrite: false));
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(outputPath);
            }
        }

        [Fact]
        public void EncryptFile_ThenDecryptFile_ReturnsOriginalContent()
        {
            string original = "Coded workflow file encryption test";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);

                _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, "testKey", Encoding.UTF8, overwrite: true);
                _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, "testKey", Encoding.UTF8, overwrite: true);

                string result = File.ReadAllText(decryptedPath, Encoding.UTF8);
                result.ShouldBe(original);
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(encryptedPath);
                File.Delete(decryptedPath);
            }
        }

        #endregion

        #region PGP guard clauses

        [Fact]
        public void PgpEncrypt_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpEncrypt(null, Stream.Null));
        }

        [Fact]
        public void PgpEncrypt_NullPublicKeyStream_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpEncrypt(new byte[1], null));
        }

        [Fact]
        public void PgpDecrypt_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpDecrypt(null, Stream.Null, "pass"));
        }

        [Fact]
        public void PgpDecrypt_NullPrivateKeyStream_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpDecrypt(new byte[1], null, "pass"));
        }

        [Fact]
        public void PgpEncryptText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpEncryptText(null, Stream.Null));
        }

        [Fact]
        public void PgpDecryptText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpDecryptText(null, Stream.Null, "pass"));
        }

        [Fact]
        public void PgpSignFile_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpSignFile(null, Stream.Null, "pass"));
        }

        [Fact]
        public void PgpClearSignFile_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpClearSignFile(null, Stream.Null, "pass"));
        }

        [Fact]
        public void PgpVerify_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpVerify(null, Stream.Null));
        }

        [Fact]
        public void PgpVerifyClear_NullInputBytes_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpVerifyClear(null, Stream.Null));
        }

        [Fact]
        public void PgpGenerateKeyPair_NullPublicKeyPath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.PgpGenerateKeyPair(null, "private.asc", "user", "pass"));
        }

        [Fact]
        public void PgpGenerateKeyPair_NullPrivateKeyPath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.PgpGenerateKeyPair("public.asc", null, "user", "pass"));
        }

        #endregion

        #region SecureString key overloads

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_SecureStringKey_ThenDecryptText_SecureStringKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, SecureString!";
            SecureString key = ToSecureString("mySecretKey");

            string encrypted = _service.EncryptText(original, algorithm, key, Encoding.UTF8);
            string decrypted = _service.DecryptText(encrypted, algorithm, key, Encoding.UTF8);

            decrypted.ShouldBe(original);
        }

        [Fact]
        public void EncryptText_SecureStringKey_NullKey_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, (SecureString)null, Encoding.UTF8));
        }

        [Fact]
        public void EncryptFile_SecureStringKey_ThenDecryptFile_ReturnsOriginalContent()
        {
            string original = "SecureString file encryption test";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            SecureString key = ToSecureString("secureFileKey");

            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);

                _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, key, Encoding.UTF8, overwrite: true);
                _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, key, Encoding.UTF8, overwrite: true);

                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(encryptedPath);
                File.Delete(decryptedPath);
            }
        }

        [Fact]
        public void KeyedHashText_SecureStringKey_MatchesStringKeyHash()
        {
            const string input = "hello";
            const string keyStr = "myHmacKey";
            SecureString keySecure = ToSecureString(keyStr);

            string hashFromString = _service.KeyedHashText(input, KeyedHashAlgorithms.HMACSHA256, keyStr, Encoding.UTF8);
            string hashFromSecure = _service.KeyedHashText(input, KeyedHashAlgorithms.HMACSHA256, keySecure, Encoding.UTF8);

            hashFromSecure.ShouldBe(hashFromString);
        }

        [Fact]
        public void KeyedHashText_SecureStringKey_NullKey_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, (SecureString)null, Encoding.UTF8));
        }

        [Fact]
        public void PgpEncrypt_SecureStringPassphrase_NullPassphrase_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpEncrypt(new byte[1], Stream.Null, Stream.Null, (SecureString)null));
        }

        [Fact]
        public void PgpDecrypt_SecureStringPassphrase_NullPassphrase_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.PgpDecrypt(new byte[1], Stream.Null, (SecureString)null));
        }

        #endregion

        #region byte[] key overloads

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ByteArrayKey_ThenDecryptText_ByteArrayKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, byte[] key!";
            byte[] keyBytes = Encoding.UTF8.GetBytes("myRawKeyBytes!!");

            string encrypted = _service.EncryptText(original, algorithm, keyBytes, Encoding.UTF8);
            string decrypted = _service.DecryptText(encrypted, algorithm, keyBytes, Encoding.UTF8);

            decrypted.ShouldBe(original);
        }

        [Fact]
        public void EncryptText_ByteArrayKey_NullKeyBytes_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, (byte[])null, Encoding.UTF8));
        }

        [Fact]
        public void EncryptText_ByteArrayKey_EmptyKeyBytes_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText("input", EncryptionAlgorithm.AES, Array.Empty<byte>(), Encoding.UTF8));
        }

        [Fact]
        public void EncryptFile_ByteArrayKey_ThenDecryptFile_ReturnsOriginalContent()
        {
            string original = "byte[] key file encryption test";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            byte[] keyBytes = Encoding.UTF8.GetBytes("rawFileKeyBytes!!");

            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);

                _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, keyBytes, overwrite: true);
                _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, keyBytes, overwrite: true);

                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(encryptedPath);
                File.Delete(decryptedPath);
            }
        }

        [Fact]
        public void EncryptFile_ByteArrayKey_NullKeyBytes_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile("in.txt", "out.bin", EncryptionAlgorithm.AES, (byte[])null, overwrite: true));
        }

        [Fact]
        public void KeyedHashText_ByteArrayKey_MatchesEquivalentStringKeyHash()
        {
            const string input = "hello";
            byte[] keyBytes = Encoding.UTF8.GetBytes("hmacKey");

            string hashFromBytes = _service.KeyedHashText(input, KeyedHashAlgorithms.HMACSHA256, keyBytes, Encoding.UTF8);
            string hashFromString = _service.KeyedHashText(input, KeyedHashAlgorithms.HMACSHA256, "hmacKey", Encoding.UTF8);

            // Both must be valid hex and the same — the byte[] overload skips the PBKDF2 path,
            // but KeyEncoding(encoding, key, null) == encoding.GetBytes(key) for ASCII keys, so they match.
            hashFromBytes.ShouldBe(hashFromString);
        }

        [Fact]
        public void KeyedHashText_ByteArrayKey_NullKeyBytes_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.KeyedHashText("input", KeyedHashAlgorithms.HMACSHA256, (byte[])null, Encoding.UTF8));
        }

        [Fact]
        public void KeyedHashFile_ByteArrayKey_NullKeyBytes_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.KeyedHashFile("file.txt", KeyedHashAlgorithms.HMACSHA256, (byte[])null));
        }

        #endregion

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SecureString ToSecureString(string value)
        {
            var ss = new SecureString();
            foreach (char c in value)
                ss.AppendChar(c);
            ss.MakeReadOnly();
            return ss;
        }
    }
}
