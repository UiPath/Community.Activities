using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// Uploads a file to an FTP server. 
    /// </summary>
    [ViewModelClass(typeof(UploadFilesViewModel))]
    public partial class UploadFiles
    {
    }
}

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class UploadFilesViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public UploadFilesViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The local path of the files that are to be uploaded.
        /// </summary>
        public DesignInArgument<string> LocalPath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The path on the FTP server where the file is to be uploaded.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this box is checked, the folders will be uploaded with their respective subfolders.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// If this box is checked, the folder path will be created on the FTP server in case it does not already exist.
        /// </summary>
        public DesignProperty<bool> Create { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// If this box is checked, the files will be overwritten on the FTP server if they're already stored there.
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
