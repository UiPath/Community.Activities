using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.FTP
{
    // TODO: Need to test if all sessions get disposed in case of an exception.
    public interface IFtpSession : IDisposable
    {
        void Close();
        Task CloseAsync(CancellationToken cancellationToken);
        void CreateDirectory(string path);
        Task CreateDirectoryAsync(string path, CancellationToken cancellationToken);
        void Delete(string path);
        Task DeleteAsync(string path, CancellationToken cancellationToken);
        bool DirectoryExists(string path);
        Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken);
        void Download(string remotePath, string localPath, bool overwrite, bool recursive);
        Task DownloadAsync(string remotePath, string localPath, bool overwrite, bool recursive, CancellationToken cancellationToken);
        bool FileExists(string path);
        Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken);
        FtpObjectType GetObjectType(string path);
        Task<FtpObjectType> GetObjectTypeAsync(string path, CancellationToken cancellationToken);
        bool IsConnected();
        Task<bool> IsConnectedAsync(CancellationToken cancellationToken);
        IEnumerable<FtpObjectInfo> EnumerateObjects(string remotePath, bool recursive);
        Task<IEnumerable<FtpObjectInfo>> EnumerateObjectsAsync(string remotePath, bool recursive, CancellationToken cancellationToken);
        void Open();
        Task OpenAsync(CancellationToken cancellationToken);
        void Upload(string localPath, string remotePath, bool overwrite, bool recursive);
        Task UploadAsync(string localPath, string remotePath, bool overwrite, bool recursive, CancellationToken cancellationToken);
    }
}
