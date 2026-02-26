using System.Runtime.CompilerServices;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Studio.Activities.Api;

namespace UiPath.Cryptography.Activities
{
    internal class ActivitySynonymApiRegistration : ApiRegistrationBase
    {
        public override bool CanPerformRegistration(IWorkflowDesignApi api)
        {
            return api != null && api.HasFeature(DesignFeatureKeys.ActivitySynonyms);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void PerformRegistration(IWorkflowDesignApi api)
        {
            var synonym = new[] { Resources.ActivitySynonymCryptography };

            api.ActivitySynonymService.SetActivitySynonyms(typeof(DecryptFile), synonym);
            api.ActivitySynonymService.SetActivitySynonyms(typeof(DecryptText), synonym);
            api.ActivitySynonymService.SetActivitySynonyms(typeof(EncryptFile), synonym);
            api.ActivitySynonymService.SetActivitySynonyms(typeof(EncryptText), synonym);
            api.ActivitySynonymService.SetActivitySynonyms(typeof(KeyedHashFile), synonym);
            api.ActivitySynonymService.SetActivitySynonyms(typeof(KeyedHashText), synonym);
        }
    }
}
