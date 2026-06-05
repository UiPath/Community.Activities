using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618

namespace UiPath.Cryptography.Activities.Tests
{
    public class CryptographyTests
    {

        [Theory]
        [InlineData(KeyedHashAlgorithms.HMACMD5)]
        [InlineData(KeyedHashAlgorithms.HMACSHA1)]
        [InlineData(KeyedHashAlgorithms.HMACSHA256)]
        [InlineData(KeyedHashAlgorithms.HMACSHA384)]
        [InlineData(KeyedHashAlgorithms.HMACSHA512)]
        public void KeyedHashAlgorithmsMatch(KeyedHashAlgorithms enumValue)
        {
            string toHash = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            KeyedHashText keyedHash = new KeyedHashText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyEncodingString = null // see the ctor of KeyedHashText
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(KeyedHashText.Input), toHash);
            arguments.Add(nameof(KeyedHashText.Key), key);

            WorkflowInvoker invoker = new WorkflowInvoker(keyedHash);
            string activityString = (string)invoker.Invoke(arguments)[nameof(keyedHash.Result)];

            byte[] algorithmBytes = CryptographyHelper.HashDataWithKey(enumValue, Encoding.Unicode.GetBytes(toHash), Encoding.Unicode.GetBytes(key));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        [InlineData(EncryptionAlgorithm.DES)]
        [InlineData(EncryptionAlgorithm.RC2)]
        [InlineData(EncryptionAlgorithm.Rijndael)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptionAlgorithmEncryptionMatches(EncryptionAlgorithm enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            EncryptText symmetricAlgorithm = new EncryptText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyEncodingString = null // see the ctor of EncryptText
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(EncryptText.Input), toProcess);
            arguments.Add(nameof(EncryptText.Key), key);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            byte[] algorithmBytes = CryptographyHelper.DecryptData(enumValue, Convert.FromBase64String(activityString), Encoding.Unicode.GetBytes(key));

            Assert.Equal(toProcess, Encoding.Unicode.GetString(algorithmBytes));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        [InlineData(EncryptionAlgorithm.DES)]
        [InlineData(EncryptionAlgorithm.RC2)]
        [InlineData(EncryptionAlgorithm.Rijndael)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptionAlgorithmDecryptionMatches(EncryptionAlgorithm enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            byte[] algorithmBytes = CryptographyHelper.EncryptData(enumValue, Encoding.Unicode.GetBytes(toProcess), Encoding.Unicode.GetBytes(key));

            DecryptText symmetricAlgorithm = new DecryptText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyEncodingString = null // see the ctor of DecryptText
            };

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(DecryptText.Input), Convert.ToBase64String(algorithmBytes));
            arguments.Add(nameof(DecryptText.Key), key);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            Assert.Equal(toProcess, activityString);
        }

        [Theory]
        [InlineData("Hello World Is Not Enough", "8888f610-529b-4cbb-be1a-20b095402faf")]
        [InlineData("This is just a test", "97dbfca4-7a3c-4fa3-90f3-d17603bbc4b7")]
        public void AesGcmEncryptionMatches(string plainText, string key)
        {
            var encrypted = CryptographyHelper.EncryptData(EncryptionAlgorithm.AESGCM, Encoding.UTF8.GetBytes(plainText), Encoding.UTF8.GetBytes(key));
            var decrypted = CryptographyHelper.DecryptData(EncryptionAlgorithm.AESGCM, encrypted, Encoding.UTF8.GetBytes(key));
            Assert.Equal(Encoding.UTF8.GetString(decrypted), plainText);
        }

