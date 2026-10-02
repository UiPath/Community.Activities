using System;
using System.Activities;
using System.IO;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Bcpg.OpenPgp;
using UiPath.Cryptography.Enums;
using Xunit;
using CryptoResources = UiPath.Cryptography.Properties.Resources;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// STUD-81645: Decrypt with signature verification must work for messages that are encrypted to
    /// an encryption subkey and SIGNED BY A SIGNING SUBKEY (GnuPG 2.4+ default layout), which
    /// PgpCore's DecryptAndVerify cannot resolve. The BouncyCastle fallback must apply the same
    /// key vetting as PgpVerify and must never release unverified plaintext or mask the translated
    /// errors for genuinely bad input.
    /// </summary>
    public class PgpDecryptVerifyFallbackTests
    {
        private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("encrypted and signed payload for decrypt+verify coverage");

        // ---------------------------------------------------------------- helpers

        private static PgpBcFixture Recipient() => Create(o => o.WithEncryptionSubkey = true);

        private static PgpBcFixture Create(Action<PgpBcFixture.Options> configure)
        {
            var options = new PgpBcFixture.Options();
            configure(options);
            return PgpBcFixture.Create(options);
        }

        private static byte[] Decrypt(byte[] encrypted, PgpBcFixture recipient, byte[] verifierRing, string passphrase = null)
        {
            using var privateKey = new MemoryStream(recipient.PrivateKeyRing);
            using var publicKey = new MemoryStream(verifierRing);
            return CryptographyHelper.PgpDecrypt(encrypted, privateKey, passphrase ?? recipient.Passphrase, publicKey, verifySignature: true);
        }

        private static string DecryptText(string armored, PgpBcFixture recipient, byte[] verifierRing, string passphrase = null)
        {
            using var privateKey = new MemoryStream(recipient.PrivateKeyRing);
            using var publicKey = new MemoryStream(verifierRing);
            return CryptographyHelper.PgpDecryptText(armored, privateKey, passphrase ?? recipient.Passphrase, publicKey, verifySignature: true);
        }

        // The BouncyCastle fallback on its own. PgpCore (tried first by the public API) resolves a
        // single subkey signature itself, but it applies no revocation, expiry or binding checks;
        // those checks only exist in the fallback, so they are pinned against it directly.
        private static bool FallbackAccepts(byte[] encrypted, PgpBcFixture recipient, byte[] verifierRing)
        {
            using var plaintext = new MemoryStream();
            return CryptographyHelper.BcDecryptAndVerify(encrypted, recipient.PrivateKeyRing, recipient.Passphrase, verifierRing, plaintext);
        }

        private static void AssertSignatureVerificationFailed(Action action)
        {
            var ex = Assert.Throws<InvalidOperationException>(action);
            Assert.Equal(CryptoResources.PgpSignatureVerificationFailed, ex.Message);
        }

        // ---------------------------------------------------------------- the fix

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SubkeySigned_EncryptedToSubkey_DecryptsAndVerifies(bool compress)
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress, null, armor: true, user);

            Assert.Equal(Plaintext, Decrypt(encrypted, user, user.PublicKeyRing));
        }

        [Fact]
        public void SubkeySigned_RawBinaryMessage_DecryptsAndVerifies()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: false, user);

            Assert.Equal(Plaintext, Decrypt(encrypted, user, user.PublicKeyRing));
        }

        [Fact]
        public void SubkeySigned_SenderAndRecipientAreDifferentKeys_DecryptsAndVerifies()
        {
            var sender = PgpBcFixture.Create();
            var recipient = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, recipient, compress: true, null, armor: true, sender);

            Assert.Equal(Plaintext, Decrypt(encrypted, recipient, sender.PublicKeyRing));
        }

        [Fact]
        public void SubkeySigned_LargePayload_DecryptsAndVerifies()
        {
            var user = Recipient();
            var payload = new byte[8192 * 3 + 137];
            new Random(42).NextBytes(payload);
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(payload, user, compress: true, null, armor: true, user);

            Assert.Equal(payload, Decrypt(encrypted, user, user.PublicKeyRing));
        }

        [Fact]
        public void SubkeySigned_PgpDecryptStream_WritesPlaintextToOutput()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            using var input = new NonSeekableStream(encrypted);
            using var output = new MemoryStream();
            using var privateKey = new MemoryStream(user.PrivateKeyRing);
            using var publicKey = new MemoryStream(user.PublicKeyRing);
            CryptographyHelper.PgpDecryptStream(input, output, privateKey, user.Passphrase, publicKey, verifySignature: true);

            Assert.Equal(Plaintext, output.ToArray());
        }

        [Fact]
        public void SubkeySigned_PgpDecryptText_DecryptsAndVerifies()
        {
            var user = Recipient();
            var text = "text payload ✓ with non-ASCII";
            var armored = Encoding.ASCII.GetString(PgpBcFixture.CreateEncryptedSignedMessage(
                Encoding.UTF8.GetBytes(text), user, compress: true, null, armor: true, user));

            Assert.Equal(text, DecryptText(armored, user, user.PublicKeyRing));
        }

        [Fact]
        public void TwoSigners_OnlySecondSignersKeyInRing_DecryptsAndVerifies()
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();
            var recipient = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, recipient, compress: true, null, armor: true, a, b);

            Assert.Equal(Plaintext, Decrypt(encrypted, recipient, b.PublicKeyRing));
            Assert.Equal(Plaintext, Decrypt(encrypted, recipient, a.PublicKeyRing));
            Assert.Equal(Plaintext, Decrypt(encrypted, recipient, PgpBcFixture.CombinePublicKeyRings(a, b)));
        }

        // ---------------------------------------------------------------- the same key vetting as PgpVerify

        [Fact]
        public void Fallback_ValidSigningSubkey_Accepts()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            Assert.True(FallbackAccepts(encrypted, user, user.PublicKeyRing));
        }

        [Fact]
        public void Fallback_RevokedSigningSubkey_Rejects()
        {
            var user = Create(o =>
            {
                o.WithEncryptionSubkey = true;
                o.RevokeSubkey = true;
            });
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            Assert.False(FallbackAccepts(encrypted, user, user.PublicKeyRing));
        }

        [Fact]
        public void Fallback_ExpiredSigningSubkey_Rejects()
        {
            var user = Create(o =>
            {
                o.WithEncryptionSubkey = true;
                o.MasterCreatedUtc = DateTime.UtcNow.AddDays(-730);
                o.SubkeyCreatedUtc = DateTime.UtcNow.AddDays(-730);
                o.SubkeyExpirySeconds = 365L * 24 * 3600;
            });
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            Assert.False(FallbackAccepts(encrypted, user, user.PublicKeyRing));
        }

        [Fact]
        public void Fallback_GraftedSigningSubkey_Rejects()
        {
            var recipient = Recipient();
            var grafted = PgpBcFixture.CreateWithGraftedSubkey();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, recipient, compress: true, null, armor: true, grafted);

            Assert.False(FallbackAccepts(encrypted, recipient, grafted.PublicKeyRing));
        }

        [Theory]
        [InlineData(false)] // no back-signature at all
        [InlineData(true)]  // back-signature made over another primary key (stolen subkey)
        public void Fallback_SigningSubkeyWithoutValidBackSignature_Rejects(bool backSignatureOverAnotherPrimary)
        {
            var user = Create(o =>
            {
                o.WithEncryptionSubkey = true;
                o.BackSignature = backSignatureOverAnotherPrimary
                    ? PgpBcFixture.BackSignatureMode.WrongPrimary
                    : PgpBcFixture.BackSignatureMode.Missing;
            });
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            Assert.False(FallbackAccepts(encrypted, user, user.PublicKeyRing));
        }

        // Through the public API the fallback is what rejects an unusable signer when PgpCore cannot
        // resolve the first signer: the plaintext must not be released.
        [Fact]
        public void OnlyUnusableSignerKnown_DoesNotDecrypt()
        {
            var valid = PgpBcFixture.Create();
            var revoked = Create(o => o.RevokeSubkey = true);
            var recipient = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, recipient, compress: true, null, armor: true, valid, revoked);

            AssertSignatureVerificationFailed(() => Decrypt(encrypted, recipient, revoked.PublicKeyRing));
        }

        // ---------------------------------------------------------------- errors keep their translated messages

        [Fact]
        public void WrongVerifierKey_ThrowsSignatureVerificationFailed()
        {
            var user = Recipient();
            var stranger = PgpBcFixture.Create();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            AssertSignatureVerificationFailed(() => Decrypt(encrypted, user, stranger.PublicKeyRing));
        }

        [Fact]
        public void WrongPassphrase_ThrowsInvalidPassphrase_NotMaskedByFallback()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            var ex = Assert.Throws<InvalidOperationException>(() => Decrypt(encrypted, user, user.PublicKeyRing, passphrase: "not-the-passphrase"));
            Assert.Equal(CryptoResources.PgpInvalidPassphrase, ex.Message);
        }

        [Fact]
        public void WrongPrivateKey_ThrowsDecryptionKeyNotFound_NotMaskedByFallback()
        {
            var user = Recipient();
            var otherRecipient = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            // PgpCore reports this as an ArgumentException; the fallback must not change or mask it.
            var ex = Assert.Throws<ArgumentException>(() => Decrypt(encrypted, otherRecipient, user.PublicKeyRing));
            Assert.Contains("Decryption key for message not found", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void UnsignedMessage_WhenVerificationRequested_Throws()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedUnsignedMessage(Plaintext, user, armor: true);

            Assert.ThrowsAny<Exception>(() => Decrypt(encrypted, user, user.PublicKeyRing));
        }

        // Flipping a byte of the raw ciphertext must fail (integrity / decryption) and must NOT hand
        // the caller any plaintext: nothing may be written to the output stream.
        [Fact]
        public void TamperedCiphertext_Throws_AndWritesNothing()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: false, null, armor: false, user);
            encrypted[encrypted.Length - 5] ^= 0xFF;

            using var output = new MemoryStream();
            using var privateKey = new MemoryStream(user.PrivateKeyRing);
            using var publicKey = new MemoryStream(user.PublicKeyRing);
            Assert.ThrowsAny<Exception>(() => CryptographyHelper.PgpDecryptStream(
                new MemoryStream(encrypted), output, privateKey, user.Passphrase, publicKey, verifySignature: true));

            Assert.Equal(0, output.Length);
        }

        [Fact]
        public void VerificationFailure_WritesNothingToOutputStream()
        {
            var user = Recipient();
            var stranger = PgpBcFixture.Create();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            using var output = new MemoryStream();
            using var privateKey = new MemoryStream(user.PrivateKeyRing);
            using var publicKey = new MemoryStream(stranger.PublicKeyRing);
            Assert.Throws<InvalidOperationException>(() => CryptographyHelper.PgpDecryptStream(
                new MemoryStream(encrypted), output, privateKey, user.Passphrase, publicKey, verifySignature: true));

            Assert.Equal(0, output.Length);
        }

        [Fact]
        public void VerificationRequestedWithoutPublicKey_StillThrowsArgumentException()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            using var privateKey = new MemoryStream(user.PrivateKeyRing);
            Assert.Throws<ArgumentException>(() => CryptographyHelper.PgpDecrypt(
                encrypted, privateKey, user.Passphrase, publicKeyStream: null, verifySignature: true));
        }

        // verifySignature = false must keep the original PgpCore behavior (no fallback involved).
        [Fact]
        public void WithoutVerification_SubkeySignedMessageStillDecrypts()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            using var privateKey = new MemoryStream(user.PrivateKeyRing);
            Assert.Equal(Plaintext, CryptographyHelper.PgpDecrypt(encrypted, privateKey, user.Passphrase));
        }

        // ---------------------------------------------------------------- activities

        [Fact]
        public void DecryptFile_Activity_SubkeySignedMessage_VerifySignature_Succeeds()
        {
            var user = Recipient();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            using var temp = new TempFiles();
            var inputPath = temp.Write("signed.pgp", encrypted);
            var privatePath = temp.Write("private.asc", user.PrivateKeyRing);
            var publicPath = temp.Write("public.asc", user.PublicKeyRing);
            var outputPath = temp.PathFor("decrypted.txt");

            WorkflowInvoker.Invoke(new DecryptFile
            {
                Algorithm = EncryptionAlgorithm.PGP,
                InputFilePath = new InArgument<string>(inputPath),
                PrivateKeyFilePath = new InArgument<string>(privatePath),
                PublicKeyFilePath = new InArgument<string>(publicPath),
                Passphrase = new InArgument<string>(user.Passphrase),
                VerifySignature = true,
                OutputFilePath = new InArgument<string>(outputPath),
            });

            Assert.Equal(Plaintext, File.ReadAllBytes(outputPath));
        }

        [Fact]
        public void DecryptFile_Activity_WrongVerifierKey_Throws_AndWritesNoOutput()
        {
            var user = Recipient();
            var stranger = PgpBcFixture.Create();
            var encrypted = PgpBcFixture.CreateEncryptedSignedMessage(Plaintext, user, compress: true, null, armor: true, user);

            using var temp = new TempFiles();
            var outputPath = temp.PathFor("decrypted.txt");

            var ex = Assert.Throws<InvalidOperationException>(() => WorkflowInvoker.Invoke(new DecryptFile
            {
                Algorithm = EncryptionAlgorithm.PGP,
                InputFilePath = new InArgument<string>(temp.Write("signed.pgp", encrypted)),
                PrivateKeyFilePath = new InArgument<string>(temp.Write("private.asc", user.PrivateKeyRing)),
                PublicKeyFilePath = new InArgument<string>(temp.Write("public.asc", stranger.PublicKeyRing)),
                Passphrase = new InArgument<string>(user.Passphrase),
                VerifySignature = true,
                OutputFilePath = new InArgument<string>(outputPath),
            }));

            Assert.Equal(CryptoResources.PgpSignatureVerificationFailed, ex.Message);
            Assert.False(File.Exists(outputPath));
        }

        [Fact]
        public void DecryptText_Activity_SubkeySignedMessage_VerifySignature_Succeeds()
        {
            var user = Recipient();
            var text = "activity text payload";
            var armored = Encoding.ASCII.GetString(PgpBcFixture.CreateEncryptedSignedMessage(
                Encoding.UTF8.GetBytes(text), user, compress: true, null, armor: true, user));

            using var temp = new TempFiles();
            var result = WorkflowInvoker.Invoke(new DecryptText
            {
                Algorithm = EncryptionAlgorithm.PGP,
                Input = new InArgument<string>(armored),
                PrivateKeyFilePath = new InArgument<string>(temp.Write("private.asc", user.PrivateKeyRing)),
                PublicKeyFilePath = new InArgument<string>(temp.Write("public.asc", user.PublicKeyRing)),
                Passphrase = new InArgument<string>(user.Passphrase),
                VerifySignature = true,
            });

            Assert.Equal(text, result);
        }

        // ---------------------------------------------------------------- test plumbing

        private sealed class TempFiles : IDisposable
        {
            private readonly string _directory = Path.Combine(Path.GetTempPath(), $"pgp_dv_{Guid.NewGuid():N}");

            public TempFiles() => Directory.CreateDirectory(_directory);

            public string PathFor(string name) => Path.Combine(_directory, name);

            public string Write(string name, byte[] content)
            {
                var path = PathFor(name);
                File.WriteAllBytes(path, content);
                return path;
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(_directory, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }

        // PgpDecryptStream must work for forward-only input, e.g. a network or pipe stream.
        private sealed class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] buffer) : base(buffer)
            {
            }

            public override bool CanSeek => false;

            public override long Position
            {
                get => base.Position;
                set => throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
        }
    }
}
