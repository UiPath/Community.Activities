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
        public void EncryptText_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.EncryptText(null));
        }

        [Fact]
        public void EncryptText_NoInputData_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText(new CryptoOptions { Key = "key", Encoding = Encoding.UTF8 }));
        }

        [Fact]
        public void EncryptText_NoKeyMaterial_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptText(new CryptoOptions { Input = "hello", Encoding = Encoding.UTF8 }));
        }

        [Fact]
        public void DecryptText_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.DecryptText(null));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_StringKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            const string original = "Hello, coded workflows!";
            var encryptOptions = new CryptoOptions { Input = original, Key = "mySecretKey", Algorithm = algorithm, Encoding = Encoding.UTF8 };

            string ciphertext = _service.EncryptText(encryptOptions);
            string plaintext = _service.DecryptText(new CryptoOptions { Input = ciphertext, Key = "mySecretKey", Algorithm = algorithm, Encoding = Encoding.UTF8 });

            plaintext.ShouldBe(original);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_SecureStringKey_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            const string original = "Hello, SecureString!";
            var encryptOptions = new CryptoOptions { Input = original, KeySecure = ToSecureString("mySecretKey"), Algorithm = algorithm, Encoding = Encoding.UTF8 };

            string ciphertext = _service.EncryptText(encryptOptions);
            string plaintext = _service.DecryptText(new CryptoOptions { Input = ciphertext, KeySecure = ToSecureString("mySecretKey"), Algorithm = algorithm, Encoding = Encoding.UTF8 });

            plaintext.ShouldBe(original);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_RawKeyBytes_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            const string original = "Hello, byte[] key!";
            byte[] keyBytes = Encoding.UTF8.GetBytes("myRawKeyBytes!!");
            var encryptOptions = new CryptoOptions { Input = original, KeyRaw = keyBytes, Algorithm = algorithm, Encoding = Encoding.UTF8 };

            string ciphertext = _service.EncryptText(encryptOptions);
            string plaintext = _service.DecryptText(new CryptoOptions { Input = ciphertext, KeyRaw = keyBytes, Algorithm = algorithm, Encoding = Encoding.UTF8 });

            plaintext.ShouldBe(original);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_InputRaw_ReturnsOriginal(EncryptionAlgorithm algorithm)
        {
            const string original = "Hello, raw input!";
            byte[] rawInput = Encoding.UTF8.GetBytes(original);
            byte[] keyBytes = Encoding.UTF8.GetBytes("myRawKeyBytes!!");

            string ciphertext = _service.EncryptText(new CryptoOptions { InputRaw = rawInput, KeyRaw = keyBytes, Algorithm = algorithm, Encoding = Encoding.UTF8 });
            string plaintext = _service.DecryptText(new CryptoOptions { Input = ciphertext, KeyRaw = keyBytes, Algorithm = algorithm, Encoding = Encoding.UTF8 });

            plaintext.ShouldBe(original);
        }

        #endregion

        #region KeyedHashText

        [Fact]
        public void KeyedHashText_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _service.KeyedHashText(null));
        }

        [Fact]
        public void KeyedHashText_ReturnsHexString()
        {
            string result = _service.KeyedHashText(new CryptoOptions { Input = "hello", Key = "key", KeyedHashAlgorithm = KeyedHashAlgorithms.HMACSHA256, Encoding = Encoding.UTF8 });

            result.ShouldNotBeNull();
            result.ShouldMatch("^[0-9A-F]+$");
        }

        [Fact]
        public void KeyedHashText_SameInputAndKey_ReturnsSameHash()
        {
            var options = new CryptoOptions { Input = "hello", Key = "key", KeyedHashAlgorithm = KeyedHashAlgorithms.HMACSHA256, Encoding = Encoding.UTF8 };

            string hash1 = _service.KeyedHashText(options);
            string hash2 = _service.KeyedHashText(options);

            hash1.ShouldBe(hash2);
        }

        [Fact]
        public void KeyedHashText_DifferentKeys_ReturnsDifferentHash()
        {
            string hash1 = _service.KeyedHashText(new CryptoOptions { Input = "hello", Key = "key1", KeyedHashAlgorithm = KeyedHashAlgorithms.HMACSHA256, Encoding = Encoding.UTF8 });
            string hash2 = _service.KeyedHashText(new CryptoOptions { Input = "hello", Key = "key2", KeyedHashAlgorithm = KeyedHashAlgorithms.HMACSHA256, Encoding = Encoding.UTF8 });

            hash1.ShouldNotBe(hash2);
        }

        [Fact]
        public void KeyedHashText_SecureStringKey_MatchesStringKeyHash()
        {
            const string input = "hello";
            const string keyStr = "myHmacKey";

            string hashFromString = _service.KeyedHashText(new CryptoOptions { Input = input, Key = keyStr, KeyedHashAlgorithm = KeyedHashAlgorithms.HMACSHA256, Encoding = Encoding.UTF8 });
            string hashFromSecure = _service.KeyedHashText(new CryptoOptions { Input = input, KeySecure = ToSecureString(keyStr), KeyedHashAlgorithm = KeyedHashAlgorithms.HMACSHA256, Encoding = Encoding.UTF8 });

            hashFromSecure.ShouldBe(hashFromString);
        }

        #endregion

        #region EncryptFile / DecryptFile

        [Fact]
        public void EncryptFile_NullInputFilePath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile(new CryptoOptions { OutputFile = "out.bin", Key = "key", Overwrite = true }));
        }

        [Fact]
        public void EncryptFile_NullOutputFile_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile(new CryptoOptions { Input = "in.txt", Key = "key", Overwrite = true }));
        }

        [Fact]
        public void DecryptFile_NullInputFilePath_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                _service.DecryptFile(new CryptoOptions { OutputFile = "out.txt", Key = "key", Overwrite = true }));
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
                    _service.EncryptFile(new CryptoOptions { Input = inputPath, OutputFile = outputPath, Key = "key", Overwrite = false }));
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(outputPath);
            }
        }

        [Fact]
        public void EncryptFile_ThenDecryptFile_StringKey_ReturnsOriginalContent()
        {
            const string original = "Coded workflow file encryption test";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);

                _service.EncryptFile(new CryptoOptions { Input = inputPath, OutputFile = encryptedPath, Key = "testKey", Algorithm = EncryptionAlgorithm.AES, Encoding = Encoding.UTF8, Overwrite = true });
                _service.DecryptFile(new CryptoOptions { Input = encryptedPath, OutputFile = decryptedPath, Key = "testKey", Algorithm = EncryptionAlgorithm.AES, Encoding = Encoding.UTF8, Overwrite = true });

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
        public void EncryptFile_ThenDecryptFile_SecureStringKey_ReturnsOriginalContent()
        {
            const string original = "SecureString file encryption test";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);

                _service.EncryptFile(new CryptoOptions { Input = inputPath, OutputFile = encryptedPath, KeySecure = ToSecureString("secureFileKey"), Algorithm = EncryptionAlgorithm.AES, Encoding = Encoding.UTF8, Overwrite = true });
                _service.DecryptFile(new CryptoOptions { Input = encryptedPath, OutputFile = decryptedPath, KeySecure = ToSecureString("secureFileKey"), Algorithm = EncryptionAlgorithm.AES, Encoding = Encoding.UTF8, Overwrite = true });

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
        public void EncryptFile_ThenDecryptFile_RawKeyBytes_ReturnsOriginalContent()
        {
            const string original = "byte[] key file encryption test";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            byte[] keyBytes = Encoding.UTF8.GetBytes("rawFileKeyBytes!!");

            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);

                _service.EncryptFile(new CryptoOptions { Input = inputPath, OutputFile = encryptedPath, KeyRaw = keyBytes, Algorithm = EncryptionAlgorithm.AES, Overwrite = true });
                _service.DecryptFile(new CryptoOptions { Input = encryptedPath, OutputFile = decryptedPath, KeyRaw = keyBytes, Algorithm = EncryptionAlgorithm.AES, Overwrite = true });

                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(encryptedPath);
                File.Delete(decryptedPath);
            }
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
