using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UiPath.Shared.Activities
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    internal static class PathExtensions
    {
        /// <summary>
        /// Returns true if the given path does not contain any invalid characters, false otherwise.
        /// An empty path is considered invalid
        /// </summary>
        public static bool IsPathValid(this string path)
        {
            return !string.IsNullOrEmpty(path) && !PathContainsInvalidCharacters(path) || Uri.TryCreate(path, UriKind.Absolute, out _);
        }

        /// <summary>
        /// Given a string that represents a full path or part of a path, adjusts it so that
        /// it uses a valid separator for the current platform (e.g. Linux/Windows).
        /// </summary>
        /// <param name="originalPath"></param>
        /// <returns></returns>
        public static string AdjustPathSeparator(this string originalPath)
        {
            return originalPath?.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        public static bool PathContainsInvalidCharacters(this string path)
        {
            try
            {
                _ = Path.GetFullPath(path);

                var invalidChars = Path.GetInvalidPathChars();

                if (path.Any(x => invalidChars.Contains(x)))
                {
                    return true;
                }

                var fileInfo = new FileInfo(path);
                return fileInfo.Name.Any(x => Path.GetInvalidFileNameChars().Contains(x));
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (PathTooLongException)
            {
                return true;
            }
            catch (NotSupportedException)
            {
                return true;
            }
        }

        /// <summary>
        /// Attempts to parse the given expression as a path. If the parsing fails, returns null.
        /// Only valid expressions starting with string.Format can be paths.
        /// Returns true if the constant part of the string format doesn't contain invalid characters
        /// and none of the arguments is Environment.NewLine.
        /// Works with both VB and C# expressions.
        /// </summary>
        public static bool? IsExpressionValidPath(this string expression)
        {
            if (expression == null)
                return null;

            var re = new Regex(@"string.Format\(""(?<path>(\\""|[^""])+)""\s*(,\s*(?<args>.+)\)$|\)$)");
            var match = re.Match(expression);

            // not a string.Format expression, so there's nothing we can check
            if (!match.Success)
                return null;

            var path = match.Groups["path"].Value;
            var args = match.Groups["args"].Value;

            return IsPathValid(path) && !args.Contains("Environment.NewLine");
        }
    }
}
