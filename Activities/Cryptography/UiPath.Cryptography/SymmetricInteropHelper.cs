using System;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Enums;
using UiPath.Cryptography.Properties;

#pragma warning disable CS0618 // obsolete encryption algorithms reachable via opt-in formats

namespace UiPath.Cryptography
{
    /// <summary>
    /// Validation + dispatch glue for the four <see cref="SymmetricWireFormat"/> modes.
    /// Activities and the coded-workflow service both call into here after resolving their
    /// own input values; the helper enforces the cross-property invariants
    /// (see <c>docs/symmetric-wire-format.md</c>) and routes to <see cref="CryptographyHelper"/>.
    /// </summary>
    /// <remarks>
    /// This class is <c>public</c> only because it crosses assembly boundaries inside the
    /// package (the activities and the coded-workflow service both consume it). Treat it as
    /// internal: it may change, gain or lose methods, or be removed without notice.
    /// </remarks>
    public static class SymmetricInteropHelper
    {
        public const int MinKdfIterations = 1000;

        public static void ValidateInteropSettings(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            KeyBytesFormat keyFormat,
            string ivString,
            int kdfIterations,
            int? rawKeyLengthBytes,
            bool isDecrypt = false)
        {
            if (format == SymmetricWireFormat.Raw && keyFormat == KeyBytesFormat.Encoded)
                throw new ArgumentException(Resources.Validation_RawKeyFormat_EncodedNotAllowed);
            if (format != SymmetricWireFormat.Raw && keyFormat != KeyBytesFormat.Encoded)
                throw new ArgumentException(Resources.Validation_PasswordFormat_NonEncodedNotAllowed);

            if (format != SymmetricWireFormat.Raw && !string.IsNullOrEmpty(ivString))
                throw new ArgumentException(Resources.Validation_Iv_OnlyForRaw);

            if (kdfIterations != 0 && (format == SymmetricWireFormat.Classic || format == SymmetricWireFormat.Raw))
                throw new ArgumentException(Resources.Validation_KdfIterations_NotForClassicOrRaw);
            // Negative iterations were silently swallowed by the dispatch (treated as "use default") —
            // only zero means "default". Anything below the minimum (including negatives) is invalid.
            // The positive-but-below-minimum floor is an encrypt-only guard (never produce weak ciphertext);
            // on decrypt we must honour whatever iteration count a third-party producer chose, so the floor
            // is skipped when isDecrypt is true. Negative iterations stay rejected in both directions.
            if (kdfIterations < 0 || (kdfIterations > 0 && kdfIterations < MinKdfIterations && !isDecrypt))
                throw new ArgumentException(string.Format(Resources.Validation_KdfIterations_BelowMinimum, kdfIterations, MinKdfIterations));

            if (format == SymmetricWireFormat.Raw && rawKeyLengthBytes.HasValue)
            {
                int[] legal = CryptographyHelper.GetRawKeySizes(algorithm);
                if (Array.IndexOf(legal, rawKeyLengthBytes.Value) < 0)
                {
                    throw new ArgumentException(string.Format(
                        Resources.Validation_RawKey_LengthMismatch,
                        rawKeyLengthBytes.Value,
                        string.Join(", ", legal)));
                }
            }
        }

        /// <summary>
        /// Resolves a key/IV string (either plain or via <see cref="SecureString"/>) into bytes
        /// per the chosen <see cref="KeyBytesFormat"/>. Returns <c>null</c> when both inputs are empty.
        /// </summary>
        public static byte[] ParseKeyOrIv(string value, SecureString secureValue, KeyBytesFormat format, Encoding encoding)
        {
            bool hasPlain = !string.IsNullOrEmpty(value);
            bool hasSecure = secureValue != null && secureValue.Length > 0;
            if (!hasPlain && !hasSecure)
                return null;

            if (format == KeyBytesFormat.Encoded)
                return CryptographyHelper.KeyEncoding(encoding, hasPlain ? value : null, hasSecure ? secureValue : null);

            string raw = hasPlain ? value : new NetworkCredential(string.Empty, secureValue).Password;
            return CryptographyHelper.ParseKeyBytes(raw, null, format, null);
        }

        /// <summary>
        /// Zero a freshly-materialised key buffer in place. Call in a <c>finally</c> after the
        /// dispatcher returns so the secret bytes (raw cipher key, or password material
        /// materialised from a SecureString) do not survive on the managed heap until GC.
        /// Safe to call with <c>null</c> or an empty array.
        /// </summary>
        public static void ClearKeyBytes(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
                Array.Clear(bytes, 0, bytes.Length);
        }

