using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Studio.Activities.Api;

namespace UiPath.Activities.Python.ViewModels
{
    internal static class WidgetSupportHelper
    {
        internal const string PythonLanguage = "python";

        public static string CheckWidgetSupport(IDesignServices services, string preferredWidgetType)
        {
            ArgumentNullException.ThrowIfNull(services);

            var workflowDesignApi = services.GetService<IWorkflowDesignApi>();
            if (workflowDesignApi?.HasFeature(DesignFeatureKeys.WidgetSupportInfoService) == true &&
                workflowDesignApi.WidgetSupportInfoService?.IsWidgetSupported(preferredWidgetType) == true)
            {
                return preferredWidgetType;
            }

            return ViewModelWidgetType.Text;
        }
    }
}
