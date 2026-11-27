using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.Python;
using UiPath.Python.Activities.API;
using UiPath.Python.Activities.API.Models;
using UiPath.Python.Tests;
using Xunit;

namespace UiPath.Python.Activities.API.Tests
{
    public class PythonServiceTests
    {
        private readonly PythonService _pythonService;

        public PythonServiceTests()
        {
            _pythonService = new PythonService();
        }

        [Fact]
        public async Task UsePythonScope_NullOptions_Throws()
        {
            await Should.ThrowAsync<ArgumentNullException>(() => _pythonService.UsePythonScope(null));
        }

        [Fact]
        public async Task UsePythonScope_NonExistentPath_Throws()
        {
            var options = new PythonScopeOptions { Path = @"C:\this\path\does\not\exist" };

            await Should.ThrowAsync<System.IO.DirectoryNotFoundException>(() => _pythonService.UsePythonScope(options));
        }

        [Fact]
        public async Task UsePythonScope_NonExistentWorkingFolder_Throws()
        {
            var options = new PythonScopeOptions { WorkingFolder = @"C:\this\working\folder\does\not\exist" };

            await Should.ThrowAsync<System.IO.DirectoryNotFoundException>(() => _pythonService.UsePythonScope(options));
        }

        [Fact]
        public async Task UsePythonScope_NegativeOperationTimeout_Throws()
        {
            var options = new PythonScopeOptions { OperationTimeout = TimeSpan.FromSeconds(-1) };

            await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _pythonService.UsePythonScope(options));
        }

        private static readonly string RuntimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath();
        private static readonly string LibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(RuntimePath);

        [Fact]
        public async Task UsePythonScope_ValidOptions_ReturnsHandle()
        {
            var options = new PythonScopeOptions
            {
                Path = RuntimePath,
                LibraryPath = LibraryPath
            };

            using var handle = await _pythonService.UsePythonScope(options);

            handle.ShouldNotBeNull();
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task UsePythonScope_RelativePaths_StartsHostWithResolvedPaths(bool relativeHome, bool relativeLibrary)
        {
            var projectFolder = Directory.GetParent(RuntimePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))!.FullName;
            var homePath = relativeHome ? Path.GetRelativePath(projectFolder, RuntimePath) : RuntimePath;
            var libraryPath = relativeLibrary ? Path.GetRelativePath(projectFolder, LibraryPath) : LibraryPath;
            Path.IsPathRooted(homePath).ShouldBe(!relativeHome);
            Path.IsPathRooted(libraryPath).ShouldBe(!relativeLibrary);

            var originalDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = projectFolder;

                // real embedded runtime: initialization fails if the host receives an unresolved relative path
                using var handle = await _pythonService.UsePythonScope(new PythonScopeOptions { Path = homePath, LibraryPath = libraryPath });

                handle.ShouldNotBeNull();
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
            }
        }

        [Fact]
        public async Task UsePythonScope_MissingRelativeLibraryPath_ThrowsFileNotFoundWithOriginalValue()
        {
            var missingLibrary = Path.Combine("does-not-exist", Path.GetFileName(LibraryPath));

            var ex = await Should.ThrowAsync<FileNotFoundException>(
                () => _pythonService.UsePythonScope(new PythonScopeOptions { Path = RuntimePath, LibraryPath = missingLibrary }));

            ex.Message.ShouldContain(missingLibrary);
        }
    }

    public class PythonServiceCancellationTests
    {
        private static readonly string _libraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath());

        private static PythonService BuildService(IEngine engine)
        {
            return new PythonService((_, _, _, _, _, _, _, _) => engine);
        }

        [Fact]
        public async Task UsePythonScope_RelativePaths_PassesAbsolutePathsToEngine()
        {
            var runtimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath();
            var projectFolder = Directory.GetParent(runtimePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))!.FullName;
            var relativeHome = Path.GetRelativePath(projectFolder, runtimePath);
            var relativeLibrary = Path.GetRelativePath(projectFolder, _libraryPath);

            string receivedPath = null, receivedLibrary = null;
            var engineMock = new Mock<IEngine>();
            engineMock
                .Setup(e => e.Initialize(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);
            var service = new PythonService((_, path, library, _, _, _, _, _) =>
            {
                receivedPath = path;
                receivedLibrary = library;
                return engineMock.Object;
            });

            var originalDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = projectFolder;
                using var handle = await service.UsePythonScope(new PythonScopeOptions { Path = relativeHome, LibraryPath = relativeLibrary });
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
            }

            receivedPath.ShouldBe(Path.GetFullPath(runtimePath));
            receivedLibrary.ShouldBe(Path.GetFullPath(_libraryPath));
        }

        [Fact]
        public async Task UsePythonScope_PreCancelledToken_ThrowsOperationCanceledException()
        {
            var engineMock = new Mock<IEngine>();
            engineMock
                .Setup(e => e.Initialize(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
                .Returns((string _, CancellationToken ct, double _) =>
                    Task.FromCanceled(ct));

            var service = BuildService(engineMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto, LibraryPath = _libraryPath };
            await Should.ThrowAsync<OperationCanceledException>(
                () => service.UsePythonScope(options, cts.Token));
        }

        [Fact]
        public async Task UsePythonScope_InitializeFaults_ThrowsOriginalException()
        {
            var expected = new InvalidOperationException("boom");
            var engineMock = new Mock<IEngine>();
            engineMock
                .Setup(e => e.Initialize(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
                .ThrowsAsync(expected);
            engineMock
                .Setup(e => e.Release())
                .Returns(Task.CompletedTask);

            var service = BuildService(engineMock.Object);
            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto, LibraryPath = _libraryPath };

            var ex = await Should.ThrowAsync<InvalidOperationException>(
                () => service.UsePythonScope(options));
            ex.ShouldBeSameAs(expected);
        }

        [Fact]
        public async Task UsePythonScope_InitializeFaultsAndReleaseFaults_ThrowsOriginalExceptionWithReleaseContext()
        {
            var initEx = new InvalidOperationException("init failed");
            var releaseEx = new InvalidOperationException("release failed");
            var engineMock = new Mock<IEngine>();
            engineMock
                .Setup(e => e.Initialize(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
                .ThrowsAsync(initEx);
            engineMock
                .Setup(e => e.Release())
                .ThrowsAsync(releaseEx);

            var service = BuildService(engineMock.Object);
            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto, LibraryPath = _libraryPath };

            var ex = await Should.ThrowAsync<InvalidOperationException>(
                () => service.UsePythonScope(options));
            ex.ShouldBeSameAs(initEx);
            ex.Data["ReleaseException"].ShouldNotBeNull();
        }
    }
}
