using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security;
using UiPath.Shared.Activities.Services;

namespace UiPath.Shared.Activities
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    internal static class InArgumentExtensions
    {
        public static T? GetArgumentLiteralValue<T>(this InArgument<T> inArgument) where T : struct
        {
            return (inArgument?.Expression as Literal<T>)?.Value;
        }

        public static string GetArgumentLiteralValue(this InArgument<string> inArgument)
        {
            return (inArgument?.Expression as Literal<string>)?.Value;
        }

        public static T? GetInArgumentValue<T>(this InArgument<T> inArgument) where T : struct
        {
            if (inArgument?.Expression == null)
                return null;

            return inArgument.TryGetInArgumentValue(out var value) ? value : (T?)null;
        }

        public static string GetInArgumentValue(this InArgument<string> inArgument) =>
            inArgument.TryGetInArgumentValue(out var value) ? value : null;

        public static bool TryGetInArgumentValue<T>(this InArgument<T> inArgument, out T value) where T : struct
        {
            value = default(T);

            var expression = inArgument?.Expression;
            if (expression == null)
                return true;

            if (expression is Literal<T> asLiteral)
            {
                value = asLiteral.Value;
                return true;
            }

            if (expression is ITextExpression asTextExpression && TryGetExpressionValue(asTextExpression, out value))
                return true;

            return false;
        }

        public static bool TryGetInArgumentValue(this InArgument<string> inArgument, out string value)
        {
            value = null;

            var expression = inArgument?.Expression;
            if (expression == null)
                return true;

            if (expression is Literal<string> asLiteral)
            {
                value = asLiteral.Value;
                return true;
            }

            if (expression is ITextExpression asTextExpression && TryGetExpressionValue(asTextExpression, out value))
                return true;

            return false;
        }

        public static bool TryGetInArgumentValue(this InArgument<int?> inArgument, out int? value)
        {
            value = null;

            var expression = inArgument?.Expression;
            if (expression == null)
                return true;

            if (expression is Literal<int?> asLiteral)
            {
                value = asLiteral.Value;
                return true;
            }

            if (expression is VisualBasicValue<int?> asVisualBasicValue
                && Int32.TryParse(asVisualBasicValue.ExpressionText, out var intValue))
            {
                value = intValue;
                return true;
            }

            if (expression is CSharpValue<int?> asCSharpValue
                && Int32.TryParse(asCSharpValue.ExpressionText, out var cSharpIntValue))
            {
                value = cSharpIntValue;
                return true;
            }

            if (expression is ITextExpression asTextExpression && TryGetExpressionValue(asTextExpression, out value))
                return true;

            return false;
        }

        public static bool IsEmpty(this InArgument<string> inArgument)
        {
            // check if we have an expression
            var expression = inArgument?.Expression;
            if (expression == null)
                return true;

            // check if it's a literal
            if (expression is Literal<string> asLiteral)
                return string.IsNullOrWhiteSpace(asLiteral.Value);

            return false;
        }

        public static bool IsEmpty<T>(this InArgument<T> inArgument)
        {
            if (inArgument?.Expression == null)
                return true;

            if (inArgument.Expression is ITextExpression asTextExpression)
                return string.IsNullOrWhiteSpace(asTextExpression.ExpressionText);

            return false;
        }

        public static int GetAsMilliseconds(this InArgument<double> inArgument, ActivityContext context)
        {
            var seconds = inArgument?.Get(context) ?? 0;
            return (int)(seconds * 1000);
        }
        public static int GetAsMilliseconds(this InArgument<double> inArgument)
        {
            double seconds = 0;
            if (inArgument != null && inArgument.Expression != null)
            {
                seconds = (inArgument.Expression as Literal<double>).Value;
            }
            return (int)(seconds * 1000);
        }

        public static bool SetIfNull<T>(this InArgument<T> inArgument, ActivityContext context, T valueToSet)
        {
            if (inArgument == null)
                return false;
            if (inArgument.Expression != null)
                return false;

            inArgument.Set(context, valueToSet);
            return true;
        }

        public static string GetString(this InArgument<SecureString> inArgument, ActivityContext context, bool escapeSpecialKeys = false)
        {
            if (inArgument?.Expression != null)
            {
                var unmanagedString = IntPtr.Zero;
                try
                {
                    unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(inArgument.Get(context));
                    var text = Marshal.PtrToStringUni(unmanagedString);
                    if (escapeSpecialKeys)
                    {
                        const string specialKeyPrefix = "[";
                        const string specialKeyPrefixEscape = "[[";
                        return text?.Replace(specialKeyPrefix, specialKeyPrefixEscape);
                    }

                    return text;
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }

            return null;
        }

        /// <summary>
        /// For a string argument that represents a filesystem path, gets its value adjusted for the current platform,
        /// e.g. uses the correct path separator.
        /// </summary>
        /// <param name="pathArgument"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetAdjustedPath(this InArgument<string> pathArgument, ActivityContext context)
        {
            return pathArgument?.Get(context)?.AdjustPathSeparator();
        }

        /// <summary>
        /// Returns true if the given InArgument is a valid path.
        /// </summary>
        public static bool IsPathValid(this InArgument<string> pathArgument)
        {
            // First we check if the expression is a literal, in which case we just get the
            // constant string value and check if it's valid.
            if (pathArgument.Expression is Literal<string> literal)
            {
                return literal.Value?.IsPathValid() ?? true;
            }

            // Otherwise we are working with an expression, in which case we take the
            // ExpressionText and check if it's valid.
            if (pathArgument.Expression is ITextExpression expression)
            {
                return expression.ExpressionText?.IsExpressionValidPath() ?? true;
            }

            // other types of expressions are considered valid
            return true;
        }

        public static void AddPathValidationErrorIfNeeded(this InArgument<string> pathArgument, Action<string> addValidationError, string message)
        {
            if (pathArgument?.Expression != null && !pathArgument.IsPathValid())
            {
                addValidationError(message);
            }
        }

        private static bool TryGetExpressionValue(ITextExpression expression, out string value) =>
            (ArgumentFactoryHelper.ProjectLanguage == Language.CSharp ?
                ArgumentFactoryHelper.CSharpExpressionParser.TryGetStringLiteral(expression as CSharpValue<string>, out value) :
                TryGetExpressionValue<string>(expression, out value));

        private static bool TryGetExpressionValue<T>(ITextExpression expression, out T value)
        {
            value = default(T);
            if (expression == null)
                return false;

            try
            {
                if (!(expression.GetExpressionTree() is Expression<Func<ActivityContext, T>> expressionTree))
                    return false;

                var func = expressionTree.Compile();
                value = func(null);
                return true;
            }
            catch (Exception ex)
            {
                // do not trace this type of exception
                // We have to pass null as the ActivityContext at design time (the func(null) call).
                // Because of this a NullReferenceException is thrown when using variables in the expression.
                if (!(ex is InvalidOperationException || ex is NullReferenceException))
                    System.Diagnostics.Trace.TraceError(ex.ToString());

                return false;
            }
        }

        /// <summary>
        /// Get enum value. This is a best attempty effort if the argument is an expression.
        /// Note: This method cannot in all cases correctly parse an expression represention an enum value to an instance of that enum. This is a primitive implementation which relies on the expression text to be either the value of the enum or it's fully qualified name value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="argument"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetEnumValue<T>(this InArgument<T> argument, out T value) where T : struct
        {
            value = default(T);
            if (!typeof(T).IsEnum || argument == null)
                return false;

            if (argument.Expression is ITextExpression expression)
            {
                const char nsSeparator = '.';
                // the expression text might fully qualify the enum, or not
                return Enum.TryParse<T>(expression.ExpressionText.Split(nsSeparator).LastOrDefault(), out value);
            }

            if (argument.Expression is Literal<T> literal)
            {
                value = literal.Value;
                return true;
            }
            return false;
        }
    }
}
