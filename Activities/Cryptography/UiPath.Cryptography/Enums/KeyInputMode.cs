using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    public enum KeyInputMode
    {
        /// <summary>
        /// Key in a text format
        /// </summary>
        [LocalizedDescription(nameof(Resources.KEY))]
        Key,

        /// <summary>
        /// Key in a secure string format
        /// </summary>
        [LocalizedDescription(nameof(Resources.SECUREKEY))]
        SecureKey,

    }
}
