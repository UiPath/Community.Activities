using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.FTP.Activities.Properties;

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
        public DesignInArgument<string> RemotePath { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();
        }
    }
}
