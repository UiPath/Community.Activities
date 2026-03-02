using System;
using FluentFTP;
using System.Reflection;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class FtpSessionTests
    {
        [Fact]
        public void FTP_TestConnect()
        {
            //Maybe to initialize a in-memory FTP Server using FubarDev.FtpServer??
            IFtpSession session = new FtpSession(new FtpConfiguration("ToThrow"), default);
            Assert.ThrowsAny<Exception>(session.Open);
        }

        [Fact]
        public void FTP_TimeoutIsApplied()
        {
            var config = new FtpConfiguration("localhost") { Timeout = 5000 };
            var session = new FtpSession(config, FtpsMode.None);

            var ftpClientField = typeof(FtpSession).GetField("_ftpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ftpClientField);
            var ftpClient = (FtpClient)ftpClientField.GetValue(session);

            Assert.Equal(5000, ftpClient.Config.ConnectTimeout);
            Assert.Equal(5000, ftpClient.Config.ReadTimeout);
            Assert.Equal(5000, ftpClient.Config.DataConnectionConnectTimeout);
            Assert.Equal(5000, ftpClient.Config.DataConnectionReadTimeout);
        }

        [Fact]
        public void FTP_DefaultTimeoutWhenNotSet()
        {
            var config = new FtpConfiguration("localhost");
            var session = new FtpSession(config, FtpsMode.None);

            var ftpClientField = typeof(FtpSession).GetField("_ftpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ftpClientField);
            var ftpClient = (FtpClient)ftpClientField.GetValue(session);

            // When no timeout is set, FluentFTP defaults should be preserved
            var defaultClient = new FtpClient();
            Assert.Equal(defaultClient.Config.ConnectTimeout, ftpClient.Config.ConnectTimeout);
            Assert.Equal(defaultClient.Config.ReadTimeout, ftpClient.Config.ReadTimeout);
            Assert.Equal(defaultClient.Config.DataConnectionConnectTimeout, ftpClient.Config.DataConnectionConnectTimeout);
            Assert.Equal(defaultClient.Config.DataConnectionReadTimeout, ftpClient.Config.DataConnectionReadTimeout);
        }
    }
}
