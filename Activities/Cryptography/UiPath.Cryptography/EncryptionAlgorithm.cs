using System;
using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    public enum EncryptionAlgorithm
    {
        [Obsolete("No longer safe")]
        [LocalizedDescription(nameof(Resources.AES))]
        AES,

        [LocalizedDescription(nameof(Resources.AESGCM))]
        AESGCM,

        [Obsolete("No longer safe")]
        [LocalizedDescription(nameof(Resources.DES))]
        DES,

        [Obsolete("No longer safe")]
        [LocalizedDescription(nameof(Resources.RC2))]
        RC2,

        [Obsolete("No longer safe")]
        [LocalizedDescription(nameof(Resources.Rijndael))]
        Rijndael,

        [Obsolete("No longer safe")]
        [LocalizedDescription(nameof(Resources.TripleDES))]
        TripleDES,

        [LocalizedDescription(nameof(Resources.PGP))]
        PGP,

        [LocalizedDescription(nameof(Resources.ChaCha20Poly1305))]
        ChaCha20Poly1305
    }
}
