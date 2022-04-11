using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// Removes a specified file from an FTP server.
    /// </summary>
    [ViewModelClass(typeof(DeleteViewModel))]
    public partial class Delete
    {
    }
}

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class DeleteViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DeleteViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The path of the file that is to be removed from the FTP server.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();

            RemotePath.IsPrincipal = true;
            RemotePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };
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
