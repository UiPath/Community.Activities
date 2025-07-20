using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;


namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class EnumerateObjectsViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public EnumerateObjectsViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The path of the directory on the FTP server whose files are to be enumerated.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// If this check box is selected, the subfolders are also included in the enumeration of the files on the FTP server.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; }

        /// <summary>
        /// A collection of files that have been found on the FTP server.
        /// </summary>
        public DesignOutArgument<System.Collections.Generic.IEnumerable<FtpObjectInfo>> Files { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            RemotePath.OrderIndex = propertyOrderIndex++;
            Files.OrderIndex = propertyOrderIndex++;
            Recursive.OrderIndex = propertyOrderIndex;

            Recursive.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
        }
    }
}
