using FluentFTP;
using Renci.SshNet.Sftp;
using System;
using UiPath.FTP.Properties;

namespace UiPath.FTP
{
    internal static class Extensions
    {
        public static FtpObjectInfo ToFtpObjectInfo(this FtpListItem ftpListItem)
        {
            FtpObjectInfo ftpObjectInfo = new FtpObjectInfo();

            ftpObjectInfo.FullName = ftpListItem.FullName;
            ftpObjectInfo.Name = ftpListItem.Name;
            ftpObjectInfo.Size = ftpListItem.Size;
            ftpObjectInfo.Created = ftpListItem.Created;
            ftpObjectInfo.Modified = ftpListItem.Modified;
            ftpObjectInfo.Type = ftpListItem.Type.ToFtpObjectType();
            ftpObjectInfo.GroupPermissions = ftpListItem.GroupPermissions.ToFtpPermissions();
            ftpObjectInfo.OthersPermissions = ftpListItem.OthersPermissions.ToFtpPermissions();
            ftpObjectInfo.OwnerPermissions = ftpListItem.OwnerPermissions.ToFtpPermissions();

            return ftpObjectInfo;
        }

        public static FtpObjectInfo ToFtpObjectInfo(this ISftpFile sftpFile)
        {
            FtpObjectInfo ftpObjectInfo = new FtpObjectInfo();

            ftpObjectInfo.FullName = sftpFile.FullName;
            ftpObjectInfo.Name = sftpFile.Name;
            ftpObjectInfo.Size = sftpFile.Length;
            ftpObjectInfo.Created = DateTime.MinValue;
            ftpObjectInfo.Modified = sftpFile.LastWriteTimeUtc;
            ftpObjectInfo.Type = sftpFile.GetFtpObjectType();
            ftpObjectInfo.GroupPermissions = ToFtpPermissions(sftpFile.GroupCanExecute, sftpFile.GroupCanWrite, sftpFile.GroupCanRead);
            ftpObjectInfo.OthersPermissions = ToFtpPermissions(sftpFile.OthersCanExecute, sftpFile.OthersCanWrite, sftpFile.OthersCanRead);
            ftpObjectInfo.OwnerPermissions = ToFtpPermissions(sftpFile.OwnerCanExecute, sftpFile.OwnerCanWrite, sftpFile.OwnerCanRead);

            return ftpObjectInfo;
        }

        public static UiPath.FTP.FtpObjectType ToFtpObjectType(this FluentFTP.FtpObjectType ftpFileSystemObjectType)
        {
            switch (ftpFileSystemObjectType)
            {
                case FluentFTP.FtpObjectType.File:
                    return UiPath.FTP.FtpObjectType.File;
                case FluentFTP.FtpObjectType.Directory:
                    return UiPath.FTP.FtpObjectType.Directory;
                case FluentFTP.FtpObjectType.Link:
                    return UiPath.FTP.FtpObjectType.Link;
                default:
                    return UiPath.FTP.FtpObjectType.Other;
            }
        }

        /// <summary>
        /// Classifies an SFTP entry. Feeds both <see cref="IFtpSession.GetObjectType"/> — the branch
        /// Download, File Exists and Directory Exists dispatch on — and the
        /// <see cref="FtpObjectInfo.Type"/> that List Files and Folders reports and filters on.
        /// <para>
        /// <b>A symbolic link is classified <see cref="FtpObjectType.File"/>, never
        /// <see cref="FtpObjectType.Link"/>, and that must not be "fixed" casually.</b> SSH.NET tests
        /// the mode word with <c>(mode &amp; S_IFREG) == S_IFREG</c> rather than against the type
        /// mask, and <c>S_IFLNK</c> (<c>0xA000</c>) contains <c>S_IFREG</c> (<c>0x8000</c>), so
        /// <see cref="ISftpFile.IsRegularFile"/> is already <see langword="true"/> for every link and
        /// wins the first test below. Consequences, all verified against SSH.NET 2024.1.0:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="FtpObjectType.Link"/> is unreachable on the SFTP path, so List Files and Folders'
        /// <c>Link</c> filter matches nothing. The FTP/FTPS path does not share this — FluentFTP has a
        /// real <c>Link</c> object type — so the two protocols disagree.
        /// </description></item>
        /// <item><description>
        /// Testing <see cref="ISftpFile.IsSymbolicLink"/> first would fix that, but it also changes
        /// what the dispatch callers see: <c>Download</c> would throw
        /// <see cref="NotImplementedException"/> on a symlinked file and <c>File Exists</c> would
        /// answer <see langword="false"/> for one, both of which work today because opening a link
        /// server-side follows it. Separating the two uses is possible, but it still changes
        /// <see cref="FtpObjectInfo.Type"/> for existing workflows.
        /// </description></item>
        /// <item><description>
        /// A link whose target is a directory is likewise classified as a file. Resolving that needs a
        /// <c>stat</c> of the target, and SSH.NET's <c>Get</c>, <c>GetAttributes</c> and <c>Exists</c>
        /// are all <c>lstat</c>.
        /// </description></item>
        /// </list>
        /// <para>
        /// All three are pre-existing and deliberately left in place: correcting them is a breaking
        /// change to output that shipped workflows already consume. Pinned by
        /// <c>SFTP_GetFtpObjectType_*</c> in <c>SftpSessionTests</c>.
        /// </para>
        /// </summary>
        public static UiPath.FTP.FtpObjectType GetFtpObjectType(this ISftpFile sftpFile)
        {
            if (sftpFile == null)
            {
                throw new ArgumentNullException(nameof(sftpFile));
            }

            if (sftpFile.IsRegularFile)
            {
                return UiPath.FTP.FtpObjectType.File;
            }
            if (sftpFile.IsDirectory)
            {
                return UiPath.FTP.FtpObjectType.Directory;
            }
            // No IsSymbolicLink branch: IsSymbolicLink implies IsRegularFile (see the remarks above),
            // so it could never be reached. Its absence is not an oversight.

            return UiPath.FTP.FtpObjectType.Other;
        }

        public static FtpPermissions ToFtpPermissions(this FtpPermission ftpPermission)
        {
            return (FtpPermissions)ftpPermission;
        }

        public static FtpPermissions ToFtpPermissions(bool execute, bool write, bool read)
        {
            FtpPermissions ftpPermissions = FtpPermissions.None;

            ftpPermissions |= execute ? FtpPermissions.Execute : FtpPermissions.None;
            ftpPermissions |= write ? FtpPermissions.Write : FtpPermissions.None;
            ftpPermissions |= read ? FtpPermissions.Read : FtpPermissions.None;

            return ftpPermissions;
        }
    }
}
