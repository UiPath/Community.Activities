using CredentialManagement;
using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace UiPath.Activities.Credentials.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal class GetSecureCredentialViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> Target { get; set; }

        public DesignProperty<CredentialType> CredentialType { get; set; }

        public DesignProperty<PersistanceType> PersistanceType { get; set; }

        public DesignOutArgument<SecureString> Password { get; set; }

        public DesignOutArgument<bool> Result { get; set; }

        public DesignOutArgument<string> Username { get; set; }

        public GetSecureCredentialViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            PersistValuesChangedDuringInit();
            base.InitializeModel();
        }
    }
}

