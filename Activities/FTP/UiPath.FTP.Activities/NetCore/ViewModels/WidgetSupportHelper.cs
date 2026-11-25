using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Studio.Activities.Api;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal static class WidgetSupportHelper
    {
        private static string CheckWidgetSupport(IDesignServices services, string preferredWidgetType)
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
        /// Builds the LocalResource picker for a <c>string</c>-typed local path argument when the host
        /// supports it, otherwise <c>null</c> so the property keeps the host's default widget for its type
        /// (the same widget it had before the picker was adopted). <c>NoWrap</c> makes the picker return the raw path
        /// instead of a <c>LocalResource.FromPath(...)</c> value, which cannot be assigned to a string argument.
        /// </summary>
        public static DefaultWidget BuildLocalResourceWidget(IDesignServices services)
        {
            if (CheckWidgetSupport(services, ViewModelWidgetType.LocalResource) != ViewModelWidgetType.LocalResource)
            {
                return null;
            }

            var widget = new DefaultWidget { Type = ViewModelWidgetType.LocalResource };
            widget.Metadata["NoWrap"] = bool.TrueString.ToLowerInvariant();
            return widget;
        }

        /// <summary>
        /// Assigns the LocalResource picker to <paramref name="property"/> when the host supports it; otherwise leaves
        /// <c>Widget</c> untouched (assigning <c>null</c> explicitly is not equivalent to leaving it unset).
        /// </summary>
        public static void ApplyLocalResourceWidget(DesignProperty property, IDesignServices services)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (BuildLocalResourceWidget(services) is { } widget)
            {
                property.Widget = widget;
            }
        }
    }
}
