using Renci.SshNet;
using Renci.SshNet.Common;
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

        internal SftpClient Client => _sftpClient;

        private const int DefaultFtpPort = 21;
        private const int DefaultProxyPort = 3128;

        public SftpSession(FtpConfiguration ftpConfiguration)
        {
            if (ftpConfiguration == null)
            {
                throw new ArgumentNullException(nameof(ftpConfiguration));
            }

            ConnectionInfo connectionInfo = null;

            var authMethods = new List<AuthenticationMethod>();

            //Add password authentication method if password is provided
            if (!String.IsNullOrEmpty(ftpConfiguration.Password))
            {
                authMethods.Add(new PasswordAuthenticationMethod(ftpConfiguration.Username, ftpConfiguration.Password));
            }

            //Add private key authentication method if private key is provided
            if (!String.IsNullOrEmpty(ftpConfiguration.ClientCertificatePath))
            {
                PrivateKeyFile keyFile = new PrivateKeyFile(ftpConfiguration.ClientCertificatePath, ftpConfiguration.ClientCertificatePassword);
                var keyFiles = new[] { keyFile };
                authMethods.Add(new PrivateKeyAuthenticationMethod(ftpConfiguration.Username, keyFiles));
            }

            // Register keyboard-interactive when a password is configured, mirroring the
            // PasswordAuthenticationMethod guard above. Servers that advertise only kbi
            // (OpenSSH with ChallengeResponseAuthentication/PAM) can then authenticate
            // using the configured password. Skipping kbi when there is no password avoids
            // a redundant failed auth round-trip on certificate-only connections.
            if (!String.IsNullOrEmpty(ftpConfiguration.Password))
            {
                var kbiMethod = new KeyboardInteractiveAuthenticationMethod(ftpConfiguration.Username);
                var kbiPassword = ftpConfiguration.Password;
                kbiMethod.AuthenticationPrompt += (sender, e) =>
                {
                    foreach (var prompt in e.Prompts)
                    {
                        if (!prompt.IsEchoed)
                        {
                            prompt.Response = kbiPassword;
                        }
                    }
                };
                authMethods.Add(kbiMethod);
            }

            //Throw an error if we ended up with no authentication method
            if (authMethods.Count == 0)
            {
                throw new ArgumentNullException(Resources.NoValidAuthenticationMethod);
            }
            if (ftpConfiguration.ProxyType == FtpProxyType.None)
            {
                if (ftpConfiguration.Port == null)
                {
                    connectionInfo = new ConnectionInfo(ftpConfiguration.Host, ftpConfiguration.Username, authMethods.ToArray());
                }
                else
                {
                    connectionInfo = new ConnectionInfo(ftpConfiguration.Host, ftpConfiguration.Port.Value, ftpConfiguration.Username, authMethods.ToArray());
                }
            }
            else
            {
                int proxyPort = (ftpConfiguration.ProxyPort == null) ? DefaultProxyPort : ftpConfiguration.ProxyPort.Value;
                int ftpPort = (ftpConfiguration.Port == null) ? DefaultFtpPort : ftpConfiguration.Port.Value;

                connectionInfo = new ConnectionInfo(ftpConfiguration.Host, ftpPort, ftpConfiguration.Username, ftpConfiguration.ProxyType.ToMaster(),ftpConfiguration.ProxyServer, proxyPort,ftpConfiguration.ProxyUsername,ftpConfiguration.ProxyPassword, authMethods.ToArray());
            }

            TimeSpan? timeoutSpan = ftpConfiguration.Timeout != null
                ? TimeSpan.FromMilliseconds(ftpConfiguration.Timeout.Value)
                : null;

            if (timeoutSpan.HasValue)
            {
                connectionInfo.Timeout = timeoutSpan.Value;
            }

            _sftpClient = new SftpClient(connectionInfo);

            if (timeoutSpan.HasValue)
            {
                _sftpClient.OperationTimeout = timeoutSpan.Value;
            }
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

        /// <remarks>
        /// <c>internal</c> rather than <c>private</c> so the download walk can be exercised directly
        /// by the tests: the only public entry point, <see cref="IFtpSession.Download"/>, writes to
        /// the local file system and pulls bytes through the real <see cref="SftpClient"/>.
        /// </remarks>
        internal IEnumerable<Tuple<string, string>> GetRemoteListing(string remotePath, string localPath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }

            return GetRemoteListing(ClientGet(remotePath), localPath, recursive);
        }

        private IEnumerable<Tuple<string, string>> GetRemoteListing(ISftpFile directory, string localPath, bool recursive)
        {
            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            List<ISftpFile> items = ClientListDirectory(directory.FullName).ToList();
            string nextLocalPath = Path.Combine(localPath, directory.Name);

            foreach (var file in items.Where(i => i.IsRegularFile))
            {
                listing.Add(new Tuple<string, string>(Path.Combine(nextLocalPath, file.Name), file.FullName));
            }

            if (recursive)
            {
                foreach (var child in items.Where(IsWalkableDirectory))
                {
                    listing.AddRange(Descend(child, () => GetRemoteListing(child, nextLocalPath, recursive)));
                }
            }

            return listing;
        }

        private IEnumerable<FtpObjectInfo> GetRemoteListing(string remotePath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            return GetRemoteListing(ClientGet(remotePath), recursive);
        }

        private IEnumerable<FtpObjectInfo> GetRemoteListing(ISftpFile directory, bool recursive)
        {
            List<FtpObjectInfo> listing = new List<FtpObjectInfo>();
            List<ISftpFile> items = ClientListDirectory(directory.FullName)
                .Where(sf => sf.Name != "." && sf.Name != "..")
                .ToList();

            listing.AddRange(items.Select(sf => sf.ToFtpObjectInfo()));

            if (recursive)
            {
                foreach (var child in items.Where(IsWalkableDirectory))
                {
                    listing.AddRange(Descend(child, () => GetRemoteListing(child, recursive)));
                }
            }

            return listing;
        }

        /// <remarks>
        /// <c>internal</c> for the same reason as its synchronous counterpart
        /// <see cref="GetRemoteListing(string, string, bool)"/>.
        /// </remarks>
        internal async Task<IEnumerable<Tuple<string, string>>> GetRemoteListingAsync(string remotePath, string localPath, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentNullException(nameof(localPath));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await GetRemoteListingAsync(ClientGet(remotePath), localPath, recursive, cancellationToken);
        }

        private async Task<IEnumerable<Tuple<string, string>>> GetRemoteListingAsync(ISftpFile directory, string localPath, bool recursive, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Tuple<string, string>> listing = new List<Tuple<string, string>>();
            List<ISftpFile> items = (await ClientListDirectoryAsync(directory.FullName)).ToList();
            string nextLocalPath = Path.Combine(localPath, directory.Name);

            foreach (var file in items.Where(i => i.IsRegularFile))
            {
                listing.Add(new Tuple<string, string>(Path.Combine(nextLocalPath, file.Name), file.FullName));
            }

            if (recursive)
            {
                foreach (var child in items.Where(IsWalkableDirectory))
                {
                    listing.AddRange(await DescendAsync(child, () => GetRemoteListingAsync(child, nextLocalPath, recursive, cancellationToken)));
                }
            }

            return listing;
        }

        private async Task<IEnumerable<FtpObjectInfo>> GetRemoteListingAsync(string remotePath, bool recursive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await GetRemoteListingAsync(ClientGet(remotePath), recursive, cancellationToken);
        }

        private async Task<IEnumerable<FtpObjectInfo>> GetRemoteListingAsync(ISftpFile directory, bool recursive, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<FtpObjectInfo> listing = new List<FtpObjectInfo>();
            List<ISftpFile> items = (await ClientListDirectoryAsync(directory.FullName))
                .Where(sf => sf.Name != "." && sf.Name != "..")
                .ToList();

            listing.AddRange(items.Select(sf => sf.ToFtpObjectInfo()));

            if (recursive)
            {
                foreach (var child in items.Where(IsWalkableDirectory))
                {
                    listing.AddRange(await DescendAsync(child, () => GetRemoteListingAsync(child, recursive, cancellationToken)));
                }
            }

            return listing;
        }

        private IEnumerable<string> GetMissingDirectories(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            List<string> missingDirectories = new List<string>();
            // TODO: We really need to find a better alternative to custom code for unix path handling.
            const char separator = '/';
            string currentPath = remotePath.StartsWith(separator.ToString()) ? separator.ToString() : string.Empty;
            foreach (string directory in remotePath.Trim(separator).Split(separator))
            {
                currentPath += directory + separator;
                if (!((IFtpSession)this).DirectoryExists(currentPath))
                {
                    missingDirectories.Add(currentPath);
                }
            }
            return missingDirectories;
        }

        private async Task<IEnumerable<string>> GetMissingDirectoriesAsync(string remotePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            List<string> missingDirectories = new List<string>();
            // TODO: We really need to find a better alternative to custom code for unix path handling.
            const char separator = '/';
            string currentPath = remotePath.StartsWith(separator.ToString()) ? separator.ToString() : string.Empty;
            foreach (string directory in remotePath.Trim(separator).Split(separator))
            {
                currentPath += directory + separator;
                if (!await ((IFtpSession)this).DirectoryExistsAsync(currentPath, cancellationToken))
                {
                    missingDirectories.Add(currentPath);
                }
            }
            return missingDirectories;
        }

        /// <summary>
        /// Calls <see cref="SftpClient.Exists"/> on the underlying client.
        /// Extracted as a <c>protected internal virtual</c> method so that tests can subclass
        /// <see cref="SftpSession"/> and override this single call without needing to mock the
        /// sealed <c>SftpClient.Exists</c> method directly.
        /// </summary>
        protected internal virtual bool ClientExists(string path) => _sftpClient.Exists(path);

        /// <summary>
        /// Resolves <paramref name="path"/> to an <see cref="ISftpFile"/> via the underlying client.
        /// Extracted as a <c>protected internal virtual</c> seam so tests can simulate a remote
        /// directory tree without a live connection (mirrors <see cref="ClientExists"/>).
        /// </summary>
        protected internal virtual ISftpFile ClientGet(string path) => _sftpClient.Get(path);

        /// <summary>
        /// Lists the entries of the directory at <paramref name="path"/> via the underlying client.
        /// Extracted as a <c>protected internal virtual</c> seam for testability (see <see cref="ClientGet"/>).
        /// </summary>
        protected internal virtual IEnumerable<ISftpFile> ClientListDirectory(string path) => _sftpClient.ListDirectory(path);

        /// <summary>
        /// Asynchronous counterpart of <see cref="ClientListDirectory"/>, kept separate so the
        /// async listing walks retain SSH.NET's true async APM call rather than blocking a thread
        /// pool thread. Also a <c>protected internal virtual</c> seam for testability.
        /// </summary>
        protected internal virtual Task<IEnumerable<ISftpFile>> ClientListDirectoryAsync(string path)
            => Task.Factory.FromAsync(_sftpClient.BeginListDirectory(path, null, null), _sftpClient.EndListDirectory);

        /// <summary>
        /// Decides whether a listing entry should be descended into during a recursive enumeration:
        /// a directory that is not <c>.</c> or <c>..</c>, which would loop forever.
        /// <para>
        /// There is deliberately no symbolic-link check here. Listing entries come from <c>readdir</c>,
        /// whose attributes are <c>lstat</c>-like, and <see cref="ISftpFile.IsDirectory"/> /
        /// <see cref="ISftpFile.IsSymbolicLink"/> both read the type field of the same mode word —
        /// <c>S_IFDIR</c> and <c>S_IFLNK</c> are distinct values of it, so a link is never reported as
        /// a directory and can never reach this predicate as one. Links are therefore not followed,
        /// which is the intended behaviour (a link cycle would recurse until the uncatchable
        /// <see cref="StackOverflowException"/>, and a link can point outside the requested subtree),
        /// but it needs no code: an explicit <c>!IsSymbolicLink</c> clause was dead and has been
        /// removed. <see cref="DeleteRecursive(ISftpFile, CancellationToken)"/> still carries one;
        /// it is equally inert and left alone as pre-existing.
        /// </para>
        /// </summary>
        private static bool IsWalkableDirectory(ISftpFile item)
            => item.IsDirectory && item.Name != "." && item.Name != "..";

        /// <summary>
        /// Runs a recursive-walk step over <paramref name="directory"/>, yielding nothing instead of
        /// failing the whole walk when the server will not open that one sub-directory.
        /// A server can return an entry from <c>readdir</c> and then refuse <c>opendir</c> on it —
        /// a dangling symbolic link it does not flag as a link, a stale mount point, an entry removed
        /// by someone else mid-walk, or a name that does not survive the path encoding round-trip.
        /// Losing an entire enumeration over one such entry is worse than skipping it, so the
        /// sub-directory is still reported in the results and only its contents are omitted, with the
        /// offending path traced. Mirrors <see cref="SafeExists"/>.
        /// Only sub-directories are treated this way: the caller's own remote path is resolved by
        /// <see cref="ClientGet"/> before any walk starts, so a bad remote path still throws.
        /// </summary>
        private IEnumerable<T> Descend<T>(ISftpFile directory, Func<IEnumerable<T>> walk)
        {
            try
            {
                return walk();
            }
            catch (SshException ex) when (IsUnreadablePath(ex))
            {
                TraceSkippedDirectory(directory, ex);
                return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// Asynchronous counterpart of <see cref="Descend{T}"/>.
        /// </summary>
        private async Task<IEnumerable<T>> DescendAsync<T>(ISftpFile directory, Func<Task<IEnumerable<T>>> walk)
        {
            try
            {
                return await walk();
            }
            catch (SshException ex) when (IsUnreadablePath(ex))
            {
                TraceSkippedDirectory(directory, ex);
                return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// True for the SFTP failures that mean "this path cannot be read", as opposed to a broken
        /// connection or a timeout. Typed subclasses such as <see cref="SshConnectionException"/> and
        /// <see cref="SshOperationTimeoutException"/> deliberately fall through so that real transport
        /// failures are never mistaken for an inaccessible directory — the same distinction
        /// <see cref="SafeExists"/> makes, including the bare <see cref="SshException"/>
        /// (SSH_FX_FAILURE) that some servers return in place of a typed error.
        /// </summary>
        private static bool IsUnreadablePath(SshException ex)
            => ex is SftpPathNotFoundException
            || ex is SftpPermissionDeniedException
            || ex.GetType() == typeof(SshException);

        private static void TraceSkippedDirectory(ISftpFile directory, SshException ex)
        {
            Trace.TraceWarning(
                "SftpSession: skipping sub-directory '{0}' during recursive enumeration; the server would not open it: {1}",
                directory.FullName,
                ex.Message);
        }

        /// <summary>
        /// Recursively deletes the SFTP object at <paramref name="path"/>.
        /// The SFTP protocol's RMDIR (issued by <see cref="ISftpFile.Delete"/> for directories) only
        /// removes empty directories, so a directory's contents must be deleted first. Files and
        /// symbolic links are unlinked directly; real directories are emptied depth-first and then
        /// removed. Symbolic links are never followed during the walk, mirroring <c>rm -rf</c>.
        /// A missing <paramref name="path"/> surfaces as the same exception SSH.NET raises for
        /// <c>Get</c>, preserving the previous "path not found" behaviour.
        /// </summary>
        private void DeleteRecursive(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DeleteRecursive(ClientGet(path), cancellationToken);
        }

        private void DeleteRecursive(ISftpFile item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.IsDirectory && !item.IsSymbolicLink)
            {
                foreach (ISftpFile child in ClientListDirectory(item.FullName))
                {
                    if (child.Name == "." || child.Name == "..")
                    {
                        continue;
                    }

                    DeleteRecursive(child, cancellationToken);
                }
            }

            item.Delete();
        }

        /// <summary>
        /// Wraps <see cref="ClientExists"/> and returns <c>false</c> for bare
        /// <see cref="SshException"/> (SSH_FX_FAILURE) thrown by some SFTP server
        /// implementations when a path does not exist, while letting typed subclasses
        /// (e.g. <see cref="SshConnectionException"/>, <see cref="SshOperationTimeoutException"/>)
        /// propagate so that real connection/timeout failures are not silently swallowed.
        /// </summary>
        private bool SafeExists(string path)
        {
            try
            {
                return ClientExists(path);
            }
            catch (SshException ex) when (ex is SftpPathNotFoundException || ex.GetType() == typeof(SshException))
            {
                Trace.TraceWarning(
                    "SftpSession.SafeExists: bare SshException treated as 'not found' for path '{0}': {1}",
                    path,
                    ex.Message);
                return false;
            }
        }

        #region IFtpSession members

        bool IFtpSession.IsConnected()
        {
            return _sftpClient.IsConnected;
        }

        Task<bool> IFtpSession.IsConnectedAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _sftpClient.IsConnected, cancellationToken);
        }

        void IFtpSession.Open()
        {
            Trace.TraceInformation("Attempting to open an SFTP connection.");

            _sftpClient.Connect();
        }

        Task IFtpSession.OpenAsync(CancellationToken cancellationToken)
        {
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
            Trace.TraceInformation("Attempting to asynchronously close an SFTP connection.");

            return Task.Run(() => _sftpClient.Disconnect(), cancellationToken);
        }

        void IFtpSession.CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            foreach (string directory in GetMissingDirectories(path))
            {
                _sftpClient.CreateDirectory(directory);
            }
        }

        async Task IFtpSession.CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            foreach (string directory in await GetMissingDirectoriesAsync(path, cancellationToken))
            {
                await Task.Run(() => _sftpClient.CreateDirectory(directory), cancellationToken);
            }
        }

        void IFtpSession.Delete(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            DeleteRecursive(path, CancellationToken.None);
        }

        Task IFtpSession.DeleteAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return Task.Run(() => DeleteRecursive(path, cancellationToken), cancellationToken);
        }

        bool IFtpSession.DirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return SafeExists(path) && ((IFtpSession)this).GetObjectType(path) == UiPath.FTP.FtpObjectType.Directory;
        }

        async Task<bool> IFtpSession.DirectoryExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (SafeExists(path))
            {
                return await ((IFtpSession)this).GetObjectTypeAsync(path, cancellationToken) == UiPath.FTP.FtpObjectType.Directory;
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

            UiPath.FTP.FtpObjectType objectType = ((IFtpSession)this).GetObjectType(remotePath);
            if (objectType == UiPath.FTP.FtpObjectType.Directory)
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
                if (objectType == UiPath.FTP.FtpObjectType.File)
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

            UiPath.FTP.FtpObjectType objectType = ((IFtpSession)this).GetObjectType(remotePath);
            if (objectType == UiPath.FTP.FtpObjectType.Directory)
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
                if (objectType == UiPath.FTP.FtpObjectType.File)
                {
                    if (File.Exists(localPath))
                    {
                        if (overwrite)
                        {
                            File.Delete(localPath);
                        }
                        else
                        {
                            throw new IOException(Resources.FileExistsException);
                        }
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

            return SafeExists(path) && ((IFtpSession)this).GetObjectType(path) == UiPath.FTP.FtpObjectType.File;
        }

        async Task<bool> IFtpSession.FileExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (SafeExists(path))
            {
                return await ((IFtpSession)this).GetObjectTypeAsync(path, cancellationToken) == UiPath.FTP.FtpObjectType.File;
            }
            else
            {
                return false;
            }
        }

        UiPath.FTP.FtpObjectType IFtpSession.GetObjectType(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!SafeExists(path))
            {
                throw new ArgumentException(string.Format(Resources.PathNotFoundException, path), nameof(path));
            }

            return _sftpClient.Get(path).GetFtpObjectType();
        }

        Task<UiPath.FTP.FtpObjectType> IFtpSession.GetObjectTypeAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!SafeExists(path))
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

            return GetRemoteListingAsync(remotePath, recursive, cancellationToken);
        }

        void IFtpSession.Move(string remotePath, string newPath, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                throw new ArgumentNullException(nameof(remotePath));
            }
            if (string.IsNullOrWhiteSpace(newPath))
            {
                throw new ArgumentNullException(nameof(newPath));
            }

            if (!SafeExists(remotePath))
            {
                throw new IOException(string.Format(Resources.PathNotFoundException, remotePath));
            }

            if (SafeExists(newPath) && _sftpClient.Get(newPath).IsRegularFile &&  !overwrite)
            {
                throw new IOException(Resources.FileExistsException);
            }

            var file = _sftpClient.Get(remotePath);

            if(SafeExists(newPath) && file.IsRegularFile)
            {
                var movePath = _sftpClient.Get(newPath);
                if (movePath.IsDirectory)
                {
                    var newFP = string.Format("{0}/{1}", movePath.FullName, file.Name);
                    if (SafeExists(newFP) && _sftpClient.Get(newFP).IsRegularFile)
                    {
                        if (overwrite)
                            _sftpClient.DeleteFile(newFP);
                        else
                            throw new IOException(Resources.FileExistsException);
                    }
                }
                else
                {
                    if (overwrite)
                        movePath.Delete();
                    else
                        throw new IOException(Resources.FileExistsException);
                }
            }
            file.MoveTo(newPath);
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
                    if (!SafeExists(directoryPath))
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
                    if (SafeExists(remotePath) && !overwrite)
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

            if (Directory.Exists(localPath))
            {
                IEnumerable<Tuple<string, string>> listing = GetLocalListing(localPath, remotePath, recursive);

                foreach (Tuple<string, string> pair in listing)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string directoryPath = FtpConfiguration.GetDirectoryPath(pair.Item2);
                    if (!SafeExists(directoryPath))
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
                    if (SafeExists(remotePath) && !overwrite)
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
