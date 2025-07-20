using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class MoveItemViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public MoveItemViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The remote path on the FTP server where the file is currently located.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// The remote path on the FTP server where the file will be moved.
        /// </summary>
        public DesignInArgument<string> NewPath { get; set; }

        /// <summary>
        /// If this box is checked, the files will be overwritten in the new remote directory if they're already stored there.
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; }

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            RemotePath.OrderIndex = propertyOrderIndex++;
            NewPath.OrderIndex = propertyOrderIndex++;
            Overwrite.OrderIndex = propertyOrderIndex++;
            ContinueOnError.OrderIndex = propertyOrderIndex;

            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
        }
    }
}
