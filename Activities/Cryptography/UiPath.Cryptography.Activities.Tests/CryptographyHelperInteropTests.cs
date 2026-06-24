using System;
using System.Security;
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

        // ────────────────────────────────────────────────────────────────────────
        // Base64 leniency (STUD-80535) — the entry surface tolerates the artefacts a
        // third-party tool actually produces, mirroring the Hex pre-cleaner.
        //   Newly enabled by FromBase64String (threw FormatException before the fix):
        //     URL-safe alphabet and missing padding.
        //   Compatibility guards (already accepted — Convert.FromBase64String ignores
        //     embedded whitespace/CR/LF — pinned so the pre-cleaner keeps tolerating them):
        //     line wraps and surrounding whitespace.
        // ────────────────────────────────────────────────────────────────────────

        private static readonly byte[] Key32 = new byte[]
        {
            0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
            16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
        };

        [Fact]
        public void ParseKeyBytes_Base64_ToleratesLineWrap()
        {
            // openssl wraps base64 output and the CLI appends a trailing newline. A 32-byte
            // key is 44 chars (doesn't wrap naturally) so inject the wrap manually, plus a
            // trailing newline, mimicking what lands in the key field after copy-paste.
            // Convert.FromBase64String already ignores these newlines; this pins that the
            // pre-cleaner does not regress that behavior.
            string b64 = Convert.ToBase64String(Key32);
            string wrapped = b64.Substring(0, 22) + "\n" + b64.Substring(22) + "\n";
            byte[] result = CryptographyHelper.ParseKeyBytes(wrapped, keySecureString: null, KeyBytesFormat.Base64, encoding: null);
            result.ShouldBe(Key32);
        }

        [Fact]
        public void ParseKeyBytes_Base64_ToleratesUrlSafeAlphabet()
        {
            // Bytes chosen so the standard encoding contains both '+' and '/', which the
            // URL-safe alphabet renders as '-' and '_'.
            byte[] expected = new byte[] { 0xFB, 0xFF, 0xBF, 0x00, 0x10, 0x83 };
            string standard = Convert.ToBase64String(expected);
            standard.ShouldContain("+");
            standard.ShouldContain("/");
            string urlSafe = standard.Replace('+', '-').Replace('/', '_');
            byte[] result = CryptographyHelper.ParseKeyBytes(urlSafe, keySecureString: null, KeyBytesFormat.Base64, encoding: null);
            result.ShouldBe(expected);
        }

        [Fact]
        public void ParseKeyBytes_Base64_ToleratesSurroundingWhitespace()
        {
            // Compatibility guard: Convert.FromBase64String already tolerates surrounding
            // whitespace/CR/LF; this pins that the pre-cleaner keeps accepting it.
            string padded = "  \t" + Convert.ToBase64String(Key32) + " \r\n";
            byte[] result = CryptographyHelper.ParseKeyBytes(padded, keySecureString: null, KeyBytesFormat.Base64, encoding: null);
            result.ShouldBe(Key32);
        }

        [Fact]
        public void ParseKeyBytes_Base64_RestoresMissingPadding()
        {
            // rem == 2 stem (one byte → "AQ==") and rem == 3 stem (two bytes → "AQI=").
            byte[] oneByte = CryptographyHelper.ParseKeyBytes("AQ", keySecureString: null, KeyBytesFormat.Base64, encoding: null);
            oneByte.ShouldBe(new byte[] { 1 });
            byte[] twoBytes = CryptographyHelper.ParseKeyBytes("AQI", keySecureString: null, KeyBytesFormat.Base64, encoding: null);
            twoBytes.ShouldBe(new byte[] { 1, 2 });
        }

        [Fact]
        public void ParseKeyBytes_Base64_GarbageCharacters_StillThrows()
        {
            // Leniency strips whitespace and remaps the URL-safe alphabet only — characters
            // outside the base64 alphabet must still be rejected by the final strict decode.
            Should.Throw<FormatException>(() =>
                CryptographyHelper.ParseKeyBytes("not base64 @#$%!", keySecureString: null, KeyBytesFormat.Base64, encoding: null));
        }

        [Fact]
        public void ParseKeyBytes_Base64_InvalidLengthStem_StillThrows()
        {
            // A length-mod-4 == 1 stem can never be valid base64; the pre-cleaner leaves it for
            // Convert.FromBase64String to reject, so malformed input still surfaces as
            // FormatException rather than being silently accepted via fabricated padding.
            Should.Throw<FormatException>(() =>
                CryptographyHelper.ParseKeyBytes("AQID Z", keySecureString: null, KeyBytesFormat.Base64, encoding: null));
        }

        [Fact]
        public void ParseKeyBytes_Base64_SecureStringPath_IsLenient()
        {
            // The SecureString key path is decoded without ever materializing a managed string
            // (STUD-80531); it must still apply the same leniency as the keyString path. Feed a
            // URL-safe, unpadded form through the SecureString overload and confirm it decodes to
            // the same bytes as the standard padded form.
            byte[] expected = new byte[] { 0xFB, 0xFF, 0xBF, 0x00, 0x10, 0x83, 0x01 };
            string urlSafeUnpadded = Convert.ToBase64String(expected)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            SecureString secure = TestingHelper.StringToSecureString(urlSafeUnpadded);
            byte[] result = CryptographyHelper.ParseKeyBytes(keyString: null, secure, KeyBytesFormat.Base64, encoding: null);
            result.ShouldBe(expected);
        }
    }
}

#pragma warning restore CS0618
