using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    public enum HashAlgorithms
    {
        [LocalizedDescription(nameof(Resources.MD5))]
        MD5,
        [LocalizedDescription(nameof(Resources.RIPEMD160))]
        RIPEMD160,
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
