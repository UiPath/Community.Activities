using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UiPath.Python;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class PythonScopeViewModel : DesignPropertiesViewModel
    {
        public PythonScopeViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> Path { get; set; }

        public DesignInArgument<string> LibraryPath { get; set; }

        public DesignInArgument<string> WorkingFolder { get; set; }

        public DesignInArgument<double> OperationTimeout { get; set; }

        public DesignInArgument<int> ScriptDataSizeLimitMB { get; set; }

        public DesignProperty<bool> LogTraces { get; set; }

        [NotMappedProperty]
        public DesignProperty<string> InstalledVersions { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            InstalledVersions.OrderIndex = orderIndex++;
            Path.OrderIndex = orderIndex++;
            LibraryPath.OrderIndex = orderIndex++;
            WorkingFolder.OrderIndex = orderIndex++;
            OperationTimeout.OrderIndex = orderIndex++;
            ScriptDataSizeLimitMB.OrderIndex = orderIndex++;
            LogTraces.OrderIndex = orderIndex;

            Path.DisplayName = Resources.PathNameDisplayName;
            Path.Tooltip = Resources.PathDescription;
            Path.Category = Resources.Input;
            Path.IsRequired = true;
            Path.IsPrincipal = true;

            LibraryPath.DisplayName = Resources.LibraryPathNameDisplayName;
            LibraryPath.Tooltip = Resources.LibraryPathDescription;
            LibraryPath.Category = Resources.Input;
            LibraryPath.IsRequired = true;
            LibraryPath.IsPrincipal = true;

            WorkingFolder.DisplayName = Resources.WorkingFolder;
            WorkingFolder.Tooltip = Resources.WorkingFolderDescription;
            WorkingFolder.Category = Resources.Input;

            OperationTimeout.DisplayName = Resources.OperationTimeout;
            OperationTimeout.Tooltip = Resources.OperationTimeoutDescription;
            OperationTimeout.Category = Resources.Input;

            ScriptDataSizeLimitMB.DisplayName = Resources.ScriptDataSizeLimitDisplayName;
            ScriptDataSizeLimitMB.Tooltip = Resources.ScriptDataSizeLimitDescription;
            ScriptDataSizeLimitMB.Category = Resources.Input;

            LogTraces.DisplayName = Resources.LogTracesDisplayName;
            LogTraces.Tooltip = Resources.LogTracesDescription;
            LogTraces.Category = Resources.Input;
            LogTraces.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            InstalledVersions.IsPrincipal = true;
            InstalledVersions.DisplayName = Resources.InstalledVersionsDisplayName;
            InstalledVersions.Tooltip = Resources.InstalledVersionsDescription;
            InstalledVersions.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            //If no python installations are detected, show a disabled dropdown with a message indicating that no installations were found, instead of showing an empty dropdown which might be confusing.
            if (!GetInstalledPythonVersions().Any())
            {
                InstalledVersions.Placeholder = Resources.NoPythonInstallations;
                InstalledVersions.IsReadOnly = true;
            }
            else
            {
                InstalledVersions.DataSource = DataSourceBuilder<PythonInstallation>
                .WithId(v => v.Key)
                .WithLabel(v => $"{v.Version} - {v.InstallPath}")
                .WithSingleItemConverter(
                    itemToValue: item => item.Key,
                    valueToItem: value => GetInstalledPythonVersions().FirstOrDefault(v => v.Key == value))
                .WithData(GetInstalledPythonVersions().ToList())
                .Build();

                // Pre-select the dropdown entry whose InstallPath/LibraryPath match the values
                // already stored in the workflow, so reopening a configured scope shows the correct
                // installation without requiring the user to re-pick it.
                var matchingInstallation = GetInstalledPythonVersions().FirstOrDefault(v =>
                    string.Equals(v.InstallPath, GetLiteralValue(Path.Value), System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(v.LibraryPath, GetLiteralValue(LibraryPath.Value), System.StringComparison.OrdinalIgnoreCase));

                if (matchingInstallation is not null)
                    InstalledVersions.Value = matchingInstallation.Key;
            }
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(InstalledVersions), OnInstalledVersionsChanged, false);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(InstalledVersions, nameof(InstalledVersions.Value), nameof(InstalledVersions));
        }

        private void OnInstalledVersionsChanged()
        {
            var value = InstalledVersions.Value;
            if (string.IsNullOrEmpty(value))
                return;

            var installation = GetInstalledPythonVersions().FirstOrDefault(v => v.Key == value);
            if (installation is null)
                return;

            Path.Value = installation.InstallPath;
            LibraryPath.Value = installation.LibraryPath;
        }

        private sealed record PythonInstallation(string Version, string InstallPath, string? LibraryPath)
        {
            // Identity must include the install path: the same version can be installed in more
            // than one location, and keying on version alone would collapse them to a single entry.
            public string Key => $"{Version}|{InstallPath}";
        }

        private static readonly Regex _launcherLineRegex = new Regex(
            @"(?<major>\d+)\.(?<minor>\d+)(?:[\/\-](?<bits>\d+))?\s+\*?\s*(?<path>[A-Za-z]:\\.+)",
            RegexOptions.Compiled,
            matchTimeout: System.TimeSpan.FromSeconds(2));

        private static readonly System.Lazy<IReadOnlyList<PythonInstallation>> _installedPythonVersions =
            new System.Lazy<IReadOnlyList<PythonInstallation>>(BuildInstalledPythonVersions);

        private static IReadOnlyList<PythonInstallation> GetInstalledPythonVersions() => _installedPythonVersions.Value;

        private static IReadOnlyList<PythonInstallation> BuildInstalledPythonVersions()
        {
            try
            {
                var versions = new List<PythonInstallation>();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    CollectFromPythonLauncher(versions);

                return versions
                    .DistinctBy(v => v.Key)
                    .OrderByDescending(v => System.Version.TryParse(v.Version, out var parsed) ? parsed : new System.Version(0, 0))
                    .ThenBy(v => v.InstallPath)
                    .ToList();
            }
            catch
            {
                return [];
            }
        }

        private static void CollectFromPythonLauncher(List<PythonInstallation> versions)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "py",
                    ArgumentList = { "-0p" },
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                if (process is null)
                    return;

                // Synchronous reads on thread-pool threads avoid capturing the UI SynchronizationContext,
                // which would otherwise deadlock when blocking on .Result from the UI thread.
                var stdoutTask = Task.Run(() => process.StandardOutput.ReadToEnd());
                var stderrTask = Task.Run(() => process.StandardError.ReadToEnd());

                if (!Task.WhenAll(stdoutTask, stderrTask).Wait(System.TimeSpan.FromSeconds(3)))
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return;
                }

                var output = stdoutTask.Result + stderrTask.Result;
                foreach (var line in output.Split('\n'))
                {
                    try
                    {
                        if (TryParseLauncherLine(line) is { } installation)
                            versions.Add(installation);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>Returns the literal string wrapped by a <see cref="System.Activities.InArgument{T}"/>,
        /// or <see langword="null"/> when the argument holds an expression instead of a constant.</summary>
        private static string GetLiteralValue(System.Activities.InArgument<string> arg) =>
            (arg?.Expression as System.Activities.Expressions.Literal<string>)?.Value;

        private static PythonInstallation? TryParseLauncherLine(string line)
        {
            // Format (old): -3.11-64 *       C:\Python311\python.exe
            // Format (new): -V:3.11 *        C:\Program Files\Python311\python.exe
            var match = _launcherLineRegex.Match(line);
            if (!match.Success)
                return null;

            if (int.TryParse(match.Groups["major"].Value, out var major) &&
                int.TryParse(match.Groups["minor"].Value, out var minor) &&
                TryBuildVersionLabel(major, minor, out var label))
            {
                // Skip 32-bit installations — only 64-bit is supported.
                if (match.Groups["bits"].Value == "32")
                    return null;

                var installPath = System.IO.Path.GetDirectoryName(match.Groups["path"].Value.Trim());
                return new PythonInstallation(label, installPath, System.IO.Path.Combine(installPath, $"python{major}{minor}.dll"));
            }

            return null;
        }

        private static bool TryBuildVersionLabel(int major, int minor, out string label)
        {
            label = null;
            if (major != 3)
                return false;

            label = $"{major}.{minor}";
            return true;
        }
    }
}
