using System;

namespace UiPath.FTP
{
    public enum FtpObjectType
    {
        Directory,
        File,
        Link,
        Other //named pipe, device, etc.
    }

    [Flags]
    public enum FtpFilterObjectType
    {
        None = 0,
        Directory = 1 << 0,
        File = 1 << 1,
        Link = 1 << 2,
        Other = 1 << 3,
    }
}
