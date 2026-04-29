using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python;
using UiPath.Python.Activities.API.Models;
using UiPath.Robot.Activities.Api;

namespace UiPath.Python.Activities.API
{
    internal class PythonService : IPythonService
    {
        private readonly Func<Version, string, string, bool, TargetPlatform, bool, IEngine> _engineFactory;

        public PythonService(IExecutorRuntime executorRuntime)
            : this((version, path, libraryPath, x, target, y) => EngineProvider.Get(version, path, libraryPath, x, target, y))
        {
        }

        // For testing: allows injecting a fake IEngine without a real Python installation.
        internal PythonService(Func<Version, string, string, bool, TargetPlatform, bool, IEngine> engineFactory)
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
                throw new InvalidOperationException($"Python version '{options.Version}' is not supported.");

            if (options.OperationTimeout.HasValue && options.OperationTimeout.Value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options), "OperationTimeout must be non-negative.");

            string workingFolder = options.WorkingFolder;
            if (!string.IsNullOrWhiteSpace(workingFolder))
            {
                var dir = new DirectoryInfo(workingFolder);
                if (!dir.Exists)
                    throw new DirectoryNotFoundException($"Working folder not found: {workingFolder}");

                workingFolder = dir.FullName; // normalize to absolute path
            }

            var operationTimeout = (options.OperationTimeout ?? TimeSpan.FromHours(1)).TotalSeconds;

            IEngine engine = _engineFactory(options.Version, path, options.LibraryPath, false, options.Target, false);

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
