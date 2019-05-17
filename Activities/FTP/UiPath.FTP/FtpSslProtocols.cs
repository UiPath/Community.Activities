using System;
using System.Security.Authentication;

namespace UiPath.FTP
{
    [Flags]
    public enum FtpSslProtocols
    {
        None = SslProtocols.None,
        Default = SslProtocols.Default,
        TLS_1_0 = SslProtocols.Tls,
        TLS_1_1 = SslProtocols.Tls11,
        TLS_1_2 = SslProtocols.Tls12
    }
}
