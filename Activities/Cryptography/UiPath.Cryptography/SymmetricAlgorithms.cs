using System;
using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    public enum SymmetricAlgorithms
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

        [LocalizedDescription(nameof(Resources.TripleDES))]
        TripleDES
    }
}