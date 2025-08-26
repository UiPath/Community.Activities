using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Python;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class LoadScriptViewModel : DesignPropertiesViewModel
    {
        public LoadScriptViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> ScriptFile { get; set; }

        public DesignInArgument<string> Code { get; set; }

        public DesignOutArgument<PythonObject> Result { get; set; }
    }
}
