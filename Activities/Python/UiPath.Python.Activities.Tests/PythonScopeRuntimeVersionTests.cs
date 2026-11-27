using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using UiPath.Python.Tests;
using UiPath.TestUtils;
using Xunit;

namespace UiPath.Python.Activities.Tests
{
    /// <summary>
    /// Runs the real activities (PythonScope &gt; LoadScript &gt; InvokeMethod &gt; GetObject&lt;T&gt;) against each
    /// embedded Python runtime, in the isolated host process, so every supported interpreter version is
    /// exercised through the activity layer and not only through the engine.
    /// </summary>
    public class PythonScopeRuntimeVersionTests
    {
        private const string Category = "Python";

        private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(2);

        private const string Script = @"
import sys

def version():
    return f'{sys.version_info.major}.{sys.version_info.minor}'

def add(a, b):
    return a + b
";

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314, "3.14")]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315, "3.15")]
        public void PythonScope_RunsOnEmbeddedRuntime_ReportsItsVersion(string embeddedVersion, string expectedVersion)
        {
            Assert.Equal(expectedVersion, Invoke<string>(embeddedVersion, "version"));
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python314)]
        [InlineData(EmbeddedPythonRuntimeBootstrap.Python315)]
        public void PythonScope_RunsOnEmbeddedRuntime_InvokesMethodWithArguments(string embeddedVersion)
        {
            Assert.Equal(42L, Invoke<long>(embeddedVersion, "add", 2, 40));
        }

        private static T Invoke<T>(string embeddedVersion, string method, params object[] parameters)
        {
            var runtimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath(embeddedVersion);
            var libraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(runtimePath);

            var scriptVar = new Variable<PythonObject>("scriptObj");
            var resultVar = new Variable<PythonObject>("resultObj");
            var outputVar = new Variable<T>("outputVal");
            var capture = new Capture<T> { Input = new InArgument<T>(outputVar) };

            var scope = new PythonScope
            {
                Path = runtimePath,
                LibraryPath = libraryPath
            };

            scope.Body.Handler = new Sequence
            {
                Variables = { scriptVar, resultVar, outputVar },
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
                        Name = method,
                        Parameters = new InArgument<IEnumerable<object>>(ctx => parameters),
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<T>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<T>(outputVar)
                    },
                    capture
                }
            };

            new WorkflowInvoker(scope).Invoke(TestTimeout);
            return capture.Value;
        }

        // Reads an InArgument at the end of the workflow body into a test-accessible field.
        private sealed class Capture<T> : CodeActivity
        {
            public InArgument<T> Input { get; set; }
            public T Value { get; private set; }

            protected override void Execute(CodeActivityContext context)
            {
                Value = Input.Get(context);
            }
        }
    }
}
