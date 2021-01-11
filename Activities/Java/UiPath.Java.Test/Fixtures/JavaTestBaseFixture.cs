using System;
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

        public async void Dispose()
        {
            await Invoker.ReleaseAsync();
            Cts.Cancel();
            Cts?.Dispose();
        }
    }
}
