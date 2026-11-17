using System;
using System.Activities;
using System.Activities.Expressions;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// Normalizes the value the LocalResource picker writes into a <c>string</c> path argument.
    /// </summary>
    internal static class LocalResourcePathNormalizer
    {
        private const string FromPathPrefix = "LocalResource.FromPath(";
        private const char Backslash = '\\';
        private const string DoubleBackslash = "\\\\";

        /// <summary>
        /// Studio 25.10.1-25.10.14 ignore the <c>NoWrap</c> metadata and save a <c>LocalResource.FromPath("...")</c> expression
        /// with doubled backslashes, which is an <c>IResource</c> and cannot be assigned to a string argument. This unwraps it to
        /// the plain path (kept as a quoted string literal when the host sent a quoted expression) and removes the doubled backslashes.
        /// Relative literal paths are written with <c>/</c> (valid on every OS) because Studio on Windows saves them with a backslash,
        /// which Linux/macOS would treat as part of the file name. Absolute paths, unquoted expressions and other values are left as they are.
        /// </summary>
        public static object Normalize(object value)
        {
            // Studio passes the widget result as an InArgument<string> (a Literal<string> or a text expression)
            if (value is not InArgument<string> argument)
            {
                return value;
            }

            var text = argument.Expression switch
            {
                Literal<string> literal => "\"" + literal.Value + "\"",
                ITextExpression expression => expression.ExpressionText,
                _ => null,
            };

            return NormalizeText(text) is string normalized
                   && normalized != text && normalized.Length >= 2 && normalized[0] == '"' && normalized[normalized.Length - 1] == '"'
                ? new InArgument<string>(new Literal<string>(normalized.Substring(1, normalized.Length - 2)))
                : value;
        }

        /// <summary>The string rules behind <see cref="Normalize"/>; returns the input itself when nothing applies.</summary>
        internal static string NormalizeText(string text)
        {
            if (text is null)
            {
                return null;
            }

            var trimmed = text.Trim();
            var unwrapped = false;
            if (trimmed.StartsWith(FromPathPrefix, StringComparison.Ordinal) && trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(FromPathPrefix.Length, trimmed.Length - FromPathPrefix.Length - 1).Trim();
                unwrapped = true;
            }

            var quoted = trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"';
            var path = quoted ? trimmed.Substring(1, trimmed.Length - 2) : trimmed;
            if (unwrapped)
            {
                path = path.Replace(DoubleBackslash, Backslash.ToString());
            }

            if (IsRelativeLiteral(path, quoted || unwrapped))
            {
                path = path.Replace(Backslash, '/');
            }
            else if (!unwrapped)
            {
                return text;
            }

            return quoted ? "\"" + path + "\"" : path;
        }

        // Only plain literals are rewritten; an unquoted expression (variable, concatenation, ...) is never touched.
        private static bool IsRelativeLiteral(string path, bool isLiteral)
        {
            if (!isLiteral || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (path.IndexOf('"') >= 0)
            {
                // embedded quote: part of an expression, not a plain path literal
                return false;
            }

            return !(path[0] == '/' || path[0] == Backslash ||
                     (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':'));
        }
    }
}
