using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UiPath.Python.Impl;
using UiPath.Python.Properties;

namespace UiPath.Python
{
    /// <summary>
    /// class providing the Python corresponding engine based on version/path
    /// </summary>
    public static class EngineProvider
    {
        public const int DefaultPayloadThresholdMB = 25;
        public const int MinPayloadThresholdMB = 1;

        private const string PythonHomeEnv = "PYTHONHOME";
        private static readonly string[] PythonExeWin = ["python.exe", "python3.exe"];
        private static readonly string[] PythonLinux = ["python", "python3"];
        private static readonly string[] PythonBinFolders = ["", "bin", "Scripts"];
        private const string PythonVersionArgument = "--version";

        // engines cache
        private static object _lock = new object();
        private static Dictionary<Version, IEngine> _cache = new Dictionary<Version, IEngine>();

        public static IEngine Get(Version version, string path, string libraryPath, bool inProcess = true, TargetPlatform target = TargetPlatform.x64, bool visible = false, bool logTrace = false, int payloadThresholdMB = DefaultPayloadThresholdMB)
        {
            IEngine engine = null;
            lock (_lock)
            {
                if (string.IsNullOrEmpty(path))
                {
                    // read path from env variable
                    path = Environment.GetEnvironmentVariable(PythonHomeEnv);
                    Trace.TraceInformation($"Found Python path {path}");
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
                    engine = new OutOfProcessEngine(version, path, libraryPath, target, visible, logTrace, payloadThresholdMB);
                }
            }
            return engine;
        }

        public static void Autodetect(string path, out Version version)
        {
            version = Version.Auto;
            Trace.TraceInformation($"Trying to autodetect Python version from path {path}");

            var exes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PythonExeWin : PythonLinux;
            var pythonCandidates = PythonBinFolders.SelectMany(folder => exes, Path.Combine).Select(p => Path.Combine(path, p)).Distinct().Select(Path.GetFullPath).ToList();
            var existingFiles = pythonCandidates.Where(File.Exists).ToList();

            if (existingFiles.Count == 0)
            {
                throw new FileNotFoundException(Resources.PythonExeNotFoundException, string.Join(", ", pythonCandidates));
            }

            Dictionary<string, Exception> errors = new Dictionary<string, Exception>();
            bool versionDetected = false;

            foreach (var python in existingFiles)
            {
                if (Autodetect(python, out version, out Exception ex))
                {
                    versionDetected = true;
                    break;
                }
                else
                {
                    errors[python] = ex;
                }
            }

            // if we are here, it means that we found some candidates but all of them failed to be detected, so we throw an aggregate exception with all the details
            if (!versionDetected)
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
                using Process process = new Process();
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
                process.WaitForExit(3000);
                // Now read the value, parse to int and add 1 (from the original script)
                string ver = process.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(ver))
                    ver = process.StandardOutput.ReadToEnd();

                version = ver.GetVersionFromStr();
                return version != Version.Auto;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary>
        /// Validates a Python installation from both its home folder (<paramref name="path"/>, via the
        /// executable) and its dynamic library (<paramref name="libraryPath"/>, via binary inspection).
        /// Either input may be absent or invalid independently; each check no-ops when its input is
        /// missing and throws <see cref="PlatformNotSupportedException"/> / <see cref="NotSupportedException"/>
        /// when it finds a 32-bit or unsupported runtime. Also cross-checks a venv at <paramref name="path"/>
        /// against the version actually loaded from <paramref name="libraryPath"/> (see
        /// <see cref="ValidateVenvVersion"/>).
        /// </summary>
        public static void ValidateInstallation(string path, string libraryPath)
        {
            // Library first: it is the exact artifact pythonnet loads and the check is cheap (no spawn).
            ValidatePythonLibrary(libraryPath);
            ValidatePythonExecutable(path);
            ValidateVenvVersion(path, libraryPath);
        }

        /// <summary>
        /// If <paramref name="path"/> is a venv, cross-checks the Python version it was created with
        /// (from its pyvenv.cfg) against the version actually loaded from <paramref name="libraryPath"/>.
        /// A mismatch means the venv's site-packages — compiled for a different ABI — would end up on
        /// sys.path for a differently-versioned interpreter. Surfaced here, before any native
        /// initialization, rather than as a confusing failure deep inside the engine (e.g. a missing
        /// _sysconfigdata module, or subtly wrong stdlib behavior). No-ops when either version can't be
        /// determined — engine initialization will surface its own error in that case.
        /// </summary>
        private static void ValidateVenvVersion(string path, string libraryPath)
        {
            var venv = VenvDetection.GetVenvInfo(path);
            if (venv?.Version == null || !TryParseVenvVersion(venv.Version, out int venvMajor, out int venvMinor))
                return;

            if (!TryGetLibraryVersion(libraryPath, out int libMajor, out int libMinor))
                return;

            if (venvMajor != libMajor || venvMinor != libMinor)
                throw new NotSupportedException(
                    string.Format(Resources.PythonVenvVersionMismatchException,
                        venv.Root, $"{venvMajor}.{venvMinor}", $"{libMajor}.{libMinor}"));
        }

        /// <summary>
        /// Parses the "major.minor(.patch)" version string pyvenv.cfg's version key always carries.
        /// </summary>
        private static bool TryParseVenvVersion(string version, out int major, out int minor)
        {
            major = 0;
            minor = 0;
            var parts = version.Split('.');
            return parts.Length >= 2 && int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
        }

        /// <summary>
        /// Validates the Python installation at <paramref name="path"/> by running the Python
        /// executable. Throws <see cref="PlatformNotSupportedException"/> for 32-bit installations
        /// and <see cref="NotSupportedException"/> for unsupported Python versions.
        /// When no executable is found the method returns silently — downstream code will surface
        /// the appropriate error.
        /// </summary>
        public static void ValidatePythonExecutable(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var exes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PythonExeWin : PythonLinux;
            var pythonExe = PythonBinFolders
                .SelectMany(folder => exes, Path.Combine)
                .Select(p => Path.GetFullPath(Path.Combine(path, p)))
                .FirstOrDefault(File.Exists);

            if (pythonExe is null)
                return; // no exe found — let downstream produce a precise error

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = pythonExe,
                    Arguments = "-c \"import sys, struct; print(sys.version_info.major, sys.version_info.minor); print(struct.calcsize('P') * 8)\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                process.Start();

                // Read both streams asynchronously before waiting: if a redirected pipe buffer
                // fills while we block on WaitForExit, the child would deadlock.
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(5000))
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                    return; // timed out — let downstream produce its own error
                }

                var output = stdoutTask.GetAwaiter().GetResult().Trim();
                _ = stderrTask.GetAwaiter().GetResult(); // drain stderr to release the pipe
                // Split on both CR and LF so Windows CRLF output parses the same as Unix LF.
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                    return; // cannot parse — let downstream fail

