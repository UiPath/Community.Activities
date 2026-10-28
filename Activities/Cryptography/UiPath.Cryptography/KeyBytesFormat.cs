using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    /// <summary>
    /// How the <c>Key</c> (and, for <see cref="SymmetricWireFormat.Raw"/>, <c>Iv</c>)
    /// strings are converted to bytes. <c>Encoded</c> matches the historical
    /// UiPath behavior (password → bytes via <c>Encoding</c>); <c>Hex</c> and
    /// <c>Base64</c> are required when supplying a literal raw key.
    /// </summary>
    public enum KeyBytesFormat
    {
        /// <summary>
        /// The <c>Key</c> string is a password — transcoded to bytes through the activity's
        /// <c>Encoding</c> property (UTF-8 by default). The resulting bytes feed into PBKDF2,
        /// which derives the actual cipher key. Default. Compatible with
        /// <see cref="SymmetricWireFormat.Classic"/>, <see cref="SymmetricWireFormat.Owasp2026"/>,
        /// and <see cref="SymmetricWireFormat.OpenSslEnc"/>; rejected for
        /// <see cref="SymmetricWireFormat.Raw"/>.
        /// </summary>
        [LocalizedDescription(nameof(Resources.KeyBytesFormat_Encoded))]
        Encoded = 0,

        /// <summary>
        /// The <c>Key</c> string is the literal cipher key encoded as hexadecimal — even
        /// number of hex digits (0-9, a-f, A-F), no KDF, no <c>Encoding</c> involvement. The
        /// decoded byte length must match a legal key size for the algorithm (e.g. 32 hex
        /// chars / 16 bytes for AES-128, 64 hex chars / 32 bytes for AES-256). Required for
        /// <see cref="SymmetricWireFormat.Raw"/>; rejected for the other formats.
        /// </summary>
        [LocalizedDescription(nameof(Resources.KeyBytesFormat_Hex))]
        Hex = 1,

        /// <summary>
        /// The <c>Key</c> string is the literal cipher key encoded as Base64 (RFC 4648,
        /// optionally padded with <c>=</c>), no KDF, no <c>Encoding</c> involvement. The
        /// decoded byte length must match a legal key size for the algorithm. Required for
        /// <see cref="SymmetricWireFormat.Raw"/>; rejected for the other formats.
        /// </summary>
        [LocalizedDescription(nameof(Resources.KeyBytesFormat_Base64))]
        Base64 = 2,
    }
}
