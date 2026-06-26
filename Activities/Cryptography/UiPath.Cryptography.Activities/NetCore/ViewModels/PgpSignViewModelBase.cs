using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public abstract class PgpSignViewModelBase : DesignPropertiesViewModel
    {
        private readonly PairedInputToggle<string, IResource> _inputFileToggle;
        private readonly PairedInputToggle<string, IResource> _privateKeyFileToggle;
        private readonly PairedInputToggle<string, SecureString> _passphraseToggle;

        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<IResource> PrivateKeyFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> Passphrase { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> PassphraseSecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected PgpSignViewModelBase(IDesignServices services) : base(services)
        {
            _inputFileToggle = new PairedInputToggle<string, IResource>(
                InputFilePath, InputFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyInputFileVisibility,
            };

            _privateKeyFileToggle = new PairedInputToggle<string, IResource>(
                PrivateKeyFilePath, PrivateKeyFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyPrivateKeyFileVisibility,
            };

            _passphraseToggle = new PairedInputToggle<string, SecureString>(
                Passphrase, PassphraseSecureString,
                Resources.MenuAction_UsePassphrase,
                Resources.MenuAction_UseSecurePassphrase)
            {
                AfterSwitch = ApplyPassphraseVisibility,
            };
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            InputFilePath.IsPrincipal = true;
            InputFilePath.IsRequired = true;
            InputFilePath.OrderIndex = orderIndex;
            InputFilePath.Category = Resources.Input;

            InputFile.IsPrincipal = true;
            InputFile.IsVisible = false;
            InputFile.OrderIndex = orderIndex;
            InputFile.Category = Resources.Input;
            orderIndex++;

            PrivateKeyFilePath.IsPrincipal = true;
            PrivateKeyFilePath.IsRequired = true;
            PrivateKeyFilePath.OrderIndex = orderIndex;
            PrivateKeyFilePath.Category = Resources.Input;

            PrivateKeyFile.IsPrincipal = true;
            PrivateKeyFile.IsVisible = false;
            PrivateKeyFile.OrderIndex = orderIndex;
            PrivateKeyFile.Category = Resources.Input;
            orderIndex++;

            Passphrase.IsPrincipal = true;
            Passphrase.IsRequired = true;
            Passphrase.OrderIndex = orderIndex;
            Passphrase.Category = Resources.Input;

            PassphraseSecureString.IsPrincipal = true;
            PassphraseSecureString.IsRequired = true;
            PassphraseSecureString.OrderIndex = orderIndex;
            PassphraseSecureString.Category = Resources.Input;
            orderIndex++;

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = orderIndex++;
            OutputFilePath.Category = Resources.Category_Options_Name;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Category_Options_Name;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            ContinueOnError.Value = false;

            _inputFileToggle.ConfigureMenuActions();
            ApplyInputFileVisibility();

            _privateKeyFileToggle.ConfigureMenuActions();
            ApplyPrivateKeyFileVisibility();

            _passphraseToggle.ConfigureMenuActions();
            ApplyPassphraseVisibility();

            InitializeOutputProperty(orderIndex);
            ConfigurePropertyTexts();
        }

        protected abstract void InitializeOutputProperty(int orderIndex);
        protected abstract void ConfigurePropertyTexts();

        private void ApplyInputFileVisibility()
        {
            bool useResource = _inputFileToggle.UseSecondary;
            InputFile.IsVisible = useResource;
            InputFile.IsRequired = useResource;
            InputFilePath.IsVisible = !useResource;
            InputFilePath.IsRequired = !useResource;
        }

        private void ApplyPrivateKeyFileVisibility()
        {
            bool useResource = _privateKeyFileToggle.UseSecondary;
            PrivateKeyFile.IsVisible = useResource;
            PrivateKeyFile.IsRequired = useResource;
            PrivateKeyFilePath.IsVisible = !useResource;
            PrivateKeyFilePath.IsRequired = !useResource;
        }

        private void ApplyPassphraseVisibility()
        {
            bool useSecure = _passphraseToggle.UseSecondary;
            Passphrase.IsVisible = !useSecure;
            Passphrase.IsRequired = !useSecure;
            PassphraseSecureString.IsVisible = useSecure;
            PassphraseSecureString.IsRequired = useSecure;
        }
    }
}
