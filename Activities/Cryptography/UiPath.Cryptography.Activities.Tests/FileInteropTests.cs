using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // tests intentionally use the obsolete KeyInputMode for legacy code paths.

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Round-trip tests for <see cref="EncryptFile"/> / <see cref="DecryptFile"/> using
    /// the new wire-format arguments (<c>Format</c>, <c>KeyFormat</c>, <c>Iv</c>,
    /// <c>KdfIterations</c>). The activity-level wiring exists on the file activities just
    /// as it does on the text activities, but until this file the file-form interop path
    /// was only covered indirectly through the service-level tests.
    /// </summary>
    public class FileInteropTests
    {
        private const string Plaintext = "File-form interop round-trip — non-ASCII payload: ăîșțâ €";
        private const string Password = "interop-file-password-{!@#}";

        [Fact]
        public void File_Classic_RoundTrips()
        {
            RoundTripPasswordBased(EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded);
        }

        [Theory]
        [InlineData(0)]            // service default = 1_300_000
        [InlineData(50_000)]
        public void File_Owasp2026_RoundTrips(int iterations)
        {
            RoundTripPasswordBased(EncryptionAlgorithm.AES, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded, iterations);
        }

        [Theory]
        [InlineData(0)]            // service default = 600_000
        [InlineData(50_000)]
        public void File_OpenSslEnc_RoundTrips(int iterations)
        {
            RoundTripPasswordBased(EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded, iterations);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES, KeyBytesFormat.Hex)]
        [InlineData(EncryptionAlgorithm.AESGCM, KeyBytesFormat.Hex)]
        [InlineData(EncryptionAlgorithm.AES, KeyBytesFormat.Base64)]
        public void File_Raw_RoundTrips_GeneratedIv(EncryptionAlgorithm algorithm, KeyBytesFormat keyFormat)
        {
            string key = keyFormat == KeyBytesFormat.Hex
                ? MakeHexKey(32)
                : Convert.ToBase64String(MakeKeyBytes(32));

            RoundTripRaw(algorithm, keyFormat, key, iv: null);
        }

        [Fact]
        public void File_Raw_RoundTrips_ExplicitHexIv()
        {
            // AES-CBC requires 16-byte IV; this pins that the explicit-IV path through
            // EncryptFile reaches CryptographyHelper.EncryptDataRaw with the supplied IV.
            const string hexKey = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";
            const string hexIv = "AABBCCDDEEFF00112233445566778899";

            string inputPath = MakeTempFile(Plaintext);
            string encryptedPath = NewTempPath();
            string decryptedPath = NewTempPath();
            try
            {
                var encrypt = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    OutputFilePath = new InArgument<string>(encryptedPath),
                    Key = new InArgument<string>(hexKey),
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = SymmetricWireFormat.Raw,
                    KeyFormat = KeyBytesFormat.Hex,
                    Iv = new InArgument<string>(hexIv),
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = true,
                };

                var decrypt = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(encryptedPath),
                    OutputFilePath = new InArgument<string>(decryptedPath),
                    Key = new InArgument<string>(hexKey),
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = SymmetricWireFormat.Raw,
                    KeyFormat = KeyBytesFormat.Hex,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = true,
                };

                WorkflowInvoker.Invoke(encrypt);
                WorkflowInvoker.Invoke(decrypt);

                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(Plaintext);

                // The explicit IV must appear as the first 16 bytes of the encrypted blob —
                // pins that the Iv argument flows through the activity, not silently regenerated.
                byte[] blob = File.ReadAllBytes(encryptedPath);
                byte[] ivBytes = HexToBytes(hexIv);
                blob.AsSpan(0, 16).ToArray().ShouldBe(ivBytes);
            }
            finally
            {
                Cleanup(inputPath, encryptedPath, decryptedPath);
            }
        }

        // EncryptFile Overwrite=false + existing output must throw across all interop
        // formats — the file-existence check sits before the format dispatch. Theory pins
        // that no format silently bypasses it.
        [Theory]
        [InlineData(SymmetricWireFormat.Classic)]
        [InlineData(SymmetricWireFormat.Owasp2026)]
        [InlineData(SymmetricWireFormat.OpenSslEnc)]
        public void File_OverwriteFalse_ExistingOutput_Throws_AcrossFormats(SymmetricWireFormat format)
        {
            string inputPath = MakeTempFile(Plaintext);
            string outputPath = MakeTempFile("existing output");
            try
            {
                var encrypt = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    OutputFilePath = new InArgument<string>(outputPath),
                    Key = new InArgument<string>(Password),
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = format,
                    KeyFormat = KeyBytesFormat.Encoded,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = false,
                };

                Should.Throw<ArgumentException>(() => WorkflowInvoker.Invoke(encrypt));
            }
            finally
            {
                Cleanup(inputPath, outputPath);
            }
        }

        // OutputFileName with no OutputFilePath should still resolve correctly under the
        // new interop arguments — pins the FilePathHelpers default-naming behaviour against
        // the Format/KeyFormat plumbing.
        [Fact]
        public void File_OutputFileName_ResolvesInInputDirectory_WithInteropFormat()
        {
            string inputPath = MakeTempFile(Plaintext);
            string inputDir = Path.GetDirectoryName(inputPath);
            string outputName = "interop-" + Guid.NewGuid().ToString("N") + ".enc";
            string expectedOutput = Path.Combine(inputDir, outputName);
            try
            {
                var encrypt = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    OutputFileName = new InArgument<string>(outputName),
                    Key = new InArgument<string>(Password),
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = SymmetricWireFormat.Owasp2026,
                    KeyFormat = KeyBytesFormat.Encoded,
                    KdfIterations = new InArgument<int>(50_000),
                    KeyInputModeSwitch = KeyInputMode.Key,
                };

                WorkflowInvoker.Invoke(encrypt);

                File.Exists(expectedOutput).ShouldBeTrue($"expected encrypted output at {expectedOutput}");
                new FileInfo(expectedOutput).Length.ShouldBeGreaterThan(0);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(expectedOutput)) File.Delete(expectedOutput);
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────

        private static void RoundTripPasswordBased(EncryptionAlgorithm algorithm, SymmetricWireFormat format, KeyBytesFormat keyFormat, int iterations = 0)
        {
            string inputPath = MakeTempFile(Plaintext);
            string encryptedPath = NewTempPath();
            string decryptedPath = NewTempPath();
            try
            {
                var encrypt = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    OutputFilePath = new InArgument<string>(encryptedPath),
                    Key = new InArgument<string>(Password),
                    Algorithm = algorithm,
                    Format = format,
                    KeyFormat = keyFormat,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = true,
                };
                if (iterations != 0) encrypt.KdfIterations = new InArgument<int>(iterations);

                var decrypt = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(encryptedPath),
                    OutputFilePath = new InArgument<string>(decryptedPath),
                    Key = new InArgument<string>(Password),
                    Algorithm = algorithm,
                    Format = format,
                    KeyFormat = keyFormat,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = true,
                };
                if (iterations != 0) decrypt.KdfIterations = new InArgument<int>(iterations);

                WorkflowInvoker.Invoke(encrypt);
                WorkflowInvoker.Invoke(decrypt);

                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(Plaintext);
            }
            finally
            {
                Cleanup(inputPath, encryptedPath, decryptedPath);
            }
        }

        private static void RoundTripRaw(EncryptionAlgorithm algorithm, KeyBytesFormat keyFormat, string key, string iv)
        {
            string inputPath = MakeTempFile(Plaintext);
            string encryptedPath = NewTempPath();
            string decryptedPath = NewTempPath();
            try
            {
                var encrypt = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    OutputFilePath = new InArgument<string>(encryptedPath),
                    Key = new InArgument<string>(key),
                    Algorithm = algorithm,
                    Format = SymmetricWireFormat.Raw,
                    KeyFormat = keyFormat,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = true,
                };
                if (!string.IsNullOrEmpty(iv)) encrypt.Iv = new InArgument<string>(iv);

                var decrypt = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(encryptedPath),
                    OutputFilePath = new InArgument<string>(decryptedPath),
                    Key = new InArgument<string>(key),
                    Algorithm = algorithm,
                    Format = SymmetricWireFormat.Raw,
                    KeyFormat = keyFormat,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = true,
                };

                WorkflowInvoker.Invoke(encrypt);
                WorkflowInvoker.Invoke(decrypt);

                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(Plaintext);
            }
            finally
            {
                Cleanup(inputPath, encryptedPath, decryptedPath);
            }
        }

        private static string MakeTempFile(string content)
        {
            string path = NewTempPath();
            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        private static string NewTempPath() =>
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        private static void Cleanup(params string[] paths)
        {
            foreach (string p in paths)
                if (File.Exists(p)) File.Delete(p);
        }

        private static string MakeHexKey(int sizeBytes)
        {
            var sb = new StringBuilder(sizeBytes * 2);
            for (int i = 0; i < sizeBytes; i++) sb.Append(((byte)(i + 1)).ToString("X2"));
            return sb.ToString();
        }

        private static byte[] MakeKeyBytes(int sizeBytes)
        {
            byte[] bytes = new byte[sizeBytes];
            for (int i = 0; i < sizeBytes; i++) bytes[i] = (byte)(i + 1);
            return bytes;
        }

        private static byte[] HexToBytes(string hex)
        {
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }
    }
}

#pragma warning restore CS0618
