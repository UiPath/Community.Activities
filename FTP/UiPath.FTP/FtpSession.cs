using FluentFTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Properties;

namespace UiPath.FTP
{
    public class FtpSession : IFtpSession
    {
        private readonly FtpClient _ftpClient;

        public FtpSession(FtpConfiguration ftpConfiguration, FtpsMode ftpsMode)
        {
            if (ftpConfiguration == null)
            {
                throw new ArgumentNullException(nameof(ftpConfiguration));
            }

            _ftpClient = new FtpClient(ftpConfiguration.Host);

            if (ftpConfiguration.Port != null)
            {
                _ftpClient.Port = ftpConfiguration.Port.Value;
            }
            if (ftpConfiguration.UseAnonymousLogin == false)
            {
                _ftpClient.Credentials = new NetworkCredential(ftpConfiguration.Username, ftpConfiguration.Password);
            }

            switch (ftpsMode)
            {
                case FtpsMode.Explicit:
                    _ftpClient.EncryptionMode = FtpEncryptionMode.Explicit;
                    break;
                case FtpsMode.Implicit:
                    _ftpClient.EncryptionMode = FtpEncryptionMode.Implicit;
                    break;
                case FtpsMode.None:
                    _ftpClient.EncryptionMode = FtpEncryptionMode.None;
                    break;
                default:
                    throw new InvalidOperationException(Resources.UnknownFTPSModeException);
            }

            if (ftpsMode != FtpsMode.None)
            {
                _ftpClient.ValidateCertificate += (control, e) => _ftpClient_ValidateCertificate(control, e, ftpConfiguration.AcceptAllCertificates);

                if (string.IsNullOrWhiteSpace(ftpConfiguration.ClientCertificatePath) == false)
                {
                    X509Certificate2 clientCertificate;
                    if (ftpConfiguration.ClientCertificatePassword == null)
                    {
                        clientCertificate = new X509Certificate2(ftpConfiguration.ClientCertificatePath);
                    }
                    else
                    {
                        clientCertificate = new X509Certificate2(ftpConfiguration.ClientCertificatePath, ftpConfiguration.ClientCertificatePassword);
                    }

                    _ftpClient.ClientCertificates.Add(clientCertificate);
                }
            }
        }

        private void _ftpClient_ValidateCertificate(FtpClient control, FtpSslValidationEventArgs e, bool acceptAllCertificates)
        {
            Trace.TraceInformation("Validating server certificate.");

            e.Accept = acceptAllCertificates ? true : e.PolicyErrors == SslPolicyErrors.None;

            // Only for development purposes.
            //if (e.Accept == false && e.Chain.ChainStatus.Length == 1 && e.Chain.ChainStatus[0].Status == X509ChainStatusFlags.RevocationStatusUnknown)
            //{
            //    e.Accept = true;
            //}
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

            string initialWorkingDirectory = _ftpClient.GetWorkingDirectory();
            _ftpClient.SetWorkingDirectory(remotePath);
            string currentWorkingDirectory = _ftpClient.GetWorkingDirectory();

            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            FtpListItem currentDirectory = _ftpClient.GetObjectInfo(currentWorkingDirectory);
            FtpListItem[] items = _ftpClient.GetListing(currentDirectory.FullName);
            string nextLocalPath = Path.Combine(localPath, currentDirectory.Name);

            foreach (FtpListItem file in items.Where(i => i.Type == FtpFileSystemObjectType.File))
            {
                listing.Add(new Tuple<string, string>(Path.Combine(nextLocalPath, file.Name), file.FullName));
            }

            if (recursive)
            {
                foreach (FtpListItem directory in items.Where(i => i.Type == FtpFileSystemObjectType.Directory))
                {
                    listing.AddRange(GetRemoteListing(directory.FullName, nextLocalPath, recursive));
                }
            }

            _ftpClient.SetWorkingDirectory(initialWorkingDirectory);

            return listing;
        }

