using System.Activities.DesignViewModels;
using UiPath.FTP.Activities.Properties;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal class FileExistsViewModel : BaseFtpViewModel
    {
        /// <summary>
        /// The path of the FTP directory in which to check whether the indicated file exists.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; }

        /// <summary>
        /// A boolean variable that states whether the indicated file was found or not.
        /// </summary>
        public DesignOutArgument<bool> Exists { get; set; }

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public FileExistsViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            int orderIndex = 1;

            RemotePath.DisplayName = Resources.Activity_FileExists_Property_RemotePath_Name;
            RemotePath.Tooltip = Resources.Activity_FileExists_Property_RemotePath_Description;
            RemotePath.EditPlaceholder = Resources.Activity_FileExists_Property_RemotePath_Placeholder;
            RemotePath.IsRequired = true;
            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = orderIndex++;
            RemotePath.Category = Resources.Input;

            ConfigureContinueOnError(ref orderIndex);

            // the output closes the property list, after the Options section
            Exists.DisplayName = Resources.Activity_FileExists_Property_Exists_Name;
            Exists.Tooltip = Resources.Activity_FileExists_Property_Exists_Description;
            Exists.IsPrincipal = false;
            Exists.OrderIndex = orderIndex;
            Exists.Category = Resources.Output;
        }
    }
}
