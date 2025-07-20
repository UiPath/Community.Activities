using System;
using System.Security.Authentication;

namespace UiPath.FTP
{
    [Flags]
    public enum FtpSslProtocols
    {
        Auto = SslProtocols.None, // see description of SslProtocols.None, it's actually Auto
        [Obsolete("Allows only obsolete protocols, use Auto instead")]
        Default = SslProtocols.Default,
        [Obsolete("Weak protocol, should not be used")]
        TLS_1_0 = SslProtocols.Tls,
        [Obsolete("Weak protocol, should not be used")]
        TLS_1_1 = SslProtocols.Tls11,
        TLS_1_2 = SslProtocols.Tls12
    }
}
