using Xunit;

namespace UiPath.Examples.Activities.Tests.Unit
{
    public class CalculatorUnitTests
    {
        [Theory]
        [InlineData(1, Operation.Add, 1, 2)]
        [InlineData(3, Operation.Subtract, 2, 1)]
        [InlineData(3, Operation.Multiply, 2, 6)]
        [InlineData(6, Operation.Divide, 2, 3)]
        public void Calculator_ReturnsAsExpected(int firstNumber, Operation operation, int secondNumber, int expectedResult)
        {
            var calculator = new Calculator()
            {
                SelectedOperation = operation
            };

            var result = calculator.ExecuteInternal(firstNumber, secondNumber);

            Assert.Equal(expectedResult, result);
        }
    }
}
