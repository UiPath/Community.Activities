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

            if (!Path.IsPathRooted(path))
            {
                path = path
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);
            }

            return Path.GetFullPath(path);
        }
    }
}
