using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class RunScriptViewModel : DesignPropertiesViewModel
    {
        public RunScriptViewModel(IDesignServices services) : base(services)
        {  
        }

        public DesignInArgument<string> ScriptFile { get; set; }

        public DesignInArgument<string> Code { get; set; }
    }
}
