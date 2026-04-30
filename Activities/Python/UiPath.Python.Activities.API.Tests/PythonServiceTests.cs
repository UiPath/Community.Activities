using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.Python;
using UiPath.Python.Activities.API;
using UiPath.Python.Activities.API.Models;
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
        public async Task UsePythonScope_UnsupportedVersion_Throws()
        {
            var options = new PythonScopeOptions { Version = (UiPath.Python.Version)999 };

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => _pythonService.UsePythonScope(options));
            ex.Message.ShouldContain("not supported");
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

        // Skip: requires a real Python installation on the machine.
        // Run manually to verify end-to-end engine initialization.
        [Fact(Skip = "Requires a real Python installation")]
        public async Task UsePythonScope_ValidOptions_ReturnsHandle()
        {
            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto };

            using var handle = await _pythonService.UsePythonScope(options);

            handle.ShouldNotBeNull();
        }
    }

    public class PythonServiceCancellationTests
    {
        private static PythonService BuildService(IEngine engine)
        {
            return new PythonService((_, _, _, _, _, _) => engine);
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

            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto };
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
            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto };

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
            var options = new PythonScopeOptions { Version = UiPath.Python.Version.Auto };

            var ex = await Should.ThrowAsync<InvalidOperationException>(
                () => service.UsePythonScope(options));
            ex.ShouldBeSameAs(initEx);
            ex.Data["ReleaseException"].ShouldNotBeNull();
        }
    }
}
