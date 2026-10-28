using System;
using UiPath.Python;

namespace UiPath.Python.Activities.API.Models
{
    /// <summary>
    /// Options for configuring a Python scope.
    /// </summary>
    public class PythonScopeOptions
    {
        /// <summary>
        /// Path to the Python installation directory.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Path to additional Python libraries.
        /// </summary>
        public string LibraryPath { get; set; }

        /// <summary>
        /// The Python version to use.
        /// </summary>
        /// <remarks>
        /// Obsolete: the Python version is detected automatically from the installation at
        /// <see cref="Path"/>/<see cref="LibraryPath"/>. Setting this property has no effect.
        /// </remarks>
        [Obsolete("Version is no longer used. The Python version is detected automatically from the installation at Path/LibraryPath.")]
        public Version Version { get; set; } = Version.Auto;

        /// <summary>
        /// The working folder for the Python process.
        /// </summary>
        public string WorkingFolder { get; set; }

        /// <summary>
        /// Maximum time to wait for Python operations to complete.
        /// When <c>null</c>, defaults to 1 hour.
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }

        /// <summary>
        /// The target CPU architecture for the Python engine.
        /// </summary>
        /// <remarks>
        /// Obsolete: only 64-bit execution is supported with direct pythonnet integration.
        /// Setting this property has no effect.
        /// </remarks>
        [Obsolete("TargetPlatform is no longer used. Only 64-bit execution is supported with direct pythonnet integration.")]
        public TargetPlatform Target { get; set; } = TargetPlatform.x64;

        /// <summary>
        /// When enabled, stdout and stderr output from the Python host process is written to a
        /// per-host log file under the folder resolved by
        /// <c>Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)</c>,
        /// in the <c>UiPath\Logs\python</c> subdirectory. Each log file is capped at 50 MB;
        /// once the cap is reached, no further output is written to that file. At most 128 log
        /// files are kept — the oldest are automatically deleted when a new file is created.
        /// The output is NOT forwarded to Orchestrator. Intended for local diagnosis only —
        /// leave disabled in production to avoid accumulating log files.
        /// </summary>
        public bool LogTraces { get; set; }

        /// <summary>
        /// Maximum size in megabytes of the request payload sent to the Python host process.
        /// When <c>null</c>, defaults to the engine default (<see cref="EngineProvider.DefaultPayloadThresholdMB"/>).
        /// Must be at least <see cref="EngineProvider.MinPayloadThresholdMB"/>.
        /// </summary>
        public int? ScriptDataSizeLimitMB { get; set; }
    }
}
