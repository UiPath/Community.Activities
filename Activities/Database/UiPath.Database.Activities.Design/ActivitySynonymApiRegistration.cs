using UiPath.Database.Activities.Design.Properties;
using UiPath.Studio.Activities.Api;

namespace UiPath.Database.Activities.Design
{
    internal class ActivitySynonymApiRegistration : ApiRegistrationBase
    {
        public override bool CanPerformRegistration(IWorkflowDesignApi api)
        {
            return api != null && api.HasFeature(DesignFeatureKeys.ActivitySynonyms);
        }

        protected override void PerformRegistration(IWorkflowDesignApi api)
        {
            api.ActivitySynonymService.SetActivitySynonyms(typeof(ExecuteNonQuery), new[] { Resources.ActivitySynonymExecuteNonQuery });
            api.ActivitySynonymService.SetActivitySynonyms(typeof(ExecuteQuery), new[] { Resources.ActivitySynonymExecuteQuery });
            api.ActivitySynonymService.SetActivitySynonyms(typeof(DatabaseConnect), new[] { Resources.ActivitySynonymDatabaseConnect });
            api.ActivitySynonymService.SetActivitySynonyms(typeof(DatabaseDisconnect), new[] { Resources.ActivitySynonymDatabaseDisconnect });
        }
    }
}
