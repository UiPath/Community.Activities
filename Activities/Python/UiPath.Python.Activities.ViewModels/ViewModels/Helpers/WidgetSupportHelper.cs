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

        /// <summary>
        /// Widget feature advertised by hosts whose LocalResource widget honors the <c>SelectFolder</c> metadata.
        /// Passed as a string because the shared widget-feature constants do not define it yet.
        /// </summary>
        internal const string FolderSelectionFeature = "LocalResource.FolderSelection";

        public static bool IsWidgetFeatureSupported(IDesignServices services, string widgetFeature)
        {
            ArgumentNullException.ThrowIfNull(services);

            var workflowDesignApi = services.GetService<IWorkflowDesignApi>();
            return workflowDesignApi?.HasFeature(DesignFeatureKeys.WidgetSupportInfoService) == true &&
                   workflowDesignApi.WidgetSupportInfoService?.IsWidgetFeatureSupported(widgetFeature) == true;
        }

        /// <summary>
        /// Builds the LocalResource picker for a <c>string</c>-typed path argument when the host
        /// supports it, otherwise <c>null</c> so the property keeps the host's default widget for its type
        /// (the same widget it had before the picker was adopted). <c>NoWrap</c> makes the picker return the raw path
        /// instead of a <c>LocalResource.FromPath(...)</c> value, which cannot be assigned to a string argument.
        /// When <paramref name="selectFolder"/> is set the picker opens the folder dialog, but only on hosts that
        /// advertise <see cref="FolderSelectionFeature"/>; older hosts would show a file dialog for a folder
        /// argument, so they keep the default widget instead.
        /// </summary>
        public static DefaultWidget BuildLocalResourceWidget(IDesignServices services, bool selectFolder = false)
        {
            var widgetType = CheckWidgetSupport(services, ViewModelWidgetType.LocalResource);
            if (widgetType != ViewModelWidgetType.LocalResource ||
                (selectFolder && !IsWidgetFeatureSupported(services, FolderSelectionFeature)))
            {
                return null;
            }

            var widget = new DefaultWidget { Type = ViewModelWidgetType.LocalResource };
            widget.Metadata["NoWrap"] = bool.TrueString.ToLowerInvariant();
            if (selectFolder)
            {
                widget.Metadata["SelectFolder"] = bool.TrueString.ToLowerInvariant();
            }

            return widget;
        }

        /// <summary>
        /// Assigns the LocalResource picker to <paramref name="property"/> when the host supports it; otherwise leaves
        /// <c>Widget</c> untouched (assigning <c>null</c> explicitly is not equivalent to leaving it unset).
        /// </summary>
        public static void ApplyLocalResourceWidget(DesignProperty property, IDesignServices services, bool selectFolder = false)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (BuildLocalResourceWidget(services, selectFolder) is { } widget)
            {
                property.Widget = widget;
            }
        }
    }
}
