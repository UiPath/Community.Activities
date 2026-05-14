using System.Activities;
using System.Activities.DesignViewModels;
using UiPath.Studio.Activities.Api;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the JavaScope activity.
    /// </summary>
    class JavaScopeViewModel : DesignPropertiesViewModel
    {
        private readonly IWorkflowDesignApi _workflowDesignApi;
        public DesignInArgument<string> JavaPath { get; set; }
        public DesignInArgument<int> TimeoutMS { get; set; }
        public DesignProperty<ActivityAction<object>> Body { get; set; }

        public JavaScopeViewModel(IDesignServices services) : base(services)
        {
            // Intentionally storing the IWorkflowDesignApi for potential future use, even though it's not currently used in this ViewModel.
            // Actually this is a workaround to force the loading of the assembly (there is a known limitation https://uipath.atlassian.net/browse/STUD-73622)
            _workflowDesignApi = services.GetService<IWorkflowDesignApi>();
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            Body.OrderIndex = orderIndex++;
            JavaPath.OrderIndex = orderIndex++;
            TimeoutMS.OrderIndex = orderIndex++;

            JavaPath.DisplayName = Resources.JavaPathDisplayName;
            JavaPath.Tooltip = Resources.JavaPathDescription;
            JavaPath.Category = Resources.Input;

            TimeoutMS.DisplayName = Resources.TimeoutMSDisplayName;
            TimeoutMS.Tooltip = Resources.TimeoutMSDescription;
            TimeoutMS.Category = Resources.Input;

            Body.IsVisible = false;
        }
    }
}
