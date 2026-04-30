using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.Java;
using UiPath.Java.Activities.API.Models;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class JavaServiceTests
    {
        private readonly JavaService _javaService;

        public JavaServiceTests()
        {
            _javaService = new JavaService();
        }

        [Fact]
        public async Task UseJavaScope_NullOptions_Throws()
        {
            await Should.ThrowAsync<ArgumentNullException>(() => _javaService.UseJavaScope(null));
        }

        [Fact]
        public async Task UseJavaScope_NegativeTimeout_Throws()
        {
            var options = new JavaScopeOptions { Timeout = TimeSpan.FromSeconds(-1) };

            await Should.ThrowAsync<ArgumentException>(() => _javaService.UseJavaScope(options));
        }

        [Fact]
        public async Task UseJavaScope_NonExistentJavaPath_Throws()
        {
            var options = new JavaScopeOptions { JavaPath = @"C:\this\path\does\not\exist" };

            await Should.ThrowAsync<ArgumentException>(() => _javaService.UseJavaScope(options));
        }

        // Skip: requires a real Java installation on the machine.
        // Run manually to verify end-to-end engine initialization.
        [Fact(Skip = "Requires a real Java installation")]
        public async Task UseJavaScope_ValidOptions_ReturnsHandle()
        {
            var options = new JavaScopeOptions();

            await using var handle = await _javaService.UseJavaScope(options);

            handle.ShouldNotBeNull();
        }
    }

    public class JavaServiceCancellationTests
    {
        [Fact]
        public async Task UseJavaScope_PreCancelledToken_ThrowsOperationCanceledException()
        {
            var invokerMock = new Mock<IInvoker>();
            invokerMock
                .Setup(i => i.StartJavaService(It.IsAny<int>()))
                .Returns((int _) => Task.Delay(Timeout.Infinite, new CancellationToken(canceled: true)));

            var service = new JavaService(_ => invokerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(
                () => service.UseJavaScope(new JavaScopeOptions(), cts.Token));
        }

        [Fact]
        public async Task UseJavaScope_StartFaults_ThrowsOriginalException()
        {
            var expected = new InvalidOperationException("boom");
            var invokerMock = new Mock<IInvoker>();
            invokerMock
                .Setup(i => i.StartJavaService(It.IsAny<int>()))
                .ThrowsAsync(expected);
            invokerMock
                .Setup(i => i.ReleaseAsync())
                .Returns(Task.CompletedTask);

            var service = new JavaService(_ => invokerMock.Object);

            var ex = await Should.ThrowAsync<InvalidOperationException>(
                () => service.UseJavaScope(new JavaScopeOptions()));
            ex.ShouldBeSameAs(expected);
        }

        [Fact]
        public async Task UseJavaScope_StartFaultsAndReleaseFaults_ThrowsOriginalExceptionWithReleaseContext()
        {
            var startEx = new InvalidOperationException("start failed");
            var releaseEx = new InvalidOperationException("release failed");
            var invokerMock = new Mock<IInvoker>();
            invokerMock
                .Setup(i => i.StartJavaService(It.IsAny<int>()))
                .ThrowsAsync(startEx);
            invokerMock
                .Setup(i => i.ReleaseAsync())
                .ThrowsAsync(releaseEx);

            var service = new JavaService(_ => invokerMock.Object);

            var ex = await Should.ThrowAsync<InvalidOperationException>(
                () => service.UseJavaScope(new JavaScopeOptions()));
            ex.ShouldBeSameAs(startEx);
            ex.Data["ReleaseException"].ShouldNotBeNull();
        }
    }
}
