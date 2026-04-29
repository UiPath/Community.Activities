using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using UiPath.FTP;
using UiPath.FTP.Activities.API.Models;
using Xunit;

namespace UiPath.FTP.Activities.API.Tests
{
    public class FtpOperationsTests : IAsyncDisposable
    {
        private readonly Mock<IFtpSession> _sessionMock;
        private readonly IFtpScopeHandle _handle;

        public FtpOperationsTests()
        {
            _sessionMock = new Mock<IFtpSession>();
            _handle = new FtpScopeHandle(_sessionMock.Object);
        }

        public async ValueTask DisposeAsync() => await _handle.DisposeAsync();

        [Fact]
        public async Task DownloadFiles_CallsSessionDownloadAsync()
        {
            _sessionMock.Setup(s => s.DownloadAsync("remote/path", "local/path", false, false, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            await _handle.DownloadFiles("remote/path", "local/path");

            _sessionMock.Verify(s => s.DownloadAsync("remote/path", "local/path", false, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadFiles_CallsSessionUploadAsync()
        {
            _sessionMock.Setup(s => s.UploadAsync("local/path", "remote/path", true, false, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            await _handle.UploadFiles("local/path", "remote/path", overwrite: true);

            _sessionMock.Verify(s => s.UploadAsync("local/path", "remote/path", true, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_CallsSessionDeleteAsync()
        {
            _sessionMock.Setup(s => s.DeleteAsync("remote/path", It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            await _handle.Delete("remote/path");

            _sessionMock.Verify(s => s.DeleteAsync("remote/path", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void MoveItem_CallsSessionMove()
        {
            _handle.MoveItem("old/path", "new/path", overwrite: true);

            _sessionMock.Verify(s => s.Move("old/path", "new/path", true), Times.Once);
        }

        [Fact]
        public async Task FileExists_CallsSessionFileExistsAsync()
        {
            _sessionMock.Setup(s => s.FileExistsAsync("remote/file.txt", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            var result = await _handle.FileExists("remote/file.txt");

            result.ShouldBeTrue();
            _sessionMock.Verify(s => s.FileExistsAsync("remote/file.txt", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DirectoryExists_CallsSessionDirectoryExistsAsync()
        {
            _sessionMock.Setup(s => s.DirectoryExistsAsync("remote/dir", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            var result = await _handle.DirectoryExists("remote/dir");

            result.ShouldBeFalse();
            _sessionMock.Verify(s => s.DirectoryExistsAsync("remote/dir", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EnumerateObjects_CallsSessionEnumerateObjectsAsync()
        {
            var expected = new List<FtpObjectInfo> { new FtpObjectInfo { Name = "file.txt" } };
            _sessionMock.Setup(s => s.EnumerateObjectsAsync("remote/dir", true, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expected);

            var result = await _handle.EnumerateObjects("remote/dir", recursive: true);

            result.ShouldBeSameAs(expected);
            _sessionMock.Verify(s => s.EnumerateObjectsAsync("remote/dir", true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DownloadFiles_NullHandle_Throws()
        {
            IFtpScopeHandle nullHandle = null;

            await Should.ThrowAsync<ArgumentNullException>(() => nullHandle.DownloadFiles("r", "l"));
        }

        [Fact]
        public async Task UploadFiles_NullHandle_Throws()
        {
            IFtpScopeHandle nullHandle = null;

            await Should.ThrowAsync<ArgumentNullException>(() => nullHandle.UploadFiles("l", "r"));
        }
    }
}
