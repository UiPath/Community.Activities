using System;
using UiPath.FTP;
using UiPath.FTP.Enums;

namespace UiPath.FTP.Activities.API.Models
{
    /// <summary>
    /// Options for configuring an FTP/SFTP session scope.
    /// </summary>
    public class FtpScopeOptions
    {
        /// <summary>
        /// The FTP server hostname or IP address.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The FTP server port. When <c>null</c> the default port for the chosen protocol is used.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Username for authentication. Leave <c>null</c> together with <see cref="UseAnonymousLogin"/> set to <c>true</c> for anonymous access.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for authentication.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// When <c>true</c> the session connects anonymously, ignoring <see cref="Username"/> and <see cref="Password"/>.
        /// </summary>
        public bool UseAnonymousLogin { get; set; }

        /// <summary>
        /// The FTPS security mode. Default is <see cref="FtpsMode.None"/> (plain FTP).
        /// </summary>
        public FtpsMode FtpsMode { get; set; } = FtpsMode.None;

        /// <summary>
        /// The SSL/TLS protocols to use when <see cref="FtpsMode"/> is not <see cref="FtpsMode.None"/>.
        /// Default is <see cref="FtpSslProtocols.Auto"/>.
        /// </summary>
        public FtpSslProtocols SslProtocols { get; set; } = FtpSslProtocols.Auto;

        /// <summary>
        /// When <c>true</c> the session uses SFTP instead of FTP/FTPS.
        /// </summary>
        public bool UseSftp { get; set; }

        /// <summary>
        /// Path to a client certificate file for FTPS/SFTP authentication.
        /// </summary>
        public string ClientCertificatePath { get; set; }

        /// <summary>
        /// Password protecting the client certificate file.
        /// </summary>
        public string ClientCertificatePassword { get; set; }

        /// <summary>
        /// When <c>true</c> all server certificates are accepted without validation.
        /// <para><b>Warning:</b> disabling certificate validation exposes the connection to
        /// man-in-the-middle attacks. Use only in development or isolated test environments,
        /// never in production.</para>
        /// </summary>
        public bool AcceptAllCertificates { get; set; }

        /// <summary>
        /// Connection timeout. <c>null</c> uses the default library timeout.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Proxy type to use. Default is <see cref="FtpProxyType.None"/>.
        /// </summary>
        public FtpProxyType ProxyType { get; set; } = FtpProxyType.None;

        /// <summary>
        /// Proxy server hostname. Required when <see cref="ProxyType"/> is not <see cref="FtpProxyType.None"/>.
        /// </summary>
        public string ProxyServer { get; set; }

        /// <summary>
        /// Proxy server port. Required when <see cref="ProxyType"/> is not <see cref="FtpProxyType.None"/>.
        /// </summary>
        public int? ProxyPort { get; set; }

        /// <summary>
        /// Username for proxy authentication.
        /// </summary>
        public string ProxyUsername { get; set; }

        /// <summary>
        /// Password for proxy authentication.
        /// </summary>
        public string ProxyPassword { get; set; }
    }
}
