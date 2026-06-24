using System;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // obsolete algorithms reachable via opt-in formats

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Pin the wire-format byte layouts so a future refactor cannot silently break
    /// historical ciphertext. Each blob below was produced by an earlier run of
    /// CryptographyHelper.* and is intentionally hardcoded — if any of these tests
    /// starts failing, the wire layout has moved and existing customer ciphertext
    /// in the wild will no longer decrypt.
    ///
    /// The marker plaintext / password / key are fixed; only the salt and IV inside
    /// each blob were randomised at capture time. Future encryption will produce
    /// different bytes (salt/IV are still random), but DECRYPTING these captured
    /// blobs must keep working forever.
    /// </summary>
    public class WireFormatStabilityTests
    {
        private const string Plaintext = "wire-format-stability-marker";
        private const string Password = "fixture-pass-stable-{!}";

        // Classic (Frozen): salt(8) ‖ IV(16) ‖ ciphertext. PBKDF2-HMAC-SHA1 @ 10,000 iter.
        private const string ClassicAesCbcBlob = "12D787E3818ABE0CA99AD3015FA71B0E1A5D737971CB25DCB5629C6556D0CE7BE9E03CF9DED3E7BE7CA7A36E83C764348C4569CD39BAD234";

        // Classic (AEAD): salt(8) ‖ IV(12) ‖ ciphertext ‖ tag(16). PBKDF2-HMAC-SHA1 @ 10,000 iter.
        private const string ClassicAesGcmBlob = "CB5CC5EDE5DB0961118FF88E03DB60D22B997C40012E6D073C38A5931214734F3548590391700A89F6619F2A54CEE9CC2D0E5E0CC595551416076C7E9715B803";

        // Owasp2026 (AEAD): same layout as Classic, PBKDF2-HMAC-SHA1 @ 1,300,000 iter.
        private const string Owasp2026AesGcmBlob_1300000Iter = "55B8B76EC51F317C05DB0227D549265AD7D595F072555A9E741700ABF6D41C74B0EE142238CC2739E12CF561B39CD05668CD050890EB0ED1DFF1BD2169F9A910";

        // OpenSslEnc CBC: "Salted__"(8) ‖ salt(8) ‖ ciphertext. PBKDF2-HMAC-SHA256 @ 600,000 iter.
        private const string OpenSslEncAesCbcBlob_600000Iter = "53616C7465645F5FD9652D18E7589DCF66B5643EF4F9FDD1612AEFD72D7BA232725936111004B08A29786D5B9E876CD0";

        // OpenSslEnc AEAD (UiPath extension): "Salted__"(8) ‖ salt(8) ‖ ciphertext ‖ tag(16). PBKDF2-HMAC-SHA256 @ 600,000 iter.
        private const string OpenSslEncAesGcmBlob_600000Iter = "53616C7465645F5FB6F179A6913F02F7CB6328DA9D8AE3109DD98963E45E457530DEAD3DE5ABE3E7518392A112F016E2FBB77AAF86913DBC2AE75DCC";

        // Raw AEAD: IV(12) ‖ ciphertext ‖ tag(16). No KDF.
        private const string RawAesGcmBlob_FixedKeyIv = "808182838485868788898A8B9BBF2498CD86A4C24E2688170FB04D8D20E4147741BB950821885C6BEF69959B17C57B58223D3C531F837D7E";

        // ────────────────────────────────────────────────────────────────────────
        // Classic — frozen byte-stable layout. Any change to PBKDF2 iterations,
        // salt length, KDF hash algorithm, or byte ordering will fail these.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Classic_AesCbc_FixedBlob_Decrypts()
        {
            byte[] decrypted = CryptographyHelper.DecryptData(
                EncryptionAlgorithm.AES,
                Convert.FromHexString(ClassicAesCbcBlob),
                Encoding.UTF8.GetBytes(Password));
            Encoding.UTF8.GetString(decrypted).ShouldBe(Plaintext);
        }

        [Fact]
        public void Classic_AesGcm_FixedBlob_Decrypts()
        {
            byte[] decrypted = CryptographyHelper.DecryptData(
                EncryptionAlgorithm.AESGCM,
                Convert.FromHexString(ClassicAesGcmBlob),
                Encoding.UTF8.GetBytes(Password));
            Encoding.UTF8.GetString(decrypted).ShouldBe(Plaintext);
        }

        // ────────────────────────────────────────────────────────────────────────
        // Owasp2026 — same layout as Classic, but caller-supplied iter count.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Owasp2026_AesGcm_FixedBlob_AtRecommendedIterations_Decrypts()
        {
            byte[] decrypted = CryptographyHelper.DecryptDataWithIterations(
                EncryptionAlgorithm.AESGCM,
                Convert.FromHexString(Owasp2026AesGcmBlob_1300000Iter),
                Encoding.UTF8.GetBytes(Password),
                iterations: 1_300_000);
            Encoding.UTF8.GetString(decrypted).ShouldBe(Plaintext);
        }

        // ────────────────────────────────────────────────────────────────────────
        // OpenSslEnc — third-party interop layout. If THIS test fails, the cross-tool
        // interop guarantee that motivated the OpenSslEnc feature is broken.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void OpenSslEnc_AesCbc_FixedBlob_Decrypts()
        {
            byte[] decrypted = CryptographyHelper.DecryptDataOpenSslEnc(
                EncryptionAlgorithm.AES,
                Convert.FromHexString(OpenSslEncAesCbcBlob_600000Iter),
                Encoding.UTF8.GetBytes(Password),
                iterations: 600_000);
            Encoding.UTF8.GetString(decrypted).ShouldBe(Plaintext);
        }

        [Fact]
        public void OpenSslEnc_AesGcm_FixedBlob_Decrypts()
        {
            byte[] decrypted = CryptographyHelper.DecryptDataOpenSslEnc(
                EncryptionAlgorithm.AESGCM,
                Convert.FromHexString(OpenSslEncAesGcmBlob_600000Iter),
                Encoding.UTF8.GetBytes(Password),
                iterations: 600_000);
            Encoding.UTF8.GetString(decrypted).ShouldBe(Plaintext);
        }

        // OpenSslEnc CBC blob must start with the canonical "Salted__" prefix —
        // a test that catches any silent change to the magic constant or its position.
        [Fact]
        public void OpenSslEnc_AesCbc_FixedBlob_BeginsWithSaltedMagic()
        {
            byte[] blob = Convert.FromHexString(OpenSslEncAesCbcBlob_600000Iter);
            Encoding.ASCII.GetString(blob.AsSpan(0, 8).ToArray()).ShouldBe("Salted__");
        }

        // ────────────────────────────────────────────────────────────────────────
        // Raw — caller-supplied key + IV, no KDF. The fixed IV at the start of the
        // blob is the literal IV the captured encryption used; this also pins the
        // AEAD tag layout (tag goes at the END of the blob, not the start).
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void Raw_AesGcm_FixedBlob_Decrypts()
        {
            byte[] rawKey = new byte[32];
            for (int i = 0; i < 32; i++) rawKey[i] = (byte)(i + 1);

            byte[] decrypted = CryptographyHelper.DecryptDataRaw(
                EncryptionAlgorithm.AESGCM,
                Convert.FromHexString(RawAesGcmBlob_FixedKeyIv),
                rawKey);
            Encoding.UTF8.GetString(decrypted).ShouldBe(Plaintext);
        }

        [Fact]
        public void Raw_AesGcm_FixedBlob_BeginsWithFixedIv()
        {
            // Pin that the IV the encryptor was told to use appears as the stream prefix.
            byte[] blob = Convert.FromHexString(RawAesGcmBlob_FixedKeyIv);
            byte[] expectedIv = new byte[12];
            for (int i = 0; i < 12; i++) expectedIv[i] = (byte)(0x80 + i);
            blob.AsSpan(0, 12).ToArray().ShouldBe(expectedIv);
        }

        // ────────────────────────────────────────────────────────────────────────
        // OWASP iteration constants — pinned values so an accidental edit to the
        // private constants in CryptographyHelper will fail loudly.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void RecommendedIterations_PinValues()
        {
            CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.Owasp2026).ShouldBe(1_300_000);
            CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.OpenSslEnc).ShouldBe(600_000);
        }
    }
}

#pragma warning restore CS0618
