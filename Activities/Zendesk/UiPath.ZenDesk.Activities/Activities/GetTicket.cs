using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Shared.Activities;
using UiPath.ZenDesk.Activities.Properties;
using ZendeskApi_v2;
using UiPath.ZenDesk.Contracts;
using ZendeskApi_v2.Models.Tickets;
using System.ComponentModel;

namespace UiPath.ZenDesk.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetTicket_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetTicket_Description))]
    public class GetTicket : ContinuableAsyncNativeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetTicket_Id_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetTicket_Id_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<long> Id { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetTicket_Data_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetTicket_Data_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        [RequiredArgument]
        public OutArgument<ZendeskApi_v2.Models.Tickets.Ticket> Ticket { get; set; }


        #endregion


        #region Constructors

        public GetTicket()
        {
            Constraints.Add(ActivityConstraints.HasParentType<GetTicket, ZendeskScope>(string.Format(Resources.ValidationScope_Error, Resources.ZendeskScope_DisplayName)));
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Id == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Id)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            // Object Container: Use objectContainer.Get<T>() to retrieve objects from the scope
            PropertyDescriptor zendeskProperty = context.DataContext.GetProperties()[ZendeskScope.ParentContainerPropertyTag];
            var objectContainer = zendeskProperty?.GetValue(context.DataContext) as IObjectContainer;

            var client = objectContainer.Get<ZendeskApi>();

            // Inputs
            var ticketId = Id.Get(context);

            ///////////////////////////
            // Add execution logic HERE
            ///////////////////////////
            var resp = client.Tickets.GetTicket(ticketId);

            // Outputs
            return (ctx) => {
                Ticket.Set(ctx, resp.Ticket);
            };
        }

        #endregion
    }
}

