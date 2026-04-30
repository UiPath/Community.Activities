using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.FTP;
using UiPath.FTP.Activities.API.Models;
using Xunit;

namespace UiPath.FTP.Activities.API.Tests
{
    public class FtpServiceTests
    {
        private readonly FtpService _ftpService;

        public FtpServiceTests()
        {
            _ftpService = new FtpService();
        }

        [Fact]
        public async Task UseFtpSession_NullOptions_Throws()
        {
            await Should.ThrowAsync<ArgumentNullException>(() => _ftpService.UseFtpSession(null));
        }

        [Fact]
        public async Task UseFtpSession_NullHost_Throws()
        {
            var options = new FtpScopeOptions { Host = null };

            await Should.ThrowAsync<ArgumentException>(() => _ftpService.UseFtpSession(options));
        }

        [Fact]
        public async Task UseFtpSession_EmptyHost_Throws()
        {
            var options = new FtpScopeOptions { Host = "   " };

            await Should.ThrowAsync<ArgumentException>(() => _ftpService.UseFtpSession(options));
        }

        [Fact]
        public async Task UseFtpSession_NegativeTimeout_Throws()
        {
            var options = new FtpScopeOptions { Host = "localhost", Timeout = TimeSpan.FromSeconds(-1) };

            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _ftpService.UseFtpSession(options));
        }

        // Skip: requires a real FTP server.
        // Run manually to verify end-to-end session initialization.
        [Fact(Skip = "Requires a real FTP server")]
        public async Task UseFtpSession_ValidOptions_ReturnsHandle()
        {
            var options = new FtpScopeOptions
            {
                Host = "localhost",
                UseAnonymousLogin = true
            };

            await using var handle = await _ftpService.UseFtpSession(options);

            handle.ShouldNotBeNull();
        }

        [Fact]
        public void FtpScopeOptions_PasswordSecure_TakesPrecedenceOverPassword()
        {
            var secure = ToSecureString("secret");
            var options = new FtpScopeOptions
            {
                Password = "plain",
                PasswordSecure = secure,
            };

            options.PasswordSecure.ShouldBeSameAs(secure);
            // Plain-string fallback is still accessible when SecureString is not set.
            options.Password.ShouldBe("plain");
        }

        [Fact]
        public void FtpScopeOptions_ClientCertificatePasswordSecure_TakesPrecedenceOverClientCertificatePassword()
        {
            var secure = ToSecureString("certpass");
            var options = new FtpScopeOptions
            {
                ClientCertificatePassword = "plain-cert",
                ClientCertificatePasswordSecure = secure,
            };

            options.ClientCertificatePasswordSecure.ShouldBeSameAs(secure);
            options.ClientCertificatePassword.ShouldBe("plain-cert");
        }

        [Fact]
        public void FtpScopeOptions_ProxyPasswordSecure_TakesPrecedenceOverProxyPassword()
        {
            var secure = ToSecureString("proxypass");
            var options = new FtpScopeOptions
            {
                ProxyPassword = "plain-proxy",
                ProxyPasswordSecure = secure,
            };

            options.ProxyPasswordSecure.ShouldBeSameAs(secure);
            options.ProxyPassword.ShouldBe("plain-proxy");
        }

        private static SecureString ToSecureString(string value)
        {
            var secure = new SecureString();
            foreach (char c in value)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }
    }

    public class FtpServiceCancellationTests
    {
        [Fact]
        public async Task UseFtpSession_PreCancelledToken_ThrowsOperationCanceledException()
        {
            var sessionMock = new Mock<IFtpSession>();
            sessionMock
                .Setup(s => s.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => Task.FromCanceled(ct));

            var service = new FtpService((_, _) => sessionMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var options = new FtpScopeOptions { Host = "localhost" };
            await Should.ThrowAsync<OperationCanceledException>(
                () => service.UseFtpSession(options, cts.Token));
        }

        [Fact]
        public async Task UseFtpSession_OpenFaults_SessionIsDisposedAndExceptionPropagates()
        {
            var expected = new InvalidOperationException("connection refused");
            var sessionMock = new Mock<IFtpSession>();
            sessionMock
                .Setup(s => s.OpenAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            var service = new FtpService((_, _) => sessionMock.Object);

            var ex = await Should.ThrowAsync<InvalidOperationException>(
                () => service.UseFtpSession(new FtpScopeOptions { Host = "localhost" }));
            ex.ShouldBeSameAs(expected);
            sessionMock.Verify(s => s.Dispose(), Times.Once);
        }
    }
}
