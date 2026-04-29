using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP;
using UiPath.FTP.Activities.API.Models;

namespace UiPath.FTP.Activities.API
{
    /// <summary>
    /// Extension methods for FTP coded workflow operations.
    /// </summary>
    public static class FtpOperations
    {
        /// <summary>
        /// Downloads a file or directory from the FTP server to a local path.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="remotePath">The remote path of the file or directory to download.</param>
        /// <param name="localPath">The local destination path.</param>
        /// <param name="overwrite">When <c>true</c>, overwrites existing local files.</param>
        /// <param name="recursive">When <c>true</c>, downloads directories recursively.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task DownloadFiles(this IFtpScopeHandle ftpScope, string remotePath, string localPath, bool overwrite = false, bool recursive = false, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            return ftpScope.GetSession().DownloadAsync(remotePath, localPath, overwrite, recursive, ct);
        }

        /// <summary>
        /// Uploads a file or directory to the FTP server.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="localPath">The local path of the file or directory to upload.</param>
        /// <param name="remotePath">The remote destination path.</param>
        /// <param name="overwrite">When <c>true</c>, overwrites existing remote files.</param>
        /// <param name="recursive">When <c>true</c>, uploads directories recursively.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task UploadFiles(this IFtpScopeHandle ftpScope, string localPath, string remotePath, bool overwrite = false, bool recursive = false, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            return ftpScope.GetSession().UploadAsync(localPath, remotePath, overwrite, recursive, ct);
        }

        /// <summary>
        /// Deletes a file or directory on the FTP server.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="remotePath">The remote path to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task Delete(this IFtpScopeHandle ftpScope, string remotePath, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            return ftpScope.GetSession().DeleteAsync(remotePath, ct);
        }

        /// <summary>
        /// Moves or renames a file or directory on the FTP server.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="remotePath">The current remote path.</param>
        /// <param name="newPath">The new remote path.</param>
        /// <param name="overwrite">When <c>true</c>, overwrites the destination if it exists.</param>
        public static void MoveItem(this IFtpScopeHandle ftpScope, string remotePath, string newPath, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            ftpScope.GetSession().Move(remotePath, newPath, overwrite);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified remote file exists.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="remotePath">The remote path to check.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<bool> FileExists(this IFtpScopeHandle ftpScope, string remotePath, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            return ftpScope.GetSession().FileExistsAsync(remotePath, ct);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified remote directory exists.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="remotePath">The remote path to check.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<bool> DirectoryExists(this IFtpScopeHandle ftpScope, string remotePath, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            return ftpScope.GetSession().DirectoryExistsAsync(remotePath, ct);
        }

        /// <summary>
        /// Enumerates files and directories at the specified remote path.
        /// </summary>
        /// <param name="ftpScope">The FTP session handle.</param>
        /// <param name="remotePath">The remote path to enumerate.</param>
        /// <param name="recursive">When <c>true</c>, enumerates sub-directories recursively.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A collection of <see cref="FtpObjectInfo"/> items.</returns>
        public static Task<IEnumerable<FtpObjectInfo>> EnumerateObjects(this IFtpScopeHandle ftpScope, string remotePath, bool recursive = false, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ftpScope);
            return ftpScope.GetSession().EnumerateObjectsAsync(remotePath, recursive, ct);
        }

        internal static IFtpSession GetSession(this IFtpScopeHandle handle) => ((FtpScopeHandle)handle).Session;
    }
}
