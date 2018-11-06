using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    public enum KeyedHashAlgorithms
    {
        [LocalizedDescription(nameof(Resources.HMACMD5))]
        HMACMD5,
        [LocalizedDescription(nameof(Resources.HMACRIPEMD160))]
        HMACRIPEMD160,
        [LocalizedDescription(nameof(Resources.HMACSHA1))]
        HMACSHA1,
        [LocalizedDescription(nameof(Resources.HMACSHA256))]
        HMACSHA256,
        [LocalizedDescription(nameof(Resources.HMACSHA384))]
        HMACSHA384,
        [LocalizedDescription(nameof(Resources.HMACSHA512))]
        HMACSHA512,
        [LocalizedDescription(nameof(Resources.MACTripleDES))]
        MACTripleDES
    }
}