        private IEnumerable<FtpObjectInfo> GetRemoteListing(string remotePath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            string initialWorkingDirectory = _ftpClient.GetWorkingDirectory();
            _ftpClient.SetWorkingDirectory(remotePath);

            FtpListItem currentDirectory = _ftpClient.GetObjectInfo(_ftpClient.GetWorkingDirectory());

            List<FtpObjectInfo> listing = new List<FtpObjectInfo>();
            FtpListItem[] items = _ftpClient.GetListing(currentDirectory?.FullName ?? remotePath);

            listing.AddRange(items.Select(fli => fli.ToFtpObjectInfo()));

            if (recursive)
            {
                foreach (FtpListItem directory in items.Where(i => i.Type == FtpFileSystemObjectType.Directory))
                {
                    listing.AddRange(GetRemoteListing(directory.FullName, recursive));
                }
            }

            _ftpClient.SetWorkingDirectory(initialWorkingDirectory);

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

            string initialWorkingDirectory = await _ftpClient.GetWorkingDirectoryAsync();
            await _ftpClient.SetWorkingDirectoryAsync(remotePath);
            string currentWorkingDirectory = await _ftpClient.GetWorkingDirectoryAsync();

            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            FtpListItem currentDirectory = await _ftpClient.GetObjectInfoAsync(currentWorkingDirectory);
            FtpListItem[] items = await _ftpClient.GetListingAsync(currentDirectory.FullName);
            string nextLocalPath = Path.Combine(localPath, currentDirectory.Name);

            foreach (FtpListItem file in items.Where(i => i.Type == FtpFileSystemObjectType.File))
            {
                listing.Add(new Tuple<string, string>(Path.Combine(nextLocalPath, file.Name), file.FullName));
            }

            if (recursive)
            {
                foreach (FtpListItem directory in items.Where(i => i.Type == FtpFileSystemObjectType.Directory))
                {
                    listing.AddRange(await GetRemoteListingAsync(directory.FullName, nextLocalPath, recursive, cancellationToken));
                }
            }

            await _ftpClient.SetWorkingDirectoryAsync(initialWorkingDirectory);

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

            string initialWorkingDirectory = _ftpClient.GetWorkingDirectory();
            _ftpClient.SetWorkingDirectory(remotePath);

            FtpListItem currentDirectory = _ftpClient.GetObjectInfo(_ftpClient.GetWorkingDirectory());

            List<FtpObjectInfo> listing = new List<FtpObjectInfo>();
            FtpListItem[] items = await _ftpClient.GetListingAsync(currentDirectory?.FullName ?? remotePath);

            listing.AddRange(items.Select(fli => fli.ToFtpObjectInfo()));

            if (recursive)
            {
                foreach (FtpListItem directory in items.Where(i => i.Type == FtpFileSystemObjectType.Directory))
                {
                    listing.AddRange(await GetRemoteListingAsync(directory.FullName, recursive, cancellationToken));
                }
            }

            _ftpClient.SetWorkingDirectory(initialWorkingDirectory);

            return listing;
        }

        #region IFtpSession members
        bool IFtpSession.IsConnected()
        {
            return _ftpClient.IsConnected;
        }

        Task<bool> IFtpSession.IsConnectedAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            return Task.Run(() => _ftpClient.IsConnected, cancellationToken);
        }

        void IFtpSession.Open()
        {
            Trace.TraceInformation("Attempting to open an FTP connection.");

            _ftpClient.Connect();
        }

        Task IFtpSession.OpenAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            Trace.TraceInformation("Attempting to asynchronously open an FTP connection.");

