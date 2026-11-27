using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpClearSignFileViewModel))]
    public partial class PgpClearSignFile
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpClearSignFileViewModel : PgpSignViewModelBase
    {
        public PgpClearSignFileViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignOutArgument<ILocalResource> ClearSignedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeOutputProperty(int orderIndex)
        {
            ClearSignedFile.IsPrincipal = false;
            ClearSignedFile.OrderIndex = orderIndex;
            ClearSignedFile.Category = Resources.Output;
        }

        protected override void ConfigurePropertyTexts()
        {
            InputFilePath.DisplayName = Resources.Activity_PgpClearSignFile_Property_InputFilePath_Name;
            InputFilePath.Tooltip = Resources.Activity_PgpClearSignFile_Property_InputFilePath_Description;
            InputFile.DisplayName = Resources.Activity_PgpClearSignFile_Property_InputFile_Name;
            InputFile.Tooltip = Resources.Activity_PgpClearSignFile_Property_InputFile_Description;
            PrivateKeyFilePath.DisplayName = Resources.Activity_PgpClearSignFile_Property_PrivateKeyFilePath_Name;
            PrivateKeyFilePath.Tooltip = Resources.Activity_PgpClearSignFile_Property_PrivateKeyFilePath_Description;
            PrivateKeyFile.DisplayName = Resources.Activity_PgpClearSignFile_Property_PrivateKeyFile_Name;
            PrivateKeyFile.Tooltip = Resources.Activity_PgpClearSignFile_Property_PrivateKeyFile_Description;
            Passphrase.DisplayName = Resources.Activity_PgpClearSignFile_Property_Passphrase_Name;
            Passphrase.Tooltip = Resources.Activity_PgpClearSignFile_Property_Passphrase_Description;
            PassphraseSecureString.DisplayName = Resources.Activity_PgpClearSignFile_Property_PassphraseSecureString_Name;
            PassphraseSecureString.Tooltip = Resources.Activity_PgpClearSignFile_Property_PassphraseSecureString_Description;
            OutputFilePath.DisplayName = Resources.Activity_PgpClearSignFile_Property_OutputFilePath_Name;
            OutputFilePath.Tooltip = Resources.Activity_PgpClearSignFile_Property_OutputFilePath_Description;
            Overwrite.DisplayName = Resources.Activity_PgpClearSignFile_Property_Overwrite_Name;
            Overwrite.Tooltip = Resources.Activity_PgpClearSignFile_Property_Overwrite_Description;
            ContinueOnError.DisplayName = Resources.Activity_PgpClearSignFile_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_PgpClearSignFile_Property_ContinueOnError_Description;
            ClearSignedFile.DisplayName = Resources.Activity_PgpClearSignFile_Property_ClearSignedFile_Name;
            ClearSignedFile.Tooltip = Resources.Activity_PgpClearSignFile_Property_ClearSignedFile_Description;
        }
    }
}
