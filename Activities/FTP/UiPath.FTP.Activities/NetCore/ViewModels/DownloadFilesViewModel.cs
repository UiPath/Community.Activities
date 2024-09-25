using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// Downloads the specified files from an FTP server to the specified local folder.
    /// </summary>
    [ViewModelClass(typeof(DownloadFilesViewModel))]
    public partial class DownloadFiles
    {
    }
}

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class DownloadFilesViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DownloadFilesViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The path of the files on the FTP server that are to be downloaded.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The local path for the files that are to be downloaded.
        /// </summary>
        public DesignInArgument<string> LocalPath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this box is checked, the folders will be downloaded with their respective subfolders.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// If this box is checked, the folder path will be created locally in case it does not already exist.
        /// </summary>
        public DesignProperty<bool> Create { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// If this box is checked, the files will be overwritten locally if they're already stored there.
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = propertyOrderIndex++;
            RemotePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            LocalPath.IsPrincipal = true;
            LocalPath.OrderIndex = propertyOrderIndex++;
            LocalPath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Create.OrderIndex = propertyOrderIndex++;
            Create.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Recursive.OrderIndex = propertyOrderIndex++;
            Recursive.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Overwrite.OrderIndex = propertyOrderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
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
