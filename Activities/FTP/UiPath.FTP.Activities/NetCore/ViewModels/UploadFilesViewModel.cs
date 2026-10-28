using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.FTP.Activities.Properties;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal class UploadFilesViewModel : BaseFtpViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public UploadFilesViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The local path of the files that are to be uploaded.
        /// </summary>
        public DesignInArgument<string> LocalPath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The path on the FTP server where the file is to be uploaded.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this box is checked, the folders will be uploaded with their respective subfolders.
        /// </summary>
        public DesignProperty<bool> Recursive { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// If this box is checked, the folder path will be created on the FTP server in case it does not already exist.
        /// </summary>
        public DesignProperty<bool> Create { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// If this box is checked, the files will be overwritten on the FTP server if they're already stored there.
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();

            int orderIndex = 1;

            // source first, then destination
            LocalPath.DisplayName = Resources.Activity_UploadFiles_Property_LocalPath_Name;
            LocalPath.Tooltip = Resources.Activity_UploadFiles_Property_LocalPath_Description;
            LocalPath.EditPlaceholder = Resources.Activity_UploadFiles_Property_LocalPath_Placeholder;
            LocalPath.IsRequired = true;
            LocalPath.IsPrincipal = true;
            LocalPath.OrderIndex = orderIndex++;
            LocalPath.Category = Resources.Input;

            RemotePath.DisplayName = Resources.Activity_UploadFiles_Property_RemotePath_Name;
            RemotePath.Tooltip = Resources.Activity_UploadFiles_Property_RemotePath_Description;
            RemotePath.EditPlaceholder = Resources.Activity_UploadFiles_Property_RemotePath_Placeholder;
            RemotePath.IsRequired = true;
            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = orderIndex++;
            RemotePath.Category = Resources.Input;

            Create.DisplayName = Resources.Activity_UploadFiles_Property_Create_Name;
            Create.Tooltip = Resources.Activity_UploadFiles_Property_Create_Description;
            Create.IsPrincipal = false;
            Create.OrderIndex = orderIndex++;
            Create.Category = Resources.Options;
            Create.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Recursive.DisplayName = Resources.Activity_UploadFiles_Property_Recursive_Name;
            Recursive.Tooltip = Resources.Activity_UploadFiles_Property_Recursive_Description;
            Recursive.IsPrincipal = false;
            Recursive.OrderIndex = orderIndex++;
            Recursive.Category = Resources.Options;
            Recursive.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Overwrite.DisplayName = Resources.Activity_UploadFiles_Property_Overwrite_Name;
            Overwrite.Tooltip = Resources.Activity_UploadFiles_Property_Overwrite_Description;
            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Options;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ConfigureContinueOnError(ref orderIndex);
        }
    }
}
