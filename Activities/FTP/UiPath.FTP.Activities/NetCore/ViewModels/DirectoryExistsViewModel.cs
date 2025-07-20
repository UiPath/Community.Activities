using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

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
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// A boolean variable that states whether the indicated directory was found or not.
        /// </summary>
        public DesignOutArgument<bool> Exists { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            RemotePath.OrderIndex = propertyOrderIndex++;
            Exists.OrderIndex = propertyOrderIndex;
        }
    }
}
