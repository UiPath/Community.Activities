using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

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
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// The path of the FTP directory in which to check whether the indicated file exists.
        /// </summary>
        public DesignOutArgument<bool> Exists { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = propertyOrderIndex++;

            Exists.OrderIndex = propertyOrderIndex;
        }
    }
}
