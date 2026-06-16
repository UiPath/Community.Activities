using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CS0618 // obsolete encryption algorithms reachable via opt-in formats

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Tests for the third-party-compatible symmetric formats: Classic, Owasp2026, Raw, OpenSslEnc.
    /// Companion to <see cref="CryptographyTests"/> (which exercises the legacy public API).
    /// </summary>
    public class SymmetricInteropTests
    {
        private const string Plaintext = "Hello, world. The quick brown fox jumps over the lazy dog. 0123456789. ăîșțâ";
        private const string Password = "shared-test-password-{>@#F09";

        // ------------------------------------------------------------------------------------
        // Classic equivalence: Format = Classic produces output interchangeable with the
        // historical default path (no Format property set in XAML).
        // ------------------------------------------------------------------------------------

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void Classic_RoundTripsViaActivities(EncryptionAlgorithm algorithm)
        {
            string encrypted = RunEncryptText(algorithm, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password);
            string decrypted = RunDecryptText(algorithm, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password, input: encrypted);
            Assert.Equal(Plaintext, decrypted);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.AES)]
        public void Classic_DecryptedBy_LegacyHelperApi(EncryptionAlgorithm algorithm)
        {
            // Blob produced by activity (Format = Classic) must decrypt via the legacy
            // CryptographyHelper.DecryptData entry point — proves Classic = the existing wire format.
            string encryptedBase64 = RunEncryptText(algorithm, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password);
            byte[] decrypted = CryptographyHelper.DecryptData(algorithm, Convert.FromBase64String(encryptedBase64), Encoding.Unicode.GetBytes(Password));
            Assert.Equal(Plaintext, Encoding.Unicode.GetString(decrypted));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.AES)]
        public void Classic_Decrypts_LegacyHelperOutput(EncryptionAlgorithm algorithm)
        {
            // Blob produced by legacy CryptographyHelper.EncryptData must decrypt via the new
            // activity with Format = Classic — proves byte-stability in the other direction.
            byte[] legacyBlob = CryptographyHelper.EncryptData(algorithm, Encoding.Unicode.GetBytes(Plaintext), Encoding.Unicode.GetBytes(Password));
            string activityResult = RunDecryptText(algorithm, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password, input: Convert.ToBase64String(legacyBlob));
            Assert.Equal(Plaintext, activityResult);
        }

        // ------------------------------------------------------------------------------------
        // Owasp2026 round-trip: same wire format as Classic, but iterations is caller-controlled.
        // ------------------------------------------------------------------------------------

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM, 0)]            // default = OWASP 1,300,000
        [InlineData(EncryptionAlgorithm.AESGCM, 50_000)]
        [InlineData(EncryptionAlgorithm.AES, 50_000)]
        public void Owasp2026_RoundTrips(EncryptionAlgorithm algorithm, int iterations)
        {
            string encrypted = RunEncryptText(algorithm, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded, password: Password, iterations: iterations);
            string decrypted = RunDecryptText(algorithm, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded, password: Password, input: encrypted, iterations: iterations);
            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void Owasp2026_With10000Iter_DecryptableByClassic()
        {
            // Owasp2026 is wire-identical to Classic when iterations match — prove it.
            string encryptedOwasp2026 = RunEncryptText(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded, password: Password, iterations: 10_000);
            string decryptedClassic = RunDecryptText(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password, input: encryptedOwasp2026);
            Assert.Equal(Plaintext, decryptedClassic);
        }

        // ------------------------------------------------------------------------------------
        // Raw round-trip: caller supplies raw key (hex) and optional IV.
        // ------------------------------------------------------------------------------------

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void Raw_RoundTrips_HexKey_GeneratedIv(EncryptionAlgorithm algorithm)
        {
            string hexKey = MakeHexKey(CryptographyHelper.GetRawKeySizes(algorithm)[CryptographyHelper.GetRawKeySizes(algorithm).Length - 1]);
            string encrypted = RunEncryptText(algorithm, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey);
            string decrypted = RunDecryptText(algorithm, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey, input: encrypted);
            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void Raw_RoundTrips_HexKey_ExplicitIv()
        {
            const string hexKey = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";  // 32 bytes
            const string hexIv = "AABBCCDDEEFF00112233445566778899";  // 16 bytes for AES
            string encrypted = RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey, iv: hexIv);
            string decrypted = RunDecryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey, input: encrypted);
            Assert.Equal(Plaintext, decrypted);

            // The encrypted stream starts with the IV we supplied.
            byte[] stream = Convert.FromBase64String(encrypted);
            byte[] expectedIv = new byte[16];
            for (int i = 0; i < 16; i++) expectedIv[i] = Convert.ToByte(hexIv.Substring(i * 2, 2), 16);
            for (int i = 0; i < 16; i++) Assert.Equal(expectedIv[i], stream[i]);
        }

        [Fact]
        public void Raw_RoundTrips_Base64Key()
        {
            string base64Key = Convert.ToBase64String(new byte[32] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 });
            string encrypted = RunEncryptText(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, KeyBytesFormat.Base64, key: base64Key);
            string decrypted = RunDecryptText(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, KeyBytesFormat.Base64, key: base64Key, input: encrypted);
            Assert.Equal(Plaintext, decrypted);
        }

        // ------------------------------------------------------------------------------------
        // OpenSslEnc round-trip: Salted__ + PBKDF2-SHA256.
        // ------------------------------------------------------------------------------------

        [Theory]
        [InlineData(EncryptionAlgorithm.AES, 0)]               // default 600,000
        [InlineData(EncryptionAlgorithm.AES, 50_000)]
        [InlineData(EncryptionAlgorithm.TripleDES, 50_000)]
        [InlineData(EncryptionAlgorithm.AESGCM, 50_000)]       // UiPath AEAD extension
        public void OpenSslEnc_RoundTrips(EncryptionAlgorithm algorithm, int iterations)
        {
            string encrypted = RunEncryptText(algorithm, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded, password: Password, iterations: iterations);
            string decrypted = RunDecryptText(algorithm, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded, password: Password, input: encrypted, iterations: iterations);
            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void OpenSslEnc_StreamStartsWithSaltedMagic()
        {
            string encrypted = RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded, password: Password, iterations: 10_000);
            byte[] bytes = Convert.FromBase64String(encrypted);
            Assert.Equal((byte)'S', bytes[0]);
            Assert.Equal((byte)'a', bytes[1]);
            Assert.Equal((byte)'l', bytes[2]);
            Assert.Equal((byte)'t', bytes[3]);
            Assert.Equal((byte)'e', bytes[4]);
            Assert.Equal((byte)'d', bytes[5]);
            Assert.Equal((byte)'_', bytes[6]);
            Assert.Equal((byte)'_', bytes[7]);
        }

        [Fact]
        public void OpenSslEnc_DecryptingNonSaltedInput_Throws()
        {
            byte[] junk = Encoding.UTF8.GetBytes("Not a salted__ blob, just text......");
            string input = Convert.ToBase64String(junk);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                RunDecryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded, password: Password, input: input, iterations: 10_000));
            Assert.IsType<CryptographicException>(ex.InnerException);
        }

        // ------------------------------------------------------------------------------------
        // Cross-tool interop: produce/consume blobs using .NET's standard primitives.
        // No external openssl process required — .NET's Aes/AesGcm produce standard layouts.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void Raw_DecryptsBlob_ProducedByDotNetAes()
        {
            // .NET's Aes.CreateEncryptor outputs the same layout as our Raw format.
            byte[] keyBytes = new byte[32];
            byte[] ivBytes = new byte[16];
            RandomNumberGenerator.Fill(keyBytes);
            RandomNumberGenerator.Fill(ivBytes);
            byte[] plain = Encoding.UTF8.GetBytes(Plaintext);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                using var ms = new System.IO.MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    cs.Write(plain, 0, plain.Length);
                cipher = ms.ToArray();
            }

            byte[] streamBytes = new byte[ivBytes.Length + cipher.Length];
            Buffer.BlockCopy(ivBytes, 0, streamBytes, 0, ivBytes.Length);
            Buffer.BlockCopy(cipher, 0, streamBytes, ivBytes.Length, cipher.Length);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex,
                key: Convert.ToHexString(keyBytes),
                input: Convert.ToBase64String(streamBytes),
                inputEncoding: Encoding.UTF8);
            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void Raw_OutputCanBeDecrypted_ByDotNetAesGcm()
        {
            byte[] keyBytes = new byte[32];
            RandomNumberGenerator.Fill(keyBytes);

            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, KeyBytesFormat.Hex,
                key: Convert.ToHexString(keyBytes),
                inputEncoding: Encoding.UTF8);

            byte[] stream = Convert.FromBase64String(encryptedBase64);
            byte[] iv = new byte[12];
            byte[] tag = new byte[16];
            byte[] ct = new byte[stream.Length - iv.Length - tag.Length];
            Buffer.BlockCopy(stream, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(stream, iv.Length, ct, 0, ct.Length);
            Buffer.BlockCopy(stream, iv.Length + ct.Length, tag, 0, tag.Length);

            byte[] plain = new byte[ct.Length];
            using (var aes = new AesGcm(keyBytes))
                aes.Decrypt(iv, ct, tag, plain);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(plain));
        }

        // ------------------------------------------------------------------------------------
        // Validation matrix.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void Validation_Raw_Encoded_Rejected()
        {
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Encoded, password: "anything"));
        }

        [Fact]
        public void Validation_Classic_Hex_Rejected()
        {
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Hex, key: "00112233"));
        }

        [Fact]
        public void Validation_OpenSslEnc_Base64_Rejected()
        {
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Base64, key: "AAAA"));
        }

        [Fact]
        public void Validation_Iv_WithClassic_Rejected()
        {
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password, iv: "deadbeef"));
        }

        [Fact]
        public void Validation_KdfIterations_OnClassic_Rejected()
        {
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, password: Password, iterations: 100_000));
        }

        [Fact]
        public void Validation_KdfIterations_OnRaw_Rejected()
        {
            string hexKey = MakeHexKey(32);
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey, iterations: 100_000));
        }

        [Fact]
        public void Validation_KdfIterations_BelowFloor_Rejected()
        {
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded, password: Password, iterations: 500));
        }

        [Fact]
        public void Validation_Raw_WrongKeyLength_Rejected()
        {
            // AES wants 16/24/32 bytes; supply 10 bytes (20 hex chars).
            Assert.Throws<ArgumentException>(() =>
                RunEncryptText(EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: "00112233445566778899"));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        [InlineData(EncryptionAlgorithm.DES)]
        public void Obsolete_Algorithm_AllowedInRaw_BothDirections(EncryptionAlgorithm algorithm)
        {
            // STUD-64429 — customer interop with legacy systems must be possible even for
            // obsolete algorithms. The [Obsolete] compiler warning is the user-facing signal;
            // the activity does not block.
            int[] sizes = CryptographyHelper.GetRawKeySizes(algorithm);
            string hexKey = MakeHexKey(sizes[sizes.Length - 1]);

            string encrypted = RunEncryptText(algorithm, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey);
            string decrypted = RunDecryptText(algorithm, SymmetricWireFormat.Raw, KeyBytesFormat.Hex, key: hexKey, input: encrypted);
            Assert.Equal(Plaintext, decrypted);
        }

        // ------------------------------------------------------------------------------------
        // OWASP defaults sanity check (guards against accidental constant changes).
        // ------------------------------------------------------------------------------------

        [Fact]
        public void RecommendedIterations_AreCurrentOwaspValues()
        {
            Assert.Equal(1_300_000, CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.Owasp2026));
            Assert.Equal(600_000, CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.OpenSslEnc));
        }

        [Fact]
        public void RecommendedIterations_ForClassicOrRaw_Throws()
        {
            Assert.Throws<ArgumentException>(() => CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.Classic));
            Assert.Throws<ArgumentException>(() => CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.Raw));
        }

        // ------------------------------------------------------------------------------------
        // Test harness helpers.
        // ------------------------------------------------------------------------------------

        // WF Literal<T> rejects closure-capturing lambdas, so encoding selection uses static lambdas.
        private static InArgument<Encoding> MakeEncodingArg(Encoding e)
        {
            if (e == Encoding.Unicode || e == null) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.Unicode));
            if (e == Encoding.UTF8) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.UTF8));
            throw new ArgumentException($"Test helper only supports Unicode/UTF8; got {e.WebName}");
        }

        private static string RunEncryptText(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            KeyBytesFormat keyFormat,
            string password = null,
            string key = null,
            string iv = null,
            int iterations = 0,
            Encoding inputEncoding = null)
        {
            var activity = new EncryptText
            {
                Algorithm = algorithm,
                Format = format,
                KeyFormat = keyFormat,
                Encoding = MakeEncodingArg(inputEncoding),
                KeyEncodingString = null,
            };

            var args = new Dictionary<string, object>
            {
                [nameof(EncryptText.Input)] = Plaintext,
                [nameof(EncryptText.Key)] = key ?? password,
            };
            if (!string.IsNullOrEmpty(iv)) args[nameof(EncryptText.Iv)] = iv;
            if (iterations != 0) args[nameof(EncryptText.KdfIterations)] = iterations;

            try
            {
                var invoker = new WorkflowInvoker(activity);
                return (string)invoker.Invoke(args)[nameof(activity.Result)];
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw tie.InnerException;
            }
        }

        private static string RunDecryptText(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            KeyBytesFormat keyFormat,
            string input,
            string password = null,
            string key = null,
            int iterations = 0,
            Encoding inputEncoding = null)
        {
            var activity = new DecryptText
            {
                Algorithm = algorithm,
                Format = format,
                KeyFormat = keyFormat,
                Encoding = MakeEncodingArg(inputEncoding),
                KeyEncodingString = null,
            };

            var args = new Dictionary<string, object>
            {
                [nameof(DecryptText.Input)] = input,
                [nameof(DecryptText.Key)] = key ?? password,
            };
            if (iterations != 0) args[nameof(DecryptText.KdfIterations)] = iterations;

            try
            {
                var invoker = new WorkflowInvoker(activity);
                return (string)invoker.Invoke(args)[nameof(activity.Result)];
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw tie.InnerException;
            }
        }

        private static string MakeHexKey(int sizeBytes)
        {
            var sb = new StringBuilder(sizeBytes * 2);
            for (int i = 0; i < sizeBytes; i++) sb.Append(((byte)(i + 1)).ToString("X2"));
            return sb.ToString();
        }
    }
}
