using System;
using System.Collections.Generic;
using System.IO;

namespace UiPath.Python.Impl
{
    /// <summary>
    /// Shared by <see cref="Engine"/> (runs inside the process that calls Py_Initialize) and
    /// <see cref="OutOfProcessEngine"/> (runs in the calling process, before the host is spawned).
    /// </summary>
    internal static class VenvDetection
    {
        internal sealed record VenvInfo(string Root, string Home, bool IncludeSystemSitePackages, string Version)
        {
            /// <summary>
            /// Mirrors CPython's own site.py venv() function: it only forces
            /// ENABLE_USER_SITE = False for a venv created without --system-site-packages. A
            /// --system-site-packages venv leaves it to the normal (non-venv) computation, which is
            /// True in the typical case — so a real, natively-activated venv like that does *not*
            /// disable user-site.
            /// </summary>
            internal bool ShouldDisableUserSite => !IncludeSystemSitePackages;
        }

        // Real venvs put their launcher/executable folder directly under the venv root, named
        // exactly this — the same names EngineProvider looks for python.exe/python3 under.
        private static readonly string[] VenvBinFolderNames = ["Scripts", "bin"];

        /// <summary>
        /// Parses pyvenv.cfg at <paramref name="path"/>, if present. Requiring at least one of the
        /// keys a real venv config always has (home/version) avoids treating an unrelated file that
        /// merely happens to be named pyvenv.cfg as a venv.
        /// </summary>
        private static VenvInfo TryReadVenvConfig(string path)
        {
            var cfgFile = Path.Combine(path, "pyvenv.cfg");
            if (!File.Exists(cfgFile))
                return null;

            var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadLines(cfgFile))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                    kv[parts[0].Trim()] = parts[1].Trim();
            }

            if (!kv.ContainsKey("home") && !kv.ContainsKey("version"))
                return null;

            kv.TryGetValue("home", out var home);
            kv.TryGetValue("version", out var version);
            var includeSystemSitePackages = kv.TryGetValue("include-system-site-packages", out var include)
                && string.Equals(include, "true", StringComparison.OrdinalIgnoreCase);

            return new VenvInfo(path, home, includeSystemSitePackages, version);
        }

        /// <summary>
        /// Detects whether <paramref name="path"/> is a venv root, or its immediate Scripts/bin
        /// launcher folder — the only two layouts a real venv actually produces. Deliberately does
        /// not walk further up than that, and only accepts the one-level-up case when the
        /// intermediate folder is actually named Scripts/bin: a bare "is there a pyvenv.cfg within
        /// N ancestor levels" search (the original implementation) can mistake an unrelated,
        /// fully-standalone Python installation that merely happens to sit a level or two beneath
        /// someone else's venv for being inside it.
        /// </summary>
        internal static VenvInfo GetVenvInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var direct = TryReadVenvConfig(path);
            if (direct != null)
                return direct;

            var folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!Array.Exists(VenvBinFolderNames, name => string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase)))
                return null;

            return TryReadVenvConfig(Path.GetDirectoryName(path));
        }
    }
}
