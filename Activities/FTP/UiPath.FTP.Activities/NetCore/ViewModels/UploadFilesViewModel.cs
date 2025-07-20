using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

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
        public DesignInArgument<string> LocalPath { get; set; }

        /// <summary>
        /// The path on the FTP server where the file is to be uploaded.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// If this box is checked, the folders will be uploaded with their respective subfolders.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; }

        /// <summary>
        /// If this box is checked, the folder path will be created on the FTP server in case it does not already exist.
        /// </summary>
        public DesignProperty<bool> Create { get; set; }

        /// <summary>
        /// If this box is checked, the files will be overwritten on the FTP server if they're already stored there.
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            RemotePath.OrderIndex = propertyOrderIndex++;
            LocalPath.OrderIndex = propertyOrderIndex++;
            Create.OrderIndex = propertyOrderIndex++;
            Recursive.OrderIndex = propertyOrderIndex++;
            Overwrite.OrderIndex = propertyOrderIndex;
        }
    }
}
