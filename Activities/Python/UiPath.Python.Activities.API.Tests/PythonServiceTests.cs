using System;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.Python.Activities.API.Models;
using UiPath.Robot.Activities.Api;
using Xunit;

namespace UiPath.Python.Activities.API.Tests
{
    public class PythonServiceTests
    {
        private readonly PythonService _pythonService;

        public PythonServiceTests()
        {
            var executorRuntime = new Mock<IExecutorRuntime>();
            _pythonService = new PythonService(executorRuntime.Object);
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
}
