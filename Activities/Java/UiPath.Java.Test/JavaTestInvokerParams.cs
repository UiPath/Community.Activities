using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Excel.Activities.Tests.Utils;
using UiPath.Java.Test.Fixtures;
using UiPath.TestUtils;
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

        [Theory]
        [InlineData(200)]
        [InlineData(15000)]
        [InlineData(30000)]
        public void ConnectToJavaTimeout(int timeout)
        {
            var exception = Record.Exception(
                () => Invoker.StartJavaService(timeout).Wait());
            if (timeout<10000)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
            
        }
    }
}
