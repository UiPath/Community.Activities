using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Security;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // tests intentionally set the obsolete PassphraseInputModeSwitch property to exercise the SecureString branch

namespace UiPath.Cryptography.Activities.Tests
{
    public class PgpTests : PgpTestBase
    {
        [Fact]
        public void PgpEncryptDecryptText_HappyPath_Works()
        {
            // Arrange
            var plainText = "Hello PGP World!";

            using (var publicKeyStream = File.OpenRead(_publicKeyPath))
            {
                var encrypted = CryptographyHelper.PgpEncryptText(plainText, publicKeyStream);

                using (var privateKeyStream = File.OpenRead(_privateKeyPath))
                {
                    // Act
                    var decrypted = CryptographyHelper.PgpDecryptText(encrypted, privateKeyStream, Passphrase);

                    // Assert
                    Assert.Equal(plainText, decrypted);
                }
            }
        }

        [Fact]
        public void PgpEncryptDecryptText_WithSigning_Works()
        {
            // Arrange
            var plainText = "Hello PGP Signed World!";

            using (var publicKeyStream = File.OpenRead(_publicKeyPath))
            using (var privateKeyStream = File.OpenRead(_privateKeyPath))
            {
                var encrypted = CryptographyHelper.PgpEncryptText(plainText, publicKeyStream, privateKeyStream, Passphrase, sign: true);

                using (var publicKeyStream2 = File.OpenRead(_publicKeyPath))
                using (var privateKeyStream2 = File.OpenRead(_privateKeyPath))
                {
                    // Act
                    var decrypted = CryptographyHelper.PgpDecryptText(encrypted, privateKeyStream2, Passphrase, publicKeyStream2, verifySignature: true);

                    // Assert
                    Assert.Equal(plainText, decrypted);
                }
            }
        }

        [Fact]
        public void PgpEncryptDecryptBytes_HappyPath_Works()
        {
            // Arrange
            var plainBytes = System.Text.Encoding.UTF8.GetBytes("Hello PGP binary world!");

            using (var publicKeyStream = File.OpenRead(_publicKeyPath))
            {
                var encrypted = CryptographyHelper.PgpEncrypt(plainBytes, publicKeyStream);

                using (var privateKeyStream = File.OpenRead(_privateKeyPath))
                {
                    // Act
                    var decrypted = CryptographyHelper.PgpDecrypt(encrypted, privateKeyStream, Passphrase);

                    // Assert
                    Assert.Equal(plainBytes, decrypted);
                }
            }
        }

        [Fact]
        public void PgpEncryptDecryptBytes_WithSigning_Works()
        {
            // Arrange
            var plainBytes = System.Text.Encoding.UTF8.GetBytes("Hello PGP signed binary!");

            using (var publicKeyStream = File.OpenRead(_publicKeyPath))
            using (var privateKeyStream = File.OpenRead(_privateKeyPath))
            {
                var encrypted = CryptographyHelper.PgpEncrypt(plainBytes, publicKeyStream, privateKeyStream, Passphrase, sign: true);

                using (var publicKeyStream2 = File.OpenRead(_publicKeyPath))
                using (var privateKeyStream2 = File.OpenRead(_privateKeyPath))
                {
                    // Act
                    var decrypted = CryptographyHelper.PgpDecrypt(encrypted, privateKeyStream2, Passphrase, publicKeyStream2, verifySignature: true);

                    // Assert
                    Assert.Equal(plainBytes, decrypted);
                }
            }
        }

        [Fact]
        public void PgpEncryptDecryptFile_Activity_Works()
        {
            var tempInputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempEncryptedFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempDecryptedFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                // Arrange
                var message = "Hello PGP file encryption!";
                File.WriteAllText(tempInputFile, message);

                var encryptFile = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(tempInputFile),
                    Algorithm = EncryptionAlgorithm.PGP,
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                    OutputFilePath = new InArgument<string>(tempEncryptedFile),
                    Overwrite = true
                };

