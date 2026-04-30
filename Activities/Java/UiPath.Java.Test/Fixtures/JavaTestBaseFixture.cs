using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace UiPath.Java.Test.Fixtures
{
    public class JavaTestBaseFixture : IDisposable
    {
        public const string Category = "Java Tests";
        public static readonly string JavaFilesPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(),
            "java_files"
        );


        public JavaInvoker Invoker { get; }
        public CancellationTokenSource Cts { get; }
        public CancellationToken Ct { get; }

        private static readonly string InvokeJarPath = Path.Combine(JavaFilesPath, "InvokeJava.jar");

        public JavaTestBaseFixture()
        {
            Invoker = new JavaInvoker(javaInvokerPath: InvokeJarPath);
            Invoker.StartJavaService(15000).Wait();
            Cts = new CancellationTokenSource();
            Ct = Cts.Token;
        }

        public void Dispose()
        {
            // ReleaseAsync sends a StopConnection request to the JVM via named pipe,
            // then calls JavaService.Dispose() which attempts Kill() on the process.
            // This was previously async void (fire-and-forget), meaning xUnit never
            // waited for cleanup — leaving the JVM alive and hanging vstest.console.exe.
            Invoker.ReleaseAsync().GetAwaiter().GetResult();

            // Kill any remaining java processes started by this fixture.
            // JavaService uses UseShellExecute = true, so its Kill() only kills the
            // shell wrapper and orphans the actual java.exe. Kill the whole tree here
            // to ensure vstest doesn't hang waiting for child processes.
            KillOrphanedJavaProcesses();

            Cts.Cancel();
            Cts?.Dispose();
        }

        private static void KillOrphanedJavaProcesses()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("java"))
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {
                        // Best effort — process may have exited between check and kill
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                // Best effort
            }
        }
    }
}
