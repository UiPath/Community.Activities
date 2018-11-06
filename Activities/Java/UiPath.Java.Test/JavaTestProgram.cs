using System;
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
    public class JavaTestProgram : IClassFixture<JavaTestDerivedFixture>
    {
        private readonly JavaInvoker _invoker;
        private readonly CancellationToken _ct;

        public JavaTestProgram(JavaTestDerivedFixture fixture)
        {
            _invoker = fixture.Invoker;
            _ct = fixture.Ct;
        }

        [Fact]
        [TestPriority(0)] // 0-based priority orderer
        public async Task InvokeComplex()
        {
            var javaobject = await _invoker.InvokeConstructor(
                "uipath.java.test.Coordinate",
                new List<object> { 1.0d, 2.0f },
                null,
                _ct
            );
            var javaobject2 = await _invoker.InvokeConstructor(
                "uipath.java.test.Coordinate",
                new List<object> { 1.3d, 2.4f },
                null,
                _ct
            );

            var resultEqaulsCoordinate = await _invoker.InvokeMethod(
                "equalsCoordinate",
                null,
                javaobject,
                new List<object> { javaobject2 },
                null,
                _ct
            );
            Assert.False(resultEqaulsCoordinate.Convert<bool>());

            var instaceCounter = await _invoker.InvokeMethod(
                "getInstanceCounter",
                "uipath.java.test.Coordinate",
                null,
                null,
                null,
                _ct
            );
            Assert.Equal(2, instaceCounter.Convert<int>());

            var getSum = await _invoker.InvokeMethod(
                "getCoordinateSum",
                null,
                javaobject2,
                null,
                null,
                _ct
            );
            Assert.True(Math.Abs(getSum.Convert<double>() - 3.7d) < 0.000001);

            var toString = await _invoker.InvokeMethod(
                "toString",
                null,
                javaobject,
                null,
                null,
                _ct
            );
            Assert.Equal("1.0 2.0", toString.Convert<string>());
        }

        [Fact]
        [TestPriority(1)]
        public async Task _invokerArrayInts()
        {
            var javaArrayobject = await _invoker.InvokeMethod(
                "getArrayInt",
                "uipath.java.test.StaticMethods",
                null,
                null,
                null,
                _ct
            );
            Assert.Equal(new[] { 1, 4, 5, 6, 7, 8 }, javaArrayobject.Convert<int[]>());

            var arraySum = await _invoker.InvokeMethod(
                "getSumInt",
                "uipath.java.test.StaticMethods",
                null,
                new List<object> { javaArrayobject },
                null,
                _ct
            );
            Assert.Equal(31, arraySum.Convert<int>());
        }

        [Fact]
        [TestPriority(2)]
        public async Task ConvertChar()
        {
            var javaobject = await _invoker.InvokeMethod("getChar", "uipath.java.test.StaticMethods", null, null, null, _ct);

            Assert.Equal('a', javaobject.Convert<char>());
            Assert.Equal("a", javaobject.Convert<string>());
        }

        [Fact]
        [TestPriority(3)]
        public async Task InvokeWrrapperType()
        {
            var javaobject = await _invoker.InvokeMethod("valueOf", "java.lang.Integer", null, new List<object> { 3 }, null, _ct);
            var sumobject = await _invoker.InvokeMethod("getSumWrapped", "uipath.java.test.StaticMethods", null, new List<object> { javaobject, 5 }, null, _ct);

            Assert.Equal(8, sumobject.Convert<int>());

        }

        [Fact]
        [TestPriority(4)]
        public async Task InvokeWrapperArrayType()
        {
            var arrayobject = await _invoker.InvokeMethod("getArrayDoubleBoxed", "uipath.java.test.StaticMethods", null, null, null, _ct);
            var sumobject = await _invoker.InvokeMethod("getSumDoubleBoxed", "uipath.java.test.StaticMethods", null, new List<object> { arrayobject }, null, _ct);

            Assert.Equal(219, sumobject.Convert<int>());
        }

        [Fact]
        [TestPriority(5)]
        public void InvokeWrapperArrayTypeFromUser()
        {
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _invoker.InvokeMethod("getSumDoubleBoxed", "uipath.java.test.StaticMethods", null, new List<object> { new List<double> { 1.3, 1.5, 1.6 } }, null, _ct);
            });

            Assert.NotNull(ex);
        }

        [Fact]
        [TestPriority(6)]
        public async Task InvokeBuildWrapperArray()
        {
            var doubleobject = await _invoker.InvokeConstructor("java.lang.Double", new List<object> { 1.3d }, null, _ct);
            var elementType = await _invoker.InvokeMethod("getClass", null, doubleobject, null, null, _ct);
            var arrayobject = await _invoker.InvokeMethod("newInstance", "java.lang.reflect.Array", null, new List<object> { elementType, 3 }, null, _ct);
            await _invoker.InvokeMethod("set", "java.lang.reflect.Array", null, new List<object> { arrayobject, 0, 2.3d }, null, _ct);
            await _invoker.InvokeMethod("set", "java.lang.reflect.Array", null, new List<object> { arrayobject, 1, 4.33d }, null, _ct);
            await _invoker.InvokeMethod("set", "java.lang.reflect.Array", null, new List<object> { arrayobject, 2, 3.13d }, null, _ct);
            var sumObject = await _invoker.InvokeMethod("getSumDoubleBoxed", "uipath.java.test.StaticMethods", null, new List<object> { arrayobject }, null, _ct);

            Assert.Equal(9, sumObject.Convert<int>());
        }

        [Fact]
        [TestPriority(6)]
        public async Task InvokeOverloadedMethod()
        {
            // it is dependent on the order the methods have been written in programs
            var integerCompareResult = await _invoker.InvokeMethod("compare", "uipath.java.test.StaticMethods", null, new List<object> { 2, 3 }, null, _ct);
            Assert.NotEqual(123, integerCompareResult.Convert<int>());
        }
    }
}
