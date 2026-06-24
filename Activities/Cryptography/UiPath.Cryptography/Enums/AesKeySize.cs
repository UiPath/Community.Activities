using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    public enum AesKeySize
    {
        [LocalizedDescription(nameof(Resources.AesKeySize_Aes128))]
        Aes128 = 128,

        [LocalizedDescription(nameof(Resources.AesKeySize_Aes192))]
        Aes192 = 192,

        [LocalizedDescription(nameof(Resources.AesKeySize_Aes256))]
        Aes256 = 256
    }
}
