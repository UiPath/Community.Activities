using System;
using System.Security;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // obsolete algorithms reachable via opt-in formats

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Direct unit coverage for <see cref="SymmetricInteropHelper"/>. Today the helper
    /// is exercised only indirectly through EncryptText/DecryptText activities; testing
    /// it head-on pins every cross-property invariant and routing decision with a clear
    /// failure message.
    /// </summary>
    public class SymmetricInteropHelperTests
    {
        // ────────────────────────────────────────────────────────────────────────
        // ValidateInteropSettings — cross-property invariants
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Validate_Raw_WithEncodedKeyFormat_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                SymmetricInteropHelper.ValidateInteropSettings(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Encoded,
                    ivString: null, kdfIterations: 0, rawKeyLengthBytes: null));
        }

        [Theory]
        [InlineData(SymmetricWireFormat.Classic, KeyBytesFormat.Hex)]
        [InlineData(SymmetricWireFormat.Classic, KeyBytesFormat.Base64)]
        [InlineData(SymmetricWireFormat.Owasp2026, KeyBytesFormat.Hex)]
        [InlineData(SymmetricWireFormat.Owasp2026, KeyBytesFormat.Base64)]
        [InlineData(SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Hex)]
        [InlineData(SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Base64)]
        public void Validate_NonRaw_WithHexOrBase64_Throws(SymmetricWireFormat format, KeyBytesFormat keyFormat)
        {
            Should.Throw<ArgumentException>(() =>
                SymmetricInteropHelper.ValidateInteropSettings(
                    EncryptionAlgorithm.AES, format, keyFormat,
                    ivString: null, kdfIterations: 0, rawKeyLengthBytes: null));
        }

        [Theory]
        [InlineData(SymmetricWireFormat.Classic)]
        [InlineData(SymmetricWireFormat.Owasp2026)]
        [InlineData(SymmetricWireFormat.OpenSslEnc)]
        public void Validate_NonRaw_WithIv_Throws(SymmetricWireFormat format)
        {
            Should.Throw<ArgumentException>(() =>
                SymmetricInteropHelper.ValidateInteropSettings(
                    EncryptionAlgorithm.AES, format, KeyBytesFormat.Encoded,
                    ivString: "AABBCCDDEEFF00112233445566778899", kdfIterations: 0, rawKeyLengthBytes: null));
        }

        [Theory]
        [InlineData(SymmetricWireFormat.Classic)]
        [InlineData(SymmetricWireFormat.Raw)]
        public void Validate_KdfIterations_OnClassicOrRaw_Throws(SymmetricWireFormat format)
        {
            // Use a key-format that's legal for the chosen wire format so we only trip the iter rule.
            KeyBytesFormat kf = format == SymmetricWireFormat.Raw ? KeyBytesFormat.Hex : KeyBytesFormat.Encoded;
            Should.Throw<ArgumentException>(() =>
                SymmetricInteropHelper.ValidateInteropSettings(
                    EncryptionAlgorithm.AES, format, kf,
                    ivString: null, kdfIterations: 100_000, rawKeyLengthBytes: null));
        }

        // Boundary on MinKdfIterations (1000) — 999 throws, 1000 passes, 1001 passes.
        // Negative iterations also throw (only 0 means "use the recommended default"; without this,
        // a caller passing -1 would silently bypass the floor and run at the default iter count).
        [Theory]
        [InlineData(999, true)]
        [InlineData(1_000, false)]
        [InlineData(1_001, false)]
        [InlineData(-1, true)]
        [InlineData(int.MinValue, true)]
        public void Validate_KdfIterations_AtFloor(int iterations, bool shouldThrow)
        {
            Action act = () => SymmetricInteropHelper.ValidateInteropSettings(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded,
                ivString: null, kdfIterations: iterations, rawKeyLengthBytes: null);

            if (shouldThrow) Should.Throw<ArgumentException>(act);
            else Should.NotThrow(act);
        }

        // STUD-80534: the positive-but-below-minimum floor is an encrypt-only guard. On decrypt
        // (isDecrypt: true) a low iteration count chosen by a third-party producer must be honoured,
        // so 999 no longer throws. Negative iterations have no legitimate use case and stay rejected
        // in both directions; zero still means "use the format default" in both directions.
        [Theory]
        [InlineData(999, false)]
        [InlineData(1_000, false)]
        [InlineData(0, false)]
        [InlineData(-1, true)]
        [InlineData(int.MinValue, true)]
        public void Validate_KdfIterations_AtFloor_DecryptSkipsPositiveFloor(int iterations, bool shouldThrow)
        {
            Action act = () => SymmetricInteropHelper.ValidateInteropSettings(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded,
                ivString: null, kdfIterations: iterations, rawKeyLengthBytes: null, isDecrypt: true);

            if (shouldThrow) Should.Throw<ArgumentException>(act);
            else Should.NotThrow(act);
        }

        [Fact]
        public void Validate_Raw_WithWrongKeyLength_ThrowsWithLegalSizesInMessage()
        {
            // AES legal raw sizes: 16, 24, 32. Supply 10 (illegal).
            var ex = Should.Throw<ArgumentException>(() =>
                SymmetricInteropHelper.ValidateInteropSettings(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.Raw, KeyBytesFormat.Hex,
                    ivString: null, kdfIterations: 0, rawKeyLengthBytes: 10));

            // The user can self-serve if the message names the legal sizes.
            ex.Message.ShouldContain("16");
            ex.Message.ShouldContain("24");
            ex.Message.ShouldContain("32");
        }

        // Happy path: a fully-valid setting tuple does not throw.
        [Theory]
        [InlineData(SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, 0, null)]
        [InlineData(SymmetricWireFormat.Owasp2026, KeyBytesFormat.Encoded, 1_300_000, null)]
        [InlineData(SymmetricWireFormat.Raw, KeyBytesFormat.Hex, 0, 32)]
        [InlineData(SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded, 600_000, null)]
        public void Validate_ValidSettings_DoNotThrow(SymmetricWireFormat format, KeyBytesFormat keyFormat, int kdfIterations, int? rawKeyLengthBytes)
        {
            Should.NotThrow(() =>
                SymmetricInteropHelper.ValidateInteropSettings(
                    EncryptionAlgorithm.AES, format, keyFormat,
                    ivString: null, kdfIterations: kdfIterations, rawKeyLengthBytes: rawKeyLengthBytes));
        }

        // ────────────────────────────────────────────────────────────────────────
        // ParseKeyOrIv — value/SecureString/format combinations
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void ParseKeyOrIv_BothEmpty_ReturnsNull()
        {
            byte[] result = SymmetricInteropHelper.ParseKeyOrIv(value: null, secureValue: null, KeyBytesFormat.Encoded, Encoding.UTF8);
            result.ShouldBeNull();

            byte[] result2 = SymmetricInteropHelper.ParseKeyOrIv(value: string.Empty, secureValue: new SecureString(), KeyBytesFormat.Encoded, Encoding.UTF8);
            result2.ShouldBeNull();
        }

        [Fact]
        public void ParseKeyOrIv_PlainStringEncoded_ReturnsEncodingBytes()
        {
            byte[] result = SymmetricInteropHelper.ParseKeyOrIv("ăîș", secureValue: null, KeyBytesFormat.Encoded, Encoding.UTF8);
            result.ShouldBe(Encoding.UTF8.GetBytes("ăîș"));
        }

        [Fact]
        public void ParseKeyOrIv_SecureStringEncoded_ReturnsEncodingBytes()
        {
            byte[] result = SymmetricInteropHelper.ParseKeyOrIv(value: null, secureValue: ToSecureString("ăîș"), KeyBytesFormat.Encoded, Encoding.UTF8);
            result.ShouldBe(Encoding.UTF8.GetBytes("ăîș"));
        }

        [Fact]
        public void ParseKeyOrIv_PlainStringHex_ProducesBytes()
        {
            byte[] result = SymmetricInteropHelper.ParseKeyOrIv("0102FF", secureValue: null, KeyBytesFormat.Hex, encoding: null);
            result.ShouldBe(new byte[] { 0x01, 0x02, 0xFF });
        }

        // Hex via SecureString routes through ParseKeyBytes' string-free materialisation.
        // Pins that this path produces the same bytes as plain hex.
        [Fact]
        public void ParseKeyOrIv_SecureStringHex_MatchesPlainHex()
        {
            byte[] viaPlain = SymmetricInteropHelper.ParseKeyOrIv("DEADBEEF", secureValue: null, KeyBytesFormat.Hex, encoding: null);
            byte[] viaSecure = SymmetricInteropHelper.ParseKeyOrIv(value: null, secureValue: ToSecureString("DEADBEEF"), KeyBytesFormat.Hex, encoding: null);
            viaSecure.ShouldBe(viaPlain);
        }

        [Fact]
        public void ParseKeyOrIv_PlainStringBase64_ProducesBytes()
        {
            byte[] expected = new byte[] { 1, 2, 3, 4, 5 };
            byte[] result = SymmetricInteropHelper.ParseKeyOrIv(Convert.ToBase64String(expected), secureValue: null, KeyBytesFormat.Base64, encoding: null);
            result.ShouldBe(expected);
        }

        // Precedence: if both plain and secure are populated, plain wins (line 72/74 of helper).
        // Pinning this so a future refactor doesn't silently swap the precedence.
        [Fact]
        public void ParseKeyOrIv_BothSet_PlainStringWins()
        {
            byte[] result = SymmetricInteropHelper.ParseKeyOrIv("0102", secureValue: ToSecureString("FFFF"), KeyBytesFormat.Hex, encoding: null);
            result.ShouldBe(new byte[] { 0x01, 0x02 });
        }

        // ────────────────────────────────────────────────────────────────────────
        // STUD-80531 — SecureString → bytes must never round-trip through a managed
        // System.String. Pinned via the SecureStringHelpers materialisation seam:
        // every non-plain branch (Encoded / Hex / Base64) must route through the
        // unmanaged-buffer helper (which increments the counter), not the old
        // NetworkCredential.Password idiom (which left the secret on the GC heap and
        // never touched this counter). Each test also asserts byte-equivalence with the
        // plain-string path so the security fix can't silently corrupt the output.
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(KeyBytesFormat.Encoded)]
        [InlineData(KeyBytesFormat.Hex)]
        [InlineData(KeyBytesFormat.Base64)]
        public void ParseKeyOrIv_SecureString_RoutesThroughSecureHelper_NeverMaterialisesString(KeyBytesFormat format)
        {
            // A value that is simultaneously valid UTF-8 text, hex, and Base64.
            const string secret = "DEADBEEF";
            Encoding encoding = format == KeyBytesFormat.Encoded ? Encoding.UTF8 : null;

            byte[] viaPlain = SymmetricInteropHelper.ParseKeyOrIv(secret, secureValue: null, format, encoding);

            long before = SecureStringHelpers.MaterialisationCount;
            byte[] viaSecure = SymmetricInteropHelper.ParseKeyOrIv(value: null, secureValue: ToSecureString(secret), format, encoding);
            long after = SecureStringHelpers.MaterialisationCount;

            after.ShouldBe(before + 1, "the SecureString must be materialised through the string-free helper exactly once");
            viaSecure.ShouldBe(viaPlain);
        }

        [Fact]
        public void ParseKeyOrIv_PlainString_DoesNotMaterialiseThroughSecureHelper()
        {
            long before = SecureStringHelpers.MaterialisationCount;
            SymmetricInteropHelper.ParseKeyOrIv("DEADBEEF", secureValue: null, KeyBytesFormat.Hex, encoding: null);
            SecureStringHelpers.MaterialisationCount.ShouldBe(before, "a plain-string key must not touch the SecureString helper");
        }

        [Fact]
        public void CryptographyHelper_KeyEncoding_SecureString_MatchesPlainAndUsesSecureHelper()
        {
            const string secret = "ăîș-password";
            byte[] viaPlain = CryptographyHelper.KeyEncoding(Encoding.UTF8, secret, keySecureString: null);

            long before = SecureStringHelpers.MaterialisationCount;
            byte[] viaSecure = CryptographyHelper.KeyEncoding(Encoding.UTF8, key: null, ToSecureString(secret));

            SecureStringHelpers.MaterialisationCount.ShouldBe(before + 1);
            viaSecure.ShouldBe(viaPlain);
        }

        [Fact]
        public void CryptographyHelper_ParseKeyBytes_SecureStringBase64_MatchesPlainAndUsesSecureHelper()
        {
            string secret = Convert.ToBase64String(new byte[] { 9, 8, 7, 6, 5, 4 });
            byte[] viaPlain = CryptographyHelper.ParseKeyBytes(secret, keySecureString: null, KeyBytesFormat.Base64, encoding: null);

            long before = SecureStringHelpers.MaterialisationCount;
            byte[] viaSecure = CryptographyHelper.ParseKeyBytes(keyString: null, ToSecureString(secret), KeyBytesFormat.Base64, encoding: null);

            SecureStringHelpers.MaterialisationCount.ShouldBe(before + 1);
            viaSecure.ShouldBe(viaPlain);
        }

        // ────────────────────────────────────────────────────────────────────────
        // DispatchEncrypt / DispatchDecrypt — each switch arm round-trips, and
        // iter=0 picks the recommended default.
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(SymmetricWireFormat.Classic, 0)]
        [InlineData(SymmetricWireFormat.Owasp2026, 50_000)]
        [InlineData(SymmetricWireFormat.OpenSslEnc, 50_000)]
        public void Dispatch_PasswordBased_RoundTrips(SymmetricWireFormat format, int iterations)
        {
            byte[] plain = Encoding.UTF8.GetBytes("dispatch round-trip");
            byte[] password = Encoding.UTF8.GetBytes("dispatch-pwd");

            byte[] cipher = SymmetricInteropHelper.DispatchEncrypt(EncryptionAlgorithm.AES, format, iterations, password, ivBytes: null, plain);
            byte[] decrypted = SymmetricInteropHelper.DispatchDecrypt(EncryptionAlgorithm.AES, format, iterations, password, cipher);

            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Dispatch_Raw_RoundTrips()
        {
            byte[] plain = Encoding.UTF8.GetBytes("dispatch raw round-trip");
            byte[] key = new byte[32];
            for (int i = 0; i < 32; i++) key[i] = (byte)(i + 1);

            byte[] cipher = SymmetricInteropHelper.DispatchEncrypt(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, kdfIterations: 0, key, ivBytes: null, plain);
            byte[] decrypted = SymmetricInteropHelper.DispatchDecrypt(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Raw, kdfIterations: 0, key, cipher);

            decrypted.ShouldBe(plain);
        }

        // Encrypt with Owasp2026 + iter=0 (dispatch picks default 1_300_000) → decrypt
        // explicitly with 1_300_000 succeeds. Pins the default-fallback wiring.
        [Fact]
        public void Dispatch_Owasp2026_IterZero_PicksRecommendedDefault()
        {
            byte[] plain = Encoding.UTF8.GetBytes("default-iter check");
            byte[] password = Encoding.UTF8.GetBytes("dispatch-pwd");

            byte[] cipher = SymmetricInteropHelper.DispatchEncrypt(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Owasp2026, kdfIterations: 0, password, ivBytes: null, plain);
            byte[] decrypted = SymmetricInteropHelper.DispatchDecrypt(EncryptionAlgorithm.AESGCM, SymmetricWireFormat.Owasp2026, kdfIterations: 1_300_000, password, cipher);

            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Dispatch_OpenSslEnc_IterZero_PicksRecommendedDefault()
        {
            byte[] plain = Encoding.UTF8.GetBytes("default-iter check");
            byte[] password = Encoding.UTF8.GetBytes("dispatch-pwd");

            byte[] cipher = SymmetricInteropHelper.DispatchEncrypt(EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, kdfIterations: 0, password, ivBytes: null, plain);
            byte[] decrypted = SymmetricInteropHelper.DispatchDecrypt(EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, kdfIterations: 600_000, password, cipher);

            decrypted.ShouldBe(plain);
        }

        // ────────────────────────────────────────────────────────────────────────
        // ClearKeyBytes — used by the activity layer to scrub freshly-materialised
        // key buffers (from ParseKeyOrIv) after Dispatch returns, so password/raw-key
        // material does not linger on the managed heap.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void ClearKeyBytes_ZeroesTheBuffer()
        {
            byte[] buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            SymmetricInteropHelper.ClearKeyBytes(buffer);
            buffer.ShouldBe(new byte[16]);
        }

        [Fact]
        public void ClearKeyBytes_NullOrEmpty_NoThrow()
        {
            Should.NotThrow(() => SymmetricInteropHelper.ClearKeyBytes(null));
            Should.NotThrow(() => SymmetricInteropHelper.ClearKeyBytes(Array.Empty<byte>()));
        }

        [Fact]
        public void Dispatch_UnknownFormat_Throws()
        {
            // Cast to an out-of-range enum value to force the default branch.
            const SymmetricWireFormat invalid = (SymmetricWireFormat)999;
            byte[] plain = Encoding.UTF8.GetBytes("x");
            byte[] password = Encoding.UTF8.GetBytes("p");

            Should.Throw<ArgumentOutOfRangeException>(() =>
                SymmetricInteropHelper.DispatchEncrypt(EncryptionAlgorithm.AES, invalid, kdfIterations: 0, password, ivBytes: null, plain));

            Should.Throw<ArgumentOutOfRangeException>(() =>
                SymmetricInteropHelper.DispatchDecrypt(EncryptionAlgorithm.AES, invalid, kdfIterations: 0, password, new byte[64]));
        }

        // ────────────────────────────────────────────────────────────────────────
        // RunSymmetricWithKeyLifecycle — the security-critical invariant the helper exists for
        // ────────────────────────────────────────────────────────────────────────

        // The whole reason the helper owns the key-zeroing finally is so a future per-activity
        // fix can't silently drop it for one of the four activities. Capture the parsed key buffer
        // from inside the dispatch lambda and assert the bytes are zeroed after the throw escapes —
        // exactly what would regress if the finally were ever removed from the helper.
        [Fact]
        public void RunSymmetricWithKeyLifecycle_DispatchThrows_KeyBytesCleared()
        {
            byte[] capturedKeyBytes = null;

            Should.Throw<InvalidOperationException>(() =>
                SymmetricInteropHelper.RunSymmetricWithKeyLifecycle<int>(
                    EncryptionAlgorithm.AES,
                    SymmetricWireFormat.Classic,
                    KeyBytesFormat.Encoded,
                    Encoding.UTF8,
                    keyString: "key-to-be-cleared",
                    keySecureString: null,
                    ivString: null,
                    kdfIterations: 0,
                    needsIv: true,
                    dispatch: (k, _) =>
                    {
                        capturedKeyBytes = k;
                        // Sanity: the buffer holds material before we throw.
                        bool hasContent = false;
                        for (int i = 0; i < k.Length; i++) if (k[i] != 0) { hasContent = true; break; }
                        hasContent.ShouldBeTrue("key buffer should contain bytes before the throw");
                        throw new InvalidOperationException("forced failure inside dispatch");
                    }));

            capturedKeyBytes.ShouldNotBeNull();
            capturedKeyBytes.ShouldBe(new byte[capturedKeyBytes.Length]);
        }

        // Happy path: the helper round-trips end-to-end, so call sites can rely on it instead of
        // re-implementing validate → parse → dispatch → clear inline.
        [Fact]
        public void RunSymmetricWithKeyLifecycle_HappyPath_EncryptThenDecrypt_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("payload");
            string key = "shared-password";

            byte[] cipher = SymmetricInteropHelper.RunSymmetricWithKeyLifecycle(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, Encoding.UTF8,
                keyString: key, keySecureString: null,
                ivString: null, kdfIterations: 0, needsIv: true,
                dispatch: (k, iv) => SymmetricInteropHelper.DispatchEncrypt(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, 0, k, iv, plain));

            byte[] roundTripped = SymmetricInteropHelper.RunSymmetricWithKeyLifecycle(
                EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, Encoding.UTF8,
                keyString: key, keySecureString: null,
                ivString: null, kdfIterations: 0, needsIv: false,
                dispatch: (k, _) => SymmetricInteropHelper.DispatchDecrypt(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, 0, k, cipher));

            roundTripped.ShouldBe(plain);
        }

        [Fact]
        public void RunSymmetricWithKeyLifecycle_NullDispatch_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                SymmetricInteropHelper.RunSymmetricWithKeyLifecycle<int>(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.Classic, KeyBytesFormat.Encoded, Encoding.UTF8,
                    keyString: "k", keySecureString: null,
                    ivString: null, kdfIterations: 0, needsIv: false,
                    dispatch: null));
        }

        // ────────────────────────────────────────────────────────────────────────

        private static SecureString ToSecureString(string s)
        {
            var secure = new SecureString();
            foreach (char c in s)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }
    }
}

#pragma warning restore CS0618
