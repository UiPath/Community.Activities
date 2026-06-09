using System;
using System.Security;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Activities.API;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.API.Tests
{
    /// <summary>
    /// Lifecycle and materialisation behaviour of <see cref="PasswordKey"/>. Pins
    /// invariants that the round-trip tests in <see cref="CryptographyServiceTests"/>
    /// do not exercise on their own — disposal, defensive copying, encoding fidelity.
    /// </summary>
#pragma warning disable CS0618 // Tests intentionally use AES via the legacy enum.
    public class PasswordKeyTests
    {
        private readonly CryptographyService _service = new CryptographyService();

        // After Dispose(), the SecureString is gone and any further use of the key
        // must throw rather than silently encrypt with an empty/all-zero key derivation.
        [Fact]
        public void Dispose_ThenEncrypt_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("disposable", Encoding.UTF8);
            key.Dispose();

            Should.Throw<ObjectDisposedException>(() =>
                _service.EncryptBytes(new byte[] { 1, 2, 3 }, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key)));
        }

        // The FromPassword(SecureString) factory defensively copies the input SecureString
        // — pinned by disposing the caller's SecureString and confirming the PasswordKey still works.
        [Fact]
        public void FromPassword_SecureString_DefensiveCopy_SurvivesCallerDispose()
        {
            var external = ToSecureString("survives-dispose");
            PasswordKey key = PasswordKey.FromPassword(external, Encoding.UTF8);
            external.Dispose();

            byte[] cipher = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key));
            byte[] plain = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key));
            plain.ShouldBe(Encoding.UTF8.GetBytes("payload"));
        }

        // Same instance, repeated use — materialisation must be deterministic. (Round-trip
        // covers the encrypt-then-decrypt case once; this pins that *both* sides of a long
        // pipeline that calls Encrypt/Decrypt multiple times keep getting the same key.)
        [Fact]
        public void RepeatedUse_ProducesSameKey()
        {
            PasswordKey key = PasswordKey.FromPassword("stable", Encoding.UTF8);

            byte[] cipherA = _service.EncryptBytes(Encoding.UTF8.GetBytes("payloadA"), EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key));
            byte[] cipherB = _service.EncryptBytes(Encoding.UTF8.GetBytes("payloadB"), EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key));

            // Both blobs decrypt under the same key on a single instance — pins that
            // the materialiser is idempotent and the SecureString isn't consumed on first use.
            _service.DecryptBytes(cipherA, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key))
                .ShouldBe(Encoding.UTF8.GetBytes("payloadA"));
            _service.DecryptBytes(cipherB, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key))
                .ShouldBe(Encoding.UTF8.GetBytes("payloadB"));
        }

        // Different encodings produce different derived keys — pins that the Encoding
        // parameter actually flows through to MaterialisePasswordBytes (and isn't accidentally
        // ignored in favour of a hardcoded UTF-8 or Unicode default).
        [Fact]
        public void Encoding_IsHonoured()
        {
            // A password whose UTF-8 and Unicode (UTF-16LE) byte representations differ.
            const string password = "ăîșțâ";
            PasswordKey utf8Key = PasswordKey.FromPassword(password, Encoding.UTF8);
            PasswordKey unicodeKey = PasswordKey.FromPassword(password, Encoding.Unicode);

            byte[] cipher = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AESGCM, SymmetricEncryptOptions.Classic(utf8Key));

            // Decrypt under the SAME encoding succeeds.
            byte[] roundTrip = _service.DecryptBytes(cipher, EncryptionAlgorithm.AESGCM, SymmetricDecryptOptions.Classic(utf8Key));
            roundTrip.ShouldBe(Encoding.UTF8.GetBytes("payload"));

            // Decrypt under a DIFFERENT encoding fails — AEAD tag check rejects the wrong key.
            Should.Throw<System.Security.Cryptography.CryptographicException>(() =>
                _service.DecryptBytes(cipher, EncryptionAlgorithm.AESGCM, SymmetricDecryptOptions.Classic(unicodeKey)));
        }

        // Two PasswordKey instances built from the same password+encoding derive the same key —
        // pins that there's no per-instance salt or randomness in materialisation (the salt
        // lives in the wire format, not in the key).
        [Fact]
        public void TwoInstances_SamePasswordSameEncoding_InterchangeableForDecrypt()
        {
            PasswordKey k1 = PasswordKey.FromPassword("interchangeable", Encoding.UTF8);
            PasswordKey k2 = PasswordKey.FromPassword("interchangeable", Encoding.UTF8);

            byte[] cipher = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(k1));
            byte[] plain = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(k2));
            plain.ShouldBe(Encoding.UTF8.GetBytes("payload"));
        }

        // Dispose() being called twice is safe — eager disposal in nested using blocks should
        // not throw on the second hit.
        [Fact]
        public void Dispose_Idempotent()
        {
            PasswordKey key = PasswordKey.FromPassword("idempotent", Encoding.UTF8);
            key.Dispose();
            key.Dispose(); // second call must not throw
        }

        private static SecureString ToSecureString(string value)
        {
            var ss = new SecureString();
            foreach (char c in value) ss.AppendChar(c);
            ss.MakeReadOnly();
            return ss;
        }
    }
#pragma warning restore CS0618
}
