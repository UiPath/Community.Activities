﻿using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

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

            return (asyncCodeActivityContext) =>
            {
                Files.Set(asyncCodeActivityContext, files);
            };
        }
    }
}
