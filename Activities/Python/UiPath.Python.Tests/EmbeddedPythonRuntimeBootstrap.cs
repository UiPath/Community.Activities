using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace UiPath.Python.Tests
{
    public static class EmbeddedPythonRuntimeBootstrap
    {
        /// <summary>The embedded runtime versions; each has a python-{version}-embed-amd64.zip under 3rdparty.</summary>
        public const string Python314 = "3.14.5";
        public const string Python315 = "3.15.0";

        /// <summary>The runtime the tests use unless they ask for a specific one.</summary>
        public const string PythonVersion = Python314;

        public static string EnsureRuntimePath() => EnsureRuntimePath(PythonVersion);

        public static string EnsureRuntimePath(string pythonVersion)
        {
            var runtimeRoot = Path.Combine(Path.GetTempPath(), "pythons", pythonVersion);
            var lockFile = Path.Combine(runtimeRoot, ".setup.lock");
            var readyFile = Path.Combine(runtimeRoot, ".setup.ready");
            var inProgressFile = Path.Combine(runtimeRoot, ".setup.inprogress");

            Directory.CreateDirectory(runtimeRoot);

            using var lockStream = AcquireSetupLock(lockFile);

            if (IsReady(runtimeRoot, readyFile))
                return runtimeRoot;

            File.WriteAllText(inProgressFile, $"PID={Environment.ProcessId};UTC={DateTime.UtcNow:O}");
            var stagingDir = Path.Combine(Path.GetTempPath(), "pythons", $"staging-{pythonVersion}-{Guid.NewGuid():N}");

            try
            {
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);

                Directory.CreateDirectory(stagingDir);

                using (var zipStream = GetEmbeddedZipStream(pythonVersion))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false))
                {
                    archive.ExtractToDirectory(stagingDir, true);
                }

                CleanRuntimeRoot(runtimeRoot, lockFile);
                CopyDirectory(stagingDir, runtimeRoot);

                if (!HasExtractedRuntime(runtimeRoot))
                    throw new InvalidOperationException($"Extracted embedded Python runtime is invalid in '{runtimeRoot}'.");

                File.WriteAllText(readyFile, DateTime.UtcNow.ToString("O"));
            }
            finally
            {
                if (File.Exists(inProgressFile))
                    File.Delete(inProgressFile);

                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);
            }

            return runtimeRoot;
        }

        /// <summary>
        /// The folder name Python's own per-user site (e.g. %APPDATA%\Roaming\Python\PythonXY on
        /// Windows) resolves to for this embedded runtime's version, derived from
        /// <see cref="PythonVersion"/> so it can't drift out of sync when that's bumped.
        /// </summary>
        public static string UserSiteVersionFolder => GetUserSiteVersionFolder(PythonVersion);

        public static string GetUserSiteVersionFolder(string pythonVersion)
        {
            var parts = pythonVersion.Split('.');
            return $"Python{parts[0]}{parts[1]}";
        }

        public static string GetPythonLibraryPath(string runtimePath)
        {
            ArgumentNullException.ThrowIfNull(runtimePath);

            // The interpreter DLL is python3NN.dll. Later runtimes also ship python3t.dll (the
            // free-threaded stub), which sorts after it and must not be picked.
            var specificDll = Directory.EnumerateFiles(runtimePath, "python3*.dll", SearchOption.TopDirectoryOnly)
                .Where(file => Regex.IsMatch(Path.GetFileName(file), @"^python3\d+\.dll$", RegexOptions.IgnoreCase))
                .OrderBy(file => file)
                .LastOrDefault();

            var dll = specificDll
                ?? Directory.EnumerateFiles(runtimePath, "python3*.dll", SearchOption.TopDirectoryOnly)
                    .OrderBy(file => file)
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(dll))
                throw new FileNotFoundException($"No python3*.dll was found in '{runtimePath}'.", runtimePath);

            return dll;
        }

        private static bool IsReady(string runtimeRoot, string readyFile)
        {
            return File.Exists(Path.Combine(runtimeRoot, "python.exe")) && File.Exists(readyFile);
        }

        private static bool HasExtractedRuntime(string runtimeRoot)
        {
            return File.Exists(Path.Combine(runtimeRoot, "python.exe"));
        }

        private static FileStream AcquireSetupLock(string lockFile)
        {
            while (true)
            {
                try
                {
                    return new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    Thread.Sleep(200);
                }
            }
        }

        private static Stream GetEmbeddedZipStream(string pythonVersion)
        {
            var embeddedZipFileName = $"python-{pythonVersion}-embed-amd64.zip";
            var assembly = typeof(EmbeddedPythonRuntimeBootstrap).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(embeddedZipFileName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
                throw new FileNotFoundException($"Embedded resource '{embeddedZipFileName}' not found in test assembly.");

            return assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Unable to open embedded resource stream '{resourceName}'.");
        }

        private static void CleanRuntimeRoot(string runtimeRoot, string lockFile)
        {
            foreach (var file in Directory.EnumerateFiles(runtimeRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                if (string.Equals(name, Path.GetFileName(lockFile), StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Delete(file);
            }

            foreach (var dir in Directory.EnumerateDirectories(runtimeRoot, "*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(dir, true);
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, directory);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);
            }
        }
    }
}
