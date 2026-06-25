using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Decrypts text based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(DecryptTextViewModel))]
    public partial class DecryptText
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class DecryptTextViewModel : DecryptCryptoViewModelBase
    {
        public DecryptTextViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> PlaintextEncodingString { get; set; } = new() { Name = nameof(PlaintextEncodingString) };
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            Input.IsPrincipal = true;
            Input.IsRequired = true;
            Input.OrderIndex = orderIndex++;
            Input.Category = Resources.Input;

            ConfigureAlgorithmAndKeyProperties(ref orderIndex);
            ConfigureEncodingDropdown(PlaintextEncodingString, ref orderIndex);
            ConfigureInteropProperties(ref orderIndex);

            Result.IsPrincipal = false;
            Result.OrderIndex = orderIndex++;
            Result.Category = Resources.Output;

            ConfigureTailProperties(ref orderIndex);
            ConfigurePropertyTexts();
            ConfigureKeyInputModeMenuActions();
            ConfigurePublicKeyFileMenuActions();
            ConfigurePassphraseInputModeMenuActions();
        }

        protected override void OnAlgorithmChanged(bool isPgp)
        {
            PlaintextEncodingString.IsVisible = !isPgp;
        }

        private void ConfigurePropertyTexts()
        {
            Algorithm.DisplayName = Resources.Activity_DecryptText_Property_Algorithm_Name;
            Algorithm.Tooltip = Resources.Activity_DecryptText_Property_Algorithm_Description;
            Input.DisplayName = Resources.Activity_DecryptText_Property_Input_Name;
            Input.Tooltip = Resources.Activity_DecryptText_Property_Input_Description;
            Key.DisplayName = Resources.Activity_DecryptText_Property_Key_Name;
            Key.Tooltip = Resources.Activity_DecryptText_Property_Key_Description;
            KeySecureString.DisplayName = Resources.Activity_DecryptText_Property_KeySecureString_Name;
            KeySecureString.Tooltip = Resources.Activity_DecryptText_Property_KeySecureString_Description;
            KeyEncodingString.DisplayName = Resources.Activity_DecryptText_Property_KeyEncodingString_Name;
            KeyEncodingString.Tooltip = Resources.Activity_DecryptText_Property_KeyEncodingString_Description;
            PlaintextEncodingString.DisplayName = Resources.Activity_DecryptText_Property_PlaintextEncodingString_Name;
            PlaintextEncodingString.Tooltip = Resources.Activity_DecryptText_Property_PlaintextEncodingString_Description;
            Format.DisplayName = Resources.Activity_DecryptText_Property_Format_Name;
            Format.Tooltip = Resources.Activity_DecryptText_Property_Format_Description;
            KeyFormat.DisplayName = Resources.Activity_DecryptText_Property_KeyFormat_Name;
            KeyFormat.Tooltip = Resources.Activity_DecryptText_Property_KeyFormat_Description;
            KdfIterations.DisplayName = Resources.Activity_DecryptText_Property_KdfIterations_Name;
            KdfIterations.Tooltip = Resources.Activity_DecryptText_Property_KdfIterations_Description;
            AesKeySize.DisplayName = Resources.Activity_DecryptText_Property_AesKeySize_Name;
            AesKeySize.Tooltip = Resources.Activity_DecryptText_Property_AesKeySize_Description;
            ContinueOnError.DisplayName = Resources.Activity_DecryptText_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_DecryptText_Property_ContinueOnError_Description;
            PrivateKeyFilePath.DisplayName = Resources.Activity_DecryptText_Property_PrivateKeyFilePath_Name;
            PrivateKeyFilePath.Tooltip = Resources.Activity_DecryptText_Property_PrivateKeyFilePath_Description;
            Passphrase.DisplayName = Resources.Activity_DecryptText_Property_Passphrase_Name;
            Passphrase.Tooltip = Resources.Activity_DecryptText_Property_Passphrase_Description;
            PassphraseSecureString.DisplayName = Resources.Activity_DecryptText_Property_PassphraseSecureString_Name;
            PassphraseSecureString.Tooltip = Resources.Activity_DecryptText_Property_PassphraseSecureString_Description;
            VerifySignature.DisplayName = Resources.Activity_DecryptText_Property_VerifySignature_Name;
            VerifySignature.Tooltip = Resources.Activity_DecryptText_Property_VerifySignature_Description;
            PublicKeyFilePath.DisplayName = Resources.Activity_DecryptText_Property_PublicKeyFilePath_Name;
            PublicKeyFilePath.Tooltip = Resources.Activity_DecryptText_Property_PublicKeyFilePath_Description;
            PublicKeyFile.DisplayName = Resources.Activity_DecryptText_Property_PublicKeyFile_Name;
            PublicKeyFile.Tooltip = Resources.Activity_DecryptText_Property_PublicKeyFile_Description;
            Result.DisplayName = Resources.Activity_DecryptText_Property_Result_Name;
            Result.Tooltip = Resources.Activity_DecryptText_Property_Result_Description;
        }
    }
}
