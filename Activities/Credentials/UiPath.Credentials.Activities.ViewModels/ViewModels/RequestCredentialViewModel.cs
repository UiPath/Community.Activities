using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace UiPath.Activities.Credentials.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal class RequestCredentialViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> Message { get; set; }

        public DesignInArgument<string> Title { get; set; }

        public DesignOutArgument<string> Password { get; set; }

        public DesignOutArgument<bool> Result { get; set; }

        public DesignOutArgument<SecureString> PasswordSecureString { get; set; }

        public DesignOutArgument<string> Username { get; set; }

        public RequestCredentialViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            PersistValuesChangedDuringInit();
            base.InitializeModel();
        }
    }
}

