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
    public class UploadFiles : ContinuableAsyncCodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.LocalPath))]
        public InArgument<string> LocalPath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.RemotePath))]
        public InArgument<string> RemotePath { get; set; }

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

            string localPath = LocalPath.Get(context);
            string remotePath = RemotePath.Get(context);

            if (Directory.Exists(localPath))
            {
                if (string.IsNullOrWhiteSpace(Path.GetExtension(remotePath)))
                {
                    if (!(await ftpSession.DirectoryExistsAsync(remotePath, cancellationToken)))
                    {
                        if (Create)
                        {
                            await ftpSession.CreateDirectoryAsync(remotePath, cancellationToken);
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(Resources.PathNotFoundException, remotePath));
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
                if (File.Exists(localPath))
                {
                    if (string.IsNullOrWhiteSpace(Path.GetExtension(remotePath)))
                    {
                        remotePath = FtpConfiguration.CombinePaths(remotePath, Path.GetFileName(localPath));
                    }

                    string directoryPath = FtpConfiguration.GetDirectoryPath(remotePath);

                    if (!(await ftpSession.DirectoryExistsAsync(directoryPath, cancellationToken)))
                    {
                        if (Create)
                        {
                            await ftpSession.CreateDirectoryAsync(directoryPath, cancellationToken);
                        }
                        else
                        {
                            throw new InvalidOperationException(string.Format(Resources.PathNotFoundException, directoryPath));
                        }
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.PathNotFoundException, localPath));
                }
            }

            await ftpSession.UploadAsync(localPath, remotePath, Overwrite, Recursive, cancellationToken);

            return (asyncCodeActivityContext) =>
            {

            };
        }
    }
}
