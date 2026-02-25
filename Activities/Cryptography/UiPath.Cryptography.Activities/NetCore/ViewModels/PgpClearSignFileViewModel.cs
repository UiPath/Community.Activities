using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
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
    public class PgpClearSignFileViewModel : DesignPropertiesViewModel
    {
        public PgpClearSignFileViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> Passphrase { get; set; } = new DesignInArgument<SecureString>();
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();
        public DesignOutArgument<ILocalResource> ClearSignedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            InputFilePath.IsPrincipal = true;
            InputFilePath.IsRequired = true;
            InputFilePath.OrderIndex = orderIndex++;

            PrivateKeyFilePath.IsPrincipal = true;
            PrivateKeyFilePath.IsRequired = true;
            PrivateKeyFilePath.OrderIndex = orderIndex++;

            Passphrase.IsPrincipal = true;
            Passphrase.IsRequired = true;
            Passphrase.OrderIndex = orderIndex++;

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = orderIndex++;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            ClearSignedFile.IsPrincipal = false;
            ClearSignedFile.OrderIndex = orderIndex++;
        }
    }
}
