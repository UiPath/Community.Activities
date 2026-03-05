using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

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
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                PropertyDescriptor ftpSessionProperty = context.DataContext.GetProperties()[WithFtpSession.FtpSessionPropertyName];
                IFtpSession ftpSession = ftpSessionProperty?.GetValue(context.DataContext) as IFtpSession;
                if (ftpSession == null)
                {
                    throw new InvalidOperationException(Resources.FTPSessionNotFoundException);
                }
                ftpSession.Move(RemotePath.Get(context), NewPath.Get(context), Overwrite);
                telemetryOperation?.Send();
            }
            catch (Exception e)
            {
                telemetryOperation?.SendWithException(e);
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
