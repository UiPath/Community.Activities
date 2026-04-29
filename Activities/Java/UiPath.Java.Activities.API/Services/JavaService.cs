using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.API.Models;
using UiPath.Robot.Activities.Api;

namespace UiPath.Java.Activities.API
{
    internal class JavaService : IJavaService
    {
        private const string JavaExeWindows = "java.exe";
        private const string JavaExeLinux = "java";

        public JavaService(IExecutorRuntime executorRuntime)
        {
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

            var invoker = new JavaInvoker(string.IsNullOrWhiteSpace(javaPath) ? null : javaPath);
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
                    throw new InvalidOperationException("Failed to start Java service.", new AggregateException(e, releaseEx));
                }

                throw new InvalidOperationException("Failed to start Java service.", e);
            }

            return new JavaScopeHandle(invoker);
        }
    }
}
