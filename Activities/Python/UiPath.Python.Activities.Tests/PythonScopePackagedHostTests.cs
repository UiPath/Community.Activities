using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.IO;
using UiPath.Python.Impl;
using UiPath.Python.Tests;
using UiPath.TestUtils;
using Xunit;

namespace UiPath.Python.Activities.Tests
{
    /// <summary>
    /// Runs LoadScript and InvokeMethod in a host started from a folder holding only the files the NuGet package ships
    /// (see UiPath.Python.Packaging.csproj). In the normal test output Python.Runtime.dll and System.Xaml.dll sit
    /// beside the host and its deps.json, so the default load context resolves them itself and the host's
    /// assembly resolver never runs; here it is the only way the host can find them.
    /// </summary>
    public class PythonScopePackagedHostTests : IDisposable
    {
        private const string Category = "Python";

        // Keep in sync with the AddPythonHost item group in UiPath.Python.Packaging.csproj.
        private static readonly string[] PackagedHostFiles =
        {
            "UiPath.Python.dll",
            "UiPath.Python.Host.dll",
            "UiPath.Python.Host.runtimeconfig.json"
        };

        private static readonly string[] ThirdPartyFiles =
        {
            "Python.Runtime.dll",
            "System.Xaml.dll",
            "UiPath.Python.Host.deps.json"
        };

        private const string Script = @"
def create_file(path, content):
    with open(path, 'w') as f:
        f.write(content)
";

        private readonly string _tempDir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "python-packaged-host-tests", Guid.NewGuid().ToString("N"))).FullName;

        public PythonScopePackagedHostTests()
        {
            var hostDir = Directory.CreateDirectory(Path.Combine(_tempDir, "bin")).FullName;
            foreach (var file in PackagedHostFiles)
                File.Copy(Path.Combine(AppContext.BaseDirectory, file), Path.Combine(hostDir, file));

            foreach (var file in ThirdPartyFiles)
                Assert.False(File.Exists(Path.Combine(hostDir, file)), $"{file} must not be beside the packaged host.");

            OutOfProcessEngine.HostFileOverride = Path.Combine(hostDir, "UiPath.Python.Host.dll");
        }

        public void Dispose()
        {
            OutOfProcessEngine.HostFileOverride = null;
            try { Directory.Delete(_tempDir, true); } catch { /* best effort cleanup */ }
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public void InvokeMethod_InHostWithOnlyPackagedFiles_CreatesFile(string embeddedVersion)
        {
            var runtimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath(embeddedVersion);
            var outputFile = Path.Combine(_tempDir, "created-by-python.txt");

            var scriptVar = new Variable<PythonObject>("scriptObj");
            var resultVar = new Variable<PythonObject>("resultObj");

            var scope = new PythonScope
            {
                Path = runtimePath,
                LibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(runtimePath)
            };
            // The method runs in the host process, so the file only exists if the host started and ran it.
            scope.Body.Handler = new Sequence
            {
                Variables = { scriptVar, resultVar },
                Activities =
                {
                    new LoadScript
                    {
                        Code = Script,
                        Result = new OutArgument<PythonObject>(scriptVar)
                    },
                    new InvokeMethod
                    {
                        Instance = new InArgument<PythonObject>(scriptVar),
                        Name = "create_file",
                        Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { outputFile, "hello from python" }),
                        Result = new OutArgument<PythonObject>(resultVar)
                    }
                }
            };

            Assert.False(File.Exists(outputFile));

            new WorkflowInvoker(scope).Invoke(TimeSpan.FromMinutes(2));

            Assert.True(File.Exists(outputFile));
            Assert.Equal("hello from python", File.ReadAllText(outputFile));
        }

    }
}