        public static byte[] DispatchEncrypt(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            int kdfIterations,
            byte[] keyOrPasswordBytes,
            byte[] ivBytes,
            byte[] inputBytes,
            AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            switch (format)
            {
                case SymmetricWireFormat.Classic:
                    return CryptographyHelper.EncryptData(algorithm, inputBytes, keyOrPasswordBytes);
                case SymmetricWireFormat.Owasp2026:
                {
                    int iter = kdfIterations > 0 ? kdfIterations : CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.Owasp2026);
                    return CryptographyHelper.EncryptDataWithIterations(algorithm, inputBytes, keyOrPasswordBytes, iter);
                }
                case SymmetricWireFormat.Raw:
                    return CryptographyHelper.EncryptDataRaw(algorithm, inputBytes, keyOrPasswordBytes, ivBytes);
                case SymmetricWireFormat.OpenSslEnc:
                {
                    int iter = kdfIterations > 0 ? kdfIterations : CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.OpenSslEnc);
                    return CryptographyHelper.EncryptDataOpenSslEnc(algorithm, inputBytes, keyOrPasswordBytes, iter, aesKeySize);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown SymmetricWireFormat");
            }
        }

        // Owns the full validate → parse key/IV → re-validate(Raw) → dispatch → finally{ClearKeyBytes}
        // lifecycle for symmetric encrypt/decrypt. The four XAML activities (EncryptText, DecryptText,
        // EncryptFile, DecryptFile) all need this exact sequence; centralising it means the key-zeroing
        // finally lives in one place and cannot be silently dropped by a future fix that touches only
        // three of the four activities. Callers pass already-resolved settings + payload via the
        // dispatch lambda (which is where per-activity error wrapping lives, e.g. Decrypt* wrapping
        // CryptographicException in InvalidOperationException).
        public static TOut RunSymmetricWithKeyLifecycle<TOut>(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            KeyBytesFormat keyFormat,
            Encoding keyEncoding,
            string keyString,
            SecureString keySecureString,
            string ivString,
            int kdfIterations,
            bool needsIv,
            Func<byte[], byte[], TOut> dispatch,
            bool isDecrypt = false)
        {
            if (dispatch == null) throw new ArgumentNullException(nameof(dispatch));

            ValidateInteropSettings(algorithm, format, keyFormat, ivString, kdfIterations, null, isDecrypt);

            byte[] keyOrPasswordBytes = ParseKeyOrIv(keyString, keySecureString, keyFormat, keyEncoding);
            byte[] ivBytes = needsIv
                ? ParseKeyOrIv(ivString, null, keyFormat, keyEncoding)
                : null;

            if (format == SymmetricWireFormat.Raw)
                ValidateInteropSettings(algorithm, format, keyFormat, ivString, kdfIterations, keyOrPasswordBytes?.Length, isDecrypt);

            try
            {
                return dispatch(keyOrPasswordBytes, ivBytes);
            }
            finally
            {
                ClearKeyBytes(keyOrPasswordBytes);
            }
        }

        public static byte[] DispatchDecrypt(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            int kdfIterations,
            byte[] keyOrPasswordBytes,
            byte[] inputBytes,
            AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            switch (format)
            {
                case SymmetricWireFormat.Classic:
                    return CryptographyHelper.DecryptData(algorithm, inputBytes, keyOrPasswordBytes);
                case SymmetricWireFormat.Owasp2026:
                {
                    int iter = kdfIterations > 0 ? kdfIterations : CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.Owasp2026);
                    return CryptographyHelper.DecryptDataWithIterations(algorithm, inputBytes, keyOrPasswordBytes, iter);
                }
                case SymmetricWireFormat.Raw:
                    return CryptographyHelper.DecryptDataRaw(algorithm, inputBytes, keyOrPasswordBytes);
                case SymmetricWireFormat.OpenSslEnc:
                {
                    int iter = kdfIterations > 0 ? kdfIterations : CryptographyHelper.GetRecommendedIterations(SymmetricWireFormat.OpenSslEnc);
                    return CryptographyHelper.DecryptDataOpenSslEnc(algorithm, inputBytes, keyOrPasswordBytes, iter, aesKeySize);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown SymmetricWireFormat");
            }
        }
    }
}
