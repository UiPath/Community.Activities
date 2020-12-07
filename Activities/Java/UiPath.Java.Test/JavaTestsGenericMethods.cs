using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using UiPath.Excel.Activities.Tests.Utils;
using UiPath.Java.Test.Fixtures;
using UiPath.TestUtils;
using Xunit;
using System.Diagnostics;

namespace UiPath.Java.Test
{

    [Trait(TestCategories.Category, JavaTestBaseFixture.Category)]
    [TestCaseOrderer("UiPath.Java.Test.Utils.PriorityOrderer", "UiPath.Java.Test")]


    public class JavaTestsGenericMethods : IClassFixture<JavaTestDerivedFixture>
    {
        private readonly JavaInvoker _invoker;
        private readonly CancellationToken _ct;

        public JavaTestsGenericMethods(JavaTestDerivedFixture fixture)
        {
            _invoker = fixture.Invoker;
            _ct = fixture.Ct;
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsConstructor_NoParams()
        {
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsConstructor_WithParams()
        {
            var objParam = new List<object> {"Test Param" };
           
            var javaObject = await _invoker.InvokeConstructor(                
                "uipath.java.test.GenericMethods",
                 objParam,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            //verifying the parameter using the property
            var javaMethod = await _invoker.InvokeMethod(
                "getMessage",
                null,
                javaObject,
                null,
                null,
                _ct
            );
            Assert.StartsWith(objParam[0].ToString(), javaMethod.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsStringArray()
        {
            var objParam = new List<object> { "Test Param" };

            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                 objParam,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            String[] Strings = { "S1", "S22", "3.3d", "4.4f", "5", "null" };
            
            var javaMethod = await _invoker.InvokeMethod(
                "GenericMethods",
                null,
                javaObject,
                new List<object> { Strings },
                new List<Type> { typeof(string) },
                _ct
            );
            Assert.Equal(Strings, javaMethod.Convert<string[]>());
        }


        [Fact]
        [TestPriority(0)]
        public async Task GenericMethods_ConcatenateParams()
        {      
            var objparam1 = "String 1";
            var objparam2 = "String 2";

            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );

            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            var javaMethod = await _invoker.InvokeMethod(
                "GenericMethods",
                null,
                javaObject,
                new List<object> { objparam1, objparam2 },
                new List<Type> { typeof(string), typeof(string) },
                _ct
            );
            string result = objparam1.ToString() + objparam2.ToString(); 
            Assert.StartsWith(result, javaMethod.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsIntArray()
        {
            
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );

            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            int[] intArray = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
           
            
            var javaMethod = await _invoker.InvokeMethod(
                "IntArr",
                null,
                javaObject,
                new List<object> { intArray },
                null,
                _ct
            );
            Assert.Equal(intArray, javaMethod.Convert<int[]>());
        }

       
        [Fact]
        [TestPriority(1)]
        public async Task FloatType()
        {
            var objParmFloat = new List<object> { 1.2f};

            var objFloatMethod = await _invoker.InvokeMethod(
                "FloatType",
                "uipath.java.test.GenericMethods",
                null,
                objParmFloat,
                null,
                _ct
            );
            Assert.Equal(objParmFloat[0], objFloatMethod.Convert<float>());
        }


        [Fact]
        [TestPriority(2)]
        public async Task GenericsExtendString()
        {
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            String testString = "StringXY";
            var stringObj = await _invoker.InvokeMethod(
                "GenericsExtString",
                null,
                javaObject,
                new List<object> { testString },
                new List<Type> { typeof(string) },
                _ct
            );

            string result = "Generic method " + testString;
            Assert.StartsWith(result, stringObj.Convert<string>());
        }

        [Fact]
        [TestPriority(2)]
        public async Task StringArrStatic()
        {
            String []testString = { "string1", "string2" };

            var stringObj = await _invoker.InvokeMethod(
                "StringArrStatic",
               "uipath.java.test.GenericMethods",
                null,
                new List<object> { testString },
                new List<Type> { typeof(string) },
                _ct
            );

            Assert.Equal(testString, stringObj.Convert<string[]>());
        }


        [Fact]
        [TestPriority(1)]
        public async Task IntType()
        {
            var intParm = new List<object> { 123 };

            var objInttMethod = await _invoker.InvokeMethod(
                "IntType",
                "uipath.java.test.GenericMethods",
                null,
                intParm,
                null,
                _ct
            );
            Assert.Equal(intParm[0], objInttMethod.Convert<int>());
        }


        [Fact]
        [TestPriority(1)]
        public async Task BoolType()
        {
            var boolParm = new List<object> { true };

            var objInttMethod = await _invoker.InvokeMethod(
                "BoolType",
                "uipath.java.test.GenericMethods",
                null,
                boolParm,
                null,
                _ct
            );
            Assert.Equal(boolParm[0], objInttMethod.Convert<bool>());
        }


        [Fact]
        [TestPriority(3)]
        public async Task GenericsExtObj1()
        {
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            var stringObj2 = await _invoker.InvokeMethod(
                "GenericsExtObj",
               null,
                javaObject,
                new List<object> { "test string" },
                null,
                _ct
            );
            Assert.StartsWith("Generic method with Object ", stringObj2.Convert<string>());
        }

        [Fact]
        [TestPriority(3)]
        public async Task GenericsExtObj2()
        {
            float floatobj = (2.3f);

            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());


            var stringObj2 = await _invoker.InvokeMethod(
                "GenericsExtObj",
                null,
                javaObject,
                new List<object> { floatobj },
                null,
                _ct
            );
            Assert.StartsWith("Generic method with Object " + floatobj, stringObj2.Convert<string>());
        }

        [Fact]
        [TestPriority(5)]
        public async Task Finalize()
        {
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );
            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            var JOFinalize = await _invoker.InvokeMethod(
                "finalize",
                null,
                javaObject,
                null,
                null,
                _ct
            );

        }

        [Fact]
        [TestPriority(4)]
        public async Task GenericsR_String()
        {
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );
            string objParam = "String Param";
            string objResponse = "Generic method with return " + objParam;

            var stringObj = await _invoker.InvokeMethod(
                "GenericsR",
                null,
                javaObject,
                new List<object> { objParam },
                null,
                _ct
            );
            Assert.StartsWith(objResponse, stringObj.Convert<string>());
        }

        [Fact]
        [TestPriority(4)]
        public async Task GenericsR_integer()
        {

            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );

            var objParam = 100;
            string objResponse = "Generic method with return " + objParam;

            var intObj = await _invoker.InvokeMethod(
               "GenericsR",
               null,
               javaObject,
               new List<object> { 100 },
               new List<Type> { typeof(int) },
               _ct
            );
            Assert.StartsWith(objResponse, intObj.Convert<string>());
        }

        [Fact]
        [TestPriority(4)]
        public async Task GenericsR_Double()
        {
            var objParam = 2.3d;
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );

            var objResponse = "Generic method with return " + 2.3;
            var doubleObj = await _invoker.InvokeMethod(
                "GenericsR",
                null,
                javaObject,
                new List<object> { objParam },
                null,
                _ct
            );
            Assert.StartsWith(objResponse, doubleObj.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task ConcatenateXYZ()
        {
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );

            Assert.StartsWith("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            var javaMethod = await _invoker.InvokeMethod(
                "ConcatenateXYZ",
                null,
                javaObject,
                null,
                null,
                _ct
            );
            Assert.StartsWith("XYZ", javaMethod.Convert<string>());
        }


        [Fact]
        [TestPriority(4)]
        public async Task RecursiveCallTestNotStatic()
        {
            var objParam = 5;
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
            );

           
            var intObj = await _invoker.InvokeMethod(
                "RecursiveCallTest",
                null,
                javaObject,
                new List<object> { objParam },
                null,
                _ct
            );
            Assert.Equal(120, intObj.Convert<int>());
        }

        [Fact]
        [TestPriority(4)]
        public async Task RecursiveCallTestStatic()
        {
            var objParam = new List<object> { 4 };

            var intObj = await _invoker.InvokeMethod(
                "StaticRecursiveCallTest",
                "uipath.java.test.GenericMethods",
                null,
                objParam,
                null,
                _ct
            );
            Assert.Equal(24, intObj.Convert<int>());
        }
    }
}

