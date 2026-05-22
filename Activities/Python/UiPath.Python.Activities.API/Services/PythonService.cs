using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python;
using UiPath.Python.Activities.API.Models;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

namespace UiPath.Python.Activities.API
{
    internal class PythonService : IPythonService
    {
        private readonly Func<Version, string, string, bool, TargetPlatform, bool, bool, int, IEngine> _engineFactory;

        public PythonService()
            : this((version, path, libraryPath, inProcess, target, visible, logTrace, payloadThresholdMB) =>
                EngineProvider.Get(version, path, libraryPath, inProcess, target, visible, logTrace, payloadThresholdMB))
        {
        }

        // For testing: allows injecting a fake IEngine without a real Python installation.
        internal PythonService(Func<Version, string, string, bool, TargetPlatform, bool, bool, int, IEngine> engineFactory)
        {
            _engineFactory = engineFactory;
        }

        public async Task<IPythonScopeHandle> UsePythonScope(PythonScopeOptions options, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            string path = options.Path;

            // if the user supplied the full path to the Python executable instead of its folder, extract the folder
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                path = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
                throw new DirectoryNotFoundException($"Python path not found: {path}");

            if (!VersionExtensions.GetSupportedVersions().Contains(options.Version))
                throw new InvalidOperationException(Resources.ValidationErrorVersionUnsupported);

            if (options.OperationTimeout.HasValue && options.OperationTimeout.Value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), "OperationTimeout must be non-negative.");

            if (options.ScriptDataSizeLimitMB.HasValue && options.ScriptDataSizeLimitMB.Value < EngineProvider.MinPayloadThresholdMB)
                throw new ArgumentOutOfRangeException(nameof(options), $"ScriptDataSizeLimitMB must be at least {EngineProvider.MinPayloadThresholdMB}.");

            string workingFolder = options.WorkingFolder;
            if (!string.IsNullOrWhiteSpace(workingFolder))
            {
                var dir = new DirectoryInfo(workingFolder);
                if (!dir.Exists)
                    throw new DirectoryNotFoundException($"Working folder not found: {workingFolder}");

                workingFolder = dir.FullName; // normalize to absolute path
            }

            var operationTimeout = (options.OperationTimeout ?? TimeSpan.FromHours(1)).TotalSeconds;

            // Resolve the version the engine will actually run with, before initializing it,
            // so version mismatches and the Python 3.10 library-path requirement can be
            // reported with clear errors instead of opaque native-load failures.
            var effectiveVersion = EngineProvider.ResolveEffectiveVersion(options.Version, path, out var autodetected);

            if (options.Version != Version.Auto && autodetected != Version.Auto)
            {
                if (!VersionExtensions.GetSupportedVersions().Contains(autodetected))
                    throw new InvalidOperationException(Resources.ValidationErrorVersionUnsupported);
                if (autodetected != options.Version)
                    throw new InvalidOperationException(
                        string.Format(Resources.InvalidVersionException, options.Version.ToFriendlyString(), autodetected.ToFriendlyString()));
            }

            // Python 3.10 requires an explicit library file (python**.dll on Windows,
            // libpython*.so on Linux).
            if (effectiveVersion == Version.Python_310 &&
                (string.IsNullOrWhiteSpace(options.LibraryPath) || !File.Exists(options.LibraryPath)))
            {
                throw new FileNotFoundException(string.Format(Resources.InvalidLibraryPathException, options.LibraryPath));
            }

            int payloadThresholdMB = options.ScriptDataSizeLimitMB ?? EngineProvider.DefaultPayloadThresholdMB;
            IEngine engine = _engineFactory(options.Version, path, options.LibraryPath, false, options.Target, false, options.LogTraces, payloadThresholdMB);

            try
            {
                await engine.Initialize(workingFolder, ct, operationTimeout);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error initializing Python engine: {e}");
                try
                {
                    await engine.Release();
                }
                catch (Exception releaseEx)
                {
                    // Release failed — record as secondary context so the original exception type is preserved.
                    e.Data["ReleaseException"] = releaseEx.ToString();
                }

                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw();
                throw; // unreachable — satisfies the compiler
            }

            return new PythonScopeHandle(engine);
        }
    }
}
