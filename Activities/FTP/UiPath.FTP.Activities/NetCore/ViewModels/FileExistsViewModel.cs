using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// Checks whether a certain file exists in the specified FTP directory. 
    /// </summary>
    [ViewModelClass(typeof(FileExistsViewModel))]
    public partial class FileExists
    {
    }
}

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class FileExistsViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public FileExistsViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// A boolean variable that states whether the indicated file was found or not.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The path of the FTP directory in which to check whether the indicated file exists.
        /// </summary>
        public DesignOutArgument<bool> Exists { get; set; } = new DesignOutArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = propertyOrderIndex++;
            RemotePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Exists.OrderIndex = propertyOrderIndex++;
            Exists.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };
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
