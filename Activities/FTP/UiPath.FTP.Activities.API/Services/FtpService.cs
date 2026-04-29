using System;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP;
using UiPath.FTP.Activities.API.Models;
using UiPath.Robot.Activities.Api;

namespace UiPath.FTP.Activities.API
{
    internal class FtpService : IFtpService
    {
        public FtpService(IExecutorRuntime executorRuntime)
        {
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
                Password = options.Password,
                UseAnonymousLogin = options.UseAnonymousLogin,
                SslProtocols = options.SslProtocols,
                ClientCertificatePath = options.ClientCertificatePath,
                ClientCertificatePassword = options.ClientCertificatePassword,
                AcceptAllCertificates = options.AcceptAllCertificates,
                Timeout = effectiveTimeoutMs,
                ProxyType = options.ProxyType,
                ProxyServer = options.ProxyServer,
                ProxyPort = options.ProxyPort,
                ProxyUsername = options.ProxyUsername,
                ProxyPassword = options.ProxyPassword,
            };

            IFtpSession session = options.UseSftp
                ? new SftpSession(config)
                : new FtpSession(config, options.FtpsMode);

            ct.ThrowIfCancellationRequested();
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
    }
}
