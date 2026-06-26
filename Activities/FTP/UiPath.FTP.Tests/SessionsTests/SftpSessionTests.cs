using Moq;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
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
        private const string TestUsername = "testUser"; // NOSONAR - dummy test credentials

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
                TestUsername,
                new PasswordAuthenticationMethod(TestUsername, "notUsed")); // NOSONAR - dummy test credentials
            using var defaultSftpClient = new SftpClient(defaultConnectionInfo);

            Assert.Equal(defaultConnectionInfo.Timeout, sftpClient.ConnectionInfo.Timeout);
            Assert.Equal(defaultSftpClient.OperationTimeout, sftpClient.OperationTimeout);
        }

        [Fact]
        public void SFTP_KeyboardInteractiveMethod_IsRegistered_WhenPasswordIsConfigured()
        {
            var config = CreateTestConfig(password: "s3cr3t"); // NOSONAR - dummy test credentials
            using var session = new SftpSession(config);

            Assert.Contains(
                session.Client.ConnectionInfo.AuthenticationMethods,
                m => m is KeyboardInteractiveAuthenticationMethod);
        }

        [Fact]
        public void SFTP_KeyboardInteractiveMethod_IsNotRegistered_WhenPasswordIsNullOrEmpty()
        {
            // With no password and no certificate, no auth methods are added and the
            // constructor throws. This confirms kbi is not registered as a fallback
            // when there is no password to respond with.
            var config = new FtpConfiguration("localhost") { Username = TestUsername, Password = null };

            Assert.Throws<ArgumentNullException>(() => new SftpSession(config));
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
        public async Task SFTP_DirectoryExistsAsync_ReturnsFalse_WhenClientExistsThrowsSftpPathNotFoundException()
        {
            using var session = new SftpSessionWithFakeExists(
                CreateTestConfig(),
                () => throw new SftpPathNotFoundException("No such file"));

            bool result = await ((IFtpSession)session).DirectoryExistsAsync("/some/path", CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task SFTP_FileExistsAsync_ReturnsFalse_WhenClientExistsThrowsSftpPathNotFoundException()
        {
            using var session = new SftpSessionWithFakeExists(
                CreateTestConfig(),
                () => throw new SftpPathNotFoundException("No such file"));

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

        // --- Recursive delete ---

        [Fact]
        public async Task SFTP_DeleteAsync_RemovesDirectoryContentsDepthFirst_ThenDirectory()
        {
            // /root
            //   /root/a.txt
            //   /root/sub
            //     /root/sub/b.txt
            var deleted = new List<string>();
            var root = FakeItem("/root", "root", isDirectory: true, deleted: deleted);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false, deleted: deleted);
            var sub = FakeItem("/root/sub", "sub", isDirectory: true, deleted: deleted);
            var bTxt = FakeItem("/root/sub/b.txt", "b.txt", isDirectory: false, deleted: deleted);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt, sub },
                    ["/root/sub"] = new[] { bTxt },
                });

            await ((IFtpSession)session).DeleteAsync("/root", CancellationToken.None);

            // Children are deleted before their parent; the target directory is removed last.
            Assert.Equal(new[] { "/root/a.txt", "/root/sub/b.txt", "/root/sub", "/root" }, deleted);
        }

        [Fact]
        public async Task SFTP_DeleteAsync_DeletesSingleFile_WithoutListing()
        {
            var deleted = new List<string>();
            var file = FakeItem("/root/file.txt", "file.txt", isDirectory: false, deleted: deleted);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root/file.txt"] = file },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>());

            await ((IFtpSession)session).DeleteAsync("/root/file.txt", CancellationToken.None);

            Assert.Equal(new[] { "/root/file.txt" }, deleted);
            Assert.Equal(0, session.ListDirectoryCallCount);
        }

        [Fact]
        public async Task SFTP_DeleteAsync_DoesNotRecurseIntoSymbolicLink()
        {
            // A symlink that points at a directory must be unlinked, not walked into.
            var deleted = new List<string>();
            var link = FakeItem("/link", "link", isDirectory: true, deleted: deleted, isSymbolicLink: true);
            var insideLink = FakeItem("/link/should-not-touch.txt", "should-not-touch.txt", isDirectory: false, deleted: deleted);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/link"] = link },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/link"] = new[] { insideLink },
                });

            await ((IFtpSession)session).DeleteAsync("/link", CancellationToken.None);

            Assert.Equal(new[] { "/link" }, deleted);
            Assert.Equal(0, session.ListDirectoryCallCount);
        }

        [Fact]
        public async Task SFTP_DeleteAsync_SkipsDotAndDotDotEntries()
        {
            var deleted = new List<string>();
            var root = FakeItem("/root", "root", isDirectory: true, deleted: deleted);
            var dot = FakeItem("/root/.", ".", isDirectory: true, deleted: deleted);
            var dotDot = FakeItem("/root/..", "..", isDirectory: true, deleted: deleted);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false, deleted: deleted);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { dot, dotDot, aTxt },
                });

            await ((IFtpSession)session).DeleteAsync("/root", CancellationToken.None);

            // "." and ".." are never deleted (deleting them would corrupt the tree / loop).
            Assert.Equal(new[] { "/root/a.txt", "/root" }, deleted);
        }

        [Fact]
        public async Task SFTP_DeleteAsync_ThrowsWhenCancelled()
        {
            var deleted = new List<string>();
            var root = FakeItem("/root", "root", isDirectory: true, deleted: deleted);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>());

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => ((IFtpSession)session).DeleteAsync("/root", cts.Token));
            Assert.Empty(deleted);
        }

        [Fact]
        public void SFTP_Delete_Sync_RemovesDirectoryContentsThenDirectory()
        {
            var deleted = new List<string>();
            var root = FakeItem("/root", "root", isDirectory: true, deleted: deleted);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false, deleted: deleted);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt },
                });

            ((IFtpSession)session).Delete("/root");

            Assert.Equal(new[] { "/root/a.txt", "/root" }, deleted);
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
        /// Subclass of <see cref="SftpSession"/> that overrides the <c>ClientGet</c>/<c>ClientListDirectory</c>
        /// seams so the recursive delete walk can be exercised against an in-memory tree without a live
        /// connection. Also counts <c>ClientListDirectory</c> calls so tests can assert that files and
        /// symbolic links are not enumerated.
        /// </summary>
        private sealed class SftpSessionWithFakeTree : SftpSession
        {
            private readonly IReadOnlyDictionary<string, ISftpFile> _itemsByPath;
            private readonly IReadOnlyDictionary<string, IEnumerable<ISftpFile>> _childrenByPath;

            public SftpSessionWithFakeTree(
                FtpConfiguration config,
                IReadOnlyDictionary<string, ISftpFile> itemsByPath,
                IReadOnlyDictionary<string, IEnumerable<ISftpFile>> childrenByPath)
                : base(config)
            {
                _itemsByPath = itemsByPath;
                _childrenByPath = childrenByPath;
            }

            public int ListDirectoryCallCount { get; private set; }

            protected internal override ISftpFile ClientGet(string path) => _itemsByPath[path];

            protected internal override IEnumerable<ISftpFile> ClientListDirectory(string path)
            {
                ListDirectoryCallCount++;
                return _childrenByPath.TryGetValue(path, out var children)
                    ? children
                    : Enumerable.Empty<ISftpFile>();
            }
        }

        /// <summary>
        /// Builds a mocked <see cref="ISftpFile"/> whose <c>Delete</c> records its full name into
        /// <paramref name="deleted"/>, so tests can assert both that an item was deleted and the order
        /// in which deletions happened.
        /// </summary>
        private static ISftpFile FakeItem(
            string fullName,
            string name,
            bool isDirectory,
            List<string> deleted,
            bool isSymbolicLink = false)
        {
            var mock = new Mock<ISftpFile>();
            mock.SetupGet(f => f.FullName).Returns(fullName);
            mock.SetupGet(f => f.Name).Returns(name);
            mock.SetupGet(f => f.IsDirectory).Returns(isDirectory);
            mock.SetupGet(f => f.IsSymbolicLink).Returns(isSymbolicLink);
            mock.Setup(f => f.Delete()).Callback(() => deleted.Add(fullName));
            return mock.Object;
        }

        /// <summary>
        /// Fires the <see cref="KeyboardInteractiveAuthenticationMethod.AuthenticationPrompt"/> event
        /// by invoking its backing delegate directly. SSH.NET does not expose a raise method, so
        /// reflection is used here purely as a test seam.
        /// The backing field is located by type rather than by name so that SSH.NET version upgrades
        /// that rename the field do not silently break the lookup.
        /// </summary>
        private static void RaiseAuthenticationPrompt(
            KeyboardInteractiveAuthenticationMethod method,
            IEnumerable<AuthenticationPrompt> prompts)
        {
            var eventArgs = new AuthenticationPromptEventArgs(TestUsername, string.Empty, string.Empty, prompts.ToList().AsReadOnly());

            var field = typeof(KeyboardInteractiveAuthenticationMethod)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType == typeof(EventHandler<AuthenticationPromptEventArgs>));
            Assert.NotNull(field); // SSH.NET no longer uses EventHandler<AuthenticationPromptEventArgs> for this event; update the test seam.

            var handler = field.GetValue(method) as EventHandler<AuthenticationPromptEventArgs>;
            Assert.NotNull(handler); // SftpSession did not subscribe to AuthenticationPrompt.
            handler.Invoke(method, eventArgs);
        }

        private static FtpConfiguration CreateTestConfig(int? timeout = null, string password = "notUsed")
        {
            return new FtpConfiguration("localhost")
            {
                Username = TestUsername,
                Password = password, // NOSONAR - dummy test credentials, never connect to any server
                Timeout = timeout
            };
        }
    }
}
