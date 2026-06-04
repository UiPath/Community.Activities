using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpVerifyViewModel))]
    public partial class PgpVerify
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpVerifyViewModel : DesignPropertiesViewModel
    {
        private InArgument<IResource> _persistedInputFile;
        private InArgument<string> _persistedInputFilePath;
        private InArgument<IResource> _persistedPublicKeyFile;
        private InArgument<string> _persistedPublicKeyFilePath;

        public PgpVerifyViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignProperty<PgpVerifyMode> Mode { get; set; } = new DesignProperty<PgpVerifyMode>();
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<IResource> PublicKeyFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();
        public DesignOutArgument<bool> Result { get; set; } = new DesignOutArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            Mode.IsPrincipal = true;
            Mode.OrderIndex = orderIndex++;
            Mode.Category = Resources.Input;
            Mode.DataSource = DataSourceHelper.ForEnum(
                PgpVerifyMode.Signature,
                PgpVerifyMode.ClearSignature,
                PgpVerifyMode.PublicKey);
            Mode.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };
            Mode.Value = PgpVerifyMode.Signature;

            InputFile.IsPrincipal = true;
            InputFile.OrderIndex = orderIndex;
            InputFile.Category = Resources.Input;

            InputFilePath.IsPrincipal = true;
            InputFilePath.OrderIndex = orderIndex++;
            InputFilePath.Category = Resources.Input;

            PublicKeyFile.IsPrincipal = true;
            PublicKeyFile.OrderIndex = orderIndex;
            PublicKeyFile.Category = Resources.Input;

            PublicKeyFilePath.IsPrincipal = true;
            PublicKeyFilePath.OrderIndex = orderIndex++;
            PublicKeyFilePath.Category = Resources.Input;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            ContinueOnError.Value = false;

            Result.IsPrincipal = false;
            Result.OrderIndex = orderIndex;
            Result.Category = Resources.Output;

            ConfigureInputFileMenuActions();
            ConfigurePublicKeyFileMenuActions();
        }

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
            bool needsInput = Mode.Value != PgpVerifyMode.PublicKey;
            InputFile.IsVisible = useFile && needsInput;
            InputFile.IsRequired = useFile && needsInput;
            InputFilePath.IsVisible = !useFile && needsInput;
            InputFilePath.IsRequired = !useFile && needsInput;
        }

        private void ConfigurePublicKeyFileMenuActions()
        {
            var useFileMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFile,
                IsMain = true,
                Handler = SwitchToPublicKeyFile,
            };
            var useFilePathMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFilePath,
                IsMain = true,
                Handler = SwitchToPublicKeyFilePath,
            };
            PublicKeyFilePath.AddMenuAction(useFileMenuAction);
            PublicKeyFile.AddMenuAction(useFilePathMenuAction);

            bool useFile = PublicKeyFile.HasValue && !PublicKeyFilePath.HasValue;
            PublicKeyFile.IsVisible = useFile;
            PublicKeyFile.IsRequired = useFile;
            PublicKeyFilePath.IsVisible = !useFile;
            PublicKeyFilePath.IsRequired = !useFile;
        }

        private Task SwitchToInputFile(MenuAction _)
        {
            if (Mode.Value == PgpVerifyMode.PublicKey) return Task.CompletedTask;
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
            if (Mode.Value == PgpVerifyMode.PublicKey) return Task.CompletedTask;
            if (InputFile.Value != null) _persistedInputFile = InputFile.Value;
            InputFile.Value = null;
            InputFilePath.Value = _persistedInputFilePath;
            InputFilePath.IsVisible = true;
            InputFilePath.IsRequired = true;
            InputFile.IsVisible = false;
            InputFile.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToPublicKeyFile(MenuAction _)
        {
            if (PublicKeyFilePath.Value != null) _persistedPublicKeyFilePath = PublicKeyFilePath.Value;
            PublicKeyFilePath.Value = null;
            PublicKeyFile.Value = _persistedPublicKeyFile;
            PublicKeyFile.IsVisible = true;
            PublicKeyFile.IsRequired = true;
            PublicKeyFilePath.IsVisible = false;
            PublicKeyFilePath.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToPublicKeyFilePath(MenuAction _)
        {
            if (PublicKeyFile.Value != null) _persistedPublicKeyFile = PublicKeyFile.Value;
            PublicKeyFile.Value = null;
            PublicKeyFilePath.Value = _persistedPublicKeyFilePath;
            PublicKeyFilePath.IsVisible = true;
            PublicKeyFilePath.IsRequired = true;
            PublicKeyFile.IsVisible = false;
            PublicKeyFile.IsRequired = false;
            return Task.CompletedTask;
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(Mode), ModeChanged_Action);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(Mode, nameof(Mode.Value), nameof(Mode));
        }

        private void ModeChanged_Action()
        {
            bool needsInput = Mode.Value != PgpVerifyMode.PublicKey;
            bool useFile = InputFile.HasValue && !InputFilePath.HasValue;
            InputFile.IsVisible = needsInput && useFile;
            InputFile.IsRequired = needsInput && useFile;
            InputFilePath.IsVisible = needsInput && !useFile;
            InputFilePath.IsRequired = needsInput && !useFile;
        }
    }
}
