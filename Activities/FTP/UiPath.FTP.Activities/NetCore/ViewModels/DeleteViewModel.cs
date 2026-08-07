using System.Activities.DesignViewModels;
using UiPath.FTP.Activities.Properties;

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
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();

            int orderIndex = 1;

            RemotePath.DisplayName = Resources.Activity_Delete_Property_RemotePath_Name;
            RemotePath.Tooltip = Resources.Activity_Delete_Property_RemotePath_Description;
            RemotePath.EditPlaceholder = Resources.Activity_Delete_Property_RemotePath_Placeholder;
            RemotePath.IsRequired = true;
            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = orderIndex++;
            RemotePath.Category = Resources.Input;

            ConfigureContinueOnError(ref orderIndex);
        }
    }
}
