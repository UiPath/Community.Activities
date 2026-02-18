using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UiPath.Python.Impl;
using UiPath.Python.Properties;

namespace UiPath.Python
{
    /// <summary>
    /// class providing the Python corresponding engine based on version/path
    /// </summary>
    public static class EngineProvider
    {
        private const string PythonHomeEnv = "PYTHONHOME";
        private static readonly string[] PythonExeWin = ["python.exe", "python3.exe"];
        private static readonly string[] PythonLinux = ["python", "python3"];
        private static readonly string[] PythonBinFolders = ["", "bin"];
        private const string PythonVersionArgument = "--version";

        // engines cache
        private static object _lock = new object();
        private static Dictionary<Version, IEngine> _cache = new Dictionary<Version, IEngine>();

        public static IEngine Get(Version version, string path, string libraryPath, bool inProcess = true, TargetPlatform target = TargetPlatform.x86, bool visible = false)
        {
            IEngine engine = null;
            lock (_lock)
            {
                if (string.IsNullOrEmpty(path))
                {
                    // read path from env variable
                    path = Environment.GetEnvironmentVariable(PythonHomeEnv);
                    Trace.TraceInformation($"Found Pyhton path {path}");
                }
                if (!version.IsValid())
                {
                    Autodetect(path, out version);
                    if (!version.IsValid())
                    {
                        throw new ArgumentException(Resources.DetectVersionException);
                    }
                }

                // TODO: target&visible are meaningless when running in-process (at least now), maybe it should be split
                if (inProcess)
                {
                    if (!_cache.TryGetValue(version, out engine))
                    {
                        engine = new Engine(version, path, libraryPath);
                    }
                    _cache[version] = engine;
                }
                else
                {
                    // TODO: do we need caching when running as service (out of process)?
                    engine = new OutOfProcessEngine(version, path, libraryPath, target, visible);
                }
            }
            return engine;
        }

        public static void Autodetect(string path, out Version version)
        {
            version = Version.Auto;
            Trace.TraceInformation($"Trying to autodetect Python version from path {path}");

            var exes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PythonExeWin : PythonLinux;
            var pythonCandidates = PythonBinFolders.SelectMany(folder => exes, Path.Combine).Select(p => Path.Combine(path, p)).Select(Path.GetFullPath).ToList();
            var existingFiles = pythonCandidates.Where(File.Exists).ToList();

            if (existingFiles.Count == 0)
            {
                throw new FileNotFoundException(Resources.PythonExeNotFoundException, string.Join(", ", pythonCandidates));
            }

            Dictionary<string, Exception> errors = new Dictionary<string, Exception>();
            bool detected = false;

            foreach (var python in existingFiles)
            {
                if (Autodetect(python, out version, out Exception ex))
                {
                    detected = true;
                    break;
                }
                else
                {
                    errors[python] = ex;
                }
            }

            // if we are here, it means that we found some candidates but all of them failed to be detected, so we throw an aggregate exception with all the details
            if (!detected)
            {
                throw new AggregateException(errors.Where(kv => kv.Value != null).Select(kv => new Exception($"{kv.Key}: {kv.Value.Message}", kv.Value)));
            }
        }

        private static bool Autodetect(string pythonFullPath, out Version version, out Exception exception)
        {
            version = Version.Auto;
            exception = null;
            try
            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = pythonFullPath,
                    Arguments = PythonVersionArgument,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                process.Start();
                // Now read the value, parse to int and add 1 (from the original script)
                string ver = process.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(ver))
                    ver = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                version = ver.GetVersionFromStr();
                return version != Version.Auto;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }
    }
}
