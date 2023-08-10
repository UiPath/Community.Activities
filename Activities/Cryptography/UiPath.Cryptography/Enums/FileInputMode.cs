using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    /// <summary>
    /// Enum used to switch input mode between IResource and string
    /// </summary>
    public enum FileInputMode
    {
        /// <summary>
        /// File in a string format
        /// </summary>
        [LocalizedDescription(nameof(Resources.General_FilePathInput))]
        FilePath,

        /// <summary>
        /// File in a IResource format
        /// </summary>
        [LocalizedDescription(nameof(Resources.General_FileInput))]
        File

    }
}
