using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Buffers.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
            if(Version == Version.Python_310 && TargetPlatform == TargetPlatform.x86)
                metadata.AddValidationError(new ValidationError(Resources.ValidationErrorPlatformUnsupported, false, nameof(Version)));
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            string path = Path.Get(context);
            string libraryPath = LibraryPath.Get(context);
            if (!path.IsNullOrEmpty() && !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(string.Format(Resources.InvalidPathException, path));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _pythonEngine = EngineProvider.Get(Version, path, libraryPath, !Isolated, TargetPlatform, ShowConsole);

            if (_pythonEngine.Version == Version.Python_310 && TargetPlatform == TargetPlatform.x86)
                throw new InvalidOperationException(Resources.ValidationErrorPlatformUnsupported);

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
                Trace.TraceError($"Error initializing Python engine: {e.ToString()}");
                try
                {
                    Cleanup();
                }
                catch (Exception) { }
                if (Version != Version.Auto)
                {
                    Version autodetected = Version.Auto;
                    EngineProvider.Autodetect(path, out autodetected);
                    if (autodetected != Version.Auto && autodetected != Version)
                        throw new InvalidOperationException(string.Format(Resources.InvalidVersionException, Version.ToFriendlyString(), autodetected.ToFriendlyString()));
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

        public static string TryGetPythonHomeOnWindows(Version desiredVersion, TargetPlatform desiredPlatform)
        {
            try
            {
                // check if the OS is Windows or not
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return null;
                }
                Process p = null;

                //use the Python launcher to find the best matching version
                var psi = new ProcessStartInfo
                {
                    FileName = "py.exe",
                    Arguments = "-0p",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                try
                {
                    p = Process.Start(psi);
                }
                catch (Win32Exception)
                {
                    Trace.TraceWarning("Python launcher (py.exe) not found on PATH.");
                    return null;
                }

                //using var p = Process.Start(psi);
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var lines = output.Split(new[]
                { '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

                //parse the output to find the best matching version
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    var versionFlag = parts[0];
                    var pythonPath = parts[parts.Length - 1];

                    if (versionFlag.StartsWith("-") && versionFlag.Length > 1)
                    {
                        var flag = versionFlag.Substring(1); // Remove dash

                        if (flag.StartsWith("V:", StringComparison.OrdinalIgnoreCase))
                        {
                            flag = flag.Substring(2);
                        }
                        flag = flag.Trim();
                        
                        if (flag.EndsWith("*"))
                        {
                            flag = flag.TrimEnd('*');
                        }
                        if (flag.StartsWith(":"))
                        {
                            flag = flag.Substring(1);
                        }


                        // Determine platform
                        var platform = TargetPlatform.x64; // default
                        if (flag.EndsWith("-32"))
                        {
                            platform = TargetPlatform.x86;
                            flag = flag.Substring(0, flag.Length - 3);
                        }
                        else if (flag.EndsWith("-64"))
                        {
                            platform = TargetPlatform.x64;
                            flag = flag.Substring(0, flag.Length - 3);
                        }

                        // Parse version
                        if (System.Version.TryParse(flag, out var parsedVersion))
                        {
                            var version = parsedVersion.Major == 3 ? parsedVersion.Minor switch
                            {
                                6 => Version.Python_36,
                                7 => Version.Python_37,
                                8 => Version.Python_38,
                                9 => Version.Python_39,
                                10 => Version.Python_310,
                                11 => Version.Python_311,
                                12 => Version.Python_312,
                                13 => Version.Python_313,
                                _ => Version.Auto
                            } : Version.Auto;

                            // Check if this matches our criteria
                            if ((desiredVersion == Version.Auto || desiredVersion == version) &&
                                desiredPlatform == platform)
                            {
                                return System.IO.Path.GetDirectoryName(pythonPath);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Error trying to autodetect Python install: {ex}");
                return null;
            }
        }

        public static string TryGetPythonLibraryPathOnWindows(string pythonHome, Version desiredVersion)
        {
            if (string.IsNullOrEmpty(pythonHome))
                return null;

            // If Auto is specified, try to detect the actual version from the Python installation
            if (desiredVersion == Version.Auto)
            {
                try
                {
                    EngineProvider.Autodetect(pythonHome, out var detectedVersion);
                    if (detectedVersion != Version.Auto)
                    {
                        desiredVersion = detectedVersion;
                        System.Diagnostics.Debug.WriteLine($"Auto-detected version {detectedVersion} for library path");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Could not auto-detect version for library path");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to auto-detect version: {ex.Message}");
                    return null;
                }
            }

            string dllName = desiredVersion switch
            {
                Version.Python_36 => "python36.dll",
                Version.Python_37 => "python37.dll",
                Version.Python_38 => "python38.dll",
                Version.Python_39 => "python39.dll",
                Version.Python_310 => "python310.dll",
                Version.Python_311 => "python311.dll",
                Version.Python_312 => "python312.dll",
                Version.Python_313 => "python313.dll",
                _ => null
            };

            if (string.IsNullOrEmpty(dllName))
            {
                System.Diagnostics.Debug.WriteLine($"No DLL mapping for version {desiredVersion}");
                return null;
            }

            var libPath = System.IO.Path.Combine(pythonHome, dllName);
            if (File.Exists(libPath))
            {
                System.Diagnostics.Debug.WriteLine($"Found library at {libPath}");
                return libPath;
            }

            System.Diagnostics.Debug.WriteLine($"Library not found at {libPath}");
            return null;
        }
    }
}