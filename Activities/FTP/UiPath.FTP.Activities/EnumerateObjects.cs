using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.FTP.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_EnumerateObjects_Name))]
    [LocalizedDescription(nameof(Resources.Activity_EnumerateObjects_Description))]
    public partial class EnumerateObjects : FtpAsyncActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EnumerateObjects_Property_RemotePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EnumerateObjects_Property_RemotePath_Description))]
        public InArgument<string> RemotePath { get; set; }

        [LocalizedCategory(nameof(Resources.Options))]
        [LocalizedDisplayName(nameof(Resources.Activity_EnumerateObjects_Property_Recursive_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EnumerateObjects_Property_Recursive_Description))]
        public bool Recursive { get; set; }

        [LocalizedCategory(nameof(Resources.Options))]
        [LocalizedDisplayName(nameof(Resources.Activity_EnumerateObjects_Property_Filter_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EnumerateObjects_Property_Filter_Description))]
        [DefaultValue(FtpFilterObjectType.Directory | FtpFilterObjectType.File | FtpFilterObjectType.Link | FtpFilterObjectType.Other)]
        public FtpFilterObjectType Filter { get; set; } = FtpFilterObjectType.Directory | FtpFilterObjectType.File | FtpFilterObjectType.Link | FtpFilterObjectType.Other;

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_EnumerateObjects_Property_Files_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EnumerateObjects_Property_Files_Description))]
        public OutArgument<IEnumerable<FtpObjectInfo>> Files { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
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

                IEnumerable<FtpObjectInfo> files = await ftpSession.EnumerateObjectsAsync(RemotePath.Get(context), Recursive, cancellationToken);

                //Filter the returned objects based on user's selection
                //If none selected, clear all
                if (Filter == FtpFilterObjectType.None)
                {
                    files = Enumerable.Empty<FtpObjectInfo>();
                }
                //filter based on what the user selection
                else if (Filter != (FtpFilterObjectType.Directory | FtpFilterObjectType.File | FtpFilterObjectType.Link | FtpFilterObjectType.Other))
                {
                    bool includeDirectories = (Filter & FtpFilterObjectType.Directory) != 0;
                    bool includeFiles = (Filter & FtpFilterObjectType.File) != 0;
                    bool includeLinks = (Filter & FtpFilterObjectType.Link) != 0;
                    bool includeOthers = (Filter & FtpFilterObjectType.Other) != 0;
                    files = files.Where(x =>
                    {
                        return x.Type switch
                        {
                            FtpObjectType.Directory => includeDirectories,
                            FtpObjectType.File => includeFiles,
                            FtpObjectType.Link => includeLinks,
                            FtpObjectType.Other => includeOthers,
                            _ => false,
                        };
                    });
                }
                var result = new Action<AsyncCodeActivityContext>(asyncCodeActivityContext =>
                {
                    Files.Set(asyncCodeActivityContext, files);
                });
                telemetryOperation?.Send();
                return result;
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }
    }
}
