using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using UiPath.ZenDesk.Activities.Properties;
using UiPath.Shared.Activities;
using ZendeskApi_v2;
using ZendeskApi_v2.Models;
using UiPath.ZenDesk.Contracts;
using System.ComponentModel;

namespace UiPath.ZenDesk.Activities
{
    [LocalizedDisplayName(nameof(Resources.UpdateTicket_DisplayName))]
    [LocalizedDescription(nameof(Resources.UpdateTicket_Description))]
    public class UpdateTicket : ContinuableAsyncNativeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }
        /*
        [LocalizedDisplayName(nameof(Resources.UpdateTicket_Id_DisplayName))]
        [LocalizedDescription(nameof(Resources.UpdateTicket_Id_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<long> Id { get; set; }
        */

        [LocalizedDisplayName(nameof(Resources.UpdateTicket_Body_DisplayName))]
        [LocalizedDescription(nameof(Resources.UpdateTicket_Body_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<ZendeskApi_v2.Models.Tickets.Ticket> Ticket { get; set; }

        [LocalizedDisplayName(nameof(Resources.UpdateTicket_Message_DisplayName))]
        [LocalizedDescription(nameof(Resources.UpdateTicket_Message_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<ZendeskApi_v2.Models.Tickets.Ticket> UpdatedTicket { get; set; }

        #endregion


        #region Constructors

        public UpdateTicket()
        {
            Constraints.Add(ActivityConstraints.HasParentType<UpdateTicket, ZendeskScope>(string.Format(Resources.ValidationScope_Error, Resources.ZendeskScope_DisplayName)));
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            //if (Id == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Id)));
            if (Ticket == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Ticket)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            // Object Container: Use objectContainer.Get<T>() to retrieve objects from the scope
            PropertyDescriptor zendeskProperty = context.DataContext.GetProperties()[ZendeskScope.ParentContainerPropertyTag];
            var objectContainer = zendeskProperty?.GetValue(context.DataContext) as IObjectContainer;
            // Inputs
            //var ticketId = Id.Get(context);
            var ticket = Ticket.Get(context);

            var client = objectContainer.Get<ZendeskApi>();
            ///////////////////////////
            // Add execution logic HERE
            ///////////////////////////
            var resp = client.Tickets.UpdateTicket(ticket);

            // Outputs
            return (ctx) => {
                UpdatedTicket.Set(ctx, resp.Ticket); // updated ticket 
            };
        }

        #endregion
    }
}

