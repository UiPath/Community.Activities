using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

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
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// The local path for the files that are to be downloaded.
        /// </summary>
        public DesignInArgument<string> LocalPath { get; set; }

        /// <summary>
        /// If this box is checked, the folders will be downloaded with their respective subfolders.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; }

        /// <summary>
        /// If this box is checked, the folder path will be created locally in case it does not already exist.
        /// </summary>
        public DesignProperty<bool> Create { get; set; }

        /// <summary>
        /// If this box is checked, the files will be overwritten locally if they're already stored there.
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
