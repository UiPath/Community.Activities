using System;
using System.Activities;
using System.Activities.Validation;
using System.IO;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // tests intentionally set the obsolete *InputModeSwitch properties to exercise legacy behavior.

namespace UiPath.Cryptography.Activities.Tests
{
    public class PgpStandaloneTests : PgpTestBase
    {
        #region CryptographyHelper Tests

        [Fact]
        public void PgpGenerateKeys_CreatesFiles()
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_gen_pub_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_gen_priv_{Guid.NewGuid()}.asc");

            try
            {
                CryptographyHelper.PgpGenerateKeys(pubPath, privPath, "gentest@test.com", "genpassword");

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

        [Theory]
        [InlineData(RsaKeySize.Rsa2048)]
        [InlineData(RsaKeySize.Rsa3072)]
        [InlineData(RsaKeySize.Rsa4096)]
        public void PgpGenerateKeys_WithKeySize_CreatesValidKey(RsaKeySize keySize)
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_gen_pub_{keySize}_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_gen_priv_{keySize}_{Guid.NewGuid()}.asc");

            try
            {
                CryptographyHelper.PgpGenerateKeys(pubPath, privPath, $"gentest{(int)keySize}@test.com", "genpassword", keySize);

                Assert.True(File.Exists(pubPath), $"Public key file not created for {keySize}");
                Assert.True(File.Exists(privPath), $"Private key file not created for {keySize}");

                using (var pubStream = File.OpenRead(pubPath))
                {
                    Assert.True(CryptographyHelper.PgpVerifyPublicKey(pubStream), $"Generated public key invalid for {keySize}");
                }

                // Prove the requested key size actually took effect by parsing the public key
                // and asserting the RSA modulus bit-length matches the requested RsaKeySize.
                using (var pubStream = File.OpenRead(pubPath))
                using (var decoded = PgpUtilities.GetDecoderStream(pubStream))
                {
                    var ring = new PgpPublicKeyRingBundle(decoded).GetKeyRings().Cast<PgpPublicKeyRing>().First();
                    var masterKey = ring.GetPublicKeys().Cast<PgpPublicKey>().First(k => k.IsMasterKey);
                    Assert.Equal((int)keySize, masterKey.BitStrength);
                }
            }
            finally
            {
                if (File.Exists(pubPath)) File.Delete(pubPath);
                if (File.Exists(privPath)) File.Delete(privPath);
            }
        }

        [Fact]
        public void PgpGenerateKeys_Activity_WithoutKeySize_DefaultsTo4096()
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_act_default_pub_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_act_default_priv_{Guid.NewGuid()}.asc");

