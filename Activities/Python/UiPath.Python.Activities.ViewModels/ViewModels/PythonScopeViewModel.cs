using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.Expressions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UiPath.Python;
using UiPath.Python.Activities;

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

        protected override void InitializeModel()
        {
            base.InitializeModel();
            DetectPaths();
            DetectLibrary();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(Version), DetectPaths, true);
            Rule(nameof(Path), DetectLibrary, true);
        }

        private void DetectPaths()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            if (HasValue(Path))
                return;

            var ver = Version.HasValue ? Version.Value : UiPath.Python.Version.Auto;
            var platform = TargetPlatform.HasValue ? TargetPlatform.Value : UiPath.Python.TargetPlatform.x64;

            var home = PythonScope.TryGetPythonHomeOnWindows(ver, platform);
            if (!string.IsNullOrWhiteSpace(home))
            {
                Path.Value = new InArgument<string>(home);

                if (!HasValue(LibraryPath))
                {
                    var lib = PythonScope.TryGetPythonLibraryPathOnWindows(home, ver);
                    if (!string.IsNullOrWhiteSpace(lib))
                        LibraryPath.Value = new InArgument<string>(lib);
                }
            }
        }

        private void DetectLibrary()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
            if (HasValue(LibraryPath))
                return;

            var lit = Path?.Value?.Expression as Literal<string>;
            var pathLiteral = lit?.Value;
            if (string.IsNullOrWhiteSpace(pathLiteral))
                return;

            var ver = Version.HasValue ? Version.Value : UiPath.Python.Version.Auto;
            var lib = PythonScope.TryGetPythonLibraryPathOnWindows(pathLiteral, ver);
            if (!string.IsNullOrWhiteSpace(lib))
                LibraryPath.Value = new InArgument<string>(lib);
        }

        private static bool HasValue(DesignInArgument<string> arg)
        {
            var expr = arg?.Value?.Expression;
            if (expr == null) return false;
            if (expr is Literal<string> lit) return !string.IsNullOrWhiteSpace(lit.Value);
            return true; 
        }
    }
}
