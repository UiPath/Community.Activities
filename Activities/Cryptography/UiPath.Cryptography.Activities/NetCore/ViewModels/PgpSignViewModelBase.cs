using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public abstract class PgpSignViewModelBase : DesignPropertiesViewModel
    {
        private InArgument<IResource> _persistedInputFile;
        private InArgument<string> _persistedInputFilePath;
        private InArgument<IResource> _persistedPrivateKeyFile;
        private InArgument<string> _persistedPrivateKeyFilePath;
        private InArgument<string> _persistedPassphrase;
        private InArgument<SecureString> _persistedPassphraseSecureString;
        private bool _useSecurePassphrase;

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
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            InputFilePath.IsPrincipal = true;
            InputFilePath.OrderIndex = orderIndex;
            InputFilePath.Category = Resources.Input;

            InputFile.IsPrincipal = true;
            InputFile.OrderIndex = orderIndex;
            InputFile.Category = Resources.Input;
            orderIndex++;

            PrivateKeyFilePath.IsPrincipal = true;
            PrivateKeyFilePath.OrderIndex = orderIndex;
            PrivateKeyFilePath.Category = Resources.Input;

            PrivateKeyFile.IsPrincipal = true;
            PrivateKeyFile.OrderIndex = orderIndex;
            PrivateKeyFile.Category = Resources.Input;
            orderIndex++;

            Passphrase.IsPrincipal = true;
            Passphrase.OrderIndex = orderIndex;
            Passphrase.Category = Resources.Input;

            PassphraseSecureString.IsPrincipal = true;
            PassphraseSecureString.OrderIndex = orderIndex;
            PassphraseSecureString.Category = Resources.Input;
            orderIndex++;

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = orderIndex++;
            OutputFilePath.Category = Resources.Input;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Category_Options_Name;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            ContinueOnError.Value = false;

            ConfigureInputFileMenuActions();
            ConfigurePrivateKeyFileMenuActions();
            ConfigurePassphraseInputModeMenuActions();

            InitializeOutputProperty(orderIndex);
        }

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

        protected abstract void InitializeOutputProperty(int orderIndex);

        private void ConfigureInputFileMenuActions()
        {
            var useFileMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFile,
                IsMain = true,
                Handler = SwitchToInputFile,
            };
            var useFilePathMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFilePath,
                IsMain = true,
                Handler = SwitchToInputFilePath,
            };
            InputFilePath.AddMenuAction(useFileMenuAction);
            InputFile.AddMenuAction(useFilePathMenuAction);

            bool useFile = InputFile.HasValue && !InputFilePath.HasValue;
            InputFile.IsVisible = useFile;
            InputFile.IsRequired = useFile;
            InputFilePath.IsVisible = !useFile;
            InputFilePath.IsRequired = !useFile;
        }

        private void ConfigurePrivateKeyFileMenuActions()
        {
            var useFileMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFile,
                IsMain = true,
                Handler = SwitchToPrivateKeyFile,
            };
            var useFilePathMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFilePath,
                IsMain = true,
                Handler = SwitchToPrivateKeyFilePath,
            };
            PrivateKeyFilePath.AddMenuAction(useFileMenuAction);
            PrivateKeyFile.AddMenuAction(useFilePathMenuAction);

            bool useFile = PrivateKeyFile.HasValue && !PrivateKeyFilePath.HasValue;
            PrivateKeyFile.IsVisible = useFile;
            PrivateKeyFile.IsRequired = useFile;
            PrivateKeyFilePath.IsVisible = !useFile;
            PrivateKeyFilePath.IsRequired = !useFile;
        }

        private Task SwitchToInputFile(MenuAction _)
        {
            if (InputFilePath.Value != null) _persistedInputFilePath = InputFilePath.Value;
            InputFilePath.Value = null;
            InputFile.Value = _persistedInputFile;
            InputFile.IsVisible = true;
            InputFile.IsRequired = true;
            InputFilePath.IsVisible = false;
            InputFilePath.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToInputFilePath(MenuAction _)
        {
            if (InputFile.Value != null) _persistedInputFile = InputFile.Value;
            InputFile.Value = null;
            InputFilePath.Value = _persistedInputFilePath;
            InputFilePath.IsVisible = true;
            InputFilePath.IsRequired = true;
            InputFile.IsVisible = false;
            InputFile.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToPrivateKeyFile(MenuAction _)
        {
            if (PrivateKeyFilePath.Value != null) _persistedPrivateKeyFilePath = PrivateKeyFilePath.Value;
            PrivateKeyFilePath.Value = null;
            PrivateKeyFile.Value = _persistedPrivateKeyFile;
            PrivateKeyFile.IsVisible = true;
            PrivateKeyFile.IsRequired = true;
            PrivateKeyFilePath.IsVisible = false;
            PrivateKeyFilePath.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToPrivateKeyFilePath(MenuAction _)
        {
            if (PrivateKeyFile.Value != null) _persistedPrivateKeyFile = PrivateKeyFile.Value;
            PrivateKeyFile.Value = null;
            PrivateKeyFilePath.Value = _persistedPrivateKeyFilePath;
            PrivateKeyFilePath.IsVisible = true;
            PrivateKeyFilePath.IsRequired = true;
            PrivateKeyFile.IsVisible = false;
            PrivateKeyFile.IsRequired = false;
            return Task.CompletedTask;
        }
    }
}