            try
            {
                var activity = new PgpGenerateKeys
                {
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                    PrivateKeyFilePath = new InArgument<string>(privPath),
                    UserId = new InArgument<string>("backcompat@test.com"),
                    Passphrase = new InArgument<string>(Passphrase)
                };

                WorkflowInvoker.Invoke(activity);

                Assert.True(File.Exists(pubPath), "Public key file not created with default KeySize");
                Assert.True(File.Exists(privPath), "Private key file not created with default KeySize");

                using (var pubStream = File.OpenRead(pubPath))
                {
                    Assert.True(CryptographyHelper.PgpVerifyPublicKey(pubStream), "Generated key with default size is invalid");
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
        public void PgpSign_DefaultsToSha256()
        {
            // Binary PGP signing must default to SHA-256 (CryptographyHelper.ExecutePgpSignOperation
            // sets pgp.HashAlgorithmTag = Sha256 before signing). Older PGP libraries default to
            // SHA-1 which is now considered weak. Parse the produced packet stream and assert.
            var plainBytes = Encoding.UTF8.GetBytes("hash-algo probe");

            using (var privateKeyStream = File.OpenRead(_privateKeyPath))
            {
                var signed = CryptographyHelper.PgpSign(plainBytes, privateKeyStream, Passphrase);

                using (var input = new MemoryStream(signed))
                using (var decoded = PgpUtilities.GetDecoderStream(input))
                {
                    var factory = new PgpObjectFactory(decoded);
                    PgpObject pgpObject;
                    HashAlgorithmTag? hashAlgo = null;
                    while ((pgpObject = factory.NextPgpObject()) != null)
                    {
                        if (pgpObject is PgpOnePassSignatureList onePassList && onePassList.Count > 0)
                        {
                            hashAlgo = onePassList[0].HashAlgorithm;
                            break;
                        }
                    }
                    Assert.NotNull(hashAlgo);
                    Assert.Equal(HashAlgorithmTag.Sha256, hashAlgo.Value);
                }
            }
        }

        [Fact]
        public void PgpClearSign_And_VerifyClear_RoundTrip()
        {
            var plainBytes = Encoding.UTF8.GetBytes("Hello PGP clearsigning!");

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

        // STUD-80430: GnuPG 2.4+ wraps the one-pass signature in a compressed packet by default.
        // The verify path must descend into the compression layer before locating the signature.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PgpVerify_SubkeySignedMessage_Verifies(bool compress)
        {
            var fixture = PgpBcFixture.Create();
            var data = Encoding.UTF8.GetBytes("Subkey-signed payload for regression coverage");

            var signed = fixture.CreateSignedMessage(data, compress);

            using (var publicKeyStream = new MemoryStream(fixture.PublicKeyRing))
            {
                Assert.True(CryptographyHelper.PgpVerify(signed, publicKeyStream));
            }
        }

        // STUD-80429: GnuPG issues signatures from a signing-capable subkey. The verify path must
        // resolve the signature's issuer key ID against every public key in the ring, not just the
        // primary key, in ClearSignature mode too (binary-mode resolution is covered by
        // PgpVerify_SubkeySignedMessage_Verifies above; same GetPublicKey(signature.KeyId) path).
        [Fact]
        public void PgpVerifyClear_SubkeySignedMessage_Verifies()
        {
            var fixture = PgpBcFixture.Create();

            var signed = fixture.CreateClearSignedMessage("Clear-signed by subkey");

            using (var publicKeyStream = new MemoryStream(fixture.PublicKeyRing))
            {
                Assert.True(CryptographyHelper.PgpVerifyClear(signed, publicKeyStream));
            }
        }

        // STUD-80429: same subkey-resolution path as above, but with a multi-line message so the
        // clear-text canonicalization loop's line-continuation branch (embedded newlines) is covered
        // too, not just the single-line/no-trailing-newline case.
        [Fact]
        public void PgpVerifyClear_SubkeySignedMultiLineMessage_Verifies()
        {
            var fixture = PgpBcFixture.Create();

            var signed = fixture.CreateClearSignedMessage("Line one\nLine two\nLine three");

            using (var publicKeyStream = new MemoryStream(fixture.PublicKeyRing))
            {
                Assert.True(CryptographyHelper.PgpVerifyClear(signed, publicKeyStream));
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
            var invalidKeyPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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

        // STUD-80428: GnuPG 2.4+ defaults to a LibrePGP AEAD Encrypted Data packet (tag 20, OCB
        // cipher) for recipient keys that advertise AEAD support. BouncyCastle 2.4.0's
        // PgpObjectFactory has no case for tag 20 and throws "unknown object in stream 20";
        // PgpDecrypt/PgpDecryptStream must translate that into the actionable
        // PgpAeadNotSupported message instead of surfacing the raw BouncyCastle exception.
        [Fact]
        public void PgpDecrypt_AeadEncryptedPacket_ThrowsActionableError()
        {
            var aeadPacket = BuildRawAeadEncryptedDataPacket();

            using (var privateKeyStream = File.OpenRead(_privateKeyPath))
            {
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    CryptographyHelper.PgpDecrypt(aeadPacket, privateKeyStream, Passphrase));

                Assert.Equal(UiPath.Cryptography.Properties.Resources.PgpAeadNotSupported, ex.Message);
            }
        }

        [Fact]
        public void DecryptFile_Activity_AeadEncryptedPacket_ThrowsActionableError()
        {
            var aeadPacket = BuildRawAeadEncryptedDataPacket();
            var inputPath = Path.Combine(Path.GetTempPath(), $"pgp_aead_in_{Guid.NewGuid()}.pgp");
            File.WriteAllBytes(inputPath, aeadPacket);

            try
            {
                var activity = new DecryptFile
                {
                    Algorithm = EncryptionAlgorithm.PGP,
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>(Path.Combine(Path.GetTempPath(), $"pgp_aead_out_{Guid.NewGuid()}.txt")),
                };

                var ex = Assert.Throws<InvalidOperationException>(() => WorkflowInvoker.Invoke(activity));
                Assert.Equal(UiPath.Cryptography.Properties.Resources.PgpAeadNotSupported, ex.Message);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
            }
        }

        /// <summary>
        /// Builds a minimal raw OpenPGP packet with tag 20 (AEAD Encrypted Data / LibrePGP,
        /// GnuPG 2.4+'s default for AEAD-capable recipients). BouncyCastle 2.4.0's
        /// <see cref="PgpObjectFactory"/> has no case for this tag and throws before any key
        /// material is consulted, so the body content is irrelevant — only a well-formed
        /// new-format packet header for tag 20 is needed to reproduce the failure.
        /// </summary>
        private static byte[] BuildRawAeadEncryptedDataPacket()
        {
            const byte tag20NewFormatHeader = 0xC0 | 20; // RFC 4880 new-format packet header, tag 20
            byte[] body = { 1, 9, 2, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // version, cipher, aead algo, chunk size, IV (arbitrary)

            using var ms = new MemoryStream();
            ms.WriteByte(tag20NewFormatHeader);
            ms.WriteByte((byte)body.Length); // one-byte new-format length (body.Length < 192)
            ms.Write(body, 0, body.Length);
            return ms.ToArray();
        }

        #endregion

        #region Activity Integration Tests

        [Fact]
        public void PgpGenerateKeys_Activity_Works()
        {
            var pubPath = Path.Combine(Path.GetTempPath(), $"pgp_act_pub_{Guid.NewGuid()}.asc");
            var privPath = Path.Combine(Path.GetTempPath(), $"pgp_act_priv_{Guid.NewGuid()}.asc");

            try
            {
                var activity = new PgpGenerateKeys
                {
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                    PrivateKeyFilePath = new InArgument<string>(privPath),
                    UserId = new InArgument<string>("acttest@test.com"),
                    Passphrase = new InArgument<string>(Passphrase)
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
        public void PgpSignFile_Activity_WithStringPaths_Works()
        {
            var inputPath = Path.Combine(Path.GetTempPath(), $"pgp_sign_in_{Guid.NewGuid()}.txt");
            var outputPath = Path.Combine(Path.GetTempPath(), $"pgp_sign_out_{Guid.NewGuid()}.txt.signed");

            try
            {
                File.WriteAllText(inputPath, "Hello PGP sign-file");

                var activity = new PgpSignFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>(outputPath),
                    Overwrite = true,
                };

                WorkflowInvoker.Invoke(activity);

                Assert.True(File.Exists(outputPath), "Signed file not created");
                using (var signed = File.OpenRead(outputPath))
                using (var publicKey = File.OpenRead(_publicKeyPath))
                {
                    Assert.True(CryptographyHelper.PgpVerify(File.ReadAllBytes(outputPath), publicKey));
                }
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void PgpClearSignFile_Activity_WithStringPaths_Works()
        {
            var inputPath = Path.Combine(Path.GetTempPath(), $"pgp_clearsign_in_{Guid.NewGuid()}.txt");
            var outputPath = Path.Combine(Path.GetTempPath(), $"pgp_clearsign_out_{Guid.NewGuid()}.txt.asc");

            try
            {
                File.WriteAllText(inputPath, "Hello PGP clearsign");

                var activity = new PgpClearSignFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>(outputPath),
                    Overwrite = true,
                };

                WorkflowInvoker.Invoke(activity);

                Assert.True(File.Exists(outputPath), "ClearSigned file not created");
                using (var publicKey = File.OpenRead(_publicKeyPath))
                {
                    Assert.True(CryptographyHelper.PgpVerifyClear(File.ReadAllBytes(outputPath), publicKey));
                }
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void PgpVerifySignature_Activity_WithStringPaths_Works()
        {
            var plainBytes = Encoding.UTF8.GetBytes("Hello PGP verify-activity");
            var signedPath = Path.Combine(Path.GetTempPath(), $"pgp_verify_signed_{Guid.NewGuid()}.dat");

            try
            {
                byte[] signedBytes;
                using (var priv = File.OpenRead(_privateKeyPath))
                    signedBytes = CryptographyHelper.PgpSign(plainBytes, priv, Passphrase);
                File.WriteAllBytes(signedPath, signedBytes);

                var activity = new PgpVerify
                {
                    Mode = PgpVerifyMode.Signature,
                    InputFilePath = new InArgument<string>(signedPath),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                };

                var result = WorkflowInvoker.Invoke(activity);
                Assert.True((bool)result["Result"]);
            }
            finally
            {
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Fact]
        public void PgpVerifyClearSignature_Activity_WithStringPaths_Works()
        {
            var plainBytes = Encoding.UTF8.GetBytes("Hello PGP verify-clear-activity");
            var signedPath = Path.Combine(Path.GetTempPath(), $"pgp_verify_clearsigned_{Guid.NewGuid()}.asc");

            try
            {
                byte[] signedBytes;
                using (var priv = File.OpenRead(_privateKeyPath))
                    signedBytes = CryptographyHelper.PgpClearSign(plainBytes, priv, Passphrase);
                File.WriteAllBytes(signedPath, signedBytes);

                var activity = new PgpVerify
                {
                    Mode = PgpVerifyMode.ClearSignature,
                    InputFilePath = new InArgument<string>(signedPath),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                };

                var result = WorkflowInvoker.Invoke(activity);
                Assert.True((bool)result["Result"]);
            }
            finally
            {
                if (File.Exists(signedPath)) File.Delete(signedPath);
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
            var isValid = (bool)result["Result"];
            Assert.True(isValid);
        }

        [Fact]
        public void PgpVerifyPublicKey_Activity_InvalidKey_ReturnsFalse()
        {
            var invalidKeyPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(invalidKeyPath, "Not a PGP key");

                var activity = new PgpVerify
                {
                    Mode = PgpVerifyMode.PublicKey,
                    PublicKeyFilePath = new InArgument<string>(invalidKeyPath)
                };

                var result = WorkflowInvoker.Invoke(activity);
                var isValid = (bool)result["Result"];
                Assert.False(isValid);
            }
            finally
            {
                File.Delete(invalidKeyPath);
            }
        }

        [Fact]
        public void PgpSignFile_And_VerifySignature_Activity_RoundTrip()
        {
            var inputPath = Path.Combine(Path.GetTempPath(), $"pgp_rt_in_{Guid.NewGuid()}.txt");
            var signedPath = Path.Combine(Path.GetTempPath(), $"pgp_rt_signed_{Guid.NewGuid()}.dat");

            try
            {
                File.WriteAllText(inputPath, "Round-trip payload");

                WorkflowInvoker.Invoke(new PgpSignFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>(signedPath),
                    Overwrite = true,
                });

                var result = WorkflowInvoker.Invoke(new PgpVerify
                {
                    Mode = PgpVerifyMode.Signature,
                    InputFilePath = new InArgument<string>(signedPath),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                });

                Assert.True((bool)result["Result"]);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Fact]
        public void PgpClearSignFile_And_VerifyClearSignature_Activity_RoundTrip()
        {
            var inputPath = Path.Combine(Path.GetTempPath(), $"pgp_rtc_in_{Guid.NewGuid()}.txt");
            var signedPath = Path.Combine(Path.GetTempPath(), $"pgp_rtc_signed_{Guid.NewGuid()}.asc");

            try
            {
                File.WriteAllText(inputPath, "Round-trip clearsign payload");

                WorkflowInvoker.Invoke(new PgpClearSignFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>(signedPath),
                    Overwrite = true,
                });

                var result = WorkflowInvoker.Invoke(new PgpVerify
                {
                    Mode = PgpVerifyMode.ClearSignature,
                    InputFilePath = new InArgument<string>(signedPath),
                    PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
                });

                Assert.True((bool)result["Result"]);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Fact]
        public void PgpFullPipeline_Activities_EndToEnd_Works()
        {
            // Mirrors the customer workflow: generate keys, sign, clearsign, verify (signature / clear / public-key).
            var dir = Path.Combine(Path.GetTempPath(), $"pgp_pipeline_{Guid.NewGuid()}");
            var pubPath = Path.Combine(dir, "public.key");
            var privPath = Path.Combine(dir, "private.key");
            var inputPath = Path.Combine(dir, "input.txt");
            var signedPath = Path.Combine(dir, "signed.file");
            var clearSignedPath = Path.Combine(dir, "signed.text");
            const string passphrase = "123abc4d";

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(inputPath, "Customer pipeline payload");

                WorkflowInvoker.Invoke(new PgpGenerateKeys
                {
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                    PrivateKeyFilePath = new InArgument<string>(privPath),
                    UserId = new InArgument<string>("A P <ap@example.com>"),
                    Passphrase = new InArgument<string>(passphrase),
                    Overwrite = true,
                });

                WorkflowInvoker.Invoke(new PgpSignFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(privPath),
                    Passphrase = new InArgument<string>(passphrase),
                    OutputFilePath = new InArgument<string>(signedPath),
                    Overwrite = true,
                });

                WorkflowInvoker.Invoke(new PgpClearSignFile
                {
                    InputFilePath = new InArgument<string>(signedPath),
                    PrivateKeyFilePath = new InArgument<string>(privPath),
                    Passphrase = new InArgument<string>(passphrase),
                    OutputFilePath = new InArgument<string>(clearSignedPath),
                    Overwrite = true,
                });

                var sigResult = WorkflowInvoker.Invoke(new PgpVerify
                {
                    Mode = PgpVerifyMode.Signature,
                    InputFilePath = new InArgument<string>(signedPath),
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                });
                Assert.True((bool)sigResult["Result"], "Signature verification failed");

                var clearResult = WorkflowInvoker.Invoke(new PgpVerify
                {
                    Mode = PgpVerifyMode.ClearSignature,
                    InputFilePath = new InArgument<string>(clearSignedPath),
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                });
                Assert.True((bool)clearResult["Result"], "ClearSignature verification failed");

                var pubResult = WorkflowInvoker.Invoke(new PgpVerify
                {
                    Mode = PgpVerifyMode.PublicKey,
                    PublicKeyFilePath = new InArgument<string>(pubPath),
                });
                Assert.True((bool)pubResult["Result"], "Public-key verification failed");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        #endregion

        #region Runtime Switch-Strict Tests

        [Fact]
        public void PgpSignFile_FilePathMode_EmptyInputFilePath_ThrowsAtRuntime()
        {
            var activity = new PgpSignFile
            {
                InputFilePath = new InArgument<string>(""),
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                Passphrase = new InArgument<string>(Passphrase),
                OutputFilePath = new InArgument<string>("ignored"),
            };
            var ex = Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(activity));
            Assert.Equal(nameof(PgpSignFile.InputFilePath), ex.ParamName);
        }

        [Fact]
        public void PgpSignFile_FilePathMode_EmptyPrivateKeyFilePath_ThrowsAtRuntime()
        {
            var inputPath = Path.Combine(Path.GetTempPath(), $"pgp_sse_{Guid.NewGuid()}.txt");
            try
            {
                File.WriteAllText(inputPath, "x");

                var activity = new PgpSignFile
                {
                    InputFilePath = new InArgument<string>(inputPath),
                    PrivateKeyFilePath = new InArgument<string>(""),
                    Passphrase = new InArgument<string>(Passphrase),
                    OutputFilePath = new InArgument<string>("ignored"),
                };
                var ex = Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(activity));
                Assert.Equal(nameof(PgpSignFile.PrivateKeyFilePath), ex.ParamName);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
            }
        }

        [Fact]
        public void PgpClearSignFile_FilePathMode_EmptyInputFilePath_ThrowsAtRuntime()
        {
            var activity = new PgpClearSignFile
            {
                InputFilePath = new InArgument<string>(""),
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                Passphrase = new InArgument<string>(Passphrase),
                OutputFilePath = new InArgument<string>("ignored"),
            };
            var ex = Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(activity));
            Assert.Equal(nameof(PgpClearSignFile.InputFilePath), ex.ParamName);
        }

        [Fact]
        public void PgpVerify_FilePathMode_EmptyInputFilePath_ThrowsAtRuntime()
        {
            var activity = new PgpVerify
            {
                Mode = PgpVerifyMode.Signature,
                InputFilePath = new InArgument<string>(""),
                PublicKeyFilePath = new InArgument<string>(_publicKeyPath),
            };
            var ex = Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(activity));
            Assert.Equal(nameof(PgpVerify.InputFilePath), ex.ParamName);
        }

        [Fact]
        public void PgpVerify_FilePathMode_EmptyPublicKeyFilePath_ThrowsAtRuntime()
        {
            var activity = new PgpVerify
            {
                Mode = PgpVerifyMode.PublicKey,
                PublicKeyFilePath = new InArgument<string>(""),
            };
            var ex = Assert.Throws<ArgumentNullException>(() => WorkflowInvoker.Invoke(activity));
            Assert.Equal(nameof(PgpVerify.PublicKeyFilePath), ex.ParamName);
        }

        #endregion

        #region IResource Tests

        [Fact]
        public void PgpSignFile_Has_IResource_Properties()
        {
            Assert.NotNull(typeof(PgpSignFile).GetProperty(nameof(PgpSignFile.InputFile)));
            Assert.NotNull(typeof(PgpSignFile).GetProperty(nameof(PgpSignFile.PrivateKeyFile)));
        }

        [Fact]
        public void PgpClearSignFile_Has_IResource_Properties()
        {
            Assert.NotNull(typeof(PgpClearSignFile).GetProperty(nameof(PgpClearSignFile.InputFile)));
            Assert.NotNull(typeof(PgpClearSignFile).GetProperty(nameof(PgpClearSignFile.PrivateKeyFile)));
        }

        [Fact]
        public void PgpVerify_Has_IResource_Properties()
        {
            Assert.NotNull(typeof(PgpVerify).GetProperty(nameof(PgpVerify.InputFile)));
            Assert.NotNull(typeof(PgpVerify).GetProperty(nameof(PgpVerify.PublicKeyFile)));
        }

        [Fact]
        public void EncryptText_Has_PrivateKeyFile_IResource_Property()
        {
            Assert.NotNull(typeof(EncryptText).GetProperty(nameof(EncryptText.PrivateKeyFile)));
        }

        [Fact]
        public void EncryptFile_Has_PrivateKeyFile_IResource_Property()
        {
            Assert.NotNull(typeof(EncryptFile).GetProperty(nameof(EncryptFile.PrivateKeyFile)));
        }

        [Fact]
        public void DecryptText_Has_PrivateKeyFile_IResource_Property()
        {
            Assert.NotNull(typeof(DecryptText).GetProperty(nameof(DecryptText.PrivateKeyFile)));
        }

        [Fact]
        public void DecryptFile_Has_PrivateKeyFile_IResource_Property()
        {
            Assert.NotNull(typeof(DecryptFile).GetProperty(nameof(DecryptFile.PrivateKeyFile)));
        }

        #endregion
    }
}

#pragma warning restore CS0618
