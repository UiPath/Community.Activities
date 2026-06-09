using System;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Activities.API;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.API.Tests
{
    /// <summary>
    /// Lifecycle and parsing behaviour of <see cref="RawKey"/>. Pins disposal,
    /// defensive copying, and the hex-parsing tolerance documented in the XML docs.
    /// </summary>
#pragma warning disable CS0618 // Tests intentionally use AES via the legacy enum.
    public class RawKeyTests
    {
        private readonly CryptographyService _service = new CryptographyService();

        // After Dispose(), the underlying byte[] has been zeroed and nulled. Subsequent
        // use must throw rather than silently produce ciphertext under an all-zero key.
        [Fact]
        public void Dispose_ThenEncrypt_Throws()
        {
            RawKey key = RawKey.FromBytes(new byte[32]);
            key.Dispose();

            Should.Throw<ObjectDisposedException>(() =>
                _service.EncryptBytes(new byte[] { 1, 2, 3 }, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Raw(key)));
        }

        // FromBytes defensively copies the input so the caller can scrub their buffer
        // (or reuse it) without affecting the RawKey instance.
        [Fact]
        public void FromBytes_DefensiveCopy_CallerMutationDoesNotAffectInstance()
        {
            byte[] sourceKey = new byte[32];
            for (int i = 0; i < 32; i++) sourceKey[i] = (byte)(i + 1);

            RawKey key = RawKey.FromBytes(sourceKey);

            byte[] cipher = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AES, SymmetricEncryptOptions.Raw(key));

            // Scribble all over the caller's array — the instance must still decrypt successfully
            // because it owns a defensive copy.
            Array.Clear(sourceKey, 0, sourceKey.Length);

            byte[] plain = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Raw(key));
            plain.ShouldBe(Encoding.UTF8.GetBytes("payload"));
        }

        // Dispose is safe to call twice (and zeroes are reflected through KeyBytes access via the throw).
        [Fact]
        public void Dispose_Idempotent()
        {
            RawKey key = RawKey.FromBytes(new byte[32]);
            key.Dispose();
            Should.NotThrow(() => key.Dispose());
        }

        // RawKey.KeyBytes returns a reference to the instance's own storage, NOT a fresh copy.
        // ReleaseMaterialisedBytes must therefore inherit the base CryptoKey no-op — clearing
        // the buffer per call would zero the key itself and break subsequent operations.
        [Fact]
        public void ReleaseMaterialisedBytes_IsNoOp_DoesNotCorruptInstance()
        {
            byte[] original = new byte[32];
            for (int i = 0; i < 32; i++) original[i] = (byte)(i + 1);
            RawKey key = RawKey.FromBytes(original);

            byte[] viewBefore = key.KeyBytes;
            key.ReleaseMaterialisedBytes(viewBefore);
            byte[] viewAfter = key.KeyBytes;

            // Both views point at the live storage and the storage is unchanged after the call.
            viewAfter.ShouldBe(original);
        }

        // ───────────────────────────────────────────────────────────────────────
        // Hex parsing tolerance — FromHex delegates to CryptographyHelper.ParseKeyBytes,
        // which strips "0x" prefix and whitespace/colons/dashes per the helper's contract.
        // Pin each tolerated variant so we don't silently regress.
        // ───────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]                                                              // bare
        [InlineData("0x000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]                                                            // 0x prefix
        [InlineData("0X000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]                                                            // 0X prefix
        [InlineData("00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F")]                              // whitespace
        [InlineData("00:01:02:03:04:05:06:07:08:09:0A:0B:0C:0D:0E:0F:10:11:12:13:14:15:16:17:18:19:1A:1B:1C:1D:1E:1F")]                              // colons
        [InlineData("00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F-10-11-12-13-14-15-16-17-18-19-1A-1B-1C-1D-1E-1F")]                              // dashes
        [InlineData("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f")]                                                              // lowercase
        [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191a1b1c1d1e1f")]                                                              // mixed case
        public void FromHex_TolerantVariants_ProduceEquivalentKey(string hex)
        {
            byte[] canonical = new byte[32];
            for (int i = 0; i < 32; i++) canonical[i] = (byte)i;

            RawKey hexKey = RawKey.FromHex(hex);
            RawKey bytesKey = RawKey.FromBytes(canonical);

            byte[] plain = Encoding.UTF8.GetBytes("tolerance-check");
            byte[] cipherFromHex = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Raw(hexKey));
            // Decrypt with the byte-equivalent key — proves both keys derive to the same bytes.
            byte[] decryptedWithBytesKey = _service.DecryptBytes(cipherFromHex, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Raw(bytesKey));
            decryptedWithBytesKey.ShouldBe(plain);
        }

        [Fact]
        public void FromHex_OddLength_Throws()
        {
            // 63 hex digits — cleaned hex has odd length after the canonicalisation pass.
            Should.Throw<ArgumentException>(() => RawKey.FromHex("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E0"));
        }

        [Fact]
        public void FromHex_NonHexCharacters_Throws()
        {
            // 'Z' is not a hex digit and not one of the tolerated separators (' ', ':', '-', whitespace).
            Should.Throw<FormatException>(() => RawKey.FromHex("ZZ01020304050607080900ZZ010203040506070809000102030405060708090A"));
        }

        // ───────────────────────────────────────────────────────────────────────
        // Base64 parsing — FromBase64 delegates to Convert.FromBase64String, which is
        // strict about padding but tolerates standard alphabet.
        // ───────────────────────────────────────────────────────────────────────

        [Fact]
        public void FromBase64_StandardPadding_Accepted()
        {
            byte[] canonical = new byte[32];
            for (int i = 0; i < 32; i++) canonical[i] = (byte)(i + 1);
            string base64 = Convert.ToBase64String(canonical);

            RawKey k = RawKey.FromBase64(base64);
            byte[] cipher = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AES, SymmetricEncryptOptions.Raw(k));
            byte[] plain = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Raw(k));
            plain.ShouldBe(Encoding.UTF8.GetBytes("payload"));
        }

        [Fact]
        public void FromBase64_InvalidCharacters_Throws()
        {
            // '*' is not in the Base64 alphabet.
            Should.Throw<FormatException>(() => RawKey.FromBase64("****never-valid-base64****"));
        }
    }
#pragma warning restore CS0618
}
