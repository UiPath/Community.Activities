using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    public enum KeyedHashAlgorithms
    {
        [LocalizedDescription(nameof(Resources.HMACMD5))]
        HMACMD5,

#if NET461

        [LocalizedDescription(nameof(Resources.HMACRIPEMD160))]
        HMACRIPEMD160,

#endif

        [LocalizedDescription(nameof(Resources.HMACSHA1))]
        HMACSHA1,

        [LocalizedDescription(nameof(Resources.HMACSHA256))]
        HMACSHA256,

        [LocalizedDescription(nameof(Resources.HMACSHA384))]
        HMACSHA384,

        [LocalizedDescription(nameof(Resources.HMACSHA512))]
        HMACSHA512,

#if NET461

        [LocalizedDescription(nameof(Resources.MACTripleDES))]
        MACTripleDES,

#endif

        [LocalizedDescription(nameof(Resources.SHA1))]
        SHA1,

        [LocalizedDescription(nameof(Resources.SHA256))]
        SHA256,

        [LocalizedDescription(nameof(Resources.SHA384))]
        SHA384,

        [LocalizedDescription(nameof(Resources.SHA512))]
        SHA512

    }
}