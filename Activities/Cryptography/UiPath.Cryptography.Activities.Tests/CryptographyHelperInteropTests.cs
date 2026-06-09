using System;
using System.Security.Cryptography;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // obsolete algorithms reachable via opt-in formats

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Byte-layout + round-trip coverage of the helper-level wire-format methods on
    /// <see cref="CryptographyHelper"/>. <see cref="SymmetricInteropTests"/> exercises
    /// these through the activity surface; these tests focus on byte-exact assertions
    /// the activity-level pass cannot make.
    /// </summary>
    public class CryptographyHelperInteropTests
    {
        private static readonly byte[] Plain = Encoding.UTF8.GetBytes("helper-interop round-trip 0123456789");
        private static readonly byte[] Password = Encoding.UTF8.GetBytes("helper-interop-pwd");

        // ────────────────────────────────────────────────────────────────────────
        // Owasp2026 — uses Classic wire layout, caller-supplied PBKDF2-HMAC-SHA1 iter.
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(EncryptionAlgorithm.AES, 75_000)]
        [InlineData(EncryptionAlgorithm.AESGCM, 75_000)]
        [InlineData(EncryptionAlgorithm.AES, 1_300_000)]
        public void EncryptDataWithIterations_RoundTrips(EncryptionAlgorithm algorithm, int iterations)
        {
            byte[] cipher = CryptographyHelper.EncryptDataWithIterations(algorithm, Plain, Password, iterations);
            byte[] plain = CryptographyHelper.DecryptDataWithIterations(algorithm, cipher, Password, iterations);
            plain.ShouldBe(Plain);
        }

        // Iteration count is NOT carried in the blob — caller must supply matching values.
        // Pin that a mismatched iter on decrypt fails (AEAD via tag check, non-AEAD via padding).
        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        public void EncryptDataWithIterations_MismatchedIterationsFails(EncryptionAlgorithm algorithm)
        {
            byte[] cipher = CryptographyHelper.EncryptDataWithIterations(algorithm, Plain, Password, 50_000);
            Should.Throw<CryptographicException>(() =>
                CryptographyHelper.DecryptDataWithIterations(algorithm, cipher, Password, 60_000));
        }

        // ────────────────────────────────────────────────────────────────────────
        // OpenSslEnc — Salted__ magic prefix at byte 0; salt randomised per call.
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptDataOpenSslEnc_StartsWithSaltedMagic(EncryptionAlgorithm algorithm)
        {
            byte[] blob = CryptographyHelper.EncryptDataOpenSslEnc(algorithm, Plain, Password, iterations: 50_000);
            Encoding.ASCII.GetString(blob.AsSpan(0, 8).ToArray()).ShouldBe("Salted__");
        }

        [Fact]
        public void EncryptDataOpenSslEnc_FreshSaltPerCall()
        {
            byte[] a = CryptographyHelper.EncryptDataOpenSslEnc(EncryptionAlgorithm.AES, Plain, Password, 50_000);
            byte[] b = CryptographyHelper.EncryptDataOpenSslEnc(EncryptionAlgorithm.AES, Plain, Password, 50_000);

            // Same input, same password, same iter → fresh salt yields different salt bytes (and
            // therefore different ciphertext). Bytes [8..16] are the salt.
            a.AsSpan(8, 8).ToArray().ShouldNotBe(b.AsSpan(8, 8).ToArray());
            a.ShouldNotBe(b);
        }

        // Stripping or corrupting the magic prefix makes the blob unreadable.
        [Fact]
        public void DecryptDataOpenSslEnc_MissingMagic_Throws()
        {
            byte[] blob = CryptographyHelper.EncryptDataOpenSslEnc(EncryptionAlgorithm.AES, Plain, Password, 50_000);
            blob[0] ^= 0x01; // corrupt the first byte of "Salted__"

            Should.Throw<CryptographicException>(() =>
                CryptographyHelper.DecryptDataOpenSslEnc(EncryptionAlgorithm.AES, blob, Password, 50_000));
        }

        // ────────────────────────────────────────────────────────────────────────
        // Raw — caller-supplied key + IV. AEAD layout: IV(12) ‖ ct ‖ tag(16);
        // non-AEAD: IV(16 for AES) ‖ ct (CBC padded).
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void EncryptDataRaw_AesGcm_LayoutIsIv12PlusCipherPlusTag16()
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[12];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(iv);

            byte[] blob = CryptographyHelper.EncryptDataRaw(EncryptionAlgorithm.AESGCM, Plain, key, iv);

            // AEAD ciphertext length equals plaintext length (no block padding); IV(12) ‖ ct ‖ tag(16).
            blob.Length.ShouldBe(12 + Plain.Length + 16);
            blob.AsSpan(0, 12).ToArray().ShouldBe(iv);
        }

        [Fact]
        public void EncryptDataRaw_AesCbc_LayoutIsIv16PlusPaddedCiphertext()
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(iv);

            byte[] blob = CryptographyHelper.EncryptDataRaw(EncryptionAlgorithm.AES, Plain, key, iv);

            // CBC pads plaintext up to a 16-byte block boundary. The padded length is the next
            // multiple of 16 strictly greater than Plain.Length (PKCS#7 always adds at least one byte).
            int expectedCtLen = ((Plain.Length / 16) + 1) * 16;
            blob.Length.ShouldBe(16 + expectedCtLen);
            blob.AsSpan(0, 16).ToArray().ShouldBe(iv);
        }

        // AEAD IV size must be exactly 12 bytes — pin the validation message so a caller
        // who supplies a 16-byte IV (a CBC-style mistake) gets a clear failure.
        [Fact]
        public void EncryptDataRaw_AeadWith16ByteIv_Throws()
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16]; // wrong size for AEAD
            Should.Throw<ArgumentException>(() =>
                CryptographyHelper.EncryptDataRaw(EncryptionAlgorithm.AESGCM, Plain, key, iv));
        }

        [Fact]
        public void EncryptDataRaw_NullIv_GeneratesFresh()
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);

            byte[] a = CryptographyHelper.EncryptDataRaw(EncryptionAlgorithm.AESGCM, Plain, key, iv: null);
            byte[] b = CryptographyHelper.EncryptDataRaw(EncryptionAlgorithm.AESGCM, Plain, key, iv: null);

            // Different IV each call → different ciphertext, and the IV-prefix bytes differ.
            a.AsSpan(0, 12).ToArray().ShouldNotBe(b.AsSpan(0, 12).ToArray());
        }

        // ────────────────────────────────────────────────────────────────────────
        // Cross-format failure — using Classic ciphertext with an OpenSslEnc decrypt
        // call must fail at the magic-prefix check before any KDF runs.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void DecryptDataOpenSslEnc_ClassicCiphertext_FailsAtMagicCheck()
        {
            byte[] classicBlob = CryptographyHelper.EncryptData(EncryptionAlgorithm.AES, Plain, Password);
            Should.Throw<CryptographicException>(() =>
                CryptographyHelper.DecryptDataOpenSslEnc(EncryptionAlgorithm.AES, classicBlob, Password, 600_000));
        }

        // ────────────────────────────────────────────────────────────────────────
        // GetRawKeySizes — AEAD always wants 32 bytes; non-AEAD reports the cipher's
        // legal sizes (AES: 16, 24, 32).
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void GetRawKeySizes_Aead_ReturnsOnly32()
        {
            CryptographyHelper.GetRawKeySizes(EncryptionAlgorithm.AESGCM).ShouldBe(new[] { 32 });
        }

        [Fact]
        public void GetRawKeySizes_Aes_ReturnsLegalSizes()
        {
            CryptographyHelper.GetRawKeySizes(EncryptionAlgorithm.AES).ShouldContain(16);
            CryptographyHelper.GetRawKeySizes(EncryptionAlgorithm.AES).ShouldContain(24);
            CryptographyHelper.GetRawKeySizes(EncryptionAlgorithm.AES).ShouldContain(32);
        }

        // ────────────────────────────────────────────────────────────────────────
        // ParseKeyBytes — hex/base64 routing. Tolerance is covered separately in
        // RawKeyTests; here we pin error paths the higher-level model class doesn't reach.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void ParseKeyBytes_Encoded_RequiresEncoding()
        {
            Should.Throw<ArgumentNullException>(() =>
                CryptographyHelper.ParseKeyBytes("password", keySecureString: null, KeyBytesFormat.Encoded, encoding: null));
        }

        [Fact]
        public void ParseKeyBytes_Hex_EmptyInput_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                CryptographyHelper.ParseKeyBytes(string.Empty, keySecureString: null, KeyBytesFormat.Hex, encoding: null));
        }

        [Fact]
        public void ParseKeyBytes_Base64_EmptyInput_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                CryptographyHelper.ParseKeyBytes(string.Empty, keySecureString: null, KeyBytesFormat.Base64, encoding: null));
        }

        [Fact]
        public void ParseKeyBytes_UnknownFormat_Throws()
        {
            Should.Throw<ArgumentOutOfRangeException>(() =>
                CryptographyHelper.ParseKeyBytes("xx", keySecureString: null, (KeyBytesFormat)999, encoding: null));
        }
    }
}

#pragma warning restore CS0618
