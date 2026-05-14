using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

        [Fact]
        public void SFTP_KeyboardInteractiveMethod_IsAlwaysRegistered()
        {
            var config = CreateTestConfig();
            using var session = new SftpSession(config);

            var authMethods = session.Client.ConnectionInfo.AuthenticationMethods;

            Assert.Contains(authMethods, m => m is KeyboardInteractiveAuthenticationMethod);
        }

        [Fact]
        public void SFTP_KeyboardInteractiveMethod_RespondsWithPasswordForNonEchoPrompt()
        {
            const string password = "s3cr3t"; // NOSONAR - dummy test credentials
            var config = CreateTestConfig(password: password);
            using var session = new SftpSession(config);

            var kbiMethod = session.Client.ConnectionInfo.AuthenticationMethods
                .OfType<KeyboardInteractiveAuthenticationMethod>()
                .Single();

            var prompt = new AuthenticationPrompt(0, false, "Password: ");
            RaiseAuthenticationPrompt(kbiMethod, new[] { prompt });

            Assert.Equal(password, prompt.Response);
        }

        [Fact]
        public void SFTP_KeyboardInteractiveMethod_LeavesEchoPromptEmpty()
        {
            var config = CreateTestConfig(password: "s3cr3t"); // NOSONAR - dummy test credentials
            using var session = new SftpSession(config);

            var kbiMethod = session.Client.ConnectionInfo.AuthenticationMethods
                .OfType<KeyboardInteractiveAuthenticationMethod>()
                .Single();

            var prompt = new AuthenticationPrompt(0, true, "Username: ");
            RaiseAuthenticationPrompt(kbiMethod, new[] { prompt });

            Assert.Null(prompt.Response);
        }

        [Fact]
        public void SFTP_KeyboardInteractiveMethod_RespondsWithEmptyStringWhenPasswordIsNull()
        {
            var config = new FtpConfiguration("localhost")
            {
                Username = "testUser",
                Password = null
            };
            using var session = new SftpSession(config);

            var kbiMethod = session.Client.ConnectionInfo.AuthenticationMethods
                .OfType<KeyboardInteractiveAuthenticationMethod>()
                .Single();

            var prompt = new AuthenticationPrompt(0, false, "Password: ");
            RaiseAuthenticationPrompt(kbiMethod, new[] { prompt });

            Assert.Equal(string.Empty, prompt.Response);
        }

        // --- SafeExists / SshException swallowing ---

        [Fact]
        public async Task SFTP_DirectoryExistsAsync_ReturnsFalse_WhenClientExistsThrowsBareSshException()
        {
            using var session = new SftpSessionWithFakeExists(
                CreateTestConfig(),
                () => throw new SshException("SSH_FX_FAILURE"));

            bool result = await ((IFtpSession)session).DirectoryExistsAsync("/some/path", CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task SFTP_FileExistsAsync_ReturnsFalse_WhenClientExistsThrowsBareSshException()
        {
            using var session = new SftpSessionWithFakeExists(
                CreateTestConfig(),
                () => throw new SshException("SSH_FX_FAILURE"));

            bool result = await ((IFtpSession)session).FileExistsAsync("/some/file.txt", CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task SFTP_DirectoryExistsAsync_Rethrows_WhenClientExistsThrowsSshConnectionException()
        {
            using var session = new SftpSessionWithFakeExists(
                CreateTestConfig(),
                () => throw new SshConnectionException("dropped"));

            await Assert.ThrowsAsync<SshConnectionException>(
                () => ((IFtpSession)session).DirectoryExistsAsync("/some/path", CancellationToken.None));
        }

        [Fact]
        public async Task SFTP_FileExistsAsync_Rethrows_WhenClientExistsThrowsSshConnectionException()
        {
            using var session = new SftpSessionWithFakeExists(
                CreateTestConfig(),
                () => throw new SshConnectionException("dropped"));

            await Assert.ThrowsAsync<SshConnectionException>(
                () => ((IFtpSession)session).FileExistsAsync("/some/file.txt", CancellationToken.None));
        }

        [Fact]
        public async Task SFTP_DirectoryExistsAsync_ReturnsFalse_WhenClientExistsReturnsFalse()
        {
            using var session = new SftpSessionWithFakeExists(CreateTestConfig(), () => false);

            bool result = await ((IFtpSession)session).DirectoryExistsAsync("/some/path", CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task SFTP_FileExistsAsync_ReturnsFalse_WhenClientExistsReturnsFalse()
        {
            using var session = new SftpSessionWithFakeExists(CreateTestConfig(), () => false);

            bool result = await ((IFtpSession)session).FileExistsAsync("/some/file.txt", CancellationToken.None);

            Assert.False(result);
        }

        // --- Helpers ---

        /// <summary>
        /// Subclass of <see cref="SftpSession"/> that overrides <see cref="SftpSession.ClientExists"/>
        /// so tests can inject arbitrary behaviour without needing to override the sealed
        /// <c>SftpClient.Exists</c> method.
        /// </summary>
        private sealed class SftpSessionWithFakeExists : SftpSession
        {
            private readonly Func<bool> _existsBehaviour;

            public SftpSessionWithFakeExists(FtpConfiguration config, Func<bool> existsBehaviour)
                : base(config)
            {
                _existsBehaviour = existsBehaviour;
            }

            protected internal override bool ClientExists(string path) => _existsBehaviour();
        }

        /// <summary>
        /// Fires the <see cref="KeyboardInteractiveAuthenticationMethod.AuthenticationPrompt"/> event
        /// by invoking its backing delegate directly. SSH.NET does not expose a raise method, so
        /// reflection is used here purely as a test seam.
        /// </summary>
        private static void RaiseAuthenticationPrompt(
            KeyboardInteractiveAuthenticationMethod method,
            IEnumerable<AuthenticationPrompt> prompts)
        {
            var eventArgs = new AuthenticationPromptEventArgs("testUser", string.Empty, string.Empty, prompts.ToList().AsReadOnly());

            var field = typeof(KeyboardInteractiveAuthenticationMethod)
                .GetField("AuthenticationPrompt", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? typeof(KeyboardInteractiveAuthenticationMethod)
                    .GetField("_authenticationPrompt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field); // SSH.NET renamed the backing field; update the lookup above.

            var handler = field.GetValue(method) as EventHandler<AuthenticationPromptEventArgs>;
            Assert.NotNull(handler); // SftpSession did not subscribe to AuthenticationPrompt.
            handler.Invoke(method, eventArgs);
        }

        private static FtpConfiguration CreateTestConfig(int? timeout = null, string password = "notUsed")
        {
            return new FtpConfiguration("localhost")
            {
                Username = "testUser",
                Password = password, // NOSONAR - dummy test credentials, never connect to any server
                Timeout = timeout
            };
        }
    }
}
