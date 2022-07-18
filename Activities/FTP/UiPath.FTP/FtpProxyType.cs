using Renci.SshNet;
using System;

namespace UiPath.FTP
{
    public enum FtpProxyType
    {
        None = 0,
        Socks4 = 1,
        Socks5 = 2,
        Http = 3
    }

    internal static class FtpProxyTypeExtensions
    {
        public static ProxyTypes ToMaster(this FtpProxyType ftpProxyType)
        {
            switch (ftpProxyType)
            {
                case FtpProxyType.None:
                    return ProxyTypes.None;
                case FtpProxyType.Socks4: 
                    return ProxyTypes.Socks4;
                case FtpProxyType.Socks5: 
                    return ProxyTypes.Socks5;
                case FtpProxyType.Http: 
                    return ProxyTypes.Http;
                default:
                    throw new NotImplementedException();
            };
        }
    }
}
