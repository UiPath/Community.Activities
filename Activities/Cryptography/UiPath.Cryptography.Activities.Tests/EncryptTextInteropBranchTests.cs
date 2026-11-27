using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Net;
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
    /// Activity-level branches under the new wire-format arguments that
    /// <see cref="SymmetricInteropTests"/> does NOT cover: SecureString key path,
    /// non-Unicode key encoding, error-message hints on corrupt input,
    /// ContinueOnError behaviour when the symmetric interop path fails.
    /// </summary>
    public class EncryptTextInteropBranchTests
    {
        private const string Plaintext = "interop-branch-coverage 🎉 ăîșțâ";
        private const string Password = "branch-test-pwd-{$}";

        // ────────────────────────────────────────────────────────────────────────
        // KeySecureString — the new formats must accept a SecureString key, not just a string.
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(SymmetricWireFormat.Owasp2026)]
        [InlineData(SymmetricWireFormat.OpenSslEnc)]
        public void EncryptText_DecryptText_SecureStringKey_RoundTrips(SymmetricWireFormat format)
        {
            // Encrypt with SecureString, decrypt with SecureString — both ends use the
            // KeySecureString path under the new format. Pins that the new dispatch reads
            // the secure-string fork, not just the plain-string fork.
            SecureString secureKey = ToSecureString(Password);

            string cipher = RunSymmetric(
                new EncryptText
                {
                    Algorithm = EncryptionAlgorithm.AESGCM,
                    Format = format,
                    KeyFormat = KeyBytesFormat.Encoded,
                    KeyInputModeSwitch = KeyInputMode.SecureKey,
                    Encoding = MakeEncodingArg(Encoding.UTF8),
                    KeyEncodingString = null,
                },
                input: Plaintext,
                secureKey: secureKey);

            string plain = RunSymmetric(
                new DecryptText
                {
                    Algorithm = EncryptionAlgorithm.AESGCM,
                    Format = format,
                    KeyFormat = KeyBytesFormat.Encoded,
                    KeyInputModeSwitch = KeyInputMode.SecureKey,
                    Encoding = MakeEncodingArg(Encoding.UTF8),
                    KeyEncodingString = null,
                },
                input: cipher,
                secureKey: ToSecureString(Password));

            plain.ShouldBe(Plaintext);
        }

        // Raw with a SecureString-supplied hex key — pins the Hex-from-SecureString path
        // (NetworkCredential trick in SymmetricInteropHelper.ParseKeyOrIv).
        [Fact]
        public void EncryptText_DecryptText_SecureStringHexKey_Raw_RoundTrips()
        {
            const string hexKey = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";

            string cipher = RunSymmetric(
                new EncryptText
                {
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = SymmetricWireFormat.Raw,
                    KeyFormat = KeyBytesFormat.Hex,
                    KeyInputModeSwitch = KeyInputMode.SecureKey,
                    Encoding = MakeEncodingArg(Encoding.UTF8),
                    KeyEncodingString = null,
                },
                input: Plaintext,
                secureKey: ToSecureString(hexKey));

            string plain = RunSymmetric(
                new DecryptText
                {
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = SymmetricWireFormat.Raw,
                    KeyFormat = KeyBytesFormat.Hex,
                    KeyInputModeSwitch = KeyInputMode.SecureKey,
                    Encoding = MakeEncodingArg(Encoding.UTF8),
                    KeyEncodingString = null,
                },
                input: cipher,
                secureKey: ToSecureString(hexKey));

            plain.ShouldBe(Plaintext);
        }

        // ────────────────────────────────────────────────────────────────────────
        // Non-Unicode KeyEncoding under Owasp2026 — existing CryptographyTests covers
        // Shift-JIS only for the legacy default format. Pin that the new format still
        // honours KeyEncodingString.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void EncryptText_DecryptText_Owasp2026_WithShiftJisKey_RoundTrips()
        {
            const string shiftJisCodePage = "932";
            const string toProcess = "Owasp2026 + Shift-JIS: こんにちは, 1234567890";
            const string key = "shift-jis-key-日本語";

            var encrypt = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.AESGCM,
                Format = SymmetricWireFormat.Owasp2026,
                KeyFormat = KeyBytesFormat.Encoded,
                KeyEncodingString = shiftJisCodePage,
            };
            string cipher = (string)new WorkflowInvoker(encrypt).Invoke(new Dictionary<string, object>
            {
                { nameof(EncryptText.Input), toProcess },
                { nameof(EncryptText.Key), key },
                { nameof(EncryptText.KdfIterations), 50_000 },
            })[nameof(encrypt.Result)];

            var decrypt = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.AESGCM,
                Format = SymmetricWireFormat.Owasp2026,
                KeyFormat = KeyBytesFormat.Encoded,
                KeyEncodingString = shiftJisCodePage,
            };
            string plain = (string)new WorkflowInvoker(decrypt).Invoke(new Dictionary<string, object>
            {
                { nameof(DecryptText.Input), cipher },
                { nameof(DecryptText.Key), key },
                { nameof(DecryptText.KdfIterations), 50_000 },
            })[nameof(decrypt.Result)];

            plain.ShouldBe(toProcess);
        }

        // ────────────────────────────────────────────────────────────────────────
        // Error-message hints on decrypt failures — pin substrings so an actionable
        // hint doesn't regress silently into a generic CryptographicException.
        // ────────────────────────────────────────────────────────────────────────

        // Padding failure on a non-AEAD algorithm — message must mention the "different tool"
        // hint so the user knows to check their producer.
        [Fact]
        public void DecryptText_Classic_WrongPassword_NonAead_HintsAtDifferentTool()
        {
            // Encrypt with one password, decrypt with another. The derived key is wrong,
            // so PKCS#7 padding fails on the final block ~255/256 of the time and the
            // CryptographicException is wrapped with the "different tool" hint.
            string goodCipher = RunSymmetric(
                new EncryptText
                {
                    Algorithm = EncryptionAlgorithm.AES,
                    Format = SymmetricWireFormat.Classic,
                    KeyFormat = KeyBytesFormat.Encoded,
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Encoding = MakeEncodingArg(Encoding.UTF8),
                    KeyEncodingString = null,
                },
                input: Plaintext,
                key: Password);

            InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
                RunSymmetric(
                    new DecryptText
                    {
                        Algorithm = EncryptionAlgorithm.AES,
                        Format = SymmetricWireFormat.Classic,
                        KeyFormat = KeyBytesFormat.Encoded,
                        KeyInputModeSwitch = KeyInputMode.Key,
                        Encoding = MakeEncodingArg(Encoding.UTF8),
                        KeyEncodingString = null,
                    },
                    input: goodCipher,
                    key: "wrong-password-totally-different"));

            ex.InnerException.ShouldBeOfType<CryptographicException>();
            ex.InnerException.Message.ShouldContain("different tool", Case.Insensitive);
        }

        // Input shorter than the wire-format minimum → "UiPath wire format" mention so
        // the user can correlate with the docs.
        [Fact]
        public void DecryptText_Classic_TooShortInput_HintsAtWireFormat()
        {
            // 4 bytes of base64 — way below the 8+IV minimum.
            string tooShort = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

            InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
                RunSymmetric(
                    new DecryptText
                    {
                        Algorithm = EncryptionAlgorithm.AES,
                        Format = SymmetricWireFormat.Classic,
                        KeyFormat = KeyBytesFormat.Encoded,
                        KeyInputModeSwitch = KeyInputMode.Key,
                        Encoding = MakeEncodingArg(Encoding.UTF8),
                        KeyEncodingString = null,
                    },
                    input: tooShort,
                    key: Password));

            ex.InnerException.ShouldBeOfType<CryptographicException>();
            ex.InnerException.Message.ShouldContain("UiPath wire format", Case.Insensitive);
        }

        // OpenSslEnc input that doesn't start with "Salted__" — the activity should
        // surface the missing-magic hint.
        [Fact]
        public void DecryptText_OpenSslEnc_MissingMagic_SurfacesHint()
        {
            // 48 bytes of zeros — long enough to clear the length check but bytes 0..7 are not "Salted__".
            string notSalted = Convert.ToBase64String(new byte[48]);

            InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
                RunSymmetric(
                    new DecryptText
                    {
                        Algorithm = EncryptionAlgorithm.AES,
                        Format = SymmetricWireFormat.OpenSslEnc,
                        KeyFormat = KeyBytesFormat.Encoded,
                        KeyInputModeSwitch = KeyInputMode.Key,
                        Encoding = MakeEncodingArg(Encoding.UTF8),
                        KeyEncodingString = null,
                    },
                    input: notSalted,
                    key: Password,
                    iterations: 600_000));

            ex.InnerException.ShouldBeOfType<CryptographicException>();
            ex.InnerException.Message.ShouldContain("Salted__");
        }

        // ────────────────────────────────────────────────────────────────────────
        // ContinueOnError — symmetric path should respect the toggle. Today this is
        // exercised in EncryptFileTests at the file activity; pin the same behaviour
        // through the text activity under the new interop arg surface.
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void DecryptText_ContinueOnError_OnBadInteropInput_SwallowsException()
        {
            string notSalted = Convert.ToBase64String(new byte[48]);

            var decrypt = new DecryptText
            {
                Algorithm = EncryptionAlgorithm.AES,
                Format = SymmetricWireFormat.OpenSslEnc,
                KeyFormat = KeyBytesFormat.Encoded,
                KeyInputModeSwitch = KeyInputMode.Key,
                Encoding = MakeEncodingArg(Encoding.UTF8),
                KeyEncodingString = null,
                ContinueOnError = new InArgument<bool>(true),
            };

            Should.NotThrow(() => new WorkflowInvoker(decrypt).Invoke(new Dictionary<string, object>
            {
                { nameof(DecryptText.Input), notSalted },
                { nameof(DecryptText.Key), Password },
                { nameof(DecryptText.KdfIterations), 600_000 },
            }));
        }

        // ────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────

        private static InArgument<Encoding> MakeEncodingArg(Encoding e)
        {
            if (e == Encoding.UTF8) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.UTF8));
            if (e == Encoding.Unicode || e == null) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.Unicode));
            throw new ArgumentException($"Test helper only supports UTF-8 and Unicode; got {e.WebName}");
        }

        private static string RunSymmetric(EncryptText activity, string input, string key = null, SecureString secureKey = null, int iterations = 0)
        {
            var args = new Dictionary<string, object>
            {
                [nameof(EncryptText.Input)] = input,
            };
            if (key != null) args[nameof(EncryptText.Key)] = key;
            if (secureKey != null) args[nameof(EncryptText.KeySecureString)] = secureKey;
            if (iterations != 0) args[nameof(EncryptText.KdfIterations)] = iterations;

            try { return (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)]; }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null) { throw tie.InnerException; }
        }

        private static string RunSymmetric(DecryptText activity, string input, string key = null, SecureString secureKey = null, int iterations = 0)
        {
            var args = new Dictionary<string, object>
            {
                [nameof(DecryptText.Input)] = input,
            };
            if (key != null) args[nameof(DecryptText.Key)] = key;
            if (secureKey != null) args[nameof(DecryptText.KeySecureString)] = secureKey;
            if (iterations != 0) args[nameof(DecryptText.KdfIterations)] = iterations;

            try { return (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)]; }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null) { throw tie.InnerException; }
        }

        private static SecureString ToSecureString(string s) => new NetworkCredential(string.Empty, s).SecurePassword;
    }
}

#pragma warning restore CS0618
