using Microsoft.CSharp.Activities;

namespace UiPath.Shared.Activities.Services.Interfaces
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    public interface ICSharpExpressionParser
    {
        /// <summary>
        ///     Tries to retrieve the string value of the indicated <see cref="CSharpValue{String}"/> if the underlying expression does not use variables or any method calls.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="text"></param>
        /// <returns>
        ///     True if the conversion is successful; false otherwise
        /// </returns>
        bool TryGetStringLiteral(CSharpValue<string> value, out string text);
    }
}
