using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class DecryptFileViewModel : DecryptCryptoViewModelBase
    {
        private readonly PairedInputToggle<string, IResource> _inputFileToggle;

        public DecryptFileViewModel(IDesignServices services) : base(services)
        {
            _inputFileToggle = new PairedInputToggle<string, IResource>(
                InputFilePath, InputFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyInputFileVisibility,
            };
        }

        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();
        public DesignOutArgument<ILocalResource> DecryptedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.OrderIndex = orderIndex;
            InputFile.Category = Resources.Input;

            InputFilePath.IsPrincipal = true;
            InputFilePath.OrderIndex = orderIndex;
            InputFilePath.Category = Resources.Input;
            orderIndex++;

            ConfigureAlgorithmAndKeyProperties(ref orderIndex);
            ConfigureInteropProperties(ref orderIndex);

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = orderIndex++;
            OutputFilePath.Category = Resources.Category_Options_Name;

            Overwrite.IsPrincipal = true;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Category_Options_Name;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ConfigureTailProperties(ref orderIndex);
            ConfigureKeyInputModeMenuActions();
            ConfigurePublicKeyFileMenuActions();
            ConfigurePrivateKeyFileMenuActions();
            ConfigureInputFileMenuActions();
            ConfigurePassphraseInputModeMenuActions();

            DecryptedFile.IsPrincipal = false;
            DecryptedFile.OrderIndex = orderIndex;
            DecryptedFile.Category = Resources.Output;

            ConfigurePropertyTexts();
        }

        private void ConfigureInputFileMenuActions()
        {
            _inputFileToggle.ConfigureMenuActions();
            ApplyInputFileVisibility();
        }

        private void ConfigurePropertyTexts()
        {
            InputFile.DisplayName = Resources.Activity_DecryptFile_Property_InputFile_Name;
            InputFile.Tooltip = Resources.Activity_DecryptFile_Property_InputFile_Description;
            InputFilePath.DisplayName = Resources.Activity_DecryptFile_Property_InputFilePath_Name;
            InputFilePath.Tooltip = Resources.Activity_DecryptFile_Property_InputFilePath_Description;
            Algorithm.DisplayName = Resources.Activity_DecryptFile_Property_Algorithm_Name;
            Algorithm.Tooltip = Resources.Activity_DecryptFile_Property_Algorithm_Description;
            Key.DisplayName = Resources.Activity_DecryptFile_Property_Key_Name;
            Key.Tooltip = Resources.Activity_DecryptFile_Property_Key_Description;
            KeySecureString.DisplayName = Resources.Activity_DecryptFile_Property_KeySecureString_Name;
            KeySecureString.Tooltip = Resources.Activity_DecryptFile_Property_KeySecureString_Description;
            OutputFilePath.DisplayName = Resources.Activity_DecryptFile_Property_OutputFilePath_Name;
            OutputFilePath.Tooltip = Resources.Activity_DecryptFile_Property_OutputFilePath_Description;
            KeyEncodingString.DisplayName = Resources.Activity_DecryptFile_Property_KeyEncodingString_Name;
            KeyEncodingString.Tooltip = Resources.Activity_DecryptFile_Property_KeyEncodingString_Description;
            Format.DisplayName = Resources.Activity_DecryptFile_Property_Format_Name;
            Format.Tooltip = Resources.Activity_DecryptFile_Property_Format_Description;
            KeyFormat.DisplayName = Resources.Activity_DecryptFile_Property_KeyFormat_Name;
            KeyFormat.Tooltip = Resources.Activity_DecryptFile_Property_KeyFormat_Description;
            KdfIterations.DisplayName = Resources.Activity_DecryptFile_Property_KdfIterations_Name;
            KdfIterations.Tooltip = Resources.Activity_DecryptFile_Property_KdfIterations_Description;
            AesKeySize.DisplayName = Resources.Activity_DecryptFile_Property_AesKeySize_Name;
            AesKeySize.Tooltip = Resources.Activity_DecryptFile_Property_AesKeySize_Description;
            Overwrite.DisplayName = Resources.Activity_DecryptFile_Property_Overwrite_Name;
            Overwrite.Tooltip = Resources.Activity_DecryptFile_Property_Overwrite_Description;
            ContinueOnError.DisplayName = Resources.Activity_DecryptFile_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_DecryptFile_Property_ContinueOnError_Description;
            PrivateKeyFilePath.DisplayName = Resources.Activity_DecryptFile_Property_PrivateKeyFilePath_Name;
            PrivateKeyFilePath.Tooltip = Resources.Activity_DecryptFile_Property_PrivateKeyFilePath_Description;
            PrivateKeyFile.DisplayName = Resources.Activity_DecryptFile_Property_PrivateKeyFile_Name;
            PrivateKeyFile.Tooltip = Resources.Activity_DecryptFile_Property_PrivateKeyFile_Description;
            Passphrase.DisplayName = Resources.Activity_DecryptFile_Property_Passphrase_Name;
            Passphrase.Tooltip = Resources.Activity_DecryptFile_Property_Passphrase_Description;
            PassphraseSecureString.DisplayName = Resources.Activity_DecryptFile_Property_PassphraseSecureString_Name;
            PassphraseSecureString.Tooltip = Resources.Activity_DecryptFile_Property_PassphraseSecureString_Description;
            VerifySignature.DisplayName = Resources.Activity_DecryptFile_Property_VerifySignature_Name;
            VerifySignature.Tooltip = Resources.Activity_DecryptFile_Property_VerifySignature_Description;
            PublicKeyFilePath.DisplayName = Resources.Activity_DecryptFile_Property_PublicKeyFilePath_Name;
            PublicKeyFilePath.Tooltip = Resources.Activity_DecryptFile_Property_PublicKeyFilePath_Description;
            PublicKeyFile.DisplayName = Resources.Activity_DecryptFile_Property_PublicKeyFile_Name;
            PublicKeyFile.Tooltip = Resources.Activity_DecryptFile_Property_PublicKeyFile_Description;
            DecryptedFile.DisplayName = Resources.Activity_DecryptFile_Property_DecryptedFile_Name;
            DecryptedFile.Tooltip = Resources.Activity_DecryptFile_Property_DecryptedFile_Description;
        }

        private void ApplyInputFileVisibility()
        {
            bool useResource = _inputFileToggle.UseSecondary;
            InputFile.IsVisible = useResource;
            InputFile.IsRequired = useResource;
            InputFilePath.IsVisible = !useResource;
            InputFilePath.IsRequired = !useResource;
        }
    }
}
