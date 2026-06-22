using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // obsolete algorithms reachable via opt-in formats

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Bidirectional external-tool interop tests using BCL-only counterpart implementations
    /// of each wire format. Goal: prove that what UiPath produces, an external tool can read,
    /// and vice-versa — without depending on an external CLI being present on the CI agent.
    ///
    /// Each format ships with a <c>Bcl*</c> static helper that emits / parses the same
    /// byte layout described in <c>docs/symmetric-wire-format.md</c>, using ONLY
    /// <see cref="Aes"/>, <see cref="AesGcm"/>, and <see cref="Rfc2898DeriveBytes"/>. The
    /// helpers consult the spec, never the production helper code, so a regression in
    /// CryptographyHelper that silently changed the wire format would surface here.
    /// </summary>
    public class ExternalInteropInProcessTests
    {
        private const string Plaintext = "External-tool interop: round-trip me. 0123456789 ăîșțâ €";
        private const string Password = "interop-test-password-{!@#}";

        static ExternalInteropInProcessTests()
        {
            // Touching EncodingHelpers runs its static ctor, which registers
            // CodePagesEncodingProvider — making legacy code pages such as windows-1252
            // resolvable in this test process (the OpenSslEnc_AesCbc_KeyEncodingDoesNotAffectPlaintextBytes test).
            UiPath.Cryptography.Activities.Helpers.EncodingHelpers.GetAvailableEncodings();
        }

        // ────────────────────────────────────────────────────────────────────────
        // OpenSslEnc — AES-256-CBC, bidirectional, AGAINST a BCL counterpart that
        // mirrors `openssl enc -aes-256-cbc -pbkdf2 -iter <N>`. This is the headline
        // interop format and the whole reason the OpenSslEnc enum exists.
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(600_000, AesKeySize.Aes128)]
        [InlineData(600_000, AesKeySize.Aes192)]
        [InlineData(600_000, AesKeySize.Aes256)]
        [InlineData(50_000, AesKeySize.Aes256)]
        public void OpenSslEnc_AesCbc_ExternalToInternal(int iterations, AesKeySize aesKeySize)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(Plaintext);
            byte[] externalBlob = BclOpenSslEnc.EncryptAesCbc(plainBytes, Password, iterations, (int)aesKeySize / 8);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, input: Convert.ToBase64String(externalBlob),
                inputEncoding: Encoding.UTF8, iterations: iterations, aesKeySize: aesKeySize);

            Assert.Equal(Plaintext, decrypted);
        }

        [Theory]
        [InlineData(600_000, AesKeySize.Aes128)]
        [InlineData(600_000, AesKeySize.Aes192)]
        [InlineData(600_000, AesKeySize.Aes256)]
        [InlineData(50_000, AesKeySize.Aes256)]
        public void OpenSslEnc_AesCbc_InternalToExternal(int iterations, AesKeySize aesKeySize)
        {
            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8, iterations: iterations, aesKeySize: aesKeySize);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclOpenSslEnc.DecryptAesCbc(blob, Password, iterations, (int)aesKeySize / 8);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(decrypted));
        }

        // ────────────────────────────────────────────────────────────────────────
        // STUD-80534 — the MinKdfIterations=1000 floor is an encrypt-only guard. A
        // third-party blob produced with a low iteration count (e.g.
        // `openssl enc -pbkdf2 -iter 100`) is mathematically decryptable, so decrypt
        // must honour whatever the producer chose; encrypt must still refuse to emit
        // weak ciphertext.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void OpenSslEnc_AesCbc_DecryptHonoursLowKdfIterations()
        {
            // Reproducer: an external producer used iterations=100 (below the 1000 floor). We can
            // derive the matching key/IV, so the round-trip must succeed. Before the fix this threw
            // ArgumentException (Validation_KdfIterations_BelowMinimum) before reaching the decrypt code.
            const int lowIterations = 100;
            byte[] plainBytes = Encoding.UTF8.GetBytes(Plaintext);
            byte[] externalBlob = BclOpenSslEnc.EncryptAesCbc(plainBytes, Password, lowIterations, 32);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, input: Convert.ToBase64String(externalBlob),
                inputEncoding: Encoding.UTF8, iterations: lowIterations, aesKeySize: AesKeySize.Aes256);

            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void OpenSslEnc_AesCbc_EncryptStillRefusesLowKdfIterations()
        {
            // Encrypt-side floor pin: we must never produce ciphertext with a weak iteration count.
            Assert.Throws<ArgumentException>(() => RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8, iterations: 100, aesKeySize: AesKeySize.Aes256));
        }

        [Fact]
        public void OpenSslEnc_AesCbc_NegativeKdfIterations_RejectedOnBothDirections()
        {
            // Negative iterations have no legitimate use case and stay rejected regardless of direction.
            // On decrypt the validator runs before the input blob is decoded, so the input is never read;
            // and a junk-blob failure would surface as InvalidOperationException, not ArgumentException —
            // asserting ArgumentException therefore pins the rejection to the iteration validator alone.
            Assert.Throws<ArgumentException>(() => RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8, iterations: -1, aesKeySize: AesKeySize.Aes256));

            byte[] validBlob = BclOpenSslEnc.EncryptAesCbc(Encoding.UTF8.GetBytes(Plaintext), Password, 600_000, 32);
            Assert.Throws<ArgumentException>(() => RunDecryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, input: Convert.ToBase64String(validBlob),
                inputEncoding: Encoding.UTF8, iterations: -1, aesKeySize: AesKeySize.Aes256));
        }

        // ────────────────────────────────────────────────────────────────────────
        // STUD-80530 — plaintext encoding must be governed by PlaintextEncoding, NOT the
        // key/password Encoding. These two tests would pass against a UiPath↔UiPath round-trip
        // (the existing suite) but only an external/BCL consumer reading the raw decrypted bytes
        // can catch the conflation.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void OpenSslEnc_AesCbc_PlaintextEncodingUtf16_PreservedForExternalConsumer()
        {
            // Repro: encrypt with PlaintextEncoding = UTF-16 (key Encoding stays UTF-8). An external
            // openssl consumer must see the UTF-16 LE bytes of the plaintext. Before the fix the
            // activity transcoded the plaintext through the key Encoding (UTF-8), so this failed.
            const int iterations = 600_000;
            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8, plaintextEncoding: Encoding.Unicode,
                iterations: iterations, aesKeySize: AesKeySize.Aes256);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclOpenSslEnc.DecryptAesCbc(blob, Password, iterations, 32);

            Assert.Equal(Encoding.Unicode.GetBytes(Plaintext), decrypted);
        }

        [Fact]
        public void OpenSslEnc_AesCbc_PublicPlaintextEncodingWinsOverProxyDefault()
        {
            // Regression for the public-surface bug: setting the public PlaintextEncoding InArgument must
            // take effect even though the hidden PlaintextEncodingString proxy still carries its UTF-8
            // constructor default. Earlier, the non-null proxy won and silently downgraded Unicode to UTF-8.
            // The helper here assigns ONLY PlaintextEncoding (it no longer clears the proxy), so a passing
            // assertion proves the public property is authoritative on its own.
            const int iterations = 600_000;
            var activity = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.AES,
                Format = SymmetricWireFormat.OpenSslEnc,
                KeyFormat = KeyBytesFormat.Encoded,
                Encoding = MakeEncodingArg(Encoding.UTF8),
                KeyEncodingString = null,
                AesKeySize = AesKeySize.Aes256,
                // Public property set to Unicode; proxy intentionally left at its UTF-8 default.
                PlaintextEncoding = MakeEncodingArg(Encoding.Unicode),
            };

            var args = new Dictionary<string, object>
            {
                [nameof(EncryptText.Input)] = Plaintext,
                [nameof(EncryptText.Key)] = Password,
                [nameof(EncryptText.KdfIterations)] = iterations,
            };

            string encryptedBase64;
            try
            {
                encryptedBase64 = (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)];
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw tie.InnerException;
            }

            byte[] decrypted = BclOpenSslEnc.DecryptAesCbc(Convert.FromBase64String(encryptedBase64), Password, iterations, 32);

            // Unicode (UTF-16 LE) bytes, NOT the proxy's UTF-8 default.
            Assert.Equal(Encoding.Unicode.GetBytes(Plaintext), decrypted);
            Assert.NotEqual(Encoding.UTF8.GetBytes(Plaintext), decrypted);
        }

        [Fact]
        public void OpenSslEnc_AesCbc_PublicKeyEncodingWinsOverProxyDefault()
        {
            // Twin of the plaintext regression above, on the key/password side [STUD-80559]: setting the public
            // key Encoding InArgument must govern key derivation even though the hidden KeyEncodingString proxy
            // still carries its UTF-8 constructor default. Earlier the non-null proxy won and silently used UTF-8.
            // UTF-16 vs UTF-8 password bytes differ even for an ASCII password, so the derived key changes.
            const int iterations = 600_000;

            var encrypt = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.AES,
                Format = SymmetricWireFormat.OpenSslEnc,
                KeyFormat = KeyBytesFormat.Encoded,
                AesKeySize = AesKeySize.Aes256,
                // Public key Encoding set to UTF-16; KeyEncodingString proxy intentionally left at its UTF-8 default.
                Encoding = MakeEncodingArg(Encoding.Unicode),
            };
            var encryptArgs = new Dictionary<string, object>
            {
                [nameof(EncryptText.Input)] = Plaintext,
                [nameof(EncryptText.Key)] = Password,
                [nameof(EncryptText.KdfIterations)] = iterations,
            };

            string encryptedBase64;
            try
            {
                encryptedBase64 = (string)new WorkflowInvoker(encrypt).Invoke(encryptArgs)[nameof(encrypt.Result)];
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw tie.InnerException;
            }

            byte[] blob = Convert.FromBase64String(encryptedBase64);

            // Positive: a UiPath round-trip with the same UTF-16 key Encoding (proxy left at default) recovers the plaintext.
            var decrypt = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.AES,
                Format = SymmetricWireFormat.OpenSslEnc,
                KeyFormat = KeyBytesFormat.Encoded,
                AesKeySize = AesKeySize.Aes256,
                Encoding = MakeEncodingArg(Encoding.Unicode),
            };
            var decryptArgs = new Dictionary<string, object>
            {
                [nameof(DecryptText.Input)] = encryptedBase64,
                [nameof(DecryptText.Key)] = Password,
                [nameof(DecryptText.KdfIterations)] = iterations,
            };
            string roundTripped;
            try
            {
                roundTripped = (string)new WorkflowInvoker(decrypt).Invoke(decryptArgs)[nameof(decrypt.Result)];
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw tie.InnerException;
            }
            Assert.Equal(Plaintext, roundTripped);

            // Negative cross-check: the BCL counterpart derives the key from UTF-8 password bytes (lines 348/373),
            // so it cannot decrypt a blob whose key came from UTF-16 bytes — the wrong key fails PKCS7 unpadding.
            // If the proxy default had won (the pre-fix bug), the key would be UTF-8 and this would SUCCEED.
            Assert.Throws<CryptographicException>(() => BclOpenSslEnc.DecryptAesCbc(blob, Password, iterations, 32));
        }

        [Fact]
        public void OpenSslEnc_AesCbc_KeyEncodingDoesNotAffectPlaintextBytes()
        {
            // Decoupling: set the key/password Encoding to windows-1252 while leaving PlaintextEncoding
            // at its UTF-8 default. The password is ASCII, so windows-1252 and UTF-8 derive the same key
            // and the BCL counterpart (UTF-8 password) still decrypts. The decoded bytes must be the
            // UTF-8 representation of the plaintext — i.e. the key Encoding did not leak into the plaintext.
            const int iterations = 600_000;
            var windows1252 = Encoding.GetEncoding(1252);

            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: windows1252,
                iterations: iterations, aesKeySize: AesKeySize.Aes256);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclOpenSslEnc.DecryptAesCbc(blob, Password, iterations, 32);

            Assert.Equal(Encoding.UTF8.GetBytes(Plaintext), decrypted);
        }

        // ────────────────────────────────────────────────────────────────────────
        // OpenSslEnc — AES-GCM, bidirectional. This is a UiPath extension of the
        // OpenSSL layout (the magic prefix + PBKDF2-SHA256 derivation, but with AEAD
        // ciphertext + trailing tag). Both halves must agree because the only
        // canonical consumer of this hybrid is "another copy of UiPath".
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void OpenSslEnc_AesGcm_ExternalToInternal()
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(Plaintext);
            byte[] externalBlob = BclOpenSslEnc.EncryptAesGcm(plainBytes, Password, 600_000);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, input: Convert.ToBase64String(externalBlob),
                inputEncoding: Encoding.UTF8);

            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void OpenSslEnc_AesGcm_InternalToExternal()
        {
            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclOpenSslEnc.DecryptAesGcm(blob, Password, 600_000);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(decrypted));
        }

        // ────────────────────────────────────────────────────────────────────────
        // Classic — UiPath's frozen wire format. The Classic format is NOT a public
        // standard so there's no canonical external tool, but the round-trip via an
        // independent BCL implementation still proves the layout is stable: a future
        // refactor that quietly reorders salt/IV/ciphertext would break the BCL counterpart.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Classic_AesCbc_ExternalToInternal()
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(Plaintext);
            byte[] externalBlob = BclClassic.EncryptAesCbc(plainBytes, Password, iterations: 10_000);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded,
                password: Password, input: Convert.ToBase64String(externalBlob),
                inputEncoding: Encoding.UTF8);

            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void Classic_AesCbc_InternalToExternal()
        {
            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclClassic.DecryptAesCbc(blob, Password, iterations: 10_000);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void Classic_AesGcm_ExternalToInternal()
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(Plaintext);
            byte[] externalBlob = BclClassic.EncryptAesGcm(plainBytes, Password, iterations: 10_000);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded,
                password: Password, input: Convert.ToBase64String(externalBlob),
                inputEncoding: Encoding.UTF8);

            Assert.Equal(Plaintext, decrypted);
        }

        [Fact]
        public void Classic_AesGcm_InternalToExternal()
        {
            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded,
                password: Password, inputEncoding: Encoding.UTF8);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclClassic.DecryptAesGcm(blob, Password, iterations: 10_000);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(decrypted));
        }

        // ────────────────────────────────────────────────────────────────────────
        // Raw — bidirectional with caller-supplied key and IV. The existing
        // SymmetricInteropTests covers one direction for AES and AES-GCM; here we
        // add the reverse for AES-GCM (BCL produces, activity consumes) and pin the
        // CBC byte layout (IV ‖ ct, no padding tricks).
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Raw_AesGcm_InternalToExternal()
        {
            byte[] keyBytes = new byte[32];
            RandomNumberGenerator.Fill(keyBytes);
            string hexKey = Convert.ToHexString(keyBytes);

            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, KeyBytesFormat.Hex,
                key: hexKey, inputEncoding: Encoding.UTF8);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclRaw.DecryptAesGcm(blob, keyBytes);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void Raw_AesCbc_InternalToExternal_ExplicitIv()
        {
            byte[] keyBytes = new byte[32];
            byte[] ivBytes = new byte[16];
            RandomNumberGenerator.Fill(keyBytes);
            RandomNumberGenerator.Fill(ivBytes);

            string encryptedBase64 = RunEncryptText(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex,
                key: Convert.ToHexString(keyBytes),
                iv: Convert.ToHexString(ivBytes),
                inputEncoding: Encoding.UTF8);

            byte[] blob = Convert.FromBase64String(encryptedBase64);
            byte[] decrypted = BclRaw.DecryptAesCbc(blob, keyBytes);

            Assert.Equal(Plaintext, Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void Raw_AesGcm_ExternalToInternal()
        {
            byte[] keyBytes = new byte[32];
            RandomNumberGenerator.Fill(keyBytes);
            byte[] plainBytes = Encoding.UTF8.GetBytes(Plaintext);

            byte[] externalBlob = BclRaw.EncryptAesGcm(plainBytes, keyBytes);

            string decrypted = RunDecryptText(
                EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, KeyBytesFormat.Hex,
                key: Convert.ToHexString(keyBytes),
                input: Convert.ToBase64String(externalBlob),
                inputEncoding: Encoding.UTF8);

            Assert.Equal(Plaintext, decrypted);
        }

        // ────────────────────────────────────────────────────────────────────────
        // BCL counterpart implementations — strictly from the wire-format spec.
        // DO NOT call into CryptographyHelper here; the value of these tests is
        // that they are an independent implementation.
        // ────────────────────────────────────────────────────────────────────────

        private static class BclOpenSslEnc
        {
            // "Salted__" — same prefix `openssl enc` writes.
            private static readonly byte[] Magic = Encoding.ASCII.GetBytes("Salted__");
            private const int SaltSize = 8;
            private const int AesCbcKeySize = 32;
            private const int AesCbcIvSize = 16;
            private const int AeadKeySize = 32;
            private const int AeadIvSize = 12;
            private const int AeadTagSize = 16;

            public static byte[] EncryptAesCbc(byte[] plain, string password, int iterations, int keySizeBytes = AesCbcKeySize)
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                (byte[] key, byte[] iv) = DeriveKeyAndIv(passwordBytes, salt, iterations, keySizeBytes, AesCbcIvSize);

                byte[] cipher;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using var ms = new MemoryStream();
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        cs.Write(plain, 0, plain.Length);
                    cipher = ms.ToArray();
                }

                return Concat(Magic, salt, cipher);
            }

            public static byte[] DecryptAesCbc(byte[] blob, string password, int iterations, int keySizeBytes = AesCbcKeySize)
            {
                AssertMagicPrefix(blob);
                byte[] salt = blob.AsSpan(Magic.Length, SaltSize).ToArray();
                byte[] cipher = blob.AsSpan(Magic.Length + SaltSize).ToArray();

                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                (byte[] key, byte[] iv) = DeriveKeyAndIv(passwordBytes, salt, iterations, keySizeBytes, AesCbcIvSize);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var inMs = new MemoryStream(cipher);
                using var cs = new CryptoStream(inMs, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var outMs = new MemoryStream();
                cs.CopyTo(outMs);
                return outMs.ToArray();
            }

            public static byte[] EncryptAesGcm(byte[] plain, string password, int iterations)
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                (byte[] key, byte[] iv) = DeriveKeyAndIv(passwordBytes, salt, iterations, AeadKeySize, AeadIvSize);

                byte[] cipher = new byte[plain.Length];
                byte[] tag = new byte[AeadTagSize];
                using (var aesGcm = new AesGcm(key))
                    aesGcm.Encrypt(iv, plain, cipher, tag);

                return Concat(Magic, salt, cipher, tag);
            }

            public static byte[] DecryptAesGcm(byte[] blob, string password, int iterations)
            {
                AssertMagicPrefix(blob);
                byte[] salt = blob.AsSpan(Magic.Length, SaltSize).ToArray();
                int cipherStart = Magic.Length + SaltSize;
                int cipherLen = blob.Length - cipherStart - AeadTagSize;
                byte[] cipher = blob.AsSpan(cipherStart, cipherLen).ToArray();
                byte[] tag = blob.AsSpan(cipherStart + cipherLen, AeadTagSize).ToArray();

                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                (byte[] key, byte[] iv) = DeriveKeyAndIv(passwordBytes, salt, iterations, AeadKeySize, AeadIvSize);

                byte[] plain = new byte[cipherLen];
                using var aesGcm = new AesGcm(key);
                aesGcm.Decrypt(iv, cipher, tag, plain);
                return plain;
            }

            private static (byte[] key, byte[] iv) DeriveKeyAndIv(byte[] password, byte[] salt, int iterations, int keySize, int ivSize)
            {
                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                byte[] derived = pbkdf2.GetBytes(keySize + ivSize);
                byte[] key = derived.AsSpan(0, keySize).ToArray();
                byte[] iv = derived.AsSpan(keySize, ivSize).ToArray();
                return (key, iv);
            }

            private static void AssertMagicPrefix(byte[] blob)
            {
                if (blob.Length < Magic.Length || !blob.AsSpan(0, Magic.Length).SequenceEqual(Magic))
                    throw new CryptographicException("Missing 'Salted__' magic prefix.");
            }
        }

        private static class BclClassic
        {
            // Classic wire layout: salt(8) ‖ IV ‖ ciphertext [‖ tag(16)]. PBKDF2-HMAC-SHA1.
            // The IV is fresh-random per encryption (NOT derived from the password).
            private const int SaltSize = 8;
            private const int AesCbcKeySize = 32;
            private const int AesCbcIvSize = 16;
            private const int AeadKeySize = 32;
            private const int AeadIvSize = 12;
            private const int AeadTagSize = 16;

            public static byte[] EncryptAesCbc(byte[] plain, string password, int iterations)
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] key = DeriveKey(passwordBytes, salt, iterations, AesCbcKeySize);

                byte[] iv = RandomNumberGenerator.GetBytes(AesCbcIvSize);
                byte[] cipher;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using var ms = new MemoryStream();
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        cs.Write(plain, 0, plain.Length);
                    cipher = ms.ToArray();
                }

                return Concat(salt, iv, cipher);
            }

            public static byte[] DecryptAesCbc(byte[] blob, string password, int iterations)
            {
                byte[] salt = blob.AsSpan(0, SaltSize).ToArray();
                byte[] iv = blob.AsSpan(SaltSize, AesCbcIvSize).ToArray();
                byte[] cipher = blob.AsSpan(SaltSize + AesCbcIvSize).ToArray();

                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] key = DeriveKey(passwordBytes, salt, iterations, AesCbcKeySize);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var inMs = new MemoryStream(cipher);
                using var cs = new CryptoStream(inMs, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var outMs = new MemoryStream();
                cs.CopyTo(outMs);
                return outMs.ToArray();
            }

            public static byte[] EncryptAesGcm(byte[] plain, string password, int iterations)
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] key = DeriveKey(passwordBytes, salt, iterations, AeadKeySize);

                byte[] iv = RandomNumberGenerator.GetBytes(AeadIvSize);
                byte[] cipher = new byte[plain.Length];
                byte[] tag = new byte[AeadTagSize];
                using (var aesGcm = new AesGcm(key))
                    aesGcm.Encrypt(iv, plain, cipher, tag);

                return Concat(salt, iv, cipher, tag);
            }

            public static byte[] DecryptAesGcm(byte[] blob, string password, int iterations)
            {
                byte[] salt = blob.AsSpan(0, SaltSize).ToArray();
                byte[] iv = blob.AsSpan(SaltSize, AeadIvSize).ToArray();
                int cipherStart = SaltSize + AeadIvSize;
                int cipherLen = blob.Length - cipherStart - AeadTagSize;
                byte[] cipher = blob.AsSpan(cipherStart, cipherLen).ToArray();
                byte[] tag = blob.AsSpan(cipherStart + cipherLen, AeadTagSize).ToArray();

                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] key = DeriveKey(passwordBytes, salt, iterations, AeadKeySize);

                byte[] plain = new byte[cipherLen];
                using var aesGcm = new AesGcm(key);
                aesGcm.Decrypt(iv, cipher, tag, plain);
                return plain;
            }

            private static byte[] DeriveKey(byte[] password, byte[] salt, int iterations, int keySize)
            {
                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA1);
                return pbkdf2.GetBytes(keySize);
            }
        }

        private static class BclRaw
        {
            // Raw wire layout: IV ‖ ciphertext [‖ tag(16) for AEAD]. No KDF — caller-supplied raw key.
            private const int AesCbcIvSize = 16;
            private const int AeadIvSize = 12;
            private const int AeadTagSize = 16;

            public static byte[] EncryptAesGcm(byte[] plain, byte[] key)
            {
                byte[] iv = RandomNumberGenerator.GetBytes(AeadIvSize);
                byte[] cipher = new byte[plain.Length];
                byte[] tag = new byte[AeadTagSize];
                using (var aesGcm = new AesGcm(key))
                    aesGcm.Encrypt(iv, plain, cipher, tag);

                return Concat(iv, cipher, tag);
            }

            public static byte[] DecryptAesGcm(byte[] blob, byte[] key)
            {
                byte[] iv = blob.AsSpan(0, AeadIvSize).ToArray();
                int cipherLen = blob.Length - AeadIvSize - AeadTagSize;
                byte[] cipher = blob.AsSpan(AeadIvSize, cipherLen).ToArray();
                byte[] tag = blob.AsSpan(AeadIvSize + cipherLen, AeadTagSize).ToArray();

                byte[] plain = new byte[cipherLen];
                using var aesGcm = new AesGcm(key);
                aesGcm.Decrypt(iv, cipher, tag, plain);
                return plain;
            }

            public static byte[] DecryptAesCbc(byte[] blob, byte[] key)
            {
                byte[] iv = blob.AsSpan(0, AesCbcIvSize).ToArray();
                byte[] cipher = blob.AsSpan(AesCbcIvSize).ToArray();

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var inMs = new MemoryStream(cipher);
                using var cs = new CryptoStream(inMs, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var outMs = new MemoryStream();
                cs.CopyTo(outMs);
                return outMs.ToArray();
            }
        }

        private static byte[] Concat(params byte[][] arrays)
        {
            int len = 0;
            foreach (byte[] a in arrays) len += a.Length;
            byte[] result = new byte[len];
            int offset = 0;
            foreach (byte[] a in arrays)
            {
                Buffer.BlockCopy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }
            return result;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Activity-surface helpers — match the pattern used in SymmetricInteropTests.
        // ────────────────────────────────────────────────────────────────────────

        private static InArgument<Encoding> MakeEncodingArg(Encoding e)
        {
            if (e == Encoding.UTF8) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.UTF8));
            if (e == Encoding.Unicode || e == null) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.Unicode));
            if (e.CodePage == 1252) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.GetEncoding(1252)));
            throw new ArgumentException($"Test helper only supports UTF-8, Unicode, and windows-1252; got {e.WebName}");
        }

        private static string RunEncryptText(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            KeyBytesFormat keyFormat,
            string password = null,
            string key = null,
            string iv = null,
            int iterations = 0,
            Encoding inputEncoding = null,
            Encoding plaintextEncoding = null,
            AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            var activity = new EncryptText
            {
                Algorithm = algorithm,
                Format = format,
                KeyFormat = keyFormat,
                Encoding = MakeEncodingArg(inputEncoding),
                KeyEncodingString = null,
                AesKeySize = aesKeySize,
            };
            // When unset, the constructor default (PlaintextEncodingString = UTF-8) applies, keeping
            // existing tests byte-stable. When set, only the public PlaintextEncoding InArgument is
            // assigned — the PlaintextEncodingString proxy is intentionally left at its UTF-8 default to
            // prove the public surface wins over the proxy without callers having to clear it (STUD-80530).
            if (plaintextEncoding != null)
            {
                activity.PlaintextEncoding = MakeEncodingArg(plaintextEncoding);
            }

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
            Encoding inputEncoding = null,
            Encoding plaintextEncoding = null,
            AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            var activity = new DecryptText
            {
                Algorithm = algorithm,
                Format = format,
                KeyFormat = keyFormat,
                Encoding = MakeEncodingArg(inputEncoding),
                KeyEncodingString = null,
                AesKeySize = aesKeySize,
            };
            // When unset, the constructor default (PlaintextEncodingString = UTF-8) applies, keeping
            // existing tests byte-stable. When set, only the public PlaintextEncoding InArgument is
            // assigned — the PlaintextEncodingString proxy is intentionally left at its UTF-8 default to
            // prove the public surface wins over the proxy without callers having to clear it (STUD-80530).
            if (plaintextEncoding != null)
            {
                activity.PlaintextEncoding = MakeEncodingArg(plaintextEncoding);
            }

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
    }
}
