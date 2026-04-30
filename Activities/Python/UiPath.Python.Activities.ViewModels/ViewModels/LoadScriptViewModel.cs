using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Python;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

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

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            ScriptFile.OrderIndex = orderIndex++;
            Code.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex++;

            ScriptFile.DisplayName = Resources.ScriptFileNameDisplayName;
            ScriptFile.Tooltip = Resources.ScriptFileDescription;
            ScriptFile.Category = Resources.Input;
            ScriptFile.IsPrincipal = true;

            Code.DisplayName = Resources.CodeNameDisplayName;
            Code.Tooltip = Resources.CodeDescription;
            Code.Category = Resources.Input;
            Code.IsPrincipal = true;

            Result.DisplayName = Resources.ResultNameDisplayName;
            Result.Tooltip = Resources.ResultDescription;
            Result.Category = Resources.Output;
            Result.IsPrincipal = true;
        }

    }
}
