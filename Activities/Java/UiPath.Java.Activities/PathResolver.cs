using System.IO;

namespace UiPath.Java.Activities
{
    internal static class PathResolver
    {
        /// <summary>
        /// Resolves a (possibly project-relative) path to an absolute one for the current OS.
        /// The LocalResource picker saves project-relative paths, and Studio on Windows writes them with
        /// <c>\</c>; on Linux/macOS a backslash is a valid file-name character, so relative paths are
        /// first normalized to the platform directory separator. Rooted paths are left as typed.
        /// Null, empty and whitespace values are returned unchanged so the caller keeps its own error handling.
        /// </summary>
        public static string ToFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            return Path.GetFullPath(NormalizeSeparators(path, Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Rewrites both separators of a relative path to <paramref name="separator"/>; rooted paths are returned as typed.
        /// The separator is a parameter so the Linux/macOS behavior can be tested on Windows.
        /// </summary>
        internal static string NormalizeSeparators(string path, char separator)
        {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            {
                return path;
            }

            return path.Replace('\\', separator).Replace('/', separator);
        }
    }
}
