using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    /// <summary>
    /// Enum used to switch input mode between string and secure string
    /// </summary>
    public enum KeyInputMode
    {
        /// <summary>
        /// Key in a text format
        /// </summary>
        [LocalizedDescription(nameof(Resources.Key))]
        Key,

        /// <summary>
        /// Key in a secure string format
        /// </summary>
        [LocalizedDescription(nameof(Resources.SecureKey))]
        SecureKey,
    }
}
