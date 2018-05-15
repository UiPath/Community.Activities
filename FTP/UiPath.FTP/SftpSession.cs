using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Properties;

namespace UiPath.FTP
{
    public class SftpSession : IFtpSession
    {
        private readonly SftpClient _sftpClient;

        public SftpSession(FtpConfiguration ftpConfiguration)
        {
            if (ftpConfiguration == null)
            {
                throw new ArgumentNullException(nameof(ftpConfiguration));
            }

            ConnectionInfo connectionInfo = null;

            if (ftpConfiguration.Port == null)
            {
                connectionInfo = new ConnectionInfo(ftpConfiguration.Host, ftpConfiguration.Username, new PasswordAuthenticationMethod(ftpConfiguration.Username, ftpConfiguration.Password));
            }
            else
            {
                connectionInfo = new ConnectionInfo(ftpConfiguration.Host, ftpConfiguration.Port.Value, ftpConfiguration.Username, new PasswordAuthenticationMethod(ftpConfiguration.Username, ftpConfiguration.Password));
            }

            _sftpClient = new SftpClient(connectionInfo);
        }

        private IEnumerable<Tuple<string, string>> GetLocalListing(string localPath, string remotePath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            DirectoryInfo currentDirectory = new DirectoryInfo(localPath);
            string nextRemotePath = FtpConfiguration.CombinePaths(remotePath, currentDirectory.Name);

            foreach (FileInfo fileInfo in currentDirectory.EnumerateFiles())
            {
                listing.Add(new Tuple<string, string>(fileInfo.FullName, FtpConfiguration.CombinePaths(nextRemotePath, fileInfo.Name)));
            }

            if (recursive)
            {
                foreach (DirectoryInfo directoryInfo in currentDirectory.EnumerateDirectories())
                {
                    listing.AddRange(GetLocalListing(directoryInfo.FullName, nextRemotePath, recursive));
                }
            }

            return listing;
        }

        private IEnumerable<Tuple<string, string>> GetRemoteListing(string remotePath, string localPath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }

            string initialWorkingDirectory = _sftpClient.WorkingDirectory;
            _sftpClient.ChangeDirectory(remotePath);

            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            SftpFile currentDirectory = _sftpClient.Get(_sftpClient.WorkingDirectory);
            IEnumerable<SftpFile> items = _sftpClient.ListDirectory(currentDirectory.FullName);
            string nextLocalPath = Path.Combine(localPath, currentDirectory.Name);

            foreach (SftpFile file in items.Where(i => i.IsRegularFile))
            {
                listing.Add(new Tuple<string, string>(Path.Combine(nextLocalPath, file.Name), file.FullName));
            }

            if (recursive)
            {
                foreach (SftpFile directory in items.Where(i => i.IsDirectory && i.Name != "." && i.Name != ".."))
                {
                    listing.AddRange(GetRemoteListing(directory.FullName, nextLocalPath, recursive));
                }
            }

            _sftpClient.ChangeDirectory(initialWorkingDirectory);

            return listing;
        }

        private IEnumerable<FtpObjectInfo> GetRemoteListing(string remotePath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            string initialWorkingDirectory = _sftpClient.WorkingDirectory;
            _sftpClient.ChangeDirectory(remotePath);

            SftpFile currentDirectory = _sftpClient.Get(_sftpClient.WorkingDirectory);

            List<FtpObjectInfo> listing = new List<FtpObjectInfo>();
            IEnumerable<SftpFile> items = _sftpClient.ListDirectory(currentDirectory.FullName).Where(sf => sf.Name != "." && sf.Name != "..");

            listing.AddRange(items.Select(sf => sf.ToFtpObjectInfo()));

            if (recursive)
            {
                foreach (SftpFile directory in items.Where(i => i.IsDirectory))
                {
                    listing.AddRange(GetRemoteListing(directory.FullName, recursive));
                }
            }

            _sftpClient.ChangeDirectory(initialWorkingDirectory);

            return listing;
        }

        private async Task<IEnumerable<Tuple<string, string>>> GetRemoteListingAsync(string remotePath, string localPath, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            cancellationToken.ThrowIfCancellationRequested();

            string initialWorkingDirectory = _sftpClient.WorkingDirectory;
            _sftpClient.ChangeDirectory(remotePath);

            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            SftpFile currentDirectory = _sftpClient.Get(_sftpClient.WorkingDirectory);
            IEnumerable<SftpFile> items = await Task.Factory.FromAsync(_sftpClient.BeginListDirectory(currentDirectory.FullName, null, null), _sftpClient.EndListDirectory);
            string nextLocalPath = Path.Combine(localPath, currentDirectory.Name);

            foreach (SftpFile file in items.Where(i => i.IsRegularFile))
            {
                listing.Add(new Tuple<string, string>(Path.Combine(nextLocalPath, file.Name), file.FullName));
            }

            if (recursive)
            {
                foreach (SftpFile directory in items.Where(i => i.IsDirectory && i.Name != "." && i.Name != ".."))
                {
                    listing.AddRange(await GetRemoteListingAsync(directory.FullName, nextLocalPath, recursive, cancellationToken));
                }
            }

            _sftpClient.ChangeDirectory(initialWorkingDirectory);

            return listing;
        }

