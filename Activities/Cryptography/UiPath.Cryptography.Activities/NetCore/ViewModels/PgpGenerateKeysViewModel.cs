using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpGenerateKeysViewModel))]
    public partial class PgpGenerateKeys
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpGenerateKeysViewModel : DesignPropertiesViewModel
    {
        private InArgument<string> _persistedPassphrase;
        private InArgument<SecureString> _persistedPassphraseSecureString;
        private bool _useSecurePassphrase;

        public PgpGenerateKeysViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> UserId { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> Passphrase { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> PassphraseSecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignInArgument<bool> Overwrite { get; set; } = new DesignInArgument<bool>();
        public DesignInArgument<RsaKeySize> KeySize { get; set; } = new DesignInArgument<RsaKeySize>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();
        public DesignOutArgument<ILocalResource> PublicKeyFile { get; set; } = new DesignOutArgument<ILocalResource>();
        public DesignOutArgument<ILocalResource> PrivateKeyFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            PublicKeyFilePath.IsPrincipal = true;
            PublicKeyFilePath.IsRequired = true;
            PublicKeyFilePath.OrderIndex = orderIndex++;
            PublicKeyFilePath.Category = Resources.Input;

            UserId.IsPrincipal = true;
            UserId.IsRequired = true;
            UserId.OrderIndex = orderIndex++;
            UserId.Category = Resources.Input;
            UserId.EditPlaceholder = Resources.Activity_PgpGenerateKeys_Property_UserId_Hint;

            PrivateKeyFilePath.IsPrincipal = true;
            PrivateKeyFilePath.IsRequired = true;
            PrivateKeyFilePath.OrderIndex = orderIndex++;
            PrivateKeyFilePath.Category = Resources.Input;

            Passphrase.IsPrincipal = true;
            Passphrase.IsRequired = true;
            Passphrase.OrderIndex = orderIndex;
            Passphrase.Category = Resources.Input;

            PassphraseSecureString.IsPrincipal = true;
            PassphraseSecureString.IsRequired = true;
            PassphraseSecureString.OrderIndex = orderIndex;
            PassphraseSecureString.Category = Resources.Input;
            orderIndex++;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Category_Options_Name;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            KeySize.IsPrincipal = false;
            KeySize.OrderIndex = orderIndex++;
            KeySize.Category = Resources.Category_Options_Name;
            KeySize.DataSource = DataSourceHelper.ForEnum(
                RsaKeySize.Rsa2048,
                RsaKeySize.Rsa3072,
                RsaKeySize.Rsa4096);
            KeySize.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };
            KeySize.Value = RsaKeySize.Rsa4096;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            ContinueOnError.Value = false;

            PublicKeyFile.IsPrincipal = false;
            PublicKeyFile.OrderIndex = orderIndex++;
            PublicKeyFile.Category = Resources.Output;
            PublicKeyFile.EditPlaceholder = Resources.Activity_PgpGenerateKeys_Property_PublicKeyFile_Hint;

            PrivateKeyFile.IsPrincipal = false;
            PrivateKeyFile.OrderIndex = orderIndex;
            PrivateKeyFile.Category = Resources.Output;
            PrivateKeyFile.EditPlaceholder = Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFile_Hint;

            ConfigurePassphraseInputModeMenuActions();
            ConfigurePropertyTexts();
        }

        private void ConfigurePropertyTexts()
        {
            PublicKeyFilePath.DisplayName = Resources.Activity_PgpGenerateKeys_Property_PublicKeyFilePath_Name;
            PublicKeyFilePath.Tooltip = Resources.Activity_PgpGenerateKeys_Property_PublicKeyFilePath_Description;
            UserId.DisplayName = Resources.Activity_PgpGenerateKeys_Property_UserId_Name;
            UserId.Tooltip = Resources.Activity_PgpGenerateKeys_Property_UserId_Description;
            PrivateKeyFilePath.DisplayName = Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFilePath_Name;
            PrivateKeyFilePath.Tooltip = Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFilePath_Description;
            Passphrase.DisplayName = Resources.Activity_PgpGenerateKeys_Property_Password_Name;
            Passphrase.Tooltip = Resources.Activity_PgpGenerateKeys_Property_Password_Description;
            PassphraseSecureString.DisplayName = Resources.Activity_PgpGenerateKeys_Property_PassphraseSecureString_Name;
            PassphraseSecureString.Tooltip = Resources.Activity_PgpGenerateKeys_Property_PassphraseSecureString_Description;
            Overwrite.DisplayName = Resources.Activity_PgpGenerateKeys_Property_Overwrite_Name;
            Overwrite.Tooltip = Resources.Activity_PgpGenerateKeys_Property_Overwrite_Description;
            KeySize.DisplayName = Resources.Activity_PgpGenerateKeys_Property_KeySize_Name;
            KeySize.Tooltip = Resources.Activity_PgpGenerateKeys_Property_KeySize_Description;
            ContinueOnError.DisplayName = Resources.Activity_PgpGenerateKeys_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_PgpGenerateKeys_Property_ContinueOnError_Description;
            PublicKeyFile.DisplayName = Resources.Activity_PgpGenerateKeys_Property_PublicKeyFile_Name;
            PublicKeyFile.Tooltip = Resources.Activity_PgpGenerateKeys_Property_PublicKeyFile_Description;
            PrivateKeyFile.DisplayName = Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFile_Name;
            PrivateKeyFile.Tooltip = Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFile_Description;
        }

        /// <summary>
        /// Registers Main-menu actions to toggle between Passphrase (string) and PassphraseSecureString (SecureString),
        /// and sets initial visibility based on which side has a persisted value.
        /// </summary>
        private void ConfigurePassphraseInputModeMenuActions()
        {
            var usePassphraseMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UsePassphrase,
                IsMain = true,
                Handler = SwitchToPassphrase,
            };
            var useSecurePassphraseMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseSecurePassphrase,
                IsMain = true,
                Handler = SwitchToPassphraseSecureString,
            };
            PassphraseSecureString.AddMenuAction(usePassphraseMenuAction);
            Passphrase.AddMenuAction(useSecurePassphraseMenuAction);

            _useSecurePassphrase = PassphraseSecureString.HasValue && !Passphrase.HasValue;
            Passphrase.IsVisible = !_useSecurePassphrase;
            Passphrase.IsRequired = !_useSecurePassphrase;
            PassphraseSecureString.IsVisible = _useSecurePassphrase;
            PassphraseSecureString.IsRequired = _useSecurePassphrase;
        }

        private Task SwitchToPassphrase(MenuAction _)
        {
            if (PassphraseSecureString.Value != null) _persistedPassphraseSecureString = PassphraseSecureString.Value;
            PassphraseSecureString.Value = null;
            Passphrase.Value = _persistedPassphrase;
            Passphrase.IsVisible = true;
            Passphrase.IsRequired = true;
            PassphraseSecureString.IsVisible = false;
            PassphraseSecureString.IsRequired = false;
            _useSecurePassphrase = false;
            return Task.CompletedTask;
        }

        private Task SwitchToPassphraseSecureString(MenuAction _)
        {
            if (Passphrase.Value != null) _persistedPassphrase = Passphrase.Value;
            Passphrase.Value = null;
            PassphraseSecureString.Value = _persistedPassphraseSecureString;
            PassphraseSecureString.IsVisible = true;
            PassphraseSecureString.IsRequired = true;
            Passphrase.IsVisible = false;
            Passphrase.IsRequired = false;
            _useSecurePassphrase = true;
            return Task.CompletedTask;
        }
    }
}
