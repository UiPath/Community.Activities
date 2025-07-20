using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_MoveItem_Name))]
    [LocalizedDescription(nameof(Resources.Activity_MoveItem_Description))]
    public partial class MoveItem : FtpCodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_MoveItem_Property_RemotePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_MoveItem_Property_RemotePath_Description))]
        public InArgument<string> RemotePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_MoveItem_Property_NewPath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_MoveItem_Property_NewPath_Description))]
        public InArgument<string> NewPath { get; set; }

        [LocalizedCategory(nameof(Resources.Options))]
        [LocalizedDisplayName(nameof(Resources.Activity_MoveItem_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_MoveItem_Property_Overwrite_Description))]
        public bool Overwrite { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_MoveItem_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_MoveItem_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; } = false;

        protected override void Execute(CodeActivityContext context)
        {
            PropertyDescriptor ftpSessionProperty = context.DataContext.GetProperties()[WithFtpSession.FtpSessionPropertyName];
            IFtpSession ftpSession = ftpSessionProperty?.GetValue(context.DataContext) as IFtpSession;
            try
            {
                if (ftpSession == null)
                {
                    throw new InvalidOperationException(Resources.FTPSessionNotFoundException);
                }
                ftpSession.Move(RemotePath.Get(context), NewPath.Get(context), Overwrite);
            }
            catch (Exception e)
            {
                if (ContinueOnError.Get(context))
                {
                    Trace.TraceError(e.ToString());
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
