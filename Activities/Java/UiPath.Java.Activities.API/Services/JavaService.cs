using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.API.Models;

namespace UiPath.Java.Activities.API
{
    internal class JavaService : IJavaService
    {
        private const string JavaExeWindows = "java.exe";
        private const string JavaExeLinux = "java";

        private readonly Func<string, IInvoker> _invokerFactory;

        public JavaService()
            : this(javaPath => new JavaInvoker(javaPath))
        {
        }

        // For testing: allows injecting a fake IInvoker without spawning a real JVM.
        internal JavaService(Func<string, IInvoker> invokerFactory)
        {
            _invokerFactory = invokerFactory;
        }

        public async Task<IJavaScopeHandle> UseJavaScope(JavaScopeOptions options, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            string javaPath = options.JavaPath;
            if (!string.IsNullOrWhiteSpace(javaPath))
            {
                var javaExec = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? JavaExeWindows : JavaExeLinux;
                javaPath = Path.Combine(javaPath, "bin", javaExec);
                if (!File.Exists(javaPath))
                    throw new ArgumentException($"Java executable not found at: {javaPath}", nameof(options));
            }

            if (options.Timeout.HasValue && options.Timeout.Value < TimeSpan.Zero)
                throw new ArgumentException("Timeout must be non-negative.", nameof(options));

            int timeout = (int)(options.Timeout ?? TimeSpan.FromSeconds(15)).TotalMilliseconds;

            ct.ThrowIfCancellationRequested();

            var invoker = _invokerFactory(string.IsNullOrWhiteSpace(javaPath) ? null : javaPath);
            try
            {
                await invoker.StartJavaService(timeout);
            }
            catch (Exception e)
            {
                try
                {
                    await invoker.ReleaseAsync();
                }
                catch (Exception releaseEx)
                {
                    // Release failed — record as secondary context so the original exception type is preserved.
                    e.Data["ReleaseException"] = releaseEx.ToString();
                }

                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw();
                throw; // unreachable — satisfies the compiler
            }

            return new JavaScopeHandle(invoker);
        }
    }
}
