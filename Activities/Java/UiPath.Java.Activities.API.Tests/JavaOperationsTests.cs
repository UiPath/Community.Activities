using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.Java;
using UiPath.Java.Activities.API.Models;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class JavaOperationsTests : IAsyncDisposable
    {
        private readonly Mock<IInvoker> _invokerMock;
        private readonly IJavaScopeHandle _handle;

        public JavaOperationsTests()
        {
            _invokerMock = new Mock<IInvoker>();
            _invokerMock.Setup(i => i.StopJavaService()).Returns(Task.CompletedTask);
            _handle = new JavaScopeHandle(_invokerMock.Object);
        }

        public async ValueTask DisposeAsync() => await _handle.DisposeAsync();

        [Fact]
        public async Task LoadJar_CallsInvokerLoadJar()
        {
            const string jarPath = "mylib.jar";
            _invokerMock.Setup(i => i.LoadJar(jarPath, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await _handle.LoadJar(jarPath);

            _invokerMock.Verify(i => i.LoadJar(jarPath, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InvokeMethod_CallsInvokerWithTargetObject()
        {
            var targetObject = new JavaObject();
            const string methodName = "toString";
            var expected = new JavaObject();
            _invokerMock
                .Setup(i => i.InvokeMethod(methodName, null, targetObject, It.IsAny<List<object>>(), It.IsAny<List<Type>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _handle.InvokeMethod(methodName, targetObject);

            result.ShouldBeSameAs(expected);
            _invokerMock.Verify(i => i.InvokeMethod(methodName, null, targetObject, It.IsAny<List<object>>(), It.IsAny<List<Type>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InvokeStaticMethod_CallsInvokerWithClassName()
        {
            const string methodName = "parseInt";
            const string className = "java.lang.Integer";
            var expected = new JavaObject();
            _invokerMock
                .Setup(i => i.InvokeMethod(methodName, className, null, It.IsAny<List<object>>(), It.IsAny<List<Type>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _handle.InvokeStaticMethod(methodName, className);

            result.ShouldBeSameAs(expected);
            _invokerMock.Verify(i => i.InvokeMethod(methodName, className, null, It.IsAny<List<object>>(), It.IsAny<List<Type>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateObject_CallsInvokerConstructor()
        {
            const string className = "java.lang.StringBuilder";
            var expected = new JavaObject();
            _invokerMock
                .Setup(i => i.InvokeConstructor(className, It.IsAny<List<object>>(), It.IsAny<List<Type>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _handle.CreateObject(className);

            result.ShouldBeSameAs(expected);
            _invokerMock.Verify(i => i.InvokeConstructor(className, It.IsAny<List<object>>(), It.IsAny<List<Type>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetField_CallsInvokerGetField()
        {
            var targetObject = new JavaObject();
            const string fieldName = "value";
            var expected = new JavaObject();
            _invokerMock
                .Setup(i => i.InvokeGetField(targetObject, fieldName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _handle.GetField(fieldName, targetObject);

            result.ShouldBeSameAs(expected);
            _invokerMock.Verify(i => i.InvokeGetField(targetObject, fieldName, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetStaticField_CallsInvokerGetFieldWithClassName()
        {
            const string fieldName = "MAX_VALUE";
            const string className = "java.lang.Integer";
            var expected = new JavaObject();
            _invokerMock
                .Setup(i => i.InvokeGetField(null, fieldName, className, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _handle.GetStaticField(fieldName, className);

            result.ShouldBeSameAs(expected);
            _invokerMock.Verify(i => i.InvokeGetField(null, fieldName, className, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InvokeMethod_NullTargetObject_Throws()
        {
            await Should.ThrowAsync<ArgumentNullException>(() => _handle.InvokeMethod("toString", (JavaObject)null));
        }

        [Fact]
        public async Task InvokeStaticMethod_NullClassName_Throws()
        {
            await Should.ThrowAsync<ArgumentException>(() => _handle.InvokeStaticMethod("parse", null));
        }

        [Fact]
        public async Task InvokeStaticMethod_EmptyClassName_Throws()
        {
            await Should.ThrowAsync<ArgumentException>(() => _handle.InvokeStaticMethod("parse", ""));
        }

        [Fact]
        public async Task CreateObject_NullClassName_Throws()
        {
            await Should.ThrowAsync<ArgumentException>(() => _handle.CreateObject(null));
        }

        [Fact]
        public async Task CreateObject_EmptyClassName_Throws()
        {
            await Should.ThrowAsync<ArgumentException>(() => _handle.CreateObject(""));
        }
    }
}
