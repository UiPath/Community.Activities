using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace UiPath.Python.Tests
{
    public static class EmbeddedPythonRuntimeBootstrap
    {
        private const string EmbeddedZipFileName = "python-3.14.5-embed-amd64.zip";
        private const string PythonVersion = "3.14.5";

        private static readonly string RuntimeRoot = Path.Combine(Path.GetTempPath(), "pythons", PythonVersion);
        private static readonly string LockFile = Path.Combine(RuntimeRoot, ".setup.lock");
        private static readonly string ReadyFile = Path.Combine(RuntimeRoot, ".setup.ready");
        private static readonly string InProgressFile = Path.Combine(RuntimeRoot, ".setup.inprogress");

        public static string EnsureRuntimePath()
        {
            Directory.CreateDirectory(RuntimeRoot);

            using var lockStream = AcquireSetupLock();

            if (IsReady())
                return RuntimeRoot;

            File.WriteAllText(InProgressFile, $"PID={Environment.ProcessId};UTC={DateTime.UtcNow:O}");
            var stagingDir = Path.Combine(Path.GetTempPath(), "pythons", $"staging-{PythonVersion}-{Guid.NewGuid():N}");

            try
            {
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);

                Directory.CreateDirectory(stagingDir);

                using (var zipStream = GetEmbeddedZipStream())
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false))
                {
                    archive.ExtractToDirectory(stagingDir, true);
                }

                CleanRuntimeRoot();
                CopyDirectory(stagingDir, RuntimeRoot);

                if (!HasExtractedRuntime())
                    throw new InvalidOperationException($"Extracted embedded Python runtime is invalid in '{RuntimeRoot}'.");

                File.WriteAllText(ReadyFile, DateTime.UtcNow.ToString("O"));
            }
            finally
            {
                if (File.Exists(InProgressFile))
                    File.Delete(InProgressFile);

                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);
            }

            return RuntimeRoot;
        }

        public static string GetPythonLibraryPath(string runtimePath)
        {
            ArgumentNullException.ThrowIfNull(runtimePath);

            var specificDll = Directory.EnumerateFiles(runtimePath, "python3*.dll", SearchOption.TopDirectoryOnly)
                .Where(file => !string.Equals(Path.GetFileName(file), "python3.dll", StringComparison.OrdinalIgnoreCase))
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

        private static bool IsReady()
        {
            return File.Exists(Path.Combine(RuntimeRoot, "python.exe")) && File.Exists(ReadyFile);
        }

        private static bool HasExtractedRuntime()
        {
            return File.Exists(Path.Combine(RuntimeRoot, "python.exe"));
        }

        private static FileStream AcquireSetupLock()
        {
            while (true)
            {
                try
                {
                    return new FileStream(LockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    Thread.Sleep(200);
                }
            }
        }

        private static Stream GetEmbeddedZipStream()
        {
            var assembly = typeof(EmbeddedPythonRuntimeBootstrap).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(EmbeddedZipFileName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
                throw new FileNotFoundException($"Embedded resource '{EmbeddedZipFileName}' not found in test assembly.");

            return assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Unable to open embedded resource stream '{resourceName}'.");
        }

        private static void CleanRuntimeRoot()
        {
            foreach (var file in Directory.EnumerateFiles(RuntimeRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                if (string.Equals(name, Path.GetFileName(LockFile), StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Delete(file);
            }

            foreach (var dir in Directory.EnumerateDirectories(RuntimeRoot, "*", SearchOption.TopDirectoryOnly))
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
