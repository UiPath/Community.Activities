using System.ComponentModel;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [Browsable(false)]
    [LocalizedDisplayName(nameof(Resources.Activity_HashText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_HashText_Description))]
    public partial class HashText : KeyedHashText
    {
    }
}