using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

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
        public DesignInArgument<bool> ContinueOnError { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
        }
    }
}
