using Moq;
using Renci.SshNet.Common;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities;
using Xunit;

namespace UiPath.FTP.Tests.ActivitiesTests
{
    /// <summary>
    /// Shared test logic for activity-level "exists" tests (directory and file).
    /// Concrete subclasses supply the three things that differ: session mock setup,
    /// activity construction, and the method-level verify call.
    /// </summary>
    public abstract class ExistsActivityTestsBase
    {
        // --- Abstract seams ---

        protected abstract Mock<IFtpSession> CreateSessionMock(bool exists);

        protected abstract Mock<IFtpSession> CreateThrowingSessionMock(Exception exception);

        protected abstract Activity CreateExistsActivity(string remotePath, OutArgument<bool> exists);

        protected abstract void VerifySessionCall(Mock<IFtpSession> session, string expectedPath);

        // --- Shared tests (inherited by every concrete subclass) ---

        [Fact]
        public void Exists_ReturnsTrue_WhenSessionReturnsTrue()
        {
            var session = CreateSessionMock(exists: true);
            Assert.True(RunActivity(session, "/remote/path"));
        }

        [Fact]
        public void Exists_ReturnsFalse_WhenSessionReturnsFalse()
        {
            var session = CreateSessionMock(exists: false);
            Assert.False(RunActivity(session, "/remote/path"));
        }

        [Fact]
        public void Exists_CallsSession_WithCorrectPath()
        {
            var session = CreateSessionMock(exists: false);
            RunActivity(session, "/expected/path");
            VerifySessionCall(session, "/expected/path");
        }

        [Fact]
        public void Exists_PropagatesException_WhenSessionThrows()
        {
            var session = CreateThrowingSessionMock(new InvalidOperationException("session error"));

            var ex = Assert.ThrowsAny<Exception>(() => RunActivity(session, "/remote/path"));
            Assert.Contains(TestsHelper.Flatten(ex), e => e is InvalidOperationException);
        }

        [Fact]
        public void Exists_PropagatesSftpPathNotFoundException_WhenSessionThrows()
        {
            var session = CreateThrowingSessionMock(new SftpPathNotFoundException("No such file"));

            var ex = Assert.ThrowsAny<Exception>(() => RunActivity(session, "/remote/path"));
            Assert.Contains(TestsHelper.Flatten(ex), e => e is SftpPathNotFoundException);
        }

        // --- Shared helper ---

        protected bool RunActivity(Mock<IFtpSession> session, string remotePath)
        {
            var result = new bool[1];
            var resultVariable = new Variable<bool>("exists");
            var activity = new WithFtpSession(session.Object)
            {
                Host = new InArgument<string>("NonUsed"),
                Username = new InArgument<string>("NonUsed"),
                Password = new InArgument<string>("NonUsed"),
            };

            Assert.True(
                activity.Body.Handler is Sequence,
                "WithFtpSession.Body.Handler is no longer a Sequence; update the test scaffolding.");
            var seq = (Sequence)activity.Body.Handler;

            seq.Variables.Add(resultVariable);
            seq.Activities.Add(CreateExistsActivity(remotePath, new OutArgument<bool>(resultVariable)));
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

            WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(5));
            return result[0];
        }
    }
}
