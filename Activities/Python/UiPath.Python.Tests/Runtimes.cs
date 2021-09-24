using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    public class Runtimes
    {
        private const string Category = "Python";

        // types to test
        private readonly Dictionary<Type, object> _types = new Dictionary<Type, object>
        {
            { typeof(object), new object() },
            { typeof(bool), true },
            { typeof(byte), (byte)1 },
            { typeof(sbyte), (sbyte)1 },
            { typeof(char), '1' },
            { typeof(decimal), 1m },
            { typeof(float), 1f },
            { typeof(double), 1d },
            { typeof(int), 1 },
            { typeof(long), 1L },
            { typeof(short), (short)1 },
            { typeof(string), "1" },
            { typeof(uint), (uint)1 },
            { typeof(ulong), (ulong)1 },
            { typeof(ushort), (ushort)1 },
        };

        private readonly CancellationToken _ct = CancellationToken.None;

        #region Test scripts

        private readonly string _typeTestScript = string.Join(
            Environment.NewLine,
            "def dummy(obj):",
            "    print(type(obj))",
            "    print(obj)",
            "    return obj"
        );

        private readonly string _unicodeTestScript = string.Join(
            Environment.NewLine,
            "def test():",
            "    return '´©Ãˆ§‰©ù¨ëéüÇïçâèàêÉîôû'"
        );

        private readonly string _basicTestScript = string.Join(
            Environment.NewLine,
            "def hello(name=None):",
            "    if name is None:",
            "        name = 'stranger'",
            "    print ('Hello ', name)",
            "    return 'Hello ' + name",
            "",
            "def fib(n):",
            "    if n==1 or n==2:",
            "        return 1",
            "    return fib(n-1)+fib(n-2)",
            "",
            "def arr(length, value=None):",
            "    return [value] * length",
            "",
            "def no_param():",
            "    print('no_param')",
            "    return 'no_param'"
        );

        #endregion Test scripts

        #region Runtimes

        public static IEnumerable<object[]> X86Engines =>
            new List<object[]>
            {
                new object[]
                {
                    @"C:\Python\python27-x86",
                    Version.Python_27
                },
                new object[]
                {
                    @"C:\Python\python34-x86",
                    Version.Python_34
                },
                new object[]
                {
                    @"C:\Python\python35-x86",
                    Version.Python_35
                },
                new object[]
                {
                    @"C:\Python\python36-x86",
                    Version.Python_36
                },
                new object[]
                {
                     @"C:\Python\python37-x86",
                    Version.Python_37
                },
                new object[]
                {
                    @"C:\Python\python38-x86",
                    Version.Python_38
                },
                new object[]
                {
                    @"C:\Python\Python39-x86",
                    Version.Python_39
                }
            };

        public static IEnumerable<object[]> X64Engines =>
            new List<object[]>
            {
                new object[]
                {
                    @"C:\Python\python27-x64",
                    Version.Python_27
                },
                new object[]
                {
                    @"C:\Python\python35-x64",
                    Version.Python_35
                },
                new object[]
                {
                    @"C:\Python\python36-x64",
                    Version.Python_36
                },
                new object[]
                {
                    @"C:\Python\python37-x64",
                    Version.Python_37
                },
                new object[]
                {
                    @"C:\Python\python38-x64",
                    Version.Python_38
                },
                new object[]
                {
                   @"C:\Python\python39-x64",
                    Version.Python_39
                }
            };

        public static IEnumerable<object[]> AllEngines = X86Engines.Union(X64Engines);

        #endregion Runtimes

        #region Test cases

        [SkippableTheory]
        [Trait(TestCategories.Category, Category)]
        [MemberData(nameof(AllEngines))]
        public void AutomaticVersionDetection(string path, Version version)
        {
            Skip.IfNot(ValidateRuntime(path));
            var target = X64Engines.Any(x => x[0].Equals(path) && x[1].Equals(version))
                ? TargetPlatform.x64
                : TargetPlatform.x86;
            var engine = EngineProvider.Get(Version.Auto, path, null, true, target, true);
            Assert.Equal(engine.Version, version);
        }

        [SkippableTheory]
        [Trait(TestCategories.Category, Category)]
        [MemberData(nameof(X86Engines))]
        public async Task Simple_InProcess(string path, Version version)
        {
            Skip.IfNot(ValidateRuntime(path));

            await RunBasicTest(path, version, true, TargetPlatform.x86);
        }

        [SkippableTheory]
        [Trait(TestCategories.Category, Category)]
        [MemberData(nameof(AllEngines))]
        public async Task Simple_OutOfProcess(string path, Version version)
        {
            Skip.IfNot(ValidateRuntime(path));

            var target = X64Engines.Any(x => x[0].Equals(path) && x[1].Equals(version))
                ? TargetPlatform.x64
                : TargetPlatform.x86;
            await RunBasicTest(path, version, false, target);
        }

        [SkippableTheory]
        [Trait(TestCategories.Category, Category)]
        [MemberData(nameof(X86Engines))]
        public async Task Types_InProcess(string path, Version version)
        {
            Skip.IfNot(ValidateRuntime(path));

            await RunTypesTest(path, version, true, TargetPlatform.x86);
            await RunUnicodeTests(path, version, true, TargetPlatform.x86);
        }

        [SkippableTheory]
        [Trait(TestCategories.Category, Category)]
        [MemberData(nameof(AllEngines))]
        public async Task Types_OutOfProcess(string path, Version version)
        {
            Skip.IfNot(ValidateRuntime(path));

            var target = X64Engines.Any(x => x[0].Equals(path) && x[1].Equals(version))
                ? TargetPlatform.x64
                : TargetPlatform.x86;
            await RunTypesTest(path, version, false, target);
            await RunUnicodeTests(path, version, false, target);
        }

        #endregion Test cases

        #region Actual tests to run

        private async Task RunTypesTest(string path, Version version, bool inProcess, TargetPlatform target)
        {
            // init engine
            var engine = EngineProvider.Get(version, path, null, inProcess, target, true);
            await engine.Initialize(null, _ct);
            // load test script
            var pyScript = await engine.LoadScript(_typeTestScript, _ct);

            foreach (var typeInfo in _types)
            {
                var element = typeInfo.Value;
                object array = Array.CreateInstance(typeInfo.Key, 10);

                // invoke with simple type
                var resObj = await engine.InvokeMethod(pyScript, "dummy", new[] { element }, _ct);
                var simpleType = engine.Convert(resObj, typeInfo.Key);
                Assert.True(typeInfo.Key == simpleType.GetType());

                // invoke with array type
                resObj = await engine.InvokeMethod(pyScript, "dummy", new[] { array }, _ct);
                var arrayType = engine.Convert(resObj, array.GetType());
                Assert.True(array.GetType() == arrayType.GetType());
            }
            await engine.Release();
        }

        private async Task RunUnicodeTests(string path, Version version, bool inProcess, TargetPlatform target)
        {
            // init engine
            var engine = EngineProvider.Get(version, path, null, inProcess, target, true);
            await engine.Initialize(null, _ct);
            // load test script
            var pyScript = await engine.LoadScript(_unicodeTestScript, _ct);
            var resNoParam = await engine.InvokeMethod(pyScript, "test", null, _ct);
            Assert.Equal("´©Ãˆ§‰©ù¨ëéüÇïçâèàêÉîôû", engine.Convert(resNoParam, typeof(string)));
            await engine.Release();
        }

        private async Task RunBasicTest(string path, Version version, bool inProcess, TargetPlatform target)
        {
            var engine = EngineProvider.Get(version, path, null, inProcess, target, true);
            await engine.Initialize(null, _ct);

            await engine.Execute(_basicTestScript, _ct);

            var pyObj = await engine.LoadScript(_basicTestScript, _ct);

            const string param = "dummy";
            var resHello = await engine.InvokeMethod(pyObj, "hello", new object[] { param }, _ct);
            Assert.Equal("Hello " + param, engine.Convert(resHello, typeof(string)));

            var resFib = await engine.InvokeMethod(pyObj, "fib", new object[] { 10 }, _ct);
            Assert.Equal(55, engine.Convert(resFib, typeof(int)));

            var resArray = await engine.InvokeMethod(pyObj, "arr", new object[] { 10, 10 }, _ct);
            Assert.Equal(10, ((int[])engine.Convert(resArray, typeof(int[]))).Length);

            var resNoParam = await engine.InvokeMethod(pyObj, "no_param", null, _ct);
            Assert.Equal("no_param", engine.Convert(resNoParam, typeof(string)));

            await engine.Release();
        }

        private static bool ValidateRuntime(string path)
        {
            return Directory.Exists(path);
        }

        #endregion Actual tests to run
    }
}