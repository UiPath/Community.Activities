using System.Runtime.CompilerServices;
using UiPath.FTP.Activities.Design.Properties;
using UiPath.Studio.Activities.Api;

namespace UiPath.FTP.Activities.Design
{
    /// <summary>
    /// Class for synonyms registration, taken from Activities Repo
    /// </summary>
    internal class ActivitySynonymApiRegistration : ApiRegistrationBase
    {
        /// <summary>
        /// Checks whether the registration of synonyms can be performed
        /// </summary>
        /// <param name="api">The api</param>
        /// <returns>True or false, meaning whether the registration can be performed or not</returns>
        public override bool CanPerformRegistration(IWorkflowDesignApi api)
        {
            return api != null && api.HasFeature(DesignFeatureKeys.ActivitySynonyms);
        }

        /// <summary>
        /// Register synonyms for the required activities
        /// </summary>
        /// <param name="api">The api</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void PerformRegistration(IWorkflowDesignApi api)
        {
            api.ActivitySynonymService.SetActivitySynonyms(typeof(WithFtpSession), new[] { Resources.ActivitySynonymUseFtpConnection });
            api.ActivitySynonymService.SetActivitySynonyms(typeof(MoveItem), new[] { Resources.ActivitySynonymMoveFileOrFolder });
            api.ActivitySynonymService.SetActivitySynonyms(typeof(Delete), new[] { Resources.ActivitySynonymDeleteFileOrFolder });
        }
    }
}
