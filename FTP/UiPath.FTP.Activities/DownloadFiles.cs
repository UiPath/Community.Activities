using System;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities
{
    public class DownloadFiles : ContinuableAsyncCodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.RemotePath))]
        public InArgument<string> RemotePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.LocalPath))]
        public InArgument<string> LocalPath { get; set; }

        [LocalizedCategory(nameof(Resources.Options))]
        [LocalizedDisplayName(nameof(Resources.Recursive))]
        public bool Recursive { get; set; }

        [LocalizedCategory(nameof(Resources.Options))]
        [LocalizedDisplayName(nameof(Resources.Create))]
        public bool Create { get; set; }

        [LocalizedCategory(nameof(Resources.Options))]
        [LocalizedDisplayName(nameof(Resources.Overwrite))]
        public bool Overwrite { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            PropertyDescriptor ftpSessionProperty = context.DataContext.GetProperties()[WithFtpSession.FtpSessionPropertyName];
            IFtpSession ftpSession = ftpSessionProperty?.GetValue(context.DataContext) as IFtpSession;

            if (ftpSession == null)
            {
                throw new InvalidOperationException(Resources.FTPSessionNotFoundException);
            }

            string remotePath = RemotePath.Get(context);
            string localPath = LocalPath.Get(context);

            FtpObjectType objectType = await ftpSession.GetObjectTypeAsync(remotePath, cancellationToken);
            if (objectType == FtpObjectType.Directory)
            {
                if (string.IsNullOrWhiteSpace(Path.GetExtension(localPath)))
                {
                    if (!Directory.Exists(localPath))
                    {
                        if (Create)
                        {
                            Directory.CreateDirectory(localPath);
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(Resources.PathNotFoundException, localPath));
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(Resources.IncompatiblePathsException);
                }
            }
            else
            {
                if (objectType == FtpObjectType.File)
                {
                    if (string.IsNullOrWhiteSpace(Path.GetExtension(localPath)))
                    {
                        localPath = Path.Combine(localPath, Path.GetFileName(remotePath));
                    }

                    string directoryPath = Path.GetDirectoryName(localPath);

                    if (!Directory.Exists(directoryPath))
                    {
                        if (Create)
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        else
                        {
                            throw new InvalidOperationException(string.Format(Resources.PathNotFoundException, directoryPath));
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException(Resources.UnsupportedObjectTypeException);
                }
            }

            await ftpSession.DownloadAsync(remotePath, localPath, Overwrite, Recursive, cancellationToken);

            return (asyncCodeActivityContext) =>
            {
                
            };
        }
    }
}
