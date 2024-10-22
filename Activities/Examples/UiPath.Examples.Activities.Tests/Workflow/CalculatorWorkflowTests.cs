using Moq;
using System.Activities;
using UiPath.Robot.Activities.Api;
using Xunit;

namespace UiPath.Examples.Activities.Tests.Workflow
{
    public class CalculatorWorkflowTests
    {
        readonly Mock<IExecutorRuntime> workflowRuntimeMock;

        public CalculatorWorkflowTests()
        {
            workflowRuntimeMock = new Mock<IExecutorRuntime>();
        }

        [Fact]
        public void Divide_ReturnsAsExpected()
        {
            var activity = new Calculator()
            {
                FirstNumber = 4,
                SecondNumber = 2,
                SelectedOperation = Operation.Divide
            };

            var runner = new WorkflowInvoker(activity);
            runner.Extensions.Add(() => workflowRuntimeMock.Object);

            var result = runner.Invoke(TimeSpan.FromSeconds(1)); //the runner will return a dictionary with the values of the OutArguments

            //verify that the result is as expected
            Assert.Equal(2, result["Result"]);

            //verify that we logged a message
            workflowRuntimeMock.Verify(x => x.LogMessage(It.IsAny<LogMessage>()), Times.Once);
        }

        [Fact]
        public void Divide_ThrowsWhenDividingByZero()
        {
            var activity = new Calculator()
            {
                FirstNumber = 4,
                SecondNumber = 0,
                SelectedOperation = Operation.Divide
            };

            var runner = new WorkflowInvoker(activity);
            runner.Extensions.Add(() => workflowRuntimeMock.Object);

            //verify that an exception is thrown when dividing by 0
            Assert.Throws<DivideByZeroException>(() => runner.Invoke(TimeSpan.FromSeconds(1)));
        }

    }
}