            // TODO: Should we check for cancellation here?
            return _ftpClient.ConnectAsync();
        }

        void IFtpSession.Close()
        {
            Trace.TraceInformation("Attempting to close an FTP connection.");

            _ftpClient.Disconnect();
        }

        Task IFtpSession.CloseAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            Trace.TraceInformation("Attempting to asynchronously close an FTP connection.");

            // TODO: Should we check for cancellation here?
            return _ftpClient.DisconnectAsync();
        }

        void IFtpSession.CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _ftpClient.CreateDirectory(path);
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

            // TODO: Should we check for cancellation here?
            return _ftpClient.CreateDirectoryAsync(path);
        }

        void IFtpSession.Delete(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            switch (((IFtpSession)this).GetObjectType(path))
            {
                case FtpObjectType.Directory:
                    _ftpClient.DeleteDirectory(path);
                    break;
                case FtpObjectType.File:
                    _ftpClient.DeleteFile(path);
                    break;
                default:
                    throw new NotImplementedException(Resources.UnsupportedObjectTypeException);
            }
        }

        async Task IFtpSession.DeleteAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            switch (await ((IFtpSession)this).GetObjectTypeAsync(path, cancellationToken))
            {
                case FtpObjectType.Directory:
                    await _ftpClient.DeleteDirectoryAsync(path);
                    break;
                case FtpObjectType.File:
                    await _ftpClient.DeleteFileAsync(path);
                    break;
                default:
                    throw new NotImplementedException(Resources.UnsupportedObjectTypeException);
            }
        }

        bool IFtpSession.DirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return _ftpClient.DirectoryExists(path);
        }

        Task<bool> IFtpSession.DirectoryExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            // TODO: Should we check for cancellation here?
            return _ftpClient.DirectoryExistsAsync(path);
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

                foreach (Tuple<string, string> pair in listing)
                {
                    _ftpClient.DownloadFile(pair.Item1, pair.Item2, overwrite);
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

                    _ftpClient.DownloadFileAsync(localPath, remotePath, overwrite); 
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

            FtpObjectType objectType = await ((IFtpSession)this).GetObjectTypeAsync(remotePath, cancellationToken);
            if (objectType == FtpObjectType.Directory)
            {
                IEnumerable<Tuple<string, string>> listing = await GetRemoteListingAsync(remotePath, localPath, recursive, cancellationToken);

                foreach (Tuple<string, string> pair in listing)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _ftpClient.DownloadFileAsync(pair.Item1, pair.Item2, overwrite, FtpVerify.None, cancellationToken, null);
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

                    await _ftpClient.DownloadFileAsync(localPath, remotePath, overwrite, FtpVerify.None, cancellationToken, null);
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

            return _ftpClient.FileExists(path);
        }

        Task<bool> IFtpSession.FileExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            // TODO: Should we check for cancellation here?
            return _ftpClient.FileExistsAsync(path);
        }

        FtpObjectType IFtpSession.GetObjectType(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            FtpObjectType objectType = FtpObjectType.File;
            FtpListItem objectInfo = _ftpClient.GetObjectInfo(path);
            if (objectInfo == null)
            {
                if (_ftpClient.FileExists(path))
                {
                    objectType = FtpObjectType.File;
                }
                else
                {
                    if (_ftpClient.DirectoryExists(path))
                    {
                        objectType = FtpObjectType.Directory;
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(Resources.PathNotFoundException, path), nameof(path));
                    }
                }
            }
            else
            {
                objectType = objectInfo.Type.ToFtpObjectType();
            }

            return objectType;
        }

        async Task<FtpObjectType> IFtpSession.GetObjectTypeAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            FtpObjectType objectType = FtpObjectType.File;
            FtpListItem objectInfo = await _ftpClient.GetObjectInfoAsync(path);
            if (objectInfo == null)
            {
                if (await _ftpClient.FileExistsAsync(path))
                {
                    objectType = FtpObjectType.File;
                }
                else
                {
                    if (await _ftpClient.DirectoryExistsAsync(path))
                    {
                        objectType = FtpObjectType.Directory;
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(Resources.PathNotFoundException, path), nameof(path));
                    }
                }
            }
            else
            {
                objectType = objectInfo.Type.ToFtpObjectType();
            }

            return objectType;
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
                    _ftpClient.UploadFile(pair.Item1, pair.Item2, overwrite ? FtpExists.Overwrite : FtpExists.Skip, true);
                }
            }
            else
            {
                if (File.Exists(localPath))
                {
                    if (_ftpClient.FileExists(remotePath) && !overwrite)
                    {
                        throw new IOException(Resources.FileExistsException);
                    }

                    _ftpClient.UploadFile(localPath, remotePath, overwrite ? FtpExists.Overwrite : FtpExists.Skip, true); 
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

                    await _ftpClient.UploadFileAsync(pair.Item1, pair.Item2, overwrite ? FtpExists.Overwrite : FtpExists.Skip, true, FtpVerify.None, cancellationToken, null);
                }
            }
            else
            {
                if (File.Exists(localPath))
                {
                    if ((await _ftpClient.FileExistsAsync(remotePath)) && !overwrite)
                    {
                        throw new IOException(Resources.FileExistsException);
                    }

                    await _ftpClient.UploadFileAsync(localPath, remotePath, overwrite ? FtpExists.Overwrite : FtpExists.Skip, true, FtpVerify.None, cancellationToken, null); 
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
                    _ftpClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FtpSession() {
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
