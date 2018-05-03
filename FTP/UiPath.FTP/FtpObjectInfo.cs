using System;

namespace UiPath.FTP
{
    public sealed class FtpObjectInfo
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public FtpObjectType Type { get; set; }
        public FtpPermissions GroupPermissions { get; set; }
        public FtpPermissions OthersPermissions { get; set; }
        public FtpPermissions OwnerPermissions { get; set; }
    }
}
