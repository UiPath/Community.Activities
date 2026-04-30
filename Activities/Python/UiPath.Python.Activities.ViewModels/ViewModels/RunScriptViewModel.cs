using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

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

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            ScriptFile.OrderIndex = orderIndex++;
            Code.OrderIndex = orderIndex++;

            ScriptFile.DisplayName = Resources.ScriptFileNameDisplayName;
            ScriptFile.Tooltip = Resources.ScriptFileDescription;
            ScriptFile.Category = Resources.Input;
            ScriptFile.IsPrincipal = true;

            Code.DisplayName = Resources.CodeNameDisplayName;
            Code.Tooltip = Resources.CodeDescription;
            Code.Category = Resources.Input;
            Code.IsPrincipal = true;
        }

    }
}
