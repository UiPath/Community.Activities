using System;
using System.Activities;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using UiPath.ZenDesk.Activities.Properties;
using UiPath.Shared.Activities;
using ZendeskApi_v2;
using UiPath.ZenDesk.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using UiPath.ZenDesk.Contracts;

namespace UiPath.ZenDesk.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetTicketFieldOption_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetTicketFieldOption_Description))]
    public class GetTicketFieldOption : ContinuableAsyncNativeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetTicketFieldOption_Id_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetTicketFieldOption_Id_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<long> Id { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetTicketFieldOption_Name_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetTicketFieldOption_Name_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Name { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetTicketFieldOption_TicketFieldOption_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetTicketFieldOption_TicketFieldOption_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<IList<TicketFieldOption> > TicketFieldOption { get; set; }

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

        public GetTicketFieldOption()
        {
            Constraints.Add(ActivityConstraints.HasParentType<GetTicketFieldOption, ZendeskScope>(string.Format(Resources.ValidationScope_Error, Resources.ZendeskScope_DisplayName)));
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Id == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Id)));
            if (Name == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Name)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var custom_field_option_id = Id.Get(context);
            var name = Name.Get(context);
            PropertyDescriptor zendeskProperty = context.DataContext.GetProperties()[ZendeskScope.ParentContainerPropertyTag];
            var objectContainer = zendeskProperty?.GetValue(context.DataContext) as IObjectContainer;
            //var objectContainer = context.GetFromContext<IObjectContainer>(ZendeskScope.ParentContainerPropertyTag);
            TicketFieldOptionResponse resp;
            IList<TicketFieldOption> list = null;

            var client = objectContainer.Get<ZendeskApi>();
            ///////////////////////////
            // Add execution logic HERE
            ///////////////////////////

            var result = client.Requests.RunRequest(string.Format("ticket_fields/{0}/options", custom_field_option_id), "GET");
            if (result.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                resp = JsonConvert.DeserializeObject<TicketFieldOptionResponse>(result.Content, this.jsonSettings);
                list = resp.TicketFieldOptions.Where(p => p.Name == name).ToList();
            }

            // Outputs
            return (ctx) => {
                TicketFieldOption.Set(ctx, list);
            };
        }

        #endregion
    }
}

