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
        public PythonService(IExecutorRuntime executorRuntime)
        {
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

            IEngine engine = EngineProvider.Get(options.Version, path, options.LibraryPath, false, TargetPlatform.x64, false);

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
                    throw new InvalidOperationException("Failed to initialize Python engine.", new AggregateException(e, releaseEx));
                }

                throw new InvalidOperationException("Failed to initialize Python engine.", e);
            }

            return new PythonScopeHandle(engine);
        }
    }
}
