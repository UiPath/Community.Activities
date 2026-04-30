using Moq;
using System;
using System.Activities;
using System.Threading;
using UiPath.FTP.Activities;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class WithFtpSessionTests
    {
        [Fact]
        public void WithFtpSession_MainScenarioTests()
        {
            var session = new Mock<IFtpSession>();
            var activity = new WithFtpSession(session.Object)
            {
                Host = new InArgument<string>("NonUsed"),
                Username = new InArgument<string>("NonUsed"),
                Password = new InArgument<string>("NonUsed"),
            };

            WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(5));

            //Check that connection and disposig API was called properly
            session.Verify(a => a.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            session.Verify(a => a.Close(), Times.Once);
            session.Verify(a => a.Dispose(), Times.Once);
        }
    }
}
