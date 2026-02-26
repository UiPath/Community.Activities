using System.Activities.Presentation.Metadata;
using UiPath.Studio.Activities.Api;

namespace UiPath.Cryptography.Activities
{
    public class DesignerMetadata : IRegisterMetadata
    {
        private void InitializeInternal(object api)
        {
            if (api is IWorkflowDesignApi wfDesignApi)
            {
                new ActivitySynonymApiRegistration().Initialize(wfDesignApi);
            }
        }

        public void Initialize(object api)
        {
            if (api == null)
            {
                return;
            }
            InitializeInternal(api);
        }

        public void Register()
        {
        }
    }
}