        private async Task<IEnumerable<FtpObjectInfo>> GetRemoteListingAsync(string remotePath, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            string initialWorkingDirectory = _sftpClient.WorkingDirectory;
            _sftpClient.ChangeDirectory(remotePath);

            SftpFile currentDirectory = _sftpClient.Get(_sftpClient.WorkingDirectory);

            List<FtpObjectInfo> listing = new List<FtpObjectInfo>();
            IEnumerable<SftpFile> items = await Task.Factory.FromAsync(_sftpClient.BeginListDirectory(currentDirectory.FullName, null, null), _sftpClient.EndListDirectory);
            items = items.Where(sf => sf.Name != "." && sf.Name != "..");

            listing.AddRange(items.Select(sf => sf.ToFtpObjectInfo()));

            if (recursive)
            {
                foreach (SftpFile directory in items.Where(i => i.IsDirectory))
                {
                    listing.AddRange(await GetRemoteListingAsync(directory.FullName, recursive, cancellationToken));
                }
            }

            _sftpClient.ChangeDirectory(initialWorkingDirectory);

            return listing;
        }

        #region IFtpSession members
        bool IFtpSession.IsConnected()
        {
            return _sftpClient.IsConnected;
        }

        Task<bool> IFtpSession.IsConnectedAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            return Task.Run(() => _sftpClient.IsConnected, cancellationToken);
        }

        void IFtpSession.Open()
        {
            Trace.TraceInformation("Attempting to open an SFTP connection.");

            _sftpClient.Connect();
        }

        Task IFtpSession.OpenAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            Trace.TraceInformation("Attempting to asynchronously open an SFTP connection.");

            return Task.Run(() => _sftpClient.Connect(), cancellationToken);
        }

        void IFtpSession.Close()
        {
            Trace.TraceInformation("Attempting to close an SFTP connection.");

            _sftpClient.Disconnect();
        }

        Task IFtpSession.CloseAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            Trace.TraceInformation("Attempting to asynchronously close an SFTP connection.");

            return Task.Run(() => _sftpClient.Disconnect(), cancellationToken);
        }

        void IFtpSession.CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _sftpClient.CreateDirectory(path);
        }

        Task IFtpSession.CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            return Task.Run(() => _sftpClient.CreateDirectory(path), cancellationToken);
        }

        void IFtpSession.Delete(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _sftpClient.Delete(path);
        }

        Task IFtpSession.DeleteAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            return Task.Run(() => _sftpClient.Delete(path), cancellationToken);
        }

        bool IFtpSession.DirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return _sftpClient.Exists(path) && ((IFtpSession)this).GetObjectType(path) == FtpObjectType.Directory;
        }

        async Task<bool> IFtpSession.DirectoryExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            if (_sftpClient.Exists(path))
            {
                return await ((IFtpSession)this).GetObjectTypeAsync(path, cancellationToken) == FtpObjectType.Directory;
            }
            else
            {
                return false;
            }
        }

        void IFtpSession.Download(string remotePath, string localPath, bool overwrite, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }

            FtpObjectType objectType = ((IFtpSession)this).GetObjectType(remotePath);
            if (objectType == FtpObjectType.Directory)
            {
                IEnumerable<Tuple<string, string>> listing = GetRemoteListing(remotePath, localPath, recursive);

                foreach (Tuple<string, string> file in listing)
                {
                    string directoryPath = Path.GetDirectoryName(file.Item1);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    using (Stream fileStream = File.OpenWrite(file.Item1))
                    {
                        _sftpClient.DownloadFile(file.Item2, fileStream);
                    }
                }
            }
            else
            {
                if (objectType == FtpObjectType.File)
                {
                    if (File.Exists(localPath) && !overwrite)
                    {
                        throw new IOException(Resources.FileExistsException);
                    }

                    using (Stream fileStream = File.OpenWrite(localPath))
                    {
                        _sftpClient.DownloadFile(remotePath, fileStream);
                    }
                }
                else
                {
                    throw new NotImplementedException(Resources.UnsupportedObjectTypeException);
                }
            }
        }

        async Task IFtpSession.DownloadAsync(string remotePath, string localPath, bool overwrite, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            FtpObjectType objectType = ((IFtpSession)this).GetObjectType(remotePath);
            if (objectType == FtpObjectType.Directory)
            {
                IEnumerable<Tuple<string, string>> listing = await GetRemoteListingAsync(remotePath, localPath, recursive, cancellationToken);

                foreach (Tuple<string, string> file in listing)
                {
                    if (File.Exists(file.Item1) && !overwrite)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    string directoryPath = Path.GetDirectoryName(file.Item1);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    using (Stream fileStream = File.OpenWrite(file.Item1))
                    {
                        await Task.Factory.FromAsync(_sftpClient.BeginDownloadFile(file.Item2, fileStream), _sftpClient.EndDownloadFile);
                    }
                }
            }
            else
            {
                if (objectType == FtpObjectType.File)
                {
                    if (File.Exists(localPath) && !overwrite)
                    {
                        throw new IOException(Resources.FileExistsException);
                    }

                    using (Stream fileStream = File.OpenWrite(localPath))
                    {
                        await Task.Factory.FromAsync(_sftpClient.BeginDownloadFile(remotePath, fileStream), _sftpClient.EndDownloadFile);
                    } 
                }
                else
                {
                    throw new NotImplementedException(Resources.UnsupportedObjectTypeException);
                }
            }
        }

        bool IFtpSession.FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return _sftpClient.Exists(path) && ((IFtpSession)this).GetObjectType(path) == FtpObjectType.File;
        }

        async Task<bool> IFtpSession.FileExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            if (_sftpClient.Exists(path))
            {
                return await ((IFtpSession)this).GetObjectTypeAsync(path, cancellationToken) == FtpObjectType.File;
            }
            else
            {
                return false;
            }
        }

        FtpObjectType IFtpSession.GetObjectType(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!_sftpClient.Exists(path))
            {
                throw new ArgumentException(string.Format(Resources.PathNotFoundException, path), nameof(path));
            }

            return _sftpClient.Get(path).GetFtpObjectType();
        }

        Task<FtpObjectType> IFtpSession.GetObjectTypeAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            if (!_sftpClient.Exists(path))
            {
                throw new ArgumentException(string.Format(Resources.PathNotFoundException, path), nameof(path));
            }

            return Task.Run(() => _sftpClient.Get(path).GetFtpObjectType(), cancellationToken);
        }

        IEnumerable<FtpObjectInfo> IFtpSession.EnumerateObjects(string remotePath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            return GetRemoteListing(remotePath, recursive);
        }

        Task<IEnumerable<FtpObjectInfo>> IFtpSession.EnumerateObjectsAsync(string remotePath, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            return GetRemoteListingAsync(remotePath, recursive, cancellationToken);
        }

        void IFtpSession.Upload(string localPath, string remotePath, bool overwrite, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            if (Directory.Exists(localPath))
            {
                IEnumerable<Tuple<string, string>> listing = GetLocalListing(localPath, remotePath, recursive);

                foreach (Tuple<string, string> pair in listing)
                {
                    string directoryPath = FtpConfiguration.GetDirectoryPath(pair.Item2);
                    if (!_sftpClient.Exists(directoryPath))
                    {
                        _sftpClient.CreateDirectory(directoryPath);
                    }

                    using (Stream fileStream = File.OpenRead(pair.Item1))
                    {
                        _sftpClient.UploadFile(fileStream, pair.Item2, overwrite);
                    }
                }
            }
            else
            {
                if (File.Exists(localPath))
                {
                    if (_sftpClient.Exists(remotePath) && !overwrite)
                    {
                        throw new IOException(Resources.FileExistsException);
                    }

                    using (Stream fileStream = File.OpenRead(localPath))
                    {
                        _sftpClient.UploadFile(fileStream, remotePath, overwrite);
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.PathNotFoundException, localPath), nameof(localPath));
                }
            }
        }

        async Task IFtpSession.UploadAsync(string localPath, string remotePath, bool overwrite, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            if (Directory.Exists(localPath))
            {
                IEnumerable<Tuple<string, string>> listing = GetLocalListing(localPath, remotePath, recursive);

                foreach (Tuple<string, string> pair in listing)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string directoryPath = FtpConfiguration.GetDirectoryPath(pair.Item2);
                    if (!_sftpClient.Exists(directoryPath))
                    {
                        _sftpClient.CreateDirectory(directoryPath);
                    }

                    using (Stream fileStream = File.OpenRead(pair.Item1))
                    {
                        await Task.Factory.FromAsync(_sftpClient.BeginUploadFile(fileStream, pair.Item2, overwrite, null, null), _sftpClient.EndUploadFile);
                    }
                }
            }
            else
            {
                if (File.Exists(localPath))
                {
                    if (_sftpClient.Exists(remotePath) && !overwrite)
                    {
                        throw new IOException(Resources.FileExistsException);
                    }

                    using (Stream fileStream = File.OpenRead(localPath))
                    {
                        await Task.Factory.FromAsync(_sftpClient.BeginUploadFile(fileStream, remotePath, overwrite, null, null), _sftpClient.EndUploadFile);
                    } 
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.PathNotFoundException, localPath), nameof(localPath));
                }
            }
        }
        #endregion

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _sftpClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SftpSession() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()  
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
