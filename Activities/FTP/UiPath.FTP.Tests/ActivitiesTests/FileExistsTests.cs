using Moq;
using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities;

namespace UiPath.FTP.Tests.ActivitiesTests
{
    public class FileExistsTests : ExistsActivityTestsBase
    {
        protected override Mock<IFtpSession> CreateSessionMock(bool exists)
        {
            var session = new Mock<IFtpSession>();
            session
                .Setup(s => s.FileExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(exists));
            return session;
        }

        protected override Mock<IFtpSession> CreateThrowingSessionMock(Exception exception)
        {
            var session = new Mock<IFtpSession>();
            session
                .Setup(s => s.FileExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            return session;
        }

        protected override Activity CreateExistsActivity(string remotePath, OutArgument<bool> exists)
            => new FileExists
            {
                RemotePath = new InArgument<string>(remotePath),
                Exists = exists
            };

        protected override void VerifySessionCall(Mock<IFtpSession> session, string expectedPath)
            => session.Verify(
                s => s.FileExistsAsync(expectedPath, It.IsAny<CancellationToken>()),
                Moq.Times.Once);
    }
}
