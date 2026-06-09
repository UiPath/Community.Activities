using System;
using System.Net;
using System.Security;
using System.Text;
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
            int? rawKeyLengthBytes)
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
            if (kdfIterations < 0 || (kdfIterations > 0 && kdfIterations < MinKdfIterations))
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

        public static byte[] DispatchEncrypt(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            int kdfIterations,
            byte[] keyOrPasswordBytes,
            byte[] ivBytes,
            byte[] inputBytes)
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
                    return CryptographyHelper.EncryptDataOpenSslEnc(algorithm, inputBytes, keyOrPasswordBytes, iter);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown SymmetricWireFormat");
            }
        }

        public static byte[] DispatchDecrypt(
            EncryptionAlgorithm algorithm,
            SymmetricWireFormat format,
            int kdfIterations,
            byte[] keyOrPasswordBytes,
            byte[] inputBytes)
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
                    return CryptographyHelper.DecryptDataOpenSslEnc(algorithm, inputBytes, keyOrPasswordBytes, iter);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown SymmetricWireFormat");
            }
        }
    }
}
