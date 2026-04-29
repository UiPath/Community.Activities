using System;
using System.Threading.Tasks;
using Shouldly;
using UiPath.Java.Activities.API.Models;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class JavaServiceTests
    {
        private readonly JavaService _javaService;

        public JavaServiceTests()
        {
            _javaService = new JavaService(executorRuntime: null);
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
}
