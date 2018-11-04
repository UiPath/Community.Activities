using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Test.Fixtures;
using UiPath.TestUtils;
using Xunit;

namespace UiPath.Java.Test
{
    [Trait(TestCategories.Category, JavaTestBaseFixture.Category)]
    public class JavaTestImportCoordinate : IClassFixture<JavaTestBaseFixture>
    {
        private readonly JavaInvoker _invoker;
        private readonly CancellationToken _ct;

        public JavaTestImportCoordinate(JavaTestBaseFixture fixture)
        {
            _invoker = fixture.Invoker;
            _ct = fixture.Ct;
        }

        [Fact]
        public async Task InvokeCoordinate()
        {
            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);

            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.Coordinate",
                new List<object> { 3.13d, 3.12f },
                null,
                _ct
            );
            var getX = await _invoker.InvokeMethod("getX",
                null,
                javaObject,
                null,
                null,
                _ct
            );
            Assert.True(getX.Convert<double>() - 3.13 < 0.00001);

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "ImportCoordinate.Jar"), _ct);

            var coordinateObject = await _invoker.InvokeConstructor(
                "uipath.java.test.Coordinate",
                new List<object> { 1.0d, 2.0f },
                null,
                _ct
            );
            var coordinate3DObject = await _invoker.InvokeConstructor(
                "uipath.java.testimport.Coordinate3D",
                new List<object> { coordinateObject },
                null,
                _ct
            );
            var xCoordinate = await _invoker.InvokeMethod(
                "getX",
                null,
                coordinateObject,
                null,
                null,
                _ct
            );
            var xCoordinate3D = await _invoker.InvokeMethod(
                "getX",
                null,
                coordinate3DObject,
                null,
                null,
                _ct
            );
            var isEqual = await _invoker.InvokeMethod(
                "equals2DCoordinate",
                null,
                coordinate3DObject,
                new List<object> { coordinateObject },
                null,
                _ct
            );

            Assert.Equal(xCoordinate.Convert<double>(), xCoordinate3D.Convert<double>());
            Assert.Equal(1.0d, xCoordinate3D.Convert<double>());
            Assert.True(isEqual.Convert<bool>());
        }
    }
}
