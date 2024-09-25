using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    namespace UiPath.FTP.Activities
    {
        /// <summary>
        /// Moves an item on an FTP server to a different remote path. 
        /// </summary>
        [ViewModelClass(typeof(MoveItemViewModel))]
        public partial class MoveItem
        {
        }
    }
}

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
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The remote path on the FTP server where the file will be moved.
        /// </summary>
        public DesignInArgument<string> NewPath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this box is checked, the files will be overwritten in the new remote directory if they're already stored there.
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = propertyOrderIndex++;
            RemotePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            NewPath.IsPrincipal = true;
            NewPath.OrderIndex = propertyOrderIndex++;
            NewPath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Overwrite.OrderIndex = propertyOrderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
        }

        protected override async ValueTask InitializeModelAsync()
        {
            await base.InitializeModelAsync();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
        }
    }
}
