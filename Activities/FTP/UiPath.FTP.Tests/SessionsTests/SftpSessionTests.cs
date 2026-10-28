using Moq;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
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
            var link = FakeItem("/link", "link", isDirectory: false, deleted: deleted, isSymbolicLink: true);
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

        // --- Recursive listing (Enumerate Objects) ---

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_DoesNotRecurseIntoSymbolicLink()
        {
            // A link is reported with S_IFLNK, never S_IFDIR, so it never enters the recursion set --
            // which is what keeps a link cycle from recursing forever.
            var root = FakeItem("/root", "root", isDirectory: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);
            var link = FakeItem("/root/link", "link", isDirectory: false, isSymbolicLink: true);
            var hidden = FakeItem("/root/link/should-not-appear.txt", "should-not-appear.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt, link },
                    ["/root/link"] = new[] { hidden },
                });

            var result = await ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None);

            // The link itself is reported; what it points at is not enumerated.
            Assert.Equal(
                new[] { "/root/a.txt", "/root/link" },
                result.Select(o => o.FullName).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(1, session.ListDirectoryCallCount);
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_ReportsSymbolicLinkAsFile_KnownLimitation()
        {
            // Characterisation test, not an endorsement: FtpObjectType.Link is unreachable on the
            // SFTP path (see SFTP_GetFtpObjectType_ClassifiesSymbolicLinkAsFile), so List Files and
            // Folders' Link filter matches nothing and links arrive as File. Changing that alters
            // output for shipped workflows, so it is left alone and pinned here instead.
            var root = FakeItem("/root", "root", isDirectory: true);
            var link = FakeItem("/root/link", "link", isDirectory: false, isSymbolicLink: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);
            var sub = FakeItem("/root/sub", "sub", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { link, aTxt, sub },
                });

            var result = await ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: false, CancellationToken.None);

            Assert.Equal(
                new[]
                {
                    ("/root/a.txt", FtpObjectType.File),
                    ("/root/link", FtpObjectType.File),   // <-- would ideally be Link; see above
                    ("/root/sub", FtpObjectType.Directory),
                },
                result.Select(o => (o.FullName, o.Type)).OrderBy(t => t.FullName, StringComparer.Ordinal));
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_SkipsUnreadableSubdirectory_AndKeepsSiblings()
        {
            // The reported failure: a plain directory (not a link) that readdir returns but opendir
            // rejects. One such entry must not discard the rest of the enumeration.
            var root = FakeItem("/root", "root", isDirectory: true);
            var bad = FakeItem("/root/bad", "bad", isDirectory: true);
            var good = FakeItem("/root/good", "good", isDirectory: true);
            var bTxt = FakeItem("/root/good/b.txt", "b.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { bad, good },
                    ["/root/good"] = new[] { bTxt },
                },
                listFailures: Failures(("/root/bad", () => new SftpPathNotFoundException("No such file"))));

            var result = await ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None);

            // The unreadable directory is still reported; only its contents are missing. Its sibling
            // is walked normally.
            Assert.Equal(
                new[] { "/root/bad", "/root/good", "/root/good/b.txt" },
                result.Select(o => o.FullName).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_SkipsSubdirectoryOnPermissionDenied()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var locked = FakeItem("/root/locked", "locked", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { locked },
                },
                listFailures: Failures(("/root/locked", () => new SftpPermissionDeniedException("Permission denied"))));

            var result = await ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None);

            Assert.Equal(new[] { "/root/locked" }, result.Select(o => o.FullName));
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_PropagatesConnectionFailureFromSubdirectory()
        {
            // A dropped connection is not an inaccessible directory and must not be swallowed,
            // otherwise a half-finished listing would be returned as if it were complete.
            var root = FakeItem("/root", "root", isDirectory: true);
            var sub = FakeItem("/root/sub", "sub", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { sub },
                },
                listFailures: Failures(("/root/sub", () => new SshConnectionException("dropped"))));

            await Assert.ThrowsAsync<SshConnectionException>(
                () => ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None));
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_ThrowsWhenTheRequestedPathCannotBeListed()
        {
            // Leniency applies to sub-directories only -- the path the user asked for must still fail.
            var root = FakeItem("/root", "root", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>(),
                listFailures: Failures(("/root", () => new SftpPathNotFoundException("No such file"))));

            await Assert.ThrowsAsync<SftpPathNotFoundException>(
                () => ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None));
        }

        [Fact]
        public void SFTP_EnumerateObjects_Sync_SkipsUnreadableSubdirectory()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var bad = FakeItem("/root/bad", "bad", isDirectory: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt, bad },
                },
                listFailures: Failures(("/root/bad", () => new SftpPathNotFoundException("No such file"))));

            var result = ((IFtpSession)session).EnumerateObjects("/root", recursive: true);

            Assert.Equal(
                new[] { "/root/a.txt", "/root/bad" },
                result.Select(o => o.FullName).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_RecursesIntoRealDirectories()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var sub = FakeItem("/root/sub", "sub", isDirectory: true);
            var deep = FakeItem("/root/sub/deep", "deep", isDirectory: true);
            var bTxt = FakeItem("/root/sub/deep/b.txt", "b.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { sub },
                    ["/root/sub"] = new[] { deep },
                    ["/root/sub/deep"] = new[] { bTxt },
                });

            var result = await ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None);

            Assert.Equal(
                new[] { "/root/sub", "/root/sub/deep", "/root/sub/deep/b.txt" },
                result.Select(o => o.FullName).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_SkipsDotAndDotDotEntries()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var dot = FakeItem("/root/.", ".", isDirectory: true);
            var dotDot = FakeItem("/root/..", "..", isDirectory: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { dot, dotDot, aTxt },
                });

            var result = await ((IFtpSession)session).EnumerateObjectsAsync("/root", recursive: true, CancellationToken.None);

            // Walking "." or ".." would loop; they are also not reported as results.
            Assert.Equal(new[] { "/root/a.txt" }, result.Select(o => o.FullName));
            Assert.Equal(1, session.ListDirectoryCallCount);
        }

        [Fact]
        public async Task SFTP_EnumerateObjectsAsync_PassesRemotePathToClientVerbatim()
        {
            // Canonicalising a relative path is SSH.NET's job (GetCanonicalPath prepends the working
            // directory); the walk must not pre-resolve it or mutate the shared session's directory.
            var root = FakeItem("/home/user", "user", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["."] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>());

            await ((IFtpSession)session).EnumerateObjectsAsync(".", recursive: false, CancellationToken.None);

            Assert.Equal(new[] { "." }, session.GetCalls);
        }

        [Fact]
        public void SFTP_EnumerateObjects_Sync_DoesNotRecurseIntoSymbolicLink()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var link = FakeItem("/root/link", "link", isDirectory: false, isSymbolicLink: true);
            var hidden = FakeItem("/root/link/should-not-appear.txt", "should-not-appear.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { link },
                    ["/root/link"] = new[] { hidden },
                });

            var result = ((IFtpSession)session).EnumerateObjects("/root", recursive: true);

            Assert.Equal(new[] { "/root/link" }, result.Select(o => o.FullName));
            Assert.Equal(1, session.ListDirectoryCallCount);
        }

        // --- Object-type classification ---
        //
        // These pin CURRENT behaviour, including a known wart, so that changing it has to be a
        // deliberate act rather than a tidy-up. See Extensions.GetFtpObjectType for the full story.

        [Fact]
        public void SFTP_GetFtpObjectType_ClassifiesSymbolicLinkAsFile()
        {
            // Not the intuitive answer, and not an accident. SSH.NET's IsRegularFile is a
            // (mode & S_IFREG) == S_IFREG test and S_IFLNK contains S_IFREG, so every link is also a
            // "regular file" and wins the first check. Reordering so links come first looks like an
            // obvious cleanup but would make Download throw NotImplementedException on a symlinked
            // file and File Exists answer false for one -- both work today.
            Assert.Equal(FtpObjectType.File, FakeItem("/l", "l", isDirectory: false, isSymbolicLink: true).GetFtpObjectType());
            Assert.Equal(FtpObjectType.File, FakeItem("/f", "f", isDirectory: false).GetFtpObjectType());
            Assert.Equal(FtpObjectType.Directory, FakeItem("/d", "d", isDirectory: true).GetFtpObjectType());
        }

        // --- Recursive listing (Download Files) ---
        //
        // The download walk is a separate pair of overloads returning (localPath, remotePath) pairs.
        // It shares the lenient descent with the enumeration walk, so it needs the same coverage. It
        // is asserted directly rather than through IFtpSession.Download, which writes to the local
        // file system and pulls bytes through the real SftpClient.
        //
        // Note the asymmetry with enumeration: only IsRegularFile entries become pairs, so a
        // directory that is unreadable contributes nothing at all to the result.

        [Fact]
        public async Task SFTP_DownloadListingAsync_TransfersSymbolicLinkAsAFile_WithoutWalkingIt()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);
            var link = FakeItem("/root/link", "link", isDirectory: false, isSymbolicLink: true);
            var hidden = FakeItem("/root/link/should-not-appear.txt", "should-not-appear.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt, link },
                    ["/root/link"] = new[] { hidden },
                });

            var listing = await session.GetRemoteListingAsync("/root", @"C:\local", recursive: true, CancellationToken.None);

            // SSH.NET reports IsRegularFile == true for a symbolic link, so the link becomes a
            // transfer entry of its own; opening it server-side follows it. It is never walked as a
            // directory, so nothing underneath it is picked up.
            Assert.Equal(new[] { "/root/a.txt", "/root/link" }, listing.Select(pair => pair.Item2));
            Assert.Equal(1, session.ListDirectoryCallCount);
        }

        [Fact]
        public async Task SFTP_DownloadListingAsync_SkipsUnreadableSubdirectory_AndKeepsSiblings()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var bad = FakeItem("/root/bad", "bad", isDirectory: true);
            var good = FakeItem("/root/good", "good", isDirectory: true);
            var bTxt = FakeItem("/root/good/b.txt", "b.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { bad, good },
                    ["/root/good"] = new[] { bTxt },
                },
                listFailures: Failures(("/root/bad", () => new SftpPathNotFoundException("No such file"))));

            var listing = await session.GetRemoteListingAsync("/root", @"C:\local", recursive: true, CancellationToken.None);

            // One unreadable directory must not cost the caller the rest of the download.
            Assert.Equal(new[] { "/root/good/b.txt" }, listing.Select(pair => pair.Item2));
        }

        [Fact]
        public async Task SFTP_DownloadListingAsync_PropagatesConnectionFailureFromSubdirectory()
        {
            // A dropped connection must not be reported as a completed download of a subset.
            var root = FakeItem("/root", "root", isDirectory: true);
            var sub = FakeItem("/root/sub", "sub", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { sub },
                },
                listFailures: Failures(("/root/sub", () => new SshConnectionException("dropped"))));

            await Assert.ThrowsAsync<SshConnectionException>(
                () => session.GetRemoteListingAsync("/root", @"C:\local", recursive: true, CancellationToken.None));
        }

        [Fact]
        public async Task SFTP_DownloadListingAsync_MapsNestedFilesToNestedLocalPaths()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);
            var sub = FakeItem("/root/sub", "sub", isDirectory: true);
            var bTxt = FakeItem("/root/sub/b.txt", "b.txt", isDirectory: false);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt, sub },
                    ["/root/sub"] = new[] { bTxt },
                });

            var listing = await session.GetRemoteListingAsync("/root", "local", recursive: true, CancellationToken.None);

            // The remote tree shape is reproduced under the local path, rooted at the directory name.
            Assert.Equal(
                new[]
                {
                    (Path.Combine("local", "root", "a.txt"), "/root/a.txt"),
                    (Path.Combine("local", "root", "sub", "b.txt"), "/root/sub/b.txt"),
                },
                listing.Select(pair => (pair.Item1, pair.Item2)));
        }

        [Fact]
        public void SFTP_DownloadListing_Sync_TransfersSymbolicLinkAndSkipsUnreadableSubdirectory()
        {
            var root = FakeItem("/root", "root", isDirectory: true);
            var aTxt = FakeItem("/root/a.txt", "a.txt", isDirectory: false);
            var link = FakeItem("/root/link", "link", isDirectory: false, isSymbolicLink: true);
            var bad = FakeItem("/root/bad", "bad", isDirectory: true);

            using var session = new SftpSessionWithFakeTree(
                CreateTestConfig(),
                itemsByPath: new Dictionary<string, ISftpFile> { ["/root"] = root },
                childrenByPath: new Dictionary<string, IEnumerable<ISftpFile>>
                {
                    ["/root"] = new[] { aTxt, link, bad },
                    ["/root/link"] = new[] { FakeItem("/root/link/hidden.txt", "hidden.txt", isDirectory: false) },
                },
                listFailures: Failures(("/root/bad", () => new SftpPathNotFoundException("No such file"))));

            var listing = session.GetRemoteListing("/root", @"C:\local", recursive: true);

            // The link transfers as a file (see the async twin); the unreadable directory contributes
            // nothing but does not fail the walk.
            Assert.Equal(new[] { "/root/a.txt", "/root/link" }, listing.Select(pair => pair.Item2));
        }

        // --- Helpers ---

        /// <summary>
        /// Builds the <c>listFailures</c> map for <see cref="SftpSessionWithFakeTree"/>: paths whose
        /// listing throws, modelling a server that returns an entry from <c>readdir</c> and then
        /// refuses <c>opendir</c> on it.
        /// </summary>
        private static IReadOnlyDictionary<string, Func<Exception>> Failures(
            params (string Path, Func<Exception> Throws)[] failures)
        {
            return failures.ToDictionary(f => f.Path, f => f.Throws);
        }

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
            private readonly IReadOnlyDictionary<string, Func<Exception>> _listFailures;

            public SftpSessionWithFakeTree(
                FtpConfiguration config,
                IReadOnlyDictionary<string, ISftpFile> itemsByPath,
                IReadOnlyDictionary<string, IEnumerable<ISftpFile>> childrenByPath,
                IReadOnlyDictionary<string, Func<Exception>> listFailures = null)
                : base(config)
            {
                _itemsByPath = itemsByPath;
                _childrenByPath = childrenByPath;
                _listFailures = listFailures ?? new Dictionary<string, Func<Exception>>();
            }

            public int ListDirectoryCallCount { get; private set; }

            /// <summary>Paths passed to <c>ClientGet</c>, in call order.</summary>
            public List<string> GetCalls { get; } = new List<string>();

            protected internal override ISftpFile ClientGet(string path)
            {
                GetCalls.Add(path);
                return _itemsByPath[path];
            }

            protected internal override IEnumerable<ISftpFile> ClientListDirectory(string path)
            {
                ListDirectoryCallCount++;

                // Models a path the server lists in its parent but then refuses to open.
                if (_listFailures.TryGetValue(path, out var failure))
                {
                    throw failure();
                }

                return _childrenByPath.TryGetValue(path, out var children)
                    ? children
                    : Enumerable.Empty<ISftpFile>();
            }

            protected internal override Task<IEnumerable<ISftpFile>> ClientListDirectoryAsync(string path)
                => Task.FromResult(ClientListDirectory(path));
        }

        /// <summary>
        /// Builds a mocked <see cref="ISftpFile"/> whose <c>Delete</c> records its full name into
        /// <paramref name="deleted"/>, so tests can assert both that an item was deleted and the order
        /// in which deletions happened.
        /// <para>
        /// The three type flags are derived from one kind rather than set independently, so that a
        /// fake cannot describe an entry SSH.NET could never produce. They are wired to match SSH.NET
        /// 2024.1.0 exactly, verified against real <c>SftpFileAttributes</c> built from POSIX mode
        /// words:
        /// </para>
        /// <list type="table">
        /// <item><description>directory <c>S_IFDIR|0755</c> -> IsDirectory, not link, <b>not</b> regular file</description></item>
        /// <item><description>symlink <c>S_IFLNK|0777</c> -> <b>not</b> directory, IsSymbolicLink, <b>and IsRegularFile</b></description></item>
        /// <item><description>regular <c>S_IFREG|0644</c> -> not directory, not link, IsRegularFile</description></item>
        /// </list>
        /// <para>
        /// The surprise is the middle row: SSH.NET tests <c>(mode &amp; S_IFREG) == S_IFREG</c>, and
        /// <c>S_IFLNK</c> (<c>0xA000</c>) contains <c>S_IFREG</c> (<c>0x8000</c>), so every link is
        /// also a "regular file". An earlier version of this helper set
        /// <c>IsRegularFile = !isDirectory &amp;&amp; !isSymbolicLink</c>, the opposite, and allowed
        /// <c>isDirectory: true, isSymbolicLink: true</c> -- a combination no conformant server can
        /// send, since <c>S_IFDIR</c> and <c>S_IFLNK</c> are distinct values of the same type field.
        /// Tests written against that fake asserted behaviour the product never exhibits.
        /// </para>
        /// </summary>
        private static ISftpFile FakeItem(
            string fullName,
            string name,
            bool isDirectory,
            List<string> deleted = null,
            bool isSymbolicLink = false)
        {
            if (isDirectory && isSymbolicLink)
            {
                throw new ArgumentException(
                    "A directory and a symbolic link are distinct values of the mode word's type field; " +
                    "no SFTP server can report an entry as both. Pass isSymbolicLink on its own -- " +
                    "a link to a directory is still just a link on the wire.");
            }

            var mock = new Mock<ISftpFile>();
            mock.SetupGet(f => f.FullName).Returns(fullName);
            mock.SetupGet(f => f.Name).Returns(name);
            mock.SetupGet(f => f.IsDirectory).Returns(isDirectory);
            mock.SetupGet(f => f.IsSymbolicLink).Returns(isSymbolicLink);
            mock.SetupGet(f => f.IsRegularFile).Returns(!isDirectory);

            if (deleted != null)
            {
                mock.Setup(f => f.Delete()).Callback(() => deleted.Add(fullName));
            }

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
