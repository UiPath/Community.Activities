using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using UiPath.Python.Activities.API;
using UiPath.Python.Activities.API.Models;
using UiPath.Python.Tests;
using Xunit;

namespace UiPath.Python.Activities.API.Tests
{
    /// <summary>
    /// Exercises the coded workflow API (<see cref="PythonService"/> and the <see cref="PythonOperations"/>
    /// extensions) against the real embedded Python runtimes, each in its own isolated host process, as
    /// opposed to the mock-based tests next to it.
    /// </summary>
    public class PythonServiceRuntimeTests : IDisposable
    {
        private const string Script = @"
import sys

def version():
    return f'{sys.version_info.major}.{sys.version_info.minor}'

def add(a, b):
    return a + b

def greet(name):
    return 'Hello ' + name

def fail():
    raise ValueError('boom from python')
";

        private readonly string _tempDir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "python-api-runtime-tests", Guid.NewGuid().ToString("N"))).FullName;

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { /* best effort cleanup */ }
            GC.SuppressFinalize(this);
        }

        private static Task<IPythonScopeHandle> UseScope(string embeddedVersion, Action<PythonScopeOptions> configure = null)
        {
            var runtimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath(embeddedVersion);
            var options = new PythonScopeOptions
            {
                Path = runtimePath,
                LibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(runtimePath)
            };
            configure?.Invoke(options);

            return new PythonService().UsePythonScope(options);
        }

        [Theory]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314, "3.14")]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315, "3.15")]
        public async Task LoadCode_InvokeMethod_GetObject_RunsOnTheSelectedRuntime(string embeddedVersion, string expectedVersion)
        {
            using var scope = await UseScope(embeddedVersion);

            var script = await scope.LoadCode(Script);
            var result = await scope.InvokeMethod(script, "version");

            scope.GetObject<string>(result).ShouldBe(expectedVersion);
        }

        [Theory]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public async Task InvokeMethod_PassesArgumentsAndConvertsResult(string embeddedVersion)
        {
            using var scope = await UseScope(embeddedVersion);

            var script = await scope.LoadCode(Script);

            scope.GetObject<long>(await scope.InvokeMethod(script, "add", new object[] { 2, 40 })).ShouldBe(42);
            scope.GetObject<string>(await scope.InvokeMethod(script, "greet", new object[] { "UiPath" })).ShouldBe("Hello UiPath");
        }

        [Theory]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public async Task LoadScript_FromFile_InvokeMethod(string embeddedVersion)
        {
            var scriptFile = Path.Combine(_tempDir, "script.py");
            File.WriteAllText(scriptFile, Script);
            using var scope = await UseScope(embeddedVersion);

            var script = await scope.LoadScript(scriptFile);
            var result = await scope.InvokeMethod(script, "add", new object[] { 20, 22 });

            scope.GetObject<long>(result).ShouldBe(42);
        }

        [Theory]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public async Task RunCode_And_RunScript_ExecuteInTheHost(string embeddedVersion)
        {
            var markerFile = Path.Combine(_tempDir, "marker.txt").Replace("\\", "/");
            var scriptFile = Path.Combine(_tempDir, "run.py");
            File.WriteAllText(scriptFile, $"open('{markerFile}', 'a').write('script\\n')");
            using var scope = await UseScope(embeddedVersion);

            await scope.RunCode($"open('{markerFile}', 'a').write('code\\n')");
            await scope.RunScript(scriptFile);

            File.ReadAllLines(markerFile).ShouldBe(new[] { "code", "script" });
        }

        [Theory]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public async Task WorkingFolder_IsTheCurrentDirectoryOfTheScript(string embeddedVersion)
        {
            using var scope = await UseScope(embeddedVersion, o => o.WorkingFolder = _tempDir);

            var script = await scope.LoadCode("import os\n\ndef cwd():\n    return os.getcwd()\n");
            var cwd = scope.GetObject<string>(await scope.InvokeMethod(script, "cwd"));

            Path.GetFullPath(cwd).TrimEnd(Path.DirectorySeparatorChar)
                .ShouldBe(Path.GetFullPath(_tempDir).TrimEnd(Path.DirectorySeparatorChar), StringCompareShould.IgnoreCase);
        }

        [Theory]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public async Task InvokeMethod_PythonException_SurfacesItsMessage(string embeddedVersion)
        {
            using var scope = await UseScope(embeddedVersion);

            var script = await scope.LoadCode(Script);

            var ex = await Should.ThrowAsync<Exception>(() => scope.InvokeMethod(script, "fail"));
            ex.ToString().ShouldContain("boom from python");
        }

        [Fact]
        public async Task UnsupportedRuntimeVersion_IsRejected()
        {
            // A real library renamed to an unsupported version, as ValidatePythonLibraryTests does.
            var runtimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath(EmbeddedPythonRuntimeBootstrap.Python314);
            var unsupported = Path.Combine(_tempDir, "python36.dll");
            File.Copy(EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(runtimePath), unsupported);

            var options = new PythonScopeOptions { Path = runtimePath, LibraryPath = unsupported };

            var ex = await Should.ThrowAsync<NotSupportedException>(() => new PythonService().UsePythonScope(options));
            ex.Message.ShouldContain("3.6");
        }
    }
}
