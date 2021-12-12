using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using UiPath.ZenDesk.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.ZenDesk;
using UiPath.ZenDesk.Models;
using ZendeskApi_v2;
using Newtonsoft.Json;
using System.Collections.Generic;
using UiPath.ZenDesk.Contracts;
using System.ComponentModel;

namespace UiPath.ZenDesk.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetUserFields_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetUserFields_Description))]
    public class GetUserFields : ContinuableAsyncNativeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetUserFields_UserFields_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetUserFields_UserFields_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<IList<UserField> > UserFields { get; set; }

        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            ContractResolver = ZendeskApi_v2.Serialization.ZendeskContractResolver.Instance
        };
        #endregion


        #region Constructors

        public GetUserFields()
        {
            Constraints.Add(ActivityConstraints.HasParentType<GetUserFields, ZendeskScope>(string.Format(Resources.ValidationScope_Error, Resources.ZendeskScope_DisplayName)));
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            PropertyDescriptor zendeskProperty = context.DataContext.GetProperties()[ZendeskScope.ParentContainerPropertyTag];
            var objectContainer = zendeskProperty?.GetValue(context.DataContext) as IObjectContainer;
            var client = objectContainer.Get<ZendeskApi>();
            UserFieldsResponse user_fields = null;


            var result = client.Requests.RunRequest("user_fields", "GET");
            if (result.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                user_fields = JsonConvert.DeserializeObject<UserFieldsResponse>(result.Content, this.jsonSettings);
            }

            // Outputs
            return (ctx) => {
                UserFields.Set(ctx, user_fields.UserFields);
            };
        }

        #endregion
    }
}

