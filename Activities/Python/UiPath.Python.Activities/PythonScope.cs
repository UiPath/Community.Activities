using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python.Activities.Properties;
using UiPath.Shared.Activities;

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
        [DefaultValue(Version.Auto)]
        public Version Version { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PathNameDisplayName))]
        [LocalizedDescription(nameof(Resources.PathDescription))]
        [DefaultValue(null)]
        public InArgument<string> Path { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TargetPlatformDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetPlatformDescription))]
        [DefaultValue(TargetPlatform.x86)]
        public TargetPlatform TargetPlatform { get; set; } = TargetPlatform.x86;

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.WorkingFolder))]
        [LocalizedDescription(nameof(Resources.WorkingFolderDescription))]
        [DefaultValue(null)]
        public InArgument<string> WorkingFolder { get; set; }


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
        #endregion

        private const string PythonEngineSessionProperty = "PythonEngineSessionProperty";
        private IEngine _pythonEngine = null;

        internal static IEngine GetPythonEngine(ActivityContext context)
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

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            string path = Path.Get(context);
            if (!path.IsNullOrEmpty() && !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(string.Format(Resources.InvalidPathException, path));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _pythonEngine = EngineProvider.Get(Version, path, !Isolated, TargetPlatform, ShowConsole);

            var workingFolder = WorkingFolder.Get(context);
            if (!workingFolder.IsNullOrEmpty())
            {
                var dir = new DirectoryInfo(workingFolder);
                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException(Resources.WorkingFolderPathInvalid);
                }
                workingFolder = dir.FullName; //we need to pass an absolute path to the python host
            }

            try
            {
                await _pythonEngine.Initialize(workingFolder, cancellationToken);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error initializing Python engine: {e.ToString()}");
                Cleanup();
                if (Version != Version.Auto)
                {
                    Version autodetected = Version.Auto;
                    EngineProvider.Autodetect(path, out autodetected);
                    if (autodetected != Version.Auto && autodetected != Version)
                        throw new InvalidOperationException(string.Format(Resources.InvalidVersionException, Version.ToString(), autodetected.ToString()));
                }
                throw new InvalidOperationException(Resources.PythonInitializeException, e);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return ctx =>
            {
                ctx.ScheduleAction(Body, _pythonEngine, OnCompleted, OnFaulted);
            };
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Cleanup();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _pythonEngine?.Release();
            _pythonEngine = null;
        }
    }
}
