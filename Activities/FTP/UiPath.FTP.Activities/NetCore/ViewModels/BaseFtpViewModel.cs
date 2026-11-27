using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.ViewModels.Helpers;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal abstract class BaseFtpViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public BaseFtpViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// Names of the <c>string</c> arguments that use the LocalResource picker (see <see cref="WidgetSupportHelper"/>).
        /// </summary>
        protected virtual IReadOnlyCollection<string> LocalResourcePathProperties => System.Array.Empty<string>();

        // Older Studio versions ignore NoWrap and would store LocalResource.FromPath("...") in a string argument;
        // unwrap it and keep relative paths portable ('/').
        public override async ValueTask<UpdateViewModelResult> UpdateAsync(string propertyName, object value)
        {
            if (propertyName != null && LocalResourcePathProperties.Contains(propertyName))
            {
                value = LocalResourcePathNormalizer.Normalize(value);
            }

            return await base.UpdateAsync(propertyName, value);
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();
        }

        /// <summary>
        /// Configures ContinueOnError as the closing property of the Options section. Every activity
        /// in the pack shares the same label, tooltip and widget for it, so it is configured once here.
        /// </summary>
        protected void ConfigureContinueOnError(ref int orderIndex)
        {
            ContinueOnError.DisplayName = Resources.Activity_WithFtpSession_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_WithFtpSession_Property_ContinueOnError_Description;
            ContinueOnError.IsPrincipal = false;
            ContinueOnError.IsRequired = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Category = Resources.Options;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
        }
    }
}
