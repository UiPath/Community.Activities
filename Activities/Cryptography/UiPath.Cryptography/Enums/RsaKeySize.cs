using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    public enum RsaKeySize
    {
        [LocalizedDescription(nameof(Resources.RsaKeySize_Rsa2048))]
        Rsa2048 = 2048,

        [LocalizedDescription(nameof(Resources.RsaKeySize_Rsa3072))]
        Rsa3072 = 3072,

        [LocalizedDescription(nameof(Resources.RsaKeySize_Rsa4096))]
        Rsa4096 = 4096
    }
}
