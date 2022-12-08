using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// Checks whether a certain directory exists on an FTP server. 
    /// </summary>
    [ViewModelClass(typeof(DirectoryExistsViewModel))]
    public partial class DirectoryExists
    {
    }
}

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class DirectoryExistsViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DirectoryExistsViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The path of the FTP directory in which to check whether the indicated directory exists.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// A boolean variable that states whether the indicated directory was found or not.
        /// </summary>
        public DesignOutArgument<bool> Exists { get; set; } = new DesignOutArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = propertyOrderIndex++;
            RemotePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Exists.IsPrincipal = false;
            Exists.OrderIndex = propertyOrderIndex++;
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
