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

        public static FtpObjectInfo ToFtpObjectInfo(this SftpFile sftpFile)
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

        public static FtpObjectType ToFtpObjectType(this FtpFileSystemObjectType ftpFileSystemObjectType)
        {
            switch (ftpFileSystemObjectType)
            {
                case FtpFileSystemObjectType.File:
                    return FtpObjectType.File;
                case FtpFileSystemObjectType.Directory:
                    return FtpObjectType.Directory;
                case FtpFileSystemObjectType.Link:
                    return FtpObjectType.Link;
                default:
                    throw new NotSupportedException(Resources.UnsupportedObjectTypeException);
            }
        }

        public static FtpObjectType GetFtpObjectType(this SftpFile sftpFile)
        {
            if (sftpFile == null)
            {
                throw new ArgumentNullException(nameof(sftpFile));
            }

            if (sftpFile.IsRegularFile)
            {
                return FtpObjectType.File;
            }
            if (sftpFile.IsDirectory)
            {
                return FtpObjectType.Directory;
            }
            if (sftpFile.IsSymbolicLink)
            {
                return FtpObjectType.Link;
            }

            throw new NotSupportedException(Resources.UnsupportedObjectTypeException);
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
