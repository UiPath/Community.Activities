using System.ComponentModel;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [Browsable(false)]
    [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_HashFile_Description))]
    public class HashFile : KeyedHashFile
    {
    }
}