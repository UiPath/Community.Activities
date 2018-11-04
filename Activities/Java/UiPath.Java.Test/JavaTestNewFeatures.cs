using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Excel.Activities.Tests.Utils;
using UiPath.Java.Test.Fixtures;
using UiPath.TestUtils;
using Xunit;

namespace UiPath.Java.Test
{
    [Trait(TestCategories.Category, JavaTestBaseFixture.Category)]
    [TestCaseOrderer("UiPath.Java.Test.Utils.PriorityOrderer", "UiPath.Java.Test")]
    public class JavaTestNewFeatures : IClassFixture<JavaTestDerivedFixture>
    {
        private readonly JavaInvoker _invoker;
        private readonly CancellationToken _ct;

        public JavaTestNewFeatures(JavaTestDerivedFixture fixture)
        {
            _invoker = fixture.Invoker;
            _ct = fixture.Ct;
        }

        [Fact]
        [TestPriority(0)]
        public async Task InvokeLambda()
        {
            var coordinateObject = await _invoker.InvokeConstructor("uipath.java.test.Coordinate", new List<object> { 1.5d, 2.3f }, null, _ct);

            var coordinate3DObject = await _invoker.InvokeConstructor("uipath.java.testimport.Coordinate3D", new List<object> { coordinateObject, 3.1d }, null, _ct);

            var lambdaObject = await _invoker.InvokeMethod("GetLambda", "uipath.java.testimport.Coordinate3D", null, null, null, _ct);

            await _invoker.InvokeMethod("ApplyTransform", null, coordinate3DObject, new List<object> { lambdaObject, 1, 1, 1 }, null, _ct);

            var x = await _invoker.InvokeMethod("getX", null, coordinate3DObject, null, null, _ct);

            var y = await _invoker.InvokeMethod("getY", null, coordinate3DObject, null, null, _ct);

            var z = await _invoker.InvokeMethod("getZ", null, coordinate3DObject, null, null, _ct);

            Assert.True(x.Convert<double>() - 2.5d < 0.00001);
            Assert.True(y.Convert<float>() - 3.3f < 0.00001);
            Assert.True(z.Convert<double>() - 4.1d < 0.00001);

        }

        [Fact]
        [TestPriority(1)]
        public async void InvokePolymorphism()
        {
            var coordinate3DObject = await _invoker.InvokeConstructor(
                "uipath.java.testimport.Coordinate3D",
                new List<object> { 1.5d, 1.7f, 3.1d },
                null,
                _ct
            );
            var coordinate3DObject2 = await _invoker.InvokeConstructor(
                "uipath.java.testimport.Coordinate3D",
                new List<object> { 1.5d, 1.7f, 3.1d },
                null,
                _ct
            );

            var result = await _invoker.InvokeMethod(
                "equals2DCoordinate",
                null,
                coordinate3DObject,
                new List<object> { coordinate3DObject2 },
                null,
                _ct
            );

            Assert.True(result.Convert<bool>());
        }
    }
}
