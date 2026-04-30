using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.Python;
using UiPath.Python.Activities.API.Models;
using Xunit;

namespace UiPath.Python.Activities.API.Tests
{
    public class PythonOperationsTests : IDisposable
    {
        private readonly Mock<IEngine> _engineMock;
        private readonly IPythonScopeHandle _handle;

        public PythonOperationsTests()
        {
            _engineMock = new Mock<IEngine>();
            _handle = new PythonScopeHandle(_engineMock.Object);
        }

        public void Dispose() => _handle.Dispose();

        [Fact]
        public async Task RunCode_CallsEngineExecute()
        {
            const string code = "print('hello')";
            _engineMock.Setup(e => e.Execute(code, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await _handle.RunCode(code);

            _engineMock.Verify(e => e.Execute(code, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunScript_ReadsFileAndCallsEngineExecute()
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            const string code = "x = 1";
            try
            {
                File.WriteAllText(scriptPath, code);
                _engineMock.Setup(e => e.Execute(code, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

                await _handle.RunScript(scriptPath);

                _engineMock.Verify(e => e.Execute(code, It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                File.Delete(scriptPath);
            }
        }

        [Fact]
        public async Task LoadCode_CallsEngineLoadScript()
        {
            const string code = "def foo(): pass";
            var expected = new PythonObject(Guid.NewGuid());
            _engineMock.Setup(e => e.LoadScript(code, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await _handle.LoadCode(code);

            result.ShouldBeSameAs(expected);
            _engineMock.Verify(e => e.LoadScript(code, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoadScript_ReadsFileAndCallsEngineLoadScript()
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            const string code = "def foo(): pass";
            var expected = new PythonObject(Guid.NewGuid());
            try
            {
                File.WriteAllText(scriptPath, code);
                _engineMock.Setup(e => e.LoadScript(code, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

                var result = await _handle.LoadScript(scriptPath);

                result.ShouldBeSameAs(expected);
                _engineMock.Verify(e => e.LoadScript(code, It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                File.Delete(scriptPath);
            }
        }

        [Fact]
        public async Task InvokeMethod_CallsEngineInvokeMethod()
        {
            var instance = new PythonObject(Guid.NewGuid());
            const string methodName = "myMethod";
            var parameters = new List<object> { 42 };
            var expected = new PythonObject(Guid.NewGuid());
            _engineMock
                .Setup(e => e.InvokeMethod(instance, methodName, parameters, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _handle.InvokeMethod(instance, methodName, parameters);

            result.ShouldBeSameAs(expected);
            _engineMock.Verify(e => e.InvokeMethod(instance, methodName, parameters, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GetObject_CallsEngineConvert()
        {
            var pyObject = new PythonObject(Guid.NewGuid());
            _engineMock.Setup(e => e.Convert(pyObject, typeof(int))).Returns(42);

            var result = _handle.GetObject<int>(pyObject);

            result.ShouldBe(42);
            _engineMock.Verify(e => e.Convert(pyObject, typeof(int)), Times.Once);
        }

        [Fact]
        public void RunScript_NullHandle_ThrowsArgumentNullException()
        {
            IPythonScopeHandle nullHandle = null;
            Should.Throw<ArgumentNullException>(() => nullHandle.RunScript("script.py"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RunScript_InvalidScriptFile_ThrowsArgumentException(string scriptFile)
        {
            Should.Throw<ArgumentException>(() => _handle.RunScript(scriptFile));
        }

        [Fact]
        public void RunCode_NullHandle_ThrowsArgumentNullException()
        {
            IPythonScopeHandle nullHandle = null;
            Should.Throw<ArgumentNullException>(() => nullHandle.RunCode("print('x')"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RunCode_InvalidCode_ThrowsArgumentException(string code)
        {
            Should.Throw<ArgumentException>(() => _handle.RunCode(code));
        }

        [Fact]
        public void LoadScript_NullHandle_ThrowsArgumentNullException()
        {
            IPythonScopeHandle nullHandle = null;
            Should.Throw<ArgumentNullException>(() => nullHandle.LoadScript("script.py"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LoadScript_InvalidScriptFile_ThrowsArgumentException(string scriptFile)
        {
            Should.Throw<ArgumentException>(() => _handle.LoadScript(scriptFile));
        }

        [Fact]
        public void LoadCode_NullHandle_ThrowsArgumentNullException()
        {
            IPythonScopeHandle nullHandle = null;
            Should.Throw<ArgumentNullException>(() => nullHandle.LoadCode("def foo(): pass"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LoadCode_InvalidCode_ThrowsArgumentException(string code)
        {
            Should.Throw<ArgumentException>(() => _handle.LoadCode(code));
        }

        [Fact]
        public void InvokeMethod_NullHandle_ThrowsArgumentNullException()
        {
            IPythonScopeHandle nullHandle = null;
            var instance = new PythonObject(Guid.NewGuid());
            Should.Throw<ArgumentNullException>(() => nullHandle.InvokeMethod(instance, "myMethod"));
        }

        [Fact]
        public void InvokeMethod_NullInstance_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => _handle.InvokeMethod(null, "myMethod"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void InvokeMethod_InvalidMethodName_ThrowsArgumentException(string methodName)
        {
            var instance = new PythonObject(Guid.NewGuid());
            Should.Throw<ArgumentException>(() => _handle.InvokeMethod(instance, methodName));
        }

        [Fact]
        public void GetObject_NullHandle_ThrowsArgumentNullException()
        {
            IPythonScopeHandle nullHandle = null;
            var pyObject = new PythonObject(Guid.NewGuid());
            Should.Throw<ArgumentNullException>(() => nullHandle.GetObject<int>(pyObject));
        }

        [Fact]
        public void GetObject_NullPythonObject_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => _handle.GetObject<int>(null));
        }

        [Fact]
        public void Dispose_CallsEngineRelease()
        {
            _engineMock.Setup(e => e.Release()).Returns(Task.CompletedTask);

            _handle.Dispose();

            _engineMock.Verify(e => e.Release(), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_CallsEngineRelease()
        {
            _engineMock.Setup(e => e.Release()).Returns(Task.CompletedTask);

            await _handle.DisposeAsync();

            _engineMock.Verify(e => e.Release(), Times.Once);
        }
    }
}
