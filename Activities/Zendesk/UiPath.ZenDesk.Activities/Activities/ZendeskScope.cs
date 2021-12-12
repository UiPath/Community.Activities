using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Statements;
using System.ComponentModel;
using UiPath.ZenDesk.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.ZenDesk.Contracts;
using ZendeskApi_v2;

namespace UiPath.ZenDesk.Activities
{
    [LocalizedDisplayName(nameof(Resources.ZendeskScope_DisplayName))]
    [LocalizedDescription(nameof(Resources.ZendeskScope_Description))]
    public class ZendeskScope : ContinuableAsyncNativeActivity
    {
        #region Properties

        [Browsable(false)]
        public ActivityAction<IObjectContainer​> Body { get; set; }

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.ZendeskScope_Subdomain_DisplayName))]
        [LocalizedDescription(nameof(Resources.ZendeskScope_Subdomain_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Subdomain { get; set; }

        [LocalizedDisplayName(nameof(Resources.ZendeskScope_Email_DisplayName))]
        [LocalizedDescription(nameof(Resources.ZendeskScope_Email_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Email { get; set; }

        [LocalizedDisplayName(nameof(Resources.ZendeskScope_APIToken_DisplayName))]
        [LocalizedDescription(nameof(Resources.ZendeskScope_APIToken_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> APIToken { get; set; }

        // A tag used to identify the scope in the activity context
        internal static string ParentContainerPropertyTag => "Zendesk";

        // Object Container: Add strongly-typed objects here and they will be available in the scope's child activities.
        private readonly IObjectContainer _objectContainer;

        private ZendeskApi _client;
        #endregion


        #region Constructors

        public ZendeskScope(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;

            Body = new ActivityAction<IObjectContainer>
            {
                Argument = new DelegateInArgument<IObjectContainer> (ParentContainerPropertyTag),
                Handler = new Sequence { DisplayName = Resources.Do }
            };
        }

        public ZendeskScope() : this(new ObjectContainer())
        {

        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Subdomain == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Subdomain)));
            if (Email == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Email)));
            if (APIToken == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(APIToken)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext  context, CancellationToken cancellationToken)
        {
            // Inputs
            var subdomain = Subdomain.Get(context);
            var email = Email.Get(context);
            var apitoken = APIToken.Get(context);

            var endpoint_url = string.Format("https://{0}.zendesk.com/api/v2", subdomain);
            var user = string.Format("{0}/token", email);

            this._client = new ZendeskApi(endpoint_url, user, apitoken);
            this._objectContainer.Add(this._client);

            return (ctx) => {
                // Schedule child activities
                if (Body != null)
				    ctx.ScheduleAction<IObjectContainer>(Body, _objectContainer, OnCompleted, OnFaulted);

                // Outputs
            };
        }

        #endregion


        #region Events

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Cleanup();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Cleanup();
        }

        #endregion


        #region Helpers
        
        private void Cleanup()
        {
            var disposableObjects = _objectContainer.Where(o => o is IDisposable);
            foreach (var obj in disposableObjects)
            {
                if (obj is IDisposable dispObject)
                    dispObject.Dispose();
            }
            _objectContainer.Clear();
        }

        #endregion
    }
}

