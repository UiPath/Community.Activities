using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.FTP.Activities.Properties;

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
