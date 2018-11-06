using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Excel.Activities.Tests.Utils;
using UiPath.Java.Test.Fixtures;
using UiPath.TestUtils;
using Xunit;

namespace UiPath.Java.Test
{
    [Trait(TestCategories.Category, JavaTestBaseFixture.Category)]
    public class JavaInvokeCoreLibraries : IClassFixture<JavaTestBaseFixture>
    {
        private readonly JavaInvoker _invoker;
        private readonly CancellationToken _ct;

        public JavaInvokeCoreLibraries(JavaTestBaseFixture fixture)
        {
            _invoker = fixture.Invoker;
            _ct = fixture.Ct;
        }

        [Fact]
        [TestPriority(0)]
        public async Task InvokeMax()
        {
            var javaObject = await _invoker.InvokeMethod(
                "max",
                "java.lang.Math",
                null,
                new List<object> { 4, 15 },
                null,
                _ct
            );

            Assert.Equal(15, javaObject.Convert<int>());
        }

        [Fact]
        [TestPriority(1)]
        public async Task LoadJarAndInvoke()
        {
            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var floatObject = await _invoker.InvokeMethod(
                "getFloat",
                "uipath.java.test.StaticMethods",
                null,
                null,
                null,
                _ct
            );

            Assert.True(floatObject.Convert<float>() - 2.3f < 0.00001f);
            Assert.True(floatObject.Convert<double>() - 2.3d < 0.00001d);
        }
    }
}
