using Microsoft.CSharp.Activities;
using System.Text;
using UiPath.Shared.Activities.Services.Interfaces;

namespace UiPath.Shared.Activities.Services
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    internal class DefaultCSharpExpressionParser : ICSharpExpressionParser
    {
        public bool TryGetStringLiteral(CSharpValue<string> value, out string text)
        {
            text = null;
            var source = value?.ExpressionText?.Trim();

            if (string.IsNullOrEmpty(source))
                return true;

            // if first or last char is not double quotes => not a string
            if (source[0] != '"' || source[source.Length - 1] != '"')
                return false;

            var isEscaped = false;
            var builder = new StringBuilder();

            int i;
            for (i = 1; i < source.Length; i++)
            {
                var c = source[i];

                // if char is backslash and is not escaped, next char will be escaped
                if (c == '\\' && !isEscaped)
                {
                    isEscaped = true;
                    continue;
                }

                // if char is double quotes and is not escaped, the string has finished 
                if (c == '"' && !isEscaped)
                    break;

                isEscaped = false;
                builder.Append(c);
            }

            // if source is bigger than the value between 2 occurrences of double quotes => not a string
            if (i < source.Length - 1)
                return false;

            text = builder.ToString();
            return true;
        }
    }
}
