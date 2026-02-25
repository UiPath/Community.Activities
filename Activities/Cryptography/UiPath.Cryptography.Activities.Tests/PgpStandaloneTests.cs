using System;
using System.Activities;
using System.IO;
using System.Security;
using System.Text;
using PgpCore;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    public class PgpStandaloneTests : IDisposable
    {
        private readonly string _publicKeyPath;
        private readonly string _privateKeyPath;
        private const string Passphrase = "testpassphrase";

        public PgpStandaloneTests()
        {
            _publicKeyPath = Path.Combine(Path.GetTempPath(), $"pgp_standalone_public_{Guid.NewGuid()}.asc");
            _privateKeyPath = Path.Combine(Path.GetTempPath(), $"pgp_standalone_private_{Guid.NewGuid()}.asc");

            using (var pgp = new PGP())
            {
                pgp.GenerateKey(
                    new FileInfo(_publicKeyPath),
                    new FileInfo(_privateKeyPath),
                    "test@test.com",
                    Passphrase);
            }
        }

        public void Dispose()
        {
            if (File.Exists(_publicKeyPath)) File.Delete(_publicKeyPath);
            if (File.Exists(_privateKeyPath)) File.Delete(_privateKeyPath);
        }

        private SecureString GetPassphraseSecureString()
        {
            return TestingHelper.StringToSecureString(Passphrase);
        }

        #region CryptographyHelper Tests

        [Fact]
        public void PgpGenerateKeyPair_CreatesFiles()
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_gen_pub_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_gen_priv_{Guid.NewGuid()}.asc");

            try
            {
                CryptographyHelper.PgpGenerateKeyPair(pubPath, privPath, "gentest@test.com", "genpassword");

                Assert.True(File.Exists(pubPath));
                Assert.True(File.Exists(privPath));

                // Verify the generated keys are usable
                using (var pubStream = File.OpenRead(pubPath))
                {
                    Assert.True(CryptographyHelper.PgpVerifyPublicKey(pubStream));
                }
            }
            finally
            {
                if (File.Exists(pubPath)) File.Delete(pubPath);
                if (File.Exists(privPath)) File.Delete(privPath);
            }
        }

        [Fact]
        public void PgpSign_And_Verify_RoundTrip()
        {
            var plainBytes = Encoding.UTF8.GetBytes("Hello PGP signing!");

            using (var privateKeyStream = File.OpenRead(_privateKeyPath))
            {
                var signed = CryptographyHelper.PgpSign(plainBytes, privateKeyStream, Passphrase);

                Assert.NotNull(signed);
                Assert.NotEmpty(signed);

                using (var publicKeyStream = File.OpenRead(_publicKeyPath))
                {
                    var isValid = CryptographyHelper.PgpVerify(signed, publicKeyStream);
                    Assert.True(isValid);
                }
            }
        }

        [Fact]
        public void PgpClearSign_And_VerifyClear_RoundTrip()
        {
            var plainBytes = Encoding.UTF8.GetBytes("Hello PGP clear signing!");

            using (var privateKeyStream = File.OpenRead(_privateKeyPath))
            {
                var signed = CryptographyHelper.PgpClearSign(plainBytes, privateKeyStream, Passphrase);

                Assert.NotNull(signed);
                Assert.NotEmpty(signed);

                using (var publicKeyStream = File.OpenRead(_publicKeyPath))
                {
                    var isValid = CryptographyHelper.PgpVerifyClear(signed, publicKeyStream);
                    Assert.True(isValid);
                }
            }
        }

        [Fact]
        public void PgpVerify_InvalidSignature_ReturnsFalse()
        {
            var unsignedBytes = Encoding.UTF8.GetBytes("This is not signed data");

            using (var publicKeyStream = File.OpenRead(_publicKeyPath))
            {
                var isValid = CryptographyHelper.PgpVerify(unsignedBytes, publicKeyStream);
                Assert.False(isValid);
            }
        }

        [Fact]
        public void PgpVerifyPublicKey_ValidKey_ReturnsTrue()
        {
            using (var publicKeyStream = File.OpenRead(_publicKeyPath))
            {
                var isValid = CryptographyHelper.PgpVerifyPublicKey(publicKeyStream);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void PgpVerifyPublicKey_InvalidKey_ReturnsFalse()
        {
            var invalidKeyPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(invalidKeyPath, "This is not a valid PGP key");

                using (var stream = File.OpenRead(invalidKeyPath))
                {
                    var isValid = CryptographyHelper.PgpVerifyPublicKey(stream);
                    Assert.False(isValid);
                }
            }
            finally
            {
                File.Delete(invalidKeyPath);
            }
        }

        #endregion

        #region Activity Integration Tests

        [Fact]
        public void PgpGenerateKeyPair_Activity_Works()
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_act_pub_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_act_priv_{Guid.NewGuid()}.asc");

            try
            {
                var activity = new PgpGenerateKeyPair
                {
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                    PrivateKeyFilePath = new InArgument<string>(privPath),
                    Username = new InArgument<string>("acttest@test.com"),
                    Password = new InArgument<SecureString>((_) => GetPassphraseSecureString())
                };

                WorkflowInvoker.Invoke(activity);

                Assert.True(File.Exists(pubPath));
                Assert.True(File.Exists(privPath));
            }
            finally
            {
                if (File.Exists(pubPath)) File.Delete(pubPath);
                if (File.Exists(privPath)) File.Delete(privPath);
            }
        }

        [Fact]
        public void PgpSignFile_Activity_Works()
        {
            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputFile, "Data to sign");

                var activity = new PgpSignFile
                {
                    InputFilePath = new InArgument<string>(inputFile),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<SecureString>((_) => GetPassphraseSecureString()),
                    OutputFilePath = new InArgument<string>(outputFile),
                    Overwrite = true
                };

                WorkflowInvoker.Invoke(activity);

                Assert.True(File.Exists(outputFile));
                Assert.True(new FileInfo(outputFile).Length > 0);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        [Fact]
        public void PgpClearSignFile_Activity_Works()
        {
            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputFile, "Data to clear-sign");

                var activity = new PgpClearSignFile
                {
                    InputFilePath = new InArgument<string>(inputFile),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<SecureString>((_) => GetPassphraseSecureString()),
                    OutputFilePath = new InArgument<string>(outputFile),
                    Overwrite = true
                };

                WorkflowInvoker.Invoke(activity);

                Assert.True(File.Exists(outputFile));
                Assert.True(new FileInfo(outputFile).Length > 0);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        [Fact]
        public void PgpVerifySignature_Activity_Works()
        {
            var inputFile = Path.GetTempFileName();
            var signedFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputFile, "Data to verify");

                // First sign the file
                var inputBytes = File.ReadAllBytes(inputFile);
                using (var privateKeyStream = File.OpenRead(_privateKeyPath))
                {
                    var signed = CryptographyHelper.PgpSign(inputBytes, privateKeyStream, Passphrase);
                    File.WriteAllBytes(signedFile, signed);
                }

                // Now verify using the activity
                var activity = new PgpVerify
                {
                    Mode = PgpVerifyMode.Signature,
                    InputFilePath = new InArgument<string>(signedFile),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath)
                };

                var result = WorkflowInvoker.Invoke(activity);
                Assert.True(result);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(signedFile);
            }
        }

        [Fact]
        public void PgpVerifyClearSignature_Activity_Works()
        {
            var inputFile = Path.GetTempFileName();
            var signedFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputFile, "Data to verify clear");

                // First clear-sign the file
                var inputBytes = File.ReadAllBytes(inputFile);
                using (var privateKeyStream = File.OpenRead(_privateKeyPath))
                {
                    var signed = CryptographyHelper.PgpClearSign(inputBytes, privateKeyStream, Passphrase);
                    File.WriteAllBytes(signedFile, signed);
                }

                // Now verify using the activity
                var activity = new PgpVerify
                {
                    Mode = PgpVerifyMode.ClearSignature,
                    InputFilePath = new InArgument<string>(signedFile),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath)
                };

                var result = WorkflowInvoker.Invoke(activity);
                Assert.True(result);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(signedFile);
            }
        }

        [Fact]
        public void PgpVerifyPublicKey_Activity_Works()
        {
            var activity = new PgpVerify
            {
                Mode = PgpVerifyMode.PublicKey,
                PublicKeyFilePath = new InArgument<string>(_publicKeyPath)
            };

            var result = WorkflowInvoker.Invoke(activity);
            Assert.True(result);
        }

        [Fact]
        public void PgpVerifyPublicKey_Activity_InvalidKey_ReturnsFalse()
        {
            var invalidKeyPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(invalidKeyPath, "Not a PGP key");

                var activity = new PgpVerify
                {
                    Mode = PgpVerifyMode.PublicKey,
                    PublicKeyFilePath = new InArgument<string>(invalidKeyPath)
                };

                var result = WorkflowInvoker.Invoke(activity);
                Assert.False(result);
            }
            finally
            {
                File.Delete(invalidKeyPath);
            }
        }

        [Fact]
        public void PgpSignFile_And_VerifySignature_Activity_RoundTrip()
        {
            var inputFile = Path.GetTempFileName();
            var signedFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputFile, "Round trip test data");

                // Sign
                var signActivity = new PgpSignFile
                {
                    InputFilePath = new InArgument<string>(inputFile),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<SecureString>((_) => GetPassphraseSecureString()),
                    OutputFilePath = new InArgument<string>(signedFile),
                    Overwrite = true
                };

                WorkflowInvoker.Invoke(signActivity);

                // Verify
                var verifyActivity = new PgpVerify
                {
                    Mode = PgpVerifyMode.Signature,
                    InputFilePath = new InArgument<string>(signedFile),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath)
                };

                var result = WorkflowInvoker.Invoke(verifyActivity);
                Assert.True(result);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(signedFile);
            }
        }

        [Fact]
        public void PgpClearSignFile_And_VerifyClearSignature_Activity_RoundTrip()
        {
            var inputFile = Path.GetTempFileName();
            var signedFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputFile, "Clear round trip test data");

                // Clear-sign
                var signActivity = new PgpClearSignFile
                {
                    InputFilePath = new InArgument<string>(inputFile),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<SecureString>((_) => GetPassphraseSecureString()),
                    OutputFilePath = new InArgument<string>(signedFile),
                    Overwrite = true
                };

                WorkflowInvoker.Invoke(signActivity);

                // Verify
                var verifyActivity = new PgpVerify
                {
                    Mode = PgpVerifyMode.ClearSignature,
                    InputFilePath = new InArgument<string>(signedFile),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath)
                };

                var result = WorkflowInvoker.Invoke(verifyActivity);
                Assert.True(result);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(signedFile);
            }
        }

        #endregion
    }
}
