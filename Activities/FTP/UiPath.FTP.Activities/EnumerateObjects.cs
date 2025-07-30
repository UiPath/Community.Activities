using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;

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
        [DefaultValue(FtpFilterObjectType.Directory | FtpFilterObjectType.File)]
        public FtpFilterObjectType Filter { get; set; } = FtpFilterObjectType.Directory | FtpFilterObjectType.File;

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_EnumerateObjects_Property_Files_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EnumerateObjects_Property_Files_Description))]
        public OutArgument<IEnumerable<FtpObjectInfo>> Files { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
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
            else if (Filter != FtpFilterObjectType.All)
            {
                files = files.Where(x =>
                {
                    return x.Type switch
                    {
                        FtpObjectType.Directory => (Filter & FtpFilterObjectType.Directory) != 0,
                        FtpObjectType.File => (Filter & FtpFilterObjectType.File) != 0,
                        FtpObjectType.Link => (Filter & FtpFilterObjectType.Link) != 0,
                        FtpObjectType.Other => (Filter & FtpFilterObjectType.Other) != 0,
                        _ => false,
                    };
                });
            }

            return (asyncCodeActivityContext) =>
            {
                Files.Set(asyncCodeActivityContext, files);
            };
        }
    }
}
