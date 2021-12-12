using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using UiPath.ZenDesk.Activities.Properties;
using UiPath.Shared.Activities;
using ZendeskApi_v2;
using UiPath.ZenDesk.Contracts;
using System.ComponentModel;

namespace UiPath.ZenDesk.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetUser_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetUser_Description))]
    public class GetUser : ContinuableAsyncNativeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetUser_Id_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetUser_Id_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<long> Id { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetUser_User_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetUser_User_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<ZendeskApi_v2.Models.Users.User> User { get; set; }

        #endregion


        #region Constructors

        public GetUser()
        {
            Constraints.Add(ActivityConstraints.HasParentType<GetUser, ZendeskScope>(string.Format(Resources.ValidationScope_Error, Resources.ZendeskScope_DisplayName)));
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
            var assignee_id = Id.Get(context);
            var user_resp = client.Users.GetUser(assignee_id);

            // Outputs
            return (ctx) => {
                User.Set(ctx, user_resp.User);
            };
        }

        #endregion
    }
}

