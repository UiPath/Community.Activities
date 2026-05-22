using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Python.Activities
{
    [LocalizedDisplayName(nameof(Resources.PythonScopeNameDisplayName))]
    [LocalizedDescription(nameof(Resources.PythonScopeDescription))]
    public class PythonScope : AsyncTaskNativeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.VersionNameDisplayName))]
        [LocalizedDescription(nameof(Resources.VersionDescription))]
        [TypeConverter(typeof(EnumTypeConverter))]
        [DefaultValue(Version.Auto)]
        public Version Version { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PathNameDisplayName))]
        [LocalizedDescription(nameof(Resources.PathDescription))]
        [DefaultValue(null)]
        public InArgument<string> Path { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.LibraryPathNameDisplayName))]
        [LocalizedDescription(nameof(Resources.LibraryPathDescription))]
        [DefaultValue(null)]
        public InArgument<string> LibraryPath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TargetPlatformDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetPlatformDescription))]
        [DefaultValue(TargetPlatform.x64)]
        public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.x64;

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.WorkingFolder))]
        [LocalizedDescription(nameof(Resources.WorkingFolderDescription))]
        [DefaultValue(null)]
        public InArgument<string> WorkingFolder { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.OperationTimeout))]
        [LocalizedDescription(nameof(Resources.OperationTimeoutDescription))]
        [DefaultValue(3600)]
        public InArgument<double> OperationTimeout { get; set; }

        [Browsable(false)]
        public ActivityAction<object> Body { get; set; }

        #region TODO: decide if these will be exposed

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(false)]
        public bool ShowConsole { get; set; } = false;

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(true)]
        public bool Isolated { get; set; } = true;

        #endregion TODO: decide if these will be exposed

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ScriptDataSizeLimitDisplayName))]
        [LocalizedDescription(nameof(Resources.ScriptDataSizeLimitDescription))]
        [DefaultValue(null)]
        public InArgument<int> ScriptDataSizeLimitMB { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.LogTracesDisplayName))]
        [LocalizedDescription(nameof(Resources.LogTracesDescription))]
        [DefaultValue(false)]
        public bool LogTraces { get; set; } = false;

        private const string PythonEngineSessionProperty = "PythonEngineSessionProperty";
        private IEngine _pythonEngine = null;

        internal static IEngine GetPythonEngine(System.Activities.ActivityContext context)
        {
            IEngine engine = context.DataContext.GetProperties()[PythonEngineSessionProperty]?.GetValue(context.DataContext) as IEngine;
            if (engine == null)
            {
                throw new InvalidOperationException(Resources.PythonEngineNotFoundException);
            }
            return engine;
        }

        public PythonScope()
        {
            Version = Version.Auto;
            Body = new ActivityAction<object>
            {
                Argument = new DelegateInArgument<object>(PythonEngineSessionProperty),
                Handler = new Sequence()
                {
                    DisplayName = Resources.Do
                }
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            if (!VersionExtensions.GetSupportedVersions().Contains(Version))
                metadata.AddValidationError(new ValidationError(Resources.ValidationErrorVersionUnsupported, false, nameof(Version)));
            if (Version == Version.Python_310 && TargetPlatform == TargetPlatform.x86)
                metadata.AddValidationError(new ValidationError(Resources.ValidationErrorPlatformUnsupported, false, nameof(Version)));
            if (ScriptDataSizeLimitMB?.Expression is Literal<int> literal && literal.Value < EngineProvider.MinPayloadThresholdMB)
                metadata.AddValidationError(new ValidationError(string.Format(Resources.ValidationErrorScriptDataSizeLimitInvalid, EngineProvider.MinPayloadThresholdMB), false, nameof(ScriptDataSizeLimitMB)));
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                string path = Path.Get(context);
                string libraryPath = LibraryPath.Get(context);
                
                // if the user supplied the full path to the Python executable instead of its folder, extract the folder
                if (!path.IsNullOrEmpty() && File.Exists(path))
                    path = System.IO.Path.GetDirectoryName(path);

                if (!path.IsNullOrEmpty() && !Directory.Exists(path))
                    throw new DirectoryNotFoundException(string.Format(Resources.InvalidPathException, path));

                cancellationToken.ThrowIfCancellationRequested();

                if (!VersionExtensions.GetSupportedVersions().Contains(Version))
                    throw new InvalidOperationException(Resources.ValidationErrorVersionUnsupported);

                int payloadThresholdMB = ScriptDataSizeLimitMB?.Expression != null ? ScriptDataSizeLimitMB.Get(context) : EngineProvider.DefaultPayloadThresholdMB;
                if (payloadThresholdMB < EngineProvider.MinPayloadThresholdMB)
                    throw new ArgumentException(string.Format(Resources.ValidationErrorScriptDataSizeLimitInvalid, EngineProvider.MinPayloadThresholdMB));

                // Resolve the version the engine will actually run with, before initializing
                // it, so version mismatches and the Python 3.10 library-path requirement can
                // be reported with clear errors instead of opaque native-load failures.
                var effectiveVersion = EngineProvider.ResolveEffectiveVersion(Version, path, out var autodetected);

                if (Version != Version.Auto && autodetected != Version.Auto)
                {
                    if (!VersionExtensions.GetSupportedVersions().Contains(autodetected))
                        throw new InvalidOperationException(Resources.ValidationErrorVersionUnsupported);
                    if (autodetected != Version)
                        throw new InvalidOperationException(string.Format(Resources.InvalidVersionException, Version.ToFriendlyString(), autodetected.ToFriendlyString()));
                }

                // Python 3.10 requires an explicit library file (python**.dll on Windows,
                // libpython*.so on Linux).
                if (effectiveVersion == Version.Python_310 && (libraryPath.IsNullOrEmpty() || !File.Exists(libraryPath)))
                    throw new FileNotFoundException(string.Format(Resources.InvalidLibraryPathException, libraryPath));

                if (effectiveVersion == Version.Python_310 && TargetPlatform == TargetPlatform.x86)
                    throw new InvalidOperationException(Resources.ValidationErrorPlatformUnsupported);

                _pythonEngine = EngineProvider.Get(effectiveVersion, path, libraryPath, !Isolated, TargetPlatform, ShowConsole, LogTraces, payloadThresholdMB);

                var workingFolder = WorkingFolder.Get(context);
                if (!workingFolder.IsNullOrEmpty())
                {
                    var dir = new DirectoryInfo(workingFolder);
                    if (!dir.Exists)
                        throw new DirectoryNotFoundException(Resources.WorkingFolderPathInvalid);

                    workingFolder = dir.FullName; //we need to pass an absolute path to the python host
                }

                var operationTimeout = OperationTimeout.Get(context);
                if (operationTimeout == 0)
                {
                    operationTimeout = 3600; //default to 1h for no values provided.
                }

                try
                {
                    await _pythonEngine.Initialize(workingFolder, cancellationToken, operationTimeout);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Error initializing Python engine: {e}");
                    Exception cleanupEx = null;
                    try
                    {
                        Cleanup();
                    }
                    catch (Exception cEx)
                    {
                        cleanupEx = cEx;
                    }

                    var innerEx = cleanupEx != null ? new AggregateException(e, cleanupEx) : e;
                    throw new InvalidOperationException(Resources.PythonInitializeException, innerEx);
                }

                cancellationToken.ThrowIfCancellationRequested();

                return ctx =>
                {
                    ctx.ScheduleAction(Body, _pythonEngine, OnCompleted, OnFaulted);
                };
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
#if ENABLE_DEFAULT_TELEMETRY
            ITelemetryOperationWrapper telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, faultContext);
            telemetryOperation?.SendWithException(propagatedException);
#endif
            faultContext.CancelChildren();
            Cleanup();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
#if ENABLE_DEFAULT_TELEMETRY
            ITelemetryOperationWrapper telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
            telemetryOperation?.Send();
#endif
            Cleanup();
        }

        private void Cleanup()
        {
            _pythonEngine?.Release();
            _pythonEngine = null;
        }
    }
}