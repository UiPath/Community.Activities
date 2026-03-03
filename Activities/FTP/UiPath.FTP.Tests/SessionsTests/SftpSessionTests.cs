using Renci.SshNet;
using System;
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
            var config = CreateTestConfig(timeout: 5000);
            using var session = new SftpSession(config);

            var sftpClient = session.Client;

            Assert.Equal(TimeSpan.FromMilliseconds(5000), sftpClient.ConnectionInfo.Timeout);
            Assert.Equal(TimeSpan.FromMilliseconds(5000), sftpClient.OperationTimeout);
        }

        [Fact]
        public void SFTP_DefaultTimeoutWhenNotSet()
        {
            var config = CreateTestConfig();
            using var session = new SftpSession(config);

            var sftpClient = session.Client;

            // When no timeout is set, SSH.NET defaults should be preserved
            var defaultConnectionInfo = new ConnectionInfo(
                "localhost",
                "testUser",
                new PasswordAuthenticationMethod("testUser", "notUsed")); // NOSONAR - dummy test credentials
            using var defaultSftpClient = new SftpClient(defaultConnectionInfo);

            Assert.Equal(defaultConnectionInfo.Timeout, sftpClient.ConnectionInfo.Timeout);
            Assert.Equal(defaultSftpClient.OperationTimeout, sftpClient.OperationTimeout);
        }

        private static FtpConfiguration CreateTestConfig(int? timeout = null)
        {
            return new FtpConfiguration("localhost")
            {
                Username = "testUser",
                Password = "notUsed", // NOSONAR - dummy test credentials, never connect to any server
                Timeout = timeout
            };
        }
    }
}
