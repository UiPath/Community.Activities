using UiPath.FTP.Properties;

namespace UiPath.FTP.Enums
{
    /// <summary>
    /// Enum used to switch input mode between string and secure string
    /// </summary>
    public enum PasswordInputMode
    {
        /// <summary>
        /// Password in a text format
        /// </summary>
        [LocalizedDescription(nameof(Resources.Password))]
        Password,

        /// <summary>
        /// Password in a secure string format
        /// </summary>
        [LocalizedDescription(nameof(Resources.SecurePassword))]
        SecurePassword,
    }
}
