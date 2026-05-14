using Moq;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities;
using Xunit;

namespace UiPath.FTP.Tests.ActivitiesTests
{
    public class DirectoryExistsTests
    {
        [Fact]
        public void DirectoryExists_SetsExistsTrue_WhenSessionReturnsTrue()
        {
            var session = CreateSessionMock(exists: true);
            bool result = RunActivity(session, "/remote/dir");
            Assert.True(result);
        }

        [Fact]
        public void DirectoryExists_SetsExistsFalse_WhenSessionReturnsFalse()
        {
            var session = CreateSessionMock(exists: false);
            bool result = RunActivity(session, "/remote/dir");
            Assert.False(result);
        }

        [Fact]
        public void DirectoryExists_CallsDirectoryExistsAsync_WithCorrectPath()
        {
            var session = CreateSessionMock(exists: false);
            RunActivity(session, "/expected/path");
            session.Verify(
                s => s.DirectoryExistsAsync("/expected/path", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void DirectoryExists_PropagatesException_WhenSessionThrows()
        {
            var session = new Mock<IFtpSession>();
            session
                .Setup(s => s.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("session error"));

            var ex = Assert.ThrowsAny<Exception>(() => RunActivity(session, "/remote/dir"));
            Assert.Contains(TestsHelper.Flatten(ex), e => e is InvalidOperationException);
        }

        // --- Helpers ---

        private static Mock<IFtpSession> CreateSessionMock(bool exists)
        {
            var session = new Mock<IFtpSession>();
            session
                .Setup(s => s.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(exists));
            return session;
        }

        private static bool RunActivity(Mock<IFtpSession> session, string remotePath)
        {
            var result = new bool[1];
            var resultVariable = new Variable<bool>("exists");
            var activity = new WithFtpSession(session.Object)
            {
                Host = new InArgument<string>("NonUsed"),
                Username = new InArgument<string>("NonUsed"),
                Password = new InArgument<string>("NonUsed"),
            };

            if (activity.Body.Handler is Sequence seq)
            {
                seq.Variables.Add(resultVariable);
                seq.Activities.Add(new DirectoryExists
                {
                    RemotePath = new InArgument<string>(remotePath),
                    Exists = new OutArgument<bool>(resultVariable)
                });
                seq.Activities.Add(new InvokeMethod
                {
                    TargetType = typeof(TestsHelper),
                    MethodName = nameof(TestsHelper.CopyBool),
                    Parameters =
                    {
                        new InArgument<bool>(ctx => resultVariable.Get(ctx)),
                        new InArgument<bool[]>(ctx => result)
                    }
                });
            }

            WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(5));
            return result[0];
        }
    }
}
