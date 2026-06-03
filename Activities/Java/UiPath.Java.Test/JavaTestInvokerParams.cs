using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UiPath.Java.Test
{
    public class JavaTestInvokerParams
    {
        
        public static readonly string JavaFilesPath = Path.Combine(
           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(),
           "java_files"
       );
        private static readonly string InvokeJarPath = Path.Combine(JavaFilesPath, "InvokeJava.jar");


        public JavaInvoker Invoker { get; }
        public CancellationTokenSource Cts { get; }
        public CancellationToken Ct { get; }


        public JavaTestInvokerParams()
        {
            Invoker = new JavaInvoker(javaInvokerPath: InvokeJarPath);
            
            Cts = new CancellationTokenSource();
            Ct = Cts.Token;
        }

        // Verifies that a very short timeout (1ms) fails before Java can initialize,
        // while longer timeouts (15s, 30s) allow the service to start successfully.
        [Theory]
        [InlineData(1, true)]
        [InlineData(15000, false)]
        [InlineData(30000, false)]
        public async Task ConnectToJavaTimeout(int timeoutMs, bool expectTimeout)
        {
            if (expectTimeout)
            {
                await Assert.ThrowsAnyAsync<Exception>(() => Invoker.StartJavaService(timeoutMs));
            }
            else
            {
                await Invoker.StartJavaService(timeoutMs);
                await Invoker.ReleaseAsync();
            }
        }
    }
}