                var versionParts = lines[0].Trim().Split(' ');
                if (versionParts.Length == 2
                    && int.TryParse(versionParts[0], out int major)
                    && int.TryParse(versionParts[1], out int minor))
                {
                    if (int.TryParse(lines[1].Trim(), out int bits) && bits == 32)
                        throw new PlatformNotSupportedException(
                            string.Format(Resources.Python32BitNotSupportedException, path));

                    if (!VersionExtensions.IsRuntimeVersionSupported(major, minor))
                        throw new NotSupportedException(
                            string.Format(Resources.PythonVersionNotSupportedException,
                                $"{major}.{minor}",
                                VersionExtensions.GetSupportedRuntimeVersionsDisplay()));
                }
            }
            catch (PlatformNotSupportedException) { throw; }
            catch (NotSupportedException) { throw; }
            catch
            {
                // If the process cannot be spawned or output cannot be read, skip validation
                // and let the engine initialization produce its own error.
            }
        }

        /// <summary>
        /// Validates the Python dynamic library at <paramref name="libraryPath"/> by inspecting the
        /// binary itself — no process is spawned. Throws <see cref="PlatformNotSupportedException"/>
        /// for 32-bit libraries and <see cref="NotSupportedException"/> for unsupported Python versions.
        /// When the file is missing or cannot be inspected the method returns silently — engine
        /// initialization will surface the appropriate error.
        /// </summary>
        public static void ValidatePythonLibrary(string libraryPath)
        {
            if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath))
                return;

            try
            {
                // Bitness comes from the binary header; the host is always 64-bit, so a 32-bit
                // library would otherwise fail the native load with an opaque BadImageFormatException.
                if (TryGetLibraryBitness(libraryPath, out int bits) && bits == 32)
                    throw new PlatformNotSupportedException(
                        string.Format(Resources.Python32BitNotSupportedException, libraryPath));

                // Version comes from the file name (pythonXY.dll / libpython3.Y.so), with the
                // file version resource as a fallback.
                if (TryGetLibraryVersion(libraryPath, out int major, out int minor)
                    && !VersionExtensions.IsRuntimeVersionSupported(major, minor))
                    throw new NotSupportedException(
                        string.Format(Resources.PythonVersionNotSupportedException,
                            $"{major}.{minor}",
                            VersionExtensions.GetSupportedRuntimeVersionsDisplay()));
            }
            catch (PlatformNotSupportedException) { throw; }
            catch (NotSupportedException) { throw; }
            catch
            {
                // Unable to inspect the library — let engine initialization produce its own error.
            }
        }

        /// <summary>
        /// Reads the target architecture of a native library from its binary header: the PE
        /// optional-header magic on Windows, the ELF class byte elsewhere. Returns 32 or 64.
        /// </summary>
        private static bool TryGetLibraryBitness(string libraryPath, out int bits)
        {
            bits = 0;
            using var stream = File.OpenRead(libraryPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Walk the PE header: DOS 'MZ' -> e_lfanew (0x3C) -> 'PE\0\0' -> COFF (20 bytes)
                // -> optional-header magic (0x10B = PE32/32-bit, 0x20B = PE32+/64-bit).
                using var reader = new BinaryReader(stream);
                if (stream.Length < 0x40 || reader.ReadUInt16() != 0x5A4D) // 'MZ'
                    return false;

                stream.Position = 0x3C;
                uint peOffset = reader.ReadUInt32();
                if (peOffset == 0 || peOffset + 4 + 20 + 2 > stream.Length)
                    return false;

                stream.Position = peOffset;
                if (reader.ReadUInt32() != 0x00004550) // 'PE\0\0'
                    return false;

                stream.Position = peOffset + 4 + 20; // skip the PE signature and COFF header
                ushort magic = reader.ReadUInt16();
                bits = magic == 0x20B ? 64 : magic == 0x10B ? 32 : 0;
                return bits != 0;
            }

            // ELF: magic 0x7F 'E' 'L' 'F' followed by EI_CLASS (1 = 32-bit, 2 = 64-bit).
            Span<byte> header = stackalloc byte[5];
            if (stream.Read(header) < header.Length)
                return false;
            if (header[0] != 0x7F || header[1] != (byte)'E' || header[2] != (byte)'L' || header[3] != (byte)'F')
                return false;

            bits = header[4] switch { 1 => 32, 2 => 64, _ => 0 };
            return bits != 0;
        }

        /// <summary>
        /// Derives the Python major.minor version from the library file name (pythonXY.dll or
        /// libpython3.Y.so), falling back to the file version resource for names that carry no
        /// minor version (e.g. the stable-ABI python3.dll).
        /// </summary>
        private static bool TryGetLibraryVersion(string libraryPath, out int major, out int minor)
        {
            major = 0;
            minor = 0;
            var fileName = Path.GetFileNameWithoutExtension(libraryPath);

            // Windows convention: pythonXY (e.g. python314 -> 3.14, python39 -> 3.9).
            var win = Regex.Match(fileName, @"^python(?<major>\d)(?<minor>\d{1,2})$", RegexOptions.IgnoreCase);
            if (win.Success
                && int.TryParse(win.Groups["major"].Value, out major)
                && int.TryParse(win.Groups["minor"].Value, out minor))
                return true;

            // Unix convention: libpython3.14 / python3.14.
            var nix = Regex.Match(fileName, @"python(?<major>\d+)\.(?<minor>\d+)", RegexOptions.IgnoreCase);
            if (nix.Success
                && int.TryParse(nix.Groups["major"].Value, out major)
                && int.TryParse(nix.Groups["minor"].Value, out minor))
                return true;

            // Fallback: the file version resource (Windows). Covers names without a minor version.
            try
            {
                var info = FileVersionInfo.GetVersionInfo(libraryPath);
                if (info.FileMajorPart > 0)
                {
                    major = info.FileMajorPart;
                    minor = info.FileMinorPart;
                    return true;
                }
            }
            catch { /* no usable version resource */ }

            return false;
        }
    }
}
