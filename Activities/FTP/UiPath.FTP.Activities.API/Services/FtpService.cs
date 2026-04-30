using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP;
using UiPath.FTP.Activities.API.Models;

namespace UiPath.FTP.Activities.API
{
    internal class FtpService : IFtpService
    {
        private readonly Func<FtpConfiguration, FtpsMode, IFtpSession> _sessionFactory;

        public FtpService()
            : this((config, mode) => new FtpSession(config, mode))
        {
        }

        // For testing: allows injecting a fake IFtpSession without a real FTP server.
        // The factory is only called for non-SFTP sessions; SFTP is not injectable here
        // because the cancel test uses the FTP path.
        internal FtpService(Func<FtpConfiguration, FtpsMode, IFtpSession> sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public async Task<IFtpScopeHandle> UseFtpSession(FtpScopeOptions options, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.Host))
                throw new ArgumentException("Host must not be null or whitespace.", nameof(options));

            if (options.Timeout.HasValue && options.Timeout.Value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be non-negative.");

            int? effectiveTimeoutMs = options.Timeout.HasValue
                ? (int)options.Timeout.Value.TotalMilliseconds
                : null;

            var config = new FtpConfiguration(options.Host)
            {
                Port = options.Port,
                Username = options.Username,
                Password = SecureStringToPlainText(options.PasswordSecure) ?? options.Password,
                UseAnonymousLogin = options.UseAnonymousLogin,
                SslProtocols = options.SslProtocols,
                ClientCertificatePath = options.ClientCertificatePath,
                ClientCertificatePassword = SecureStringToPlainText(options.ClientCertificatePasswordSecure) ?? options.ClientCertificatePassword,
                AcceptAllCertificates = options.AcceptAllCertificates,
                Timeout = effectiveTimeoutMs,
                ProxyType = options.ProxyType,
                ProxyServer = options.ProxyServer,
                ProxyPort = options.ProxyPort,
                ProxyUsername = options.ProxyUsername,
                ProxyPassword = SecureStringToPlainText(options.ProxyPasswordSecure) ?? options.ProxyPassword,
            };

            IFtpSession session = options.UseSftp
                ? new SftpSession(config)
                : _sessionFactory(config, options.FtpsMode);

            try
            {
                await session.OpenAsync(ct);
            }
            catch
            {
                session.Dispose();
                throw;
            }

            return new FtpScopeHandle(session);
        }

        /// <summary>
        /// Converts a <see cref="System.Security.SecureString"/> to a plain-text string using unmanaged memory
        /// so the secret is never copied into a managed string on the heap unnecessarily.
        /// Returns <c>null</c> when <paramref name="secure"/> is <c>null</c>.
        /// </summary>
        private static string SecureStringToPlainText(System.Security.SecureString secure)
        {
            if (secure is null)
                return null;

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secure);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
