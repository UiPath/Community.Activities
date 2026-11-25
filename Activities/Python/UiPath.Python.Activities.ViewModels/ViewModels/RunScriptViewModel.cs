using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UiPath.Shared.ViewModels.Helpers;
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

        // Older Studio versions ignore NoWrap and would store LocalResource.FromPath("...") in a string argument;
        // unwrap it and keep relative paths portable ('/').
        public override async ValueTask<UpdateViewModelResult> UpdateAsync(string propertyName, object value)
        {
            if (propertyName == nameof(ScriptFile))
            {
                value = LocalResourcePathNormalizer.Normalize(value);
            }

            return await base.UpdateAsync(propertyName, value);
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            ScriptFile.OrderIndex = orderIndex++;
            Code.OrderIndex = orderIndex;

            ScriptFile.DisplayName = Resources.ScriptFileNameDisplayName;
            ScriptFile.Tooltip = Resources.ScriptFileDescription;
            ScriptFile.Category = Resources.Input;
            ScriptFile.IsPrincipal = true;
            WidgetSupportHelper.ApplyLocalResourceWidget(ScriptFile, Services);

            Code.DisplayName = Resources.CodeNameDisplayName;
            Code.Tooltip = Resources.CodeDescription;
            Code.Category = Resources.Input;
            Code.IsPrincipal = true;
            Code.Widget = new DefaultWidget
            {
                Type = WidgetSupportHelper.CheckWidgetSupport(Services, ViewModelWidgetType.CodeEditor),
                Metadata = new Dictionary<string, string>
                {
                    ["language"] = WidgetSupportHelper.PythonLanguage,
                    ["multiline"] = bool.TrueString.ToLowerInvariant(),
                },
            };
        }
    }
}
