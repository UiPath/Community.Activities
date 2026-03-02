using Renci.SshNet;
using System;
using System.Reflection;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class SftpSessionTests
    {
        [Fact]
        public void SFTP_TestConnect()
        {
            IFtpSession session = new SftpSession(new FtpConfiguration("ToThrow") {Username = "Some", Password = "NotUsed", Host = "Some"});
            Assert.ThrowsAny<Exception>(session.Open);
        }

        [Fact]
        public void SFTP_TimeoutIsApplied()
        {
            var config = new FtpConfiguration("localhost")
            {
                Username = "user",
                Password = "pass",
                Timeout = 5000
            };
            var session = new SftpSession(config);

            var sftpClientField = typeof(SftpSession).GetField("_sftpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sftpClientField);
            var sftpClient = (SftpClient)sftpClientField.GetValue(session);

            Assert.Equal(TimeSpan.FromMilliseconds(5000), sftpClient.ConnectionInfo.Timeout);
            Assert.Equal(TimeSpan.FromMilliseconds(5000), sftpClient.OperationTimeout);
        }

        [Fact]
        public void SFTP_DefaultTimeoutWhenNotSet()
        {
            var config = new FtpConfiguration("localhost")
            {
                Username = "user",
                Password = "pass"
            };
            var session = new SftpSession(config);

            var sftpClientField = typeof(SftpSession).GetField("_sftpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sftpClientField);
            var sftpClient = (SftpClient)sftpClientField.GetValue(session);

            // When no timeout is set, SSH.NET default should be preserved (30 seconds)
            Assert.Equal(TimeSpan.FromSeconds(30), sftpClient.ConnectionInfo.Timeout);
        }
    }
}
