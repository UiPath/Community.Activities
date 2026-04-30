using System.Activities.DesignViewModels;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal class DeleteViewModel : BaseFtpViewModel
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
