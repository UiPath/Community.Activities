using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Studio.Activities.Api;
using UiPath.Python;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class PythonScopeViewModel : DesignPropertiesViewModel
    {
        public PythonScopeViewModel(IDesignServices services) : base(services)
        {
            //Force dependency here for activities.api
            //Otherwise this assembly will be ignored by studio
            //see STUD-73622
            var _ = services.GetService<IWorkflowDesignApi>();
        }

        public DesignProperty<Version> Version { get; set; }

        public DesignInArgument<string> Path { get; set; }

        public DesignInArgument<string> LibraryPath { get; set; }

        public DesignProperty<TargetPlatform> TargetPlatform { get; set; }

        public DesignInArgument<string> WorkingFolder { get; set; }

        public DesignInArgument<double> OperationTimeout { get; set; }
    }
}
