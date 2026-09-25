using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Platform.ResourceHandling;
using UiPath.Studio.Activities.Api;

namespace UiPath.Cryptography.Activities.Helpers
{
    /// <summary>
    /// Centralizes the LocalResource widget adoption pattern for STUD-80743 (epic ACTVCGD-134).
    /// Resolves whether the host supports a preferred widget and builds the matching
    /// <see cref="DefaultWidget"/> with a graceful fallback to a text box when the host
    /// has no ControlPlane / widget-support service.
    /// </summary>
    internal static class WidgetSupportHelper
    {
        /// <summary>
        /// Returns true when the host advertises support for <paramref name="widgetType"/>.
        /// Falls back to false (text box) whenever the design API or service is unavailable.
        /// </summary>
        public static bool IsWidgetSupported(IDesignServices services, string widgetType)
        {
            if (services is null)
            {
                return false;
            }

            try
            {
                var workflowDesignApi = services.GetService<IWorkflowDesignApi>();
                return workflowDesignApi?.HasFeature(DesignFeatureKeys.WidgetSupportInfoService) == true &&
                       workflowDesignApi.WidgetSupportInfoService?.IsWidgetSupported(widgetType) == true;
            }
            catch
            {
                // Default to fallback behavior if the service is unavailable.
                return false;
            }
        }

        /// <summary>
        /// Builds the LocalResource widget when supported, otherwise a Text widget.
        /// Every <c>string</c>-typed path property must pass <paramref name="applyNoWrap"/> =
        /// true, regardless of whether it has an <c>IResource</c>-typed sibling (Tier A) or not
        /// (Tier B, output-only) — otherwise the host wraps the picked file as
        /// <c>LocalResource.FromPath(...)</c>, which cannot be assigned to a string argument.
        /// Only the <c>IResource</c>/<c>ILocalResource</c>-typed properties themselves omit it
        /// (pass <paramref name="applyNoWrap"/> = false), since they need that wrap.
        /// </summary>
        public static DefaultWidget BuildLocalResourceWidget(bool isSupported, bool applyNoWrap)
        {
            if (!isSupported)
            {
                return new DefaultWidget { Type = ViewModelWidgetType.Text };
            }

            var widget = new DefaultWidget { Type = ViewModelWidgetType.LocalResource };
            if (applyNoWrap)
            {
                widget.Metadata["NoWrap"] = "true";
            }

            return widget;
        }

        /// <summary>
        /// Configures the LocalResource/NoWrap widgets shared by <c>EncryptFileViewModel</c> and
        /// <c>DecryptFileViewModel</c>, whose file/key path property sets are identical. Every
        /// <c>string</c>-typed property gets <c>applyNoWrap: true</c>; the paired
        /// <c>IResource</c>-typed properties get <c>applyNoWrap: false</c> — see
        /// <see cref="BuildLocalResourceWidget"/>.
        /// </summary>
        public static void ConfigureFilePathWidgets(
            IDesignServices services,
            DesignInArgument<string> inputFilePath, DesignInArgument<IResource> inputFile,
            DesignInArgument<string> publicKeyFilePath, DesignInArgument<IResource> publicKeyFile,
            DesignInArgument<string> privateKeyFilePath, DesignInArgument<IResource> privateKeyFile,
            DesignInArgument<string> outputFilePath, DesignInArgument<string> outputFileName)
        {
            bool isSupported = IsWidgetSupported(services, nameof(ViewModelWidgetType.LocalResource));

            inputFilePath.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: true);
            publicKeyFilePath.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: true);
            privateKeyFilePath.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: true);
            outputFilePath.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: true);
            outputFileName.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: true);

            inputFile.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: false);
            publicKeyFile.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: false);
            privateKeyFile.Widget = BuildLocalResourceWidget(isSupported, applyNoWrap: false);
        }
    }
}
