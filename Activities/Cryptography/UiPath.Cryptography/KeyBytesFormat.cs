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
        [LocalizedDescription(nameof(Resources.KeyBytesFormat_Encoded))]
        Encoded = 0,

        [LocalizedDescription(nameof(Resources.KeyBytesFormat_Hex))]
        Hex = 1,

        [LocalizedDescription(nameof(Resources.KeyBytesFormat_Base64))]
        Base64 = 2,
    }
}
