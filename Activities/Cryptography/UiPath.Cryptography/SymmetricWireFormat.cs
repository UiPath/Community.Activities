using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    /// <summary>
    /// Selects the byte layout and key-derivation strategy used by the symmetric
    /// encrypt/decrypt activities. See <c>docs/symmetric-wire-format.md</c> for the
    /// full reference, including third-party interop notes.
    /// </summary>
    public enum SymmetricWireFormat
    {
        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_Classic))]
        Classic = 0,

        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_Owasp2026))]
        Owasp2026 = 1,

        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_Raw))]
        Raw = 2,

        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_OpenSslEnc))]
        OpenSslEnc = 3,
    }
}
