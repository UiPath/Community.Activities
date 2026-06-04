using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.Helpers;
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
        private readonly PairedInputToggle<string, IResource> _inputFileToggle;
        private readonly PairedInputToggle<string, IResource> _publicKeyFileToggle;

        public PgpVerifyViewModel(IDesignServices services) : base(services)
        {
            _inputFileToggle = new PairedInputToggle<string, IResource>(
                InputFilePath, InputFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                SwitchGuard = () => Mode.Value == PgpVerifyMode.PublicKey,
                AfterSwitch = ApplyInputFileVisibility,
            };

            _publicKeyFileToggle = new PairedInputToggle<string, IResource>(
                PublicKeyFilePath, PublicKeyFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyPublicKeyVisibility,
            };
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

            _inputFileToggle.ConfigureMenuActions();
            ApplyInputFileVisibility();

            _publicKeyFileToggle.ConfigureMenuActions();
            ApplyPublicKeyVisibility();
        }

        private void ApplyInputFileVisibility()
        {
            bool useResource = _inputFileToggle.UseSecondary;
            bool needsInput = Mode.Value != PgpVerifyMode.PublicKey;
            InputFile.IsVisible = useResource && needsInput;
            InputFile.IsRequired = useResource && needsInput;
            InputFilePath.IsVisible = !useResource && needsInput;
            InputFilePath.IsRequired = !useResource && needsInput;
        }

        private void ApplyPublicKeyVisibility()
        {
            bool useResource = _publicKeyFileToggle.UseSecondary;
            PublicKeyFile.IsVisible = useResource;
            PublicKeyFile.IsRequired = useResource;
            PublicKeyFilePath.IsVisible = !useResource;
            PublicKeyFilePath.IsRequired = !useResource;
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
            ApplyInputFileVisibility();
        }
    }
}
