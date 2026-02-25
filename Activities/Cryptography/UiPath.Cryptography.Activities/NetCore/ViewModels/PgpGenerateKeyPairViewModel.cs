using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpGenerateKeyPairViewModel))]
    public partial class PgpGenerateKeyPair
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpGenerateKeyPairViewModel : DesignPropertiesViewModel
    {
        public PgpGenerateKeyPairViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> Username { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> Password { get; set; } = new DesignInArgument<SecureString>();
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();
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

            PrivateKeyFilePath.IsPrincipal = true;
            PrivateKeyFilePath.IsRequired = true;
            PrivateKeyFilePath.OrderIndex = orderIndex++;

            Username.IsPrincipal = true;
            Username.IsRequired = true;
            Username.OrderIndex = orderIndex++;

            Password.IsPrincipal = true;
            Password.IsRequired = true;
            Password.OrderIndex = orderIndex++;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            PublicKeyFile.IsPrincipal = false;
            PublicKeyFile.OrderIndex = orderIndex++;

            PrivateKeyFile.IsPrincipal = false;
            PrivateKeyFile.OrderIndex = orderIndex++;
        }
    }
}
