using System.Activities;
using System.Activities.Statements;
using System.Reflection;
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

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void PythonScope_WithWorkingFolder_InitializationScriptSetsCurrentDirectory()
        {
            var workingFolder = Path.Combine(Path.GetTempPath(), $"UiPath.Python.Activities.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(workingFolder);

            var scriptPath = Path.Combine(workingFolder, "current_dir.py");
            File.WriteAllText(scriptPath,
@"import os

def current_dir():
    return os.getcwd()");

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
                        ScriptFile = scriptPath,
                        Result = new OutArgument<PythonObject>(scriptVar)
                    },
                    new InvokeMethod
                    {
                        Instance = new InArgument<PythonObject>(scriptVar),
                        Name = "current_dir",
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<string>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<string>(outputVar)
                    },
                    capture
                }
            }, workingFolder);

            new WorkflowInvoker(scope).Invoke(TestTimeout);

            Assert.Equal(new DirectoryInfo(workingFolder).FullName, capture.Value);
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(1, 2, true)]
        [InlineData(3, 1, false)]
        [InlineData(null, 3, false)]
        public void PythonScope_WithOperationTimeout_DelayedScriptHonorsTimeout(int? scopeTimeout, int scriptTimeout, bool shouldTimeout)
        {
            const string timeoutScript = @"import time

def wait_and_return(seconds):
    time.sleep(seconds)
    return 'done'";

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
                        Code = timeoutScript,
                        Result = new OutArgument<PythonObject>(scriptVar)
                    },
                    new InvokeMethod
                    {
                        Instance = new InArgument<PythonObject>(scriptVar),
                        Name = "wait_and_return",
                        Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { scriptTimeout }),
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<string>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<string>(outputVar)
                    },
                    capture
                }
            }, operationTimeout: scopeTimeout);

            if (shouldTimeout)
            {
                var ex = Assert.Throws<InvalidOperationException>(() => new WorkflowInvoker(scope).Invoke(TestTimeout));
                Assert.True(HasInnerException<TimeoutException>(ex));
                return;
            }

            new WorkflowInvoker(scope).Invoke(TestTimeout);
            Assert.Equal("done", capture.Value);
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(2, 1, true)]
        [InlineData(1, 2, false)]
        [InlineData(1, null, false)]
        [InlineData(EngineProvider.DefaultPayloadThresholdMB + 5, null, true)]
        public void PythonScope_WithScriptDataSizeLimit_DataPayloadHonorsLimit(int dataSizeMb, int? dataSizeLimitMb, bool shouldThrow)
        {
            const string payloadScript = @"def payload_length(value):
    return len(value)";

            var data = new string('x', dataSizeMb * 1024 * 1024);
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
                        Code = payloadScript,
                        Result = new OutArgument<PythonObject>(scriptVar)
                    },
                    new InvokeMethod
                    {
                        Instance = new InArgument<PythonObject>(scriptVar),
                        Name = "payload_length",
                        Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { data }),
                        Result = new OutArgument<PythonObject>(resultVar)
                    },
                    new GetObject<long>
                    {
                        PythonObject = new InArgument<PythonObject>(resultVar),
                        Result = new OutArgument<long>(outputVar)
                    },
                    capture
                }
            }, scriptDataSizeLimitMb: dataSizeLimitMb);

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => new WorkflowInvoker(scope).Invoke(TestTimeout));
                return;
            }

            new WorkflowInvoker(scope).Invoke(TestTimeout);
            Assert.Equal(data.Length, capture.Value);
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData(true)]
        [InlineData(false)]
        public void PythonScope_WithLogTraces_WritesExpectedStdOutAndStdErrLines(bool enableLogTraces)
        {
            var logsOverrideRoot = Path.Combine(Path.GetTempPath(), $"UiPath.Python.LogTrace.Tests.{Guid.NewGuid():N}");
            var isolatedPythonLogFolder = Path.Combine(logsOverrideRoot, "python");
            var marker = $"py-log-marker-{Guid.NewGuid():N}";

            const string traceScript = @"import sys

def emit_numbers(marker):
    for i in range(1, 6):
        print(f'{marker}-stdout-{i}', flush=True)
    for i in range(6, 11):
        print(f'{marker}-stderr-{i}', file=sys.stderr, flush=True)
    return 'done'";

            var scriptVar = new Variable<PythonObject>("scriptObj");
            var resultVar = new Variable<PythonObject>("resultObj");
            var outputVar = new Variable<string>("outputVal");
            var capture = new Capture<string> { Input = new InArgument<string>(outputVar) };

            SetHostLogsFolderOverride(logsOverrideRoot);
            try
            {
                var scope = BuildScope(new Sequence
                {
                    Variables = { scriptVar, resultVar, outputVar },
                    Activities =
                    {
                        new LoadScript
                        {
                            Code = traceScript,
                            Result = new OutArgument<PythonObject>(scriptVar)
                        },
                        new InvokeMethod
                        {
                            Instance = new InArgument<PythonObject>(scriptVar),
                            Name = "emit_numbers",
                            Parameters = new InArgument<IEnumerable<object>>(ctx => new object[] { marker }),
                            Result = new OutArgument<PythonObject>(resultVar)
                        },
                        new GetObject<string>
                        {
                            PythonObject = new InArgument<PythonObject>(resultVar),
                            Result = new OutArgument<string>(outputVar)
                        },
                        capture
                    }
                }, logTraces: enableLogTraces);

                new WorkflowInvoker(scope).Invoke(TestTimeout);
                Assert.Equal("done", capture.Value);

                var logFiles = Directory.Exists(isolatedPythonLogFolder)
                    ? Directory.EnumerateFiles(isolatedPythonLogFolder, "python-host-*.log", SearchOption.TopDirectoryOnly).ToList()
                    : new List<string>();

                if (enableLogTraces)
                {
                    Assert.NotEmpty(logFiles);
                    var merged = string.Join(Environment.NewLine, logFiles.Select(ReadAllTextAllowSharedRead));
                    Assert.Contains($"{marker}-stdout-1", merged, StringComparison.Ordinal);
                    Assert.Contains($"{marker}-stdout-5", merged, StringComparison.Ordinal);
                    Assert.Contains($"{marker}-stderr-6", merged, StringComparison.Ordinal);
                    Assert.Contains($"{marker}-stderr-10", merged, StringComparison.Ordinal);
                }
                else
                {
                    Assert.Empty(logFiles);
                }
            }
            finally
            {
                SetHostLogsFolderOverride(null);
            }
        }

        /// <summary>
        /// Builds a PythonScope configured with the embedded runtime, wrapping the given body handler.
        /// </summary>
        private static PythonScope BuildScope(Activity handler, string workingFolder = null, double? operationTimeout = null, int? scriptDataSizeLimitMb = null, bool? logTraces = null)
        {
            var scope = new PythonScope
            {
                Path = RuntimePath,
                LibraryPath = LibraryPath,
                WorkingFolder = workingFolder
            };

            if (operationTimeout.HasValue)
                scope.OperationTimeout = operationTimeout.Value;

            if (scriptDataSizeLimitMb.HasValue)
                scope.ScriptDataSizeLimitMB = scriptDataSizeLimitMb.Value;

            if (logTraces.HasValue)
                scope.LogTraces = logTraces.Value;

            scope.Body.Handler = handler;
            return scope;
        }

        private static void SetHostLogsFolderOverride(string logsFolder)
        {
            var hostWrapperType = typeof(EngineProvider).Assembly.GetType("UiPath.Shared.Service.Client.HostWrapper")
                ?? throw new InvalidOperationException("HostWrapper type could not be found in UiPath.Python assembly.");

            var property = hostWrapperType.GetProperty("LogsFolderOverride", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("HostWrapper.LogsFolderOverride property could not be found.");

            property.SetValue(null, logsFolder);
        }

        private static string ReadAllTextAllowSharedRead(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool HasInnerException<TException>(Exception exception) where TException : Exception
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is TException)
                    return true;
            }

            return false;
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
