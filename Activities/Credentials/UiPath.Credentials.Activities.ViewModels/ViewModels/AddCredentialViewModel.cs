using CredentialManagement;
using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace UiPath.Activities.Credentials.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal class AddCredentialViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> Target { get; set; }
        public DesignInArgument<string> Username { get; set; }

        public DesignProperty<CredentialType> CredentialType { get; set; }

        public DesignInArgument<string> Password { get; set; }

        public DesignProperty<PersistanceType> PersistanceType { get; set; }

        public DesignInArgument<SecureString> PasswordSecureString { get; set; }

        public DesignOutArgument<bool> Result { get; set; }

        public AddCredentialViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            PersistValuesChangedDuringInit();
            base.InitializeModel();
        }
    }
}

