using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// Generates a DataTable variable that contains a list comprising the files found at the specified FTP server path.
    /// </summary>
    [ViewModelClass(typeof(EnumerateObjectsViewModel))]
    public partial class EnumerateObjects
    {
    }
}

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
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this check box is selected, the subfolders are also included in the enumeration of the files on the FTP server.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// A collection of files that have been found on the FTP server.
        /// </summary>
        public DesignOutArgument<System.Collections.Generic.IEnumerable<FtpObjectInfo>> Files { get; set; } = new DesignOutArgument<System.Collections.Generic.IEnumerable<FtpObjectInfo>>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = propertyOrderIndex++;
            RemotePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Files.OrderIndex = propertyOrderIndex++;
            Files.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Recursive.OrderIndex = propertyOrderIndex++;
            Recursive.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
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
