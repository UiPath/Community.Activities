using System.Activities;
using System.Diagnostics;
using UiPath.Examples.Activities.Helpers;

namespace UiPath.Examples.Activities
{
    public class Calculator : CodeActivity<int> // This base class exposes an OutArgument named Result
    {
        [RequiredArgument]
        public InArgument<int> FirstNumber { get; set; } //InArgument allows a varriable to be set from the workflow

        [RequiredArgument]
        public InArgument<int> SecondNumber { get; set; }

        [RequiredArgument]
        public Operation SelectedOperation { get; set; } = Operation.Multiply; // default value is optional

        /*
         * The returned value will be used to set the value of the Result argument
         */
        protected override int Execute(CodeActivityContext context)
        {
            // This is how you can log messages from your activity. logs are sent to the Robot which will forward them to Orchestrator
            context.GetExecutorRuntime().LogMessage(new Robot.Activities.Api.LogMessage()
            {
                EventType = TraceEventType.Information,
                Message = "Executing Calculator activity"
            });

            var fistNumber = FirstNumber.Get(context); //get the value from the workflow context (remember, this can be a variable)
            var secondNumber = SecondNumber.Get(context);

            if (secondNumber == 0 && SelectedOperation == Operation.Divide)
            {
                throw new DivideByZeroException("Second number should not be zero when the selected operation is divide");
            }

            return ExecuteInternal(fistNumber, secondNumber);
        }

        public int ExecuteInternal(int firstNumber, int secondNumber)
        {
            return SelectedOperation switch
            {
                Operation.Add => firstNumber + secondNumber,
                Operation.Subtract => firstNumber - secondNumber,
                Operation.Multiply => firstNumber * secondNumber,
                Operation.Divide => firstNumber / secondNumber,
                _ => throw new NotSupportedException("Operation not supported"),
            };
        }
    }

    public enum Operation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }
}
