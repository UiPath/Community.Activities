using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;

namespace UiPath.Activities.Credentials.ViewModels
{
    [ExcludeFromCodeCoverage]
    internal class DeleteCredentialViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> Target { get; set; }

        public DesignOutArgument<bool> Result { get; set; }

        public DeleteCredentialViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            PersistValuesChangedDuringInit();
            base.InitializeModel();
        }
    }
}

