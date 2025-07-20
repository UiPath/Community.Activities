using System;
using System.Activities;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_Delete_Name))]
    [LocalizedDescription(nameof(Resources.Activity_Delete_Description))]
    public partial class Delete : FtpAsyncActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_Delete_Property_RemotePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_Delete_Property_RemotePath_Description))]
        public InArgument<string> RemotePath { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            PropertyDescriptor ftpSessionProperty = context.DataContext.GetProperties()[WithFtpSession.FtpSessionPropertyName];
            IFtpSession ftpSession = ftpSessionProperty?.GetValue(context.DataContext) as IFtpSession;

            if (ftpSession == null)
            {
                throw new InvalidOperationException(Resources.FTPSessionNotFoundException);
            }

            await ftpSession.DeleteAsync(RemotePath.Get(context), cancellationToken);

            return (asyncCodeActivityContext) =>
            {

            };
        }
    }
}
