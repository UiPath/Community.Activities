using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using UiPath.Python;
using UiPath.Python.Activities;
using UiPath.Python.Tests;
using UiPath.TestUtils;
using Xunit;

namespace UiPath.Python.Activities.Tests
{
    /// <summary>
    /// Integration tests that exercise the real WF activities end-to-end:
    /// PythonScope &gt; LoadScript &gt; InvokeMethod &gt; GetObject&lt;T&gt;
    /// using the embedded Python 3.14.5 installation extracted by EmbeddedPythonRuntimeBootstrap.
    /// </summary>
    public class PythonScopeTests
    {
        private const string Category = "Python";

        // Resolved once per test-run; extraction is idempotent and process-safe.
        private static readonly string RuntimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath();
        private static readonly string LibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(RuntimePath);

        private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(2);

        private const string Script = @"
def greet(name):
    return 'Hello ' + name

def add(a, b):
    return a + b

def create_payload(number, text):
    return f'{number}:{text}'
";

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData("UiPath")]
        [InlineData("Python")]
        public void PythonScope_LoadScript_InvokeMethod_GetObject_ReturnsGreeting(string name)
        {
            var scriptVar = new Variable<PythonObject>("scriptObj");
            var resultVar = new Variable<PythonObject>("resultObj");
            var outputVar = new Variable<string>("outputVal");
            var capture = new Capture<string> { Input = new InArgument<string>(outputVar) };

            var scope = BuildScope(new Sequence
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
                        Name = "greet",
                        Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { name }),
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<string>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<string>(outputVar)
                    },
                    capture
                }
            });

            new WorkflowInvoker(scope).Invoke(TestTimeout);

            Assert.Equal($"Hello {name}", capture.Value);
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(1, 2, 3)]
        [InlineData(10, 25, 35)]
        public void PythonScope_LoadScript_InvokeMethod_GetObject_ReturnsSum(int a, int b, int expected)
        {
            var scriptVar = new Variable<PythonObject>("scriptObj");
            var resultVar = new Variable<PythonObject>("resultObj");
            var outputVar = new Variable<long>("outputVal");
            var capture = new Capture<long> { Input = new InArgument<long>(outputVar) };

            var scope = BuildScope(new Sequence
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
                        Name = "add",
                        Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { a, b }),
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<long>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<long>(outputVar)
                    },
                    capture
                }
            });

            new WorkflowInvoker(scope).Invoke(TestTimeout);

            Assert.Equal(expected, capture.Value);
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(7, "seven")]
        [InlineData(42, "answer")]
        public void PythonScope_LoadScript_InvokeMethod_GetObject_ReturnsComposedString(int number, string text)
        {
            var scriptVar = new Variable<PythonObject>("scriptObj");
            var resultVar = new Variable<PythonObject>("resultObj");
            var outputVar = new Variable<string>("outputVal");
            var capture = new Capture<string> { Input = new InArgument<string>(outputVar) };

            var scope = BuildScope(new Sequence
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
                        Name = "create_payload",
                        Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { number, text }),
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<string>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<string>(outputVar)
                    },
                    capture
                }
            });

            new WorkflowInvoker(scope).Invoke(TestTimeout);

            Assert.Equal($"{number}:{text}", capture.Value);
        }

        /// <summary>
        /// Builds a PythonScope configured with the embedded runtime, wrapping the given body handler.
        /// Isolated=false (in-process) is required for the embedded single-file Python zip.
        /// </summary>
        private static PythonScope BuildScope(Activity handler)
        {
            var scope = new PythonScope
            {
                Path = RuntimePath,
                LibraryPath = LibraryPath,
                Isolated = false
            };
            scope.Body.Handler = handler;
            return scope;
        }

        /// <summary>
        /// Helper CodeActivity that reads an InArgument at the end of the workflow body
        /// and stores it in a test-accessible field.
        /// Using InArgument&lt;T&gt; bound to a Sequence variable avoids Variable&lt;T&gt;
        /// ownership and CacheMetadata issues in CodeActivity.
        /// </summary>
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
