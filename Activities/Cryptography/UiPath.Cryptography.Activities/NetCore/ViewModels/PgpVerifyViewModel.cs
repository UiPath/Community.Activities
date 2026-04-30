using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Enums;

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
        public PgpVerifyViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignProperty<PgpVerifyMode> Mode { get; set; } = new DesignProperty<PgpVerifyMode>();
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();
        public DesignOutArgument<bool> Result { get; set; } = new DesignOutArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            Mode.IsPrincipal = true;
            Mode.OrderIndex = orderIndex++;
            Mode.DataSource = DataSourceHelper.ForEnum(
                PgpVerifyMode.Signature,
                PgpVerifyMode.ClearSignature,
                PgpVerifyMode.PublicKey);
            Mode.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            InputFilePath.IsPrincipal = true;
            InputFilePath.IsRequired = true;
            InputFilePath.OrderIndex = orderIndex++;

            PublicKeyFilePath.IsPrincipal = true;
            PublicKeyFilePath.IsRequired = true;
            PublicKeyFilePath.OrderIndex = orderIndex++;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            Result.IsPrincipal = false;
            Result.OrderIndex = orderIndex;
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
            bool needsInputFile = Mode.Value != PgpVerifyMode.PublicKey;
            InputFilePath.IsVisible = needsInputFile;
            InputFilePath.IsRequired = needsInputFile;
        }
    }
}