        [Theory]
        [InlineData("Hello World Is Not Enough", "8888f610-529b-4cbb-be1a-20b095402faf")]
        [InlineData("This is just a test", "97dbfca4-7a3c-4fa3-90f3-d17603bbc4b7")]
        public void ChaCha20Poly1305EncryptionMatches(string plainText, string key)
        {
            var encrypted = CryptographyHelper.EncryptData(EncryptionAlgorithm.ChaCha20Poly1305, Encoding.UTF8.GetBytes(plainText), Encoding.UTF8.GetBytes(key));
            var decrypted = CryptographyHelper.DecryptData(EncryptionAlgorithm.ChaCha20Poly1305, encrypted, Encoding.UTF8.GetBytes(key));
            Assert.Equal(plainText, Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void AesGcmAndChaCha20Poly1305_ProduceIncompatibleCiphertexts()
        {
            var plain = Encoding.UTF8.GetBytes("plaintext payload");
            var key = Encoding.UTF8.GetBytes("test-key-shared-across-both");

            var aesEncrypted = CryptographyHelper.EncryptData(EncryptionAlgorithm.AESGCM, plain, key);
            var chachaEncrypted = CryptographyHelper.EncryptData(EncryptionAlgorithm.ChaCha20Poly1305, plain, key);

            // Same wire layout (salt|iv|cipher|tag) but different cipher families — cross-decryption must fail.
            Assert.Throws<CryptographicException>(() =>
                CryptographyHelper.DecryptData(EncryptionAlgorithm.AESGCM, chachaEncrypted, key));
            Assert.Throws<CryptographicException>(() =>
                CryptographyHelper.DecryptData(EncryptionAlgorithm.ChaCha20Poly1305, aesEncrypted, key));
        }


        [Theory]
        [InlineData(KeyedHashAlgorithms.HMACMD5)]
        [InlineData(KeyedHashAlgorithms.HMACSHA1)]
        [InlineData(KeyedHashAlgorithms.HMACSHA256)]
        [InlineData(KeyedHashAlgorithms.HMACSHA384)]
        [InlineData(KeyedHashAlgorithms.HMACSHA512)]
        public void KeyedHashAlgorithmsMatchWithSecureString(KeyedHashAlgorithms enumValue)
        {
            string toHash = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            SecureString keySecureString = TestingHelper.StringToSecureString("{>@#F09\0");

            KeyedHashText keyedHash = new KeyedHashText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyInputModeSwitch = KeyInputMode.SecureKey,
                KeyEncodingString = null // see ctor for KeyedHashText
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(KeyedHashText.Input), toHash);
            arguments.Add(nameof(KeyedHashText.KeySecureString), keySecureString);

            WorkflowInvoker invoker = new WorkflowInvoker(keyedHash);
            string activityString = (string)invoker.Invoke(arguments)[nameof(keyedHash.Result)];


            byte[] algorithmBytes = CryptographyHelper.HashDataWithKey(enumValue, Encoding.Unicode.GetBytes(toHash), Encoding.Unicode.GetBytes(TestingHelper.SecureStringToString(keySecureString)));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.DES)]
        [InlineData(EncryptionAlgorithm.RC2)]
        [InlineData(EncryptionAlgorithm.Rijndael)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptionAlgorithmEncryptionMatchesWithSecureString(EncryptionAlgorithm enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            SecureString keySecureString = TestingHelper.StringToSecureString("{>@#F09\0");

            EncryptText symmetricAlgorithm = new EncryptText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyInputModeSwitch = KeyInputMode.SecureKey,
                KeyEncodingString = null // see the ctor of EncryptText
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(EncryptText.Input), toProcess);
            arguments.Add(nameof(EncryptText.KeySecureString), keySecureString);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            byte[] algorithmBytes = CryptographyHelper.DecryptData(enumValue, Convert.FromBase64String(activityString), Encoding.Unicode.GetBytes(TestingHelper.SecureStringToString(keySecureString)));

            Assert.Equal(toProcess, Encoding.Unicode.GetString(algorithmBytes));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.DES)]
        [InlineData(EncryptionAlgorithm.RC2)]
        [InlineData(EncryptionAlgorithm.Rijndael)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptionAlgorithmDecryptionMatchesWithSecureString(EncryptionAlgorithm enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            SecureString keySecureString = TestingHelper.StringToSecureString("{>@#F09\0");

            byte[] algorithmBytes = CryptographyHelper.EncryptData(enumValue, Encoding.Unicode.GetBytes(toProcess), Encoding.Unicode.GetBytes(TestingHelper.SecureStringToString(keySecureString)));

            DecryptText symmetricAlgorithm = new DecryptText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyInputModeSwitch = KeyInputMode.SecureKey,
                KeyEncodingString = null // see the ctor of DecryptText
            };

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(DecryptText.Input), Convert.ToBase64String(algorithmBytes));
            arguments.Add(nameof(DecryptText.KeySecureString), keySecureString);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            Assert.Equal(toProcess, activityString);
        }

        [Fact]
        public void GetAvailableEncodings_IncludesShiftJis()
        {
            var encodings = EncodingHelpers.GetAvailableEncodings();
            Assert.Contains("932", encodings);
        }

        [Fact]
        public void EncryptText_DecryptText_WithShiftJisKeyEncoding_RoundTrips()
        {
            const string toProcess = "Hello, こんにちは, 1234567890";
            const string key = "shift-jis-key-日本語";
            const string shiftJisCodePage = "932";

            EncryptText encrypt = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.AESGCM,
                KeyEncodingString = shiftJisCodePage,
            };
            var encryptArgs = new Dictionary<string, object>
            {
                { nameof(EncryptText.Input), toProcess },
                { nameof(EncryptText.Key), key },
            };
            string ciphertext = (string)new WorkflowInvoker(encrypt).Invoke(encryptArgs)[nameof(encrypt.Result)];

            DecryptText decrypt = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.AESGCM,
                KeyEncodingString = shiftJisCodePage,
            };
            var decryptArgs = new Dictionary<string, object>
            {
                { nameof(DecryptText.Input), ciphertext },
                { nameof(DecryptText.Key), key },
            };
            string roundTripped = (string)new WorkflowInvoker(decrypt).Invoke(decryptArgs)[nameof(decrypt.Result)];

            Assert.Equal(toProcess, roundTripped);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.TripleDES, 8)]
        [InlineData(EncryptionAlgorithm.AES, 16)]
        [InlineData(EncryptionAlgorithm.DES, 8)]
        public void SymmetricEncrypt_WireFormat_HasSaltIvPrefixWithFreshSalt(EncryptionAlgorithm algorithm, int ivSize)
        {
            const int saltSize = 8;
            var plaintext = Encoding.UTF8.GetBytes("hello");
            var key = Encoding.UTF8.GetBytes("a-stable-test-key");

            var first  = CryptographyHelper.EncryptData(algorithm, plaintext, key);
            var second = CryptographyHelper.EncryptData(algorithm, plaintext, key);

            Assert.True(first.Length >= saltSize + ivSize + 1, $"Ciphertext for {algorithm} is shorter than salt|IV|cipher minimum.");
            var firstSalt = new byte[saltSize];
            var secondSalt = new byte[saltSize];
            Buffer.BlockCopy(first, 0, firstSalt, 0, saltSize);
            Buffer.BlockCopy(second, 0, secondSalt, 0, saltSize);
            Assert.NotEqual(firstSalt, secondSalt);
        }

        [Fact]
        public void EncryptText_SamePlaintextAndKey_ProducesDifferentCiphertexts()
        {
            const string plaintext = "hello I am a simple test sentence";
            const string key = "shared-key";

            EncryptText activity = new EncryptText { Algorithm = EncryptionAlgorithm.TripleDES };
            var args = new Dictionary<string, object>
            {
                { nameof(EncryptText.Input), plaintext },
                { nameof(EncryptText.Key), key },
            };
            string first  = (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)];
            string second = (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)];

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void SymmetricDecrypt_InputTooShort_ThrowsWithWireFormatHint()
        {
            var key = Encoding.UTF8.GetBytes("any-key");
            var shortInput = new byte[4];

            var ex = Assert.Throws<CryptographicException>(
                () => CryptographyHelper.DecryptData(EncryptionAlgorithm.TripleDES, shortInput, key));

            Assert.Contains("too short", ex.Message);
            Assert.Contains("UiPath wire format", ex.Message);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        public void AeadDecrypt_InputTooShort_ThrowsWithWireFormatHint(EncryptionAlgorithm algorithm)
        {
            var key = Encoding.UTF8.GetBytes("any-key");
            // AEAD wire format = 8-byte salt + 12-byte IV + ciphertext + 16-byte tag (min 36 bytes).
            // A 4-byte input is too short and used to produce an OverflowException from negative-length arithmetic
            // in InitializeDecryptAead; the guard makes it surface the same wire-format hint as the non-AEAD path.
            var shortInput = new byte[4];

            var ex = Assert.Throws<CryptographicException>(
                () => CryptographyHelper.DecryptData(algorithm, shortInput, key));

            Assert.Contains("too short", ex.Message);
            Assert.Contains("UiPath wire format", ex.Message);
        }

        [Fact]
        public void SymmetricDecrypt_PaddingFailure_ThrowsWithExternalToolHint()
        {
            var key = Encoding.UTF8.GetBytes("any-key");
            // 8-byte salt + 8-byte TripleDES IV + 8 bytes of random "ciphertext".
            // PBKDF2-derived key vs. random ciphertext will fail PKCS7 padding on read.
            var bogus = new byte[8 + 8 + 8];
            new Random(1234).NextBytes(bogus);

            var ex = Assert.Throws<CryptographicException>(
                () => CryptographyHelper.DecryptData(EncryptionAlgorithm.TripleDES, bogus, key));

            Assert.Contains("UiPath wire format", ex.Message);
            Assert.Contains("different tool", ex.Message);
            Assert.NotNull(ex.InnerException);
        }

        // ── IsFipsCompliant — pins the FIPS-compliance map per algorithm.
        [Theory]
        [InlineData(EncryptionAlgorithm.AES,              true)]
        [InlineData(EncryptionAlgorithm.AESGCM,           true)]
        [InlineData(EncryptionAlgorithm.DES,              true)]
        [InlineData(EncryptionAlgorithm.TripleDES,        true)]
        [InlineData(EncryptionAlgorithm.RC2,              false)]
        [InlineData(EncryptionAlgorithm.Rijndael,         false)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305, false)]
        [InlineData(EncryptionAlgorithm.PGP,              false)]
        public void IsFipsCompliant_ReturnsExpected(EncryptionAlgorithm algorithm, bool expected)
        {
            Assert.Equal(expected, CryptographyHelper.IsFipsCompliant(algorithm));
        }

        // ── EncodingHelpers.KeyEncodingOrString — three branches: numeric string, named string, null string.
        [Fact]
        public void KeyEncodingOrString_NumericCodePageString_ReturnsThatEncoding()
        {
            var result = EncodingHelpers.KeyEncodingOrString(Encoding.ASCII, "65001"); // UTF-8 code page
            Assert.Equal(Encoding.UTF8.CodePage, result.CodePage);
        }

        [Fact]
        public void KeyEncodingOrString_NamedEncodingString_ReturnsThatEncoding()
        {
            var result = EncodingHelpers.KeyEncodingOrString(null, "utf-8");
            Assert.Equal(Encoding.UTF8.CodePage, result.CodePage);
        }

        [Fact]
        public void KeyEncodingOrString_NullString_FallsBackToFirstArg()
        {
            var result = EncodingHelpers.KeyEncodingOrString(Encoding.ASCII, null);
            Assert.Same(Encoding.ASCII, result);
        }

        // ── EncodingHelpers.GetCodePageName — catch path returns "" for unknown CodePages values.
        [Fact]
        public void GetCodePageName_UnknownValue_ReturnsEmpty()
        {
            // An enum value outside the declared set causes GetEnumName -> null, which makes
            // typeof(CodePages).GetField(null) throw; the catch returns string.Empty.
            var result = EncodingHelpers.GetCodePageName((CodePages)int.MaxValue);
            Assert.Equal(string.Empty, result);
        }

        // ── FilePathHelpers.GetDefaultFileNameAndLocation — exercises the branches that the
        // activity Execute paths transitively rely on, without spinning up WorkflowInvoker.
        [Fact]
        public void GetDefaultFileNameAndLocation_NoOutputPath_DerivesFromInputAndSuffix()
        {
            var dir = Path.GetTempPath();
            var input = Path.Combine(dir, $"flh_{Guid.NewGuid():N}.txt");
            try
            {
                var (fileName, filePath, returnedInputPath) =
                    FilePathHelpers.GetDefaultFileNameAndLocation(null, input, outputFileName: null, overwrite: false, outputFilePath: null, suffix: "_enc");

                Assert.Equal(Path.GetFileNameWithoutExtension(input) + "_enc.txt", fileName);
                Assert.Equal(Path.Combine(dir, fileName), filePath);
                Assert.Equal(input, returnedInputPath);
            }
            finally
            {
                if (File.Exists(input)) File.Delete(input);
            }
        }

        [Fact]
        public void GetDefaultFileNameAndLocation_BareFilename_KeepsFileNameAsPath()
        {
            // No directory component -> filePath collapses to just the file name.
            var (fileName, filePath, _) =
                FilePathHelpers.GetDefaultFileNameAndLocation(null, "file.txt", outputFileName: null, overwrite: false, outputFilePath: null, suffix: "_x");

            Assert.Equal("file_x.txt", fileName);
            Assert.Equal("file_x.txt", filePath);
        }

        [Fact]
        public void GetDefaultFileNameAndLocation_DerivedPathAlreadyExists_ThrowsWhenNotOverwriting()
        {
            // The derived path collides with an existing file and overwrite is false → ArgumentException.
            var dir = Path.GetTempPath();
            var input = Path.Combine(dir, $"flh_{Guid.NewGuid():N}.bin");
            // The helper derives "<basename>_enc.bin" alongside the input. Pre-create it to provoke the guard.
            var collision = Path.Combine(dir, Path.GetFileNameWithoutExtension(input) + "_enc.bin");
            File.WriteAllText(input, "in");
            File.WriteAllText(collision, "preexisting");
            try
            {
                Assert.Throws<ArgumentException>(() =>
                    FilePathHelpers.GetDefaultFileNameAndLocation(null, input, outputFileName: null, overwrite: false, outputFilePath: null, suffix: "_enc"));
            }
            finally
            {
                if (File.Exists(input)) File.Delete(input);
                if (File.Exists(collision)) File.Delete(collision);
            }
        }

        [Fact]
        public void GetDefaultFileNameAndLocation_OutputFileNameOnly_WritesIntoInputDir()
        {
            var dir = Path.GetTempPath();
            var input = Path.Combine(dir, $"flh_{Guid.NewGuid():N}.txt");
            var (fileName, filePath, _) =
                FilePathHelpers.GetDefaultFileNameAndLocation(null, input, outputFileName: "out.bin", overwrite: false, outputFilePath: null, suffix: "_enc");

            Assert.Equal("out.bin", fileName);
            Assert.Equal(Path.Combine(dir, "out.bin"), filePath);
        }

        [Fact]
        public void GetDefaultFileNameAndLocation_OutputFilePathProvided_ShortCircuits()
        {
            // When outputFilePath is set the helper just propagates the requested fileName; filePath is left empty.
            var (fileName, filePath, _) =
                FilePathHelpers.GetDefaultFileNameAndLocation(null, "in.txt", outputFileName: "explicit.out", overwrite: false, outputFilePath: @"C:\some\path.out", suffix: "_enc");

            Assert.Equal("explicit.out", fileName);
            Assert.Equal(string.Empty, filePath);
        }
    }
}