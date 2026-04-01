using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UiPath.Python;
using PythonTargetPlatform = UiPath.Python.TargetPlatform;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class PythonScopeViewModel : DesignPropertiesViewModel
    {
        public PythonScopeViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignProperty<Version> Version { get; set; }

        public DesignInArgument<string> Path { get; set; }

        public DesignInArgument<string> LibraryPath { get; set; }

        public DesignProperty<TargetPlatform> TargetPlatform { get; set; }

        public DesignInArgument<string> WorkingFolder { get; set; }

        public DesignInArgument<double> OperationTimeout { get; set; }

        [NotMappedProperty]
        public DesignProperty<string> InstalledVersions { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            InstalledVersions.OrderIndex = orderIndex++;
            Version.OrderIndex = orderIndex++;
            Path.OrderIndex = orderIndex++;
            LibraryPath.OrderIndex = orderIndex++;
            TargetPlatform.OrderIndex = orderIndex++;
            WorkingFolder.OrderIndex = orderIndex++;
            OperationTimeout.OrderIndex = orderIndex++;

            Version.DisplayName = Resources.VersionNameDisplayName;
            Version.Tooltip = Resources.VersionDescription;
            Version.Category = Resources.Input;
            Version.IsRequired = true;
            Version.DataSource = DataSourceBuilder<Version>
                .WithId(v => v.ToString())
                .WithLabel(v => v.ToFriendlyString())
                .WithSingleItemConverter(
                    itemToValue: item => item,
                    valueToItem: value => value)
                .WithData(VersionExtensions.GetSupportedVersion())
                .Build();

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

            TargetPlatform.DisplayName = Resources.TargetPlatformDisplayName;
            TargetPlatform.Tooltip = Resources.TargetPlatformDescription;
            TargetPlatform.Category = Resources.Input;

            WorkingFolder.DisplayName = Resources.WorkingFolder;
            WorkingFolder.Tooltip = Resources.WorkingFolderDescription;
            WorkingFolder.Category = Resources.Input;

            OperationTimeout.DisplayName = Resources.OperationTimeout;
            OperationTimeout.Tooltip = Resources.OperationTimeoutDescription;
            OperationTimeout.Category = Resources.Input;

            InstalledVersions.IsPrincipal = true;
            InstalledVersions.DisplayName = Resources.InstalledVersionsDisplayName;
            InstalledVersions.Tooltip = Resources.InstalledVersionsDescription;
            InstalledVersions.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };
            InstalledVersions.DataSource = DataSourceBuilder<PythonInstallation>
                .WithId(v => v.Key)
                .WithLabel(v => v.TargetPlatform is { } platform
                    ? $"{v.Version} ({platform}) - {v.InstallPath}"
                    : $"{v.Version} - {v.InstallPath}")
                .WithSingleItemConverter(
                    itemToValue: item => item.Key,
                    valueToItem: value => GetInstalledPythonVersions().FirstOrDefault(v => v.Key == value))
                .WithData(GetInstalledPythonVersions().ToList())
                .Build();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(InstalledVersions), OnInstalledVersionsChanged);
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
            TargetPlatform.Value = installation.TargetPlatform ?? PythonTargetPlatform.x64;
            Version.Value = UiPath.Python.Version.Auto; // Let the engine decide the exact version based on the library, but set to Auto to avoid mismatches
        }

        private sealed record PythonInstallation(string Version, string InstallPath, string? LibraryPath, PythonTargetPlatform? TargetPlatform)
        {
            public string Key => $"{Version}_{TargetPlatform}";
        }

        private static readonly Regex _launcherLineRegex = new Regex(
            @"(?<major>\d+)\.(?<minor>\d+)(?:[\/\-](?<bits>\d+))?\s+\*?\s*(?<path>[A-Za-z]:\\.+)",
            RegexOptions.Compiled);

        private static readonly System.Lazy<IReadOnlyList<PythonInstallation>> _installedPythonVersions =
            new System.Lazy<IReadOnlyList<PythonInstallation>>(BuildInstalledPythonVersions);

        private static IReadOnlyList<PythonInstallation> GetInstalledPythonVersions() => _installedPythonVersions.Value;

        private static IReadOnlyList<PythonInstallation> BuildInstalledPythonVersions()
        {
            var versions = new List<PythonInstallation>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CollectFromPythonLauncher(versions);

            return versions
                .DistinctBy(v => (v.Version, v.TargetPlatform))
                .OrderBy(v => v.Version)
                .ThenBy(v => v.TargetPlatform)
                .ToList();
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

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit(3000);

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
                var bitsValue = match.Groups["bits"].Value;
                PythonTargetPlatform? platform = bitsValue == "32" ? PythonTargetPlatform.x86 : bitsValue == "64" ? PythonTargetPlatform.x64 : null;
                var installPath = System.IO.Path.GetDirectoryName(match.Groups["path"].Value.Trim());
                return new PythonInstallation(label, installPath, System.IO.Path.Combine(installPath, $"python{major}{minor}.dll"), platform);
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
