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
        public async Task GenericMethodsNoParams()
        {
           
        await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
        var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct
        );
        Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsOneString()
        {
        await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
        var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            new List<object> { "String Test" },
            new List<Type> { typeof(string) },
            _ct
        );
        Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsTwoStrings()
        {
        await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
        var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            new List<object> { "string1", "string2" },
            null,
            _ct
        );
        Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsIntArray()
        {
                int[] intArray = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
                var javaObject = await _invoker.InvokeConstructor(
                    "uipath.java.test.GenericMethods",
                    new List<object> { intArray },
                    null,
                    _ct
                );
         Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        }

        [Fact]
        [TestPriority(0)]
        public async Task GenericMethodsStringArray()
        {
                String[] Strings = { "S1", "S22", "3.3d", "4.4f", "5", "null" };
                await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
                var javaObject = await _invoker.InvokeConstructor(
                    "uipath.java.test.GenericMethods",
                    new List<object> { Strings },
                    new List<Type> { typeof(string) },
                    _ct
                );
         Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        }

        [Fact]
        [TestPriority(1)]
        public async Task WriteFloat()
        {
            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var JOWriteFloat = await _invoker.InvokeMethod(
            "Write",
            "uipath.java.test.GenericMethods",
            null,
            new List<object> { 1.2f },
            null,
            _ct
        );
        }

        [Fact]
        [TestPriority(1)]
        public async Task WriteArrayString()
        {

            String[] Strings = { "S1", "S22", "3.3d", "4.4f", "5", "null" };
            await _invoker.LoadJar(System.IO.Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct);

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var JOWrite = await _invoker.InvokeMethod(
            "Write",
            null,
            javaObject,
            new List<object> { Strings },
            null,
            _ct
        );
        }

        [Fact]
        [TestPriority(1)]
        public async Task WriteString()
        {

            await _invoker.LoadJar(System.IO.Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct);

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var JOWrite = await _invoker.InvokeMethod(
            "Write",
            null,
            javaObject,
            new List<object> { "String one" },
            null,
            _ct
        );
        }

        [Fact]
        [TestPriority(2)]
        public async Task WriteObj_String()
        {
            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct
        );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            await _invoker.LoadJar(System.IO.Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var javaObject2 = await _invoker.InvokeMethod(
            "WriteObj",
            null,
            javaObject,
            new List<object> { "String name" },
            new List<Type> { typeof(string) },
            _ct
        );
    
        }

        [Fact]
        [TestPriority(2)]
        public async Task WriteObj_StringArray()
        {
            String[] Strings = { "S1", "S22", "3.3d", "4.4f", "5", "null" };
            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct
            );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            await _invoker.LoadJar(System.IO.Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var javaObject2 = await _invoker.InvokeMethod(
            "WriteObj",
            null,
            javaObject,
            new List<object> { Strings },
            null,
            _ct
        );
        }

        [Fact]
        [TestPriority(2)]
        public async Task WriteObj_Float()
        {

            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct
            );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var floatObj = await _invoker.InvokeMethod(
            "WriteObj",
            null,
            javaObject,
            new List<object> { 1.2f },
            null,
            _ct
            );
        }

        [Fact]
        [TestPriority(2)]
        public async Task GenericsExtendString()
        {
            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
                );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            String testString = "StringXY";
            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var stringObj = await _invoker.InvokeMethod(
                "GenericsExtString",
               null,
                javaObject,
                new List<object> { testString },
                new List<Type> { typeof(string) },
                _ct
                );
            Assert.Contains("Generic method: " + testString, stringObj.Convert<string>());
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
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var stringObj2 = await _invoker.InvokeMethod(
                "GenericsExtObj",
               null,
                javaObject,
                new List<object> { "test string" },
                null,
                _ct
                );
            Assert.Contains("Generic method with Object ", stringObj2.Convert<string>());
        }

        [Fact]
        [TestPriority(3)]
        public async Task GenericsExtObj2()
        {
            float floatobj = (2.3f);
            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
                );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());


            var stringObj2 = await _invoker.InvokeMethod(
                "GenericsExtObj",
               null,
                javaObject,
                new List<object> { floatobj },
                null,
                _ct
                );
            Assert.Contains("Generic method with Object " + floatobj, stringObj2.Convert<string>());
        }

        [Fact]
        [TestPriority(4)]
        public async Task GenericsR()
        {
            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct
            );
            string objParam = "String Param";
                await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
                var StringObject = await _invoker.InvokeMethod(
                    "GenericsR",
                    null,
                    javaObject,
                    new List<object> { objParam },
                    null,
                    _ct
                );
            Assert.Contains(objParam , StringObject.Convert<string>());


            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var IntObject = await _invoker.InvokeMethod(
                "GenericsR",
                null,
                javaObject,
                new List<object> { 100 },
                new List<Type> { typeof(int) },
                _ct
            );
            Assert.Equal(100, IntObject.Convert<int>());

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
            var DoubleObject = await _invoker.InvokeMethod(
                "GenericsR",
                null,
                javaObject,
                new List<object> { 2.3d },
                null,
                _ct
            );
            Assert.Equal(2.3 , DoubleObject.Convert<double>());
        }

        [Fact]
        [TestPriority(4)]
        public async Task GenericsS()
        {
            var javaObject = await _invoker.InvokeConstructor(
            "uipath.java.test.GenericMethods",
            null,
            null,
            _ct
            );
 
                var TestString = "Test String";
                await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
                var StringObj = await _invoker.InvokeMethod(
                "GenericsS",
                null,
                javaObject,
                new List<object> { TestString },
                null,
                _ct
                );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());
        }

        [Fact]
        [TestPriority(5)]
        public async Task Finalize()
        {
                await _invoker.LoadJar(System.IO.Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
                var javaObject = await _invoker.InvokeConstructor(
                "uipath.java.test.GenericMethods",
                null,
                null,
                _ct
             );
            Assert.Contains("uipath.java.test.GenericMethods", javaObject.Convert<string>());

            await _invoker.LoadJar(Path.Combine(JavaTestBaseFixture.JavaFilesPath, "TestProgram.Jar"), _ct);
                var JOFinalize = await _invoker.InvokeMethod(
                "finalize",
                null,
                javaObject,
                null,
                null,
                _ct
            );

        }
    }
}