                var decryptFile = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(tempEncryptedFile),
                    Algorithm = EncryptionAlgorithm.PGP,
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>(tempDecryptedFile),
                    Overwrite = true
                };

                var sequence = new Sequence();
                sequence.Activities.Add(encryptFile);
                sequence.Activities.Add(decryptFile);

                // Act
                WorkflowInvoker.Invoke(sequence);

                // Assert
                var outputMessage = File.ReadAllText(tempDecryptedFile);
                Assert.Equal(message, outputMessage);
            }
            finally
            {
                File.Delete(tempInputFile);
                File.Delete(tempEncryptedFile);
                File.Delete(tempDecryptedFile);
            }
        }

        [Fact]
        public void PgpEncryptFile_WithoutPublicKey_Throws()
        {
            var tempInputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(tempInputFile, "test");

                var encryptFile = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(tempInputFile),
                    Algorithm = EncryptionAlgorithm.PGP,
                    Overwrite = true
                };

                Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(encryptFile));
            }
            finally
            {
                File.Delete(tempInputFile);
            }
        }

        [Fact]
        public void PgpDecryptFile_WithoutPrivateKey_Throws()
        {
            var tempInputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(tempInputFile, "test");

                var decryptFile = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(tempInputFile),
                    Algorithm = EncryptionAlgorithm.PGP,
                    Overwrite = true
                };

                Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(decryptFile));
            }
            finally
            {
                File.Delete(tempInputFile);
            }
        }

        [Fact]
        public void PgpEncryptDecryptText_Activity_Works()
        {
            // Arrange
            var plainText = "Hello PGP text activity!";

            var encryptText = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                PublicKeyFilePath = new InArgument<string>(_publicKeyPath)
            };

            var encryptArgs = new System.Collections.Generic.Dictionary<string, object>();
            encryptArgs.Add(nameof(EncryptText.Input), plainText);

            var encryptInvoker = new WorkflowInvoker(encryptText);
            var encryptedText = (string)encryptInvoker.Invoke(encryptArgs)[nameof(encryptText.Result)];

            Assert.NotNull(encryptedText);
            Assert.NotEqual(plainText, encryptedText);

            var decryptText = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                Passphrase = new InArgument<string>(Passphrase)
            };

            var decryptArgs = new System.Collections.Generic.Dictionary<string, object>();
            decryptArgs.Add(nameof(DecryptText.Input), encryptedText);

            var decryptInvoker = new WorkflowInvoker(decryptText);
            var decryptedText = (string)decryptInvoker.Invoke(decryptArgs)[nameof(decryptText.Result)];

            // Assert
            Assert.Equal(plainText, decryptedText);
        }

        [Fact]
        public void PgpEncryptDecryptFile_WithSignAndVerify_RoundTrips()
        {
            // Mirrors the customer workflow that uses EncryptFile / DecryptFile with PGP +
            // SignData=true / VerifySignature=true, passing both public and private keys.
            var tempInputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempEncryptedFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempDecryptedFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                var message = "Hello PGP file encrypt+sign / decrypt+verify round-trip!";
                File.WriteAllText(tempInputFile, message);

                var encryptFile = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(tempInputFile),
                    Algorithm = EncryptionAlgorithm.PGP,
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    SignData = true,
                    OutputFilePath = new InArgument<string>(tempEncryptedFile),
                    Overwrite = true,
                };

                var decryptFile = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(tempEncryptedFile),
                    Algorithm = EncryptionAlgorithm.PGP,
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    VerifySignature = true,
                    OutputFilePath = new InArgument<string>(tempDecryptedFile),
                    Overwrite = true,
                };

                var sequence = new Sequence();
                sequence.Activities.Add(encryptFile);
                sequence.Activities.Add(decryptFile);

                WorkflowInvoker.Invoke(sequence);

                Assert.Equal(message, File.ReadAllText(tempDecryptedFile));
            }
            finally
            {
                if (File.Exists(tempInputFile)) File.Delete(tempInputFile);
                if (File.Exists(tempEncryptedFile)) File.Delete(tempEncryptedFile);
                if (File.Exists(tempDecryptedFile)) File.Delete(tempDecryptedFile);
            }
        }

        [Fact]
        public void PgpEncryptDecryptText_WithSignAndVerify_RoundTrips()
        {
            // Mirrors the customer workflow that calls EncryptText/DecryptText with PGP +
            // SignData=true / VerifySignature=true, using both public and private keys plus a passphrase.
            var plainText = "Hello PGP Encrypt+Sign / Decrypt+Verify round-trip!";

            var encryptText = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                Passphrase = new InArgument<string>(Passphrase),
                SignData = true,
            };
            var encryptArgs = new System.Collections.Generic.Dictionary<string, object>
            {
                { nameof(EncryptText.Input), plainText },
            };
            var encryptedText = (string)new WorkflowInvoker(encryptText).Invoke(encryptArgs)[nameof(encryptText.Result)];

            Assert.NotNull(encryptedText);
            Assert.NotEqual(plainText, encryptedText);

            var decryptText = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                Passphrase = new InArgument<string>(Passphrase),
                VerifySignature = true,
            };
            var decryptArgs = new System.Collections.Generic.Dictionary<string, object>
            {
                { nameof(DecryptText.Input), encryptedText },
            };
            var decryptedText = (string)new WorkflowInvoker(decryptText).Invoke(decryptArgs)[nameof(decryptText.Result)];

            Assert.Equal(plainText, decryptedText);
        }

        [Fact]
        public void PgpDecryptText_WithSecureStringPassphrase_RoundTrips()
        {
            // Encrypts with the default path, then decrypts using PassphraseSecureString +
            // PassphraseInputModeSwitch = SecureKey, proving the SecureString branch wires through end-to-end.
            var plainText = "Hello secure-passphrase round-trip!";

            var encryptText = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
            };
            var encryptArgs = new System.Collections.Generic.Dictionary<string, object>
            {
                { nameof(EncryptText.Input), plainText },
            };
            string encryptedText = (string)new WorkflowInvoker(encryptText).Invoke(encryptArgs)[nameof(encryptText.Result)];

            var decryptText = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                PassphraseSecureString = new InArgument<SecureString>((_) => GetPassphraseSecureString()),
            };
            var decryptArgs = new System.Collections.Generic.Dictionary<string, object>
            {
                { nameof(DecryptText.Input), encryptedText },
            };
            string decryptedText = (string)new WorkflowInvoker(decryptText).Invoke(decryptArgs)[nameof(decryptText.Result)];

            Assert.Equal(plainText, decryptedText);
        }

        [Theory]
        [InlineData(RsaKeySize.Rsa2048)]
        [InlineData(RsaKeySize.Rsa3072)]
        [InlineData(RsaKeySize.Rsa4096)]
        public void PgpEncryptDecryptText_WithGeneratedKeySize_Roundtrip(RsaKeySize keySize)
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_enc_{keySize}_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_enc_priv_{keySize}_{Guid.NewGuid()}.asc");

            try
            {
                // Generate key at specified size
                CryptographyHelper.PgpGenerateKeys(pubPath, privPath, $"enctest{(int)keySize}@test.com", "encpassword", keySize);

                // Encrypt and decrypt roundtrip
                var plainText = $"Test message encrypted with {keySize} key";

                using (var pubStream = File.OpenRead(pubPath))
                {
                    var encrypted = CryptographyHelper.PgpEncryptText(plainText, pubStream);

                    Assert.NotNull(encrypted);
                    Assert.NotEmpty(encrypted);

                    using (var privStream = File.OpenRead(privPath))
                    {
                        var decrypted = CryptographyHelper.PgpDecryptText(encrypted, privStream, "encpassword");
                        Assert.Equal(plainText, decrypted);
                    }
                }
            }
            finally
            {
                if (File.Exists(pubPath)) File.Delete(pubPath);
                if (File.Exists(privPath)) File.Delete(privPath);
            }
        }
    }
}
