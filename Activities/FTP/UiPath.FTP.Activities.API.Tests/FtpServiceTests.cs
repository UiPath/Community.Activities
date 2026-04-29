using System;
using System.Threading.Tasks;
using Shouldly;
using UiPath.FTP.Activities.API.Models;
using Xunit;

namespace UiPath.FTP.Activities.API.Tests
{
    public class FtpServiceTests
    {
        private readonly FtpService _ftpService;

        public FtpServiceTests()
        {
            _ftpService = new FtpService(executorRuntime: null);
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
    }
}
