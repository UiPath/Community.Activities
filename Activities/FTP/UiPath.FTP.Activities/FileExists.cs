using System;
using System.Activities;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_FileExists_Name))]
    [LocalizedDescription(nameof(Resources.Activity_FileExists_Description))]
    public partial class FileExists : FtpAsyncActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_FileExists_Property_RemotePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_FileExists_Property_RemotePath_Description))]
        public InArgument<string> RemotePath { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_FileExists_Property_Exists_Name))]
        [LocalizedDescription(nameof(Resources.Activity_FileExists_Property_Exists_Description))]
        public OutArgument<bool> Exists { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            PropertyDescriptor ftpSessionProperty = context.DataContext.GetProperties()[WithFtpSession.FtpSessionPropertyName];
            IFtpSession ftpSession = ftpSessionProperty?.GetValue(context.DataContext) as IFtpSession;

            if (ftpSession == null)
            {
                throw new InvalidOperationException(Resources.FTPSessionNotFoundException);
            }

            bool exists = await ftpSession.FileExistsAsync(RemotePath.Get(context), cancellationToken);

            return (asyncCodeActivityContext) =>
            {
                Exists.Set(asyncCodeActivityContext, exists);
            };
        }
    }
}
