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
        /// merely happens to be named pyvenv.cfg as a venv. Returns null (rather than throwing) when
        /// the file exists but can't be read — e.g. ACLs on a locked-down robot account, an AV
        /// sharing violation, or a concurrent pip rewrite — matching the "let engine initialization
        /// produce its own, precise error" convention every other validator on this path follows.
        /// </summary>
        private static VenvInfo TryReadVenvConfig(string path)
        {
            var cfgFile = Path.Combine(path, "pyvenv.cfg");
            if (!File.Exists(cfgFile))
                return null;

            var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var line in File.ReadLines(cfgFile))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        kv[parts[0].Trim()] = parts[1].Trim();
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            kv.TryGetValue("home", out var home);

            // CPython's stdlib venv writes "version"; virtualenv and uv write "version_info"
            // instead (e.g. "version_info = 3.10.4.final.0") — fall back to it so the venv/library
            // version cross-check (EngineProvider.ValidateVenvVersion) isn't silently skipped for
            // venvs created by those tools. TryParseVenvVersion only reads the first two dot-
            // separated parts, so the extra ".final.0" segments are harmless.
            if (!kv.TryGetValue("version", out var version))
                kv.TryGetValue("version_info", out version);

            if (home == null && version == null)
                return null;

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

            var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var folderName = Path.GetFileName(trimmedPath);
            if (!Array.Exists(VenvBinFolderNames, name => string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase)))
                return null;

            // Must derive the parent from the trimmed path: Path.GetDirectoryName on a
            // separator-terminated path (e.g. ".../venv/bin/") only strips the trailing separator
            // and returns the launcher folder itself, not its parent — which would silently miss
            // the venv root's pyvenv.cfg for any Path ending in a separator.
            return TryReadVenvConfig(Path.GetDirectoryName(trimmedPath));
        }
    }
}
