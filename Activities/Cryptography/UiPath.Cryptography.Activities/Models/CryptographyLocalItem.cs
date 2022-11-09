using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.Models
{
    /// <summary>
    /// Used to convert cryptography files as IResources.
    /// </summary>
    public class CryptographyLocalItem : ILocalResource
    {
        /// <summary>
        /// Create the ILocalResource implementation
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="fullName"></param>
        internal CryptographyLocalItem(byte[] byteArray, string fullName, string path = null)
        {
            if (path == null)
            {
                path = Path.GetRandomFileName();
            }

            File.WriteAllBytes(path, byteArray);

            _fileInfo = new FileInfo(path);

            //assign a new Guid for the resource (no Id from remoteItem as there is no remoteItem) 
            _guid = Guid.NewGuid();

            FullName = !string.IsNullOrEmpty(fullName) ? fullName : _fileInfo.FullName;
        }

        #region Internal properties and fields

        private readonly FileInfo _fileInfo;

        private readonly Guid _guid;

        #endregion Internal properties and fields

        #region ILocalResource implementation

        /// <inheritdoc />
        public DateTime? CreationDate => _fileInfo?.CreationTime;

        /// <inheritdoc />
        public string FullName { get; }

        /// <inheritdoc />
        public string IconUri => string.Empty;

        /// <inheritdoc />
        public string ID => _guid.ToString();

        /// <inheritdoc />
        public bool IsFolder => false;

        // File is considered resolved if a local path for it has been defined AND a file exists at that path.
        public async Task ResolveAsync(bool force = false, CancellationToken ct = new CancellationToken())
        {
            //nothing to resolve as there is no RemoteItem
        }

        /// <summary>
        /// Always true
        /// </summary>
        public bool IsResolved => true;

        /// <inheritdoc />
        public DateTime? LastModifiedDate => _fileInfo?.LastWriteTime;

        /// <inheritdoc />
        public string LocalPath => _fileInfo.FullName;

        /// <inheritdoc />
        public Dictionary<string, string> Metadata => new Dictionary<string, string>();

        /// <inheritdoc />
        public string MimeType => string.Empty;

        #endregion ILocalResource implementation
    }
}