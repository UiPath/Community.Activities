using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    public enum SymmetricAlgorithms
    {
        [LocalizedDescription(nameof(Resources.AES))]
        AES,

        [LocalizedDescription(nameof(Resources.AESGCM))]
        AESGCM,

        [LocalizedDescription(nameof(Resources.DES))]
        DES,

        [LocalizedDescription(nameof(Resources.RC2))]
        RC2,

        [LocalizedDescription(nameof(Resources.Rijndael))]
        Rijndael,

        [LocalizedDescription(nameof(Resources.TripleDES))]
        TripleDES
    }
}