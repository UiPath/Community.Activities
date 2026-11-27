using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.FTP.Activities.Properties;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal class MoveItemViewModel : BaseFtpViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public MoveItemViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The remote path on the FTP server where the file is currently located.
        /// </summary>
        public DesignInArgument<string> RemotePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The remote path on the FTP server where the file will be moved.
        /// </summary>
        public DesignInArgument<string> NewPath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If this box is checked, the files will be overwritten in the new remote directory if they're already stored there.
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();

            int orderIndex = 1;

            // source first, then destination
            RemotePath.DisplayName = Resources.Activity_MoveItem_Property_RemotePath_Name;
            RemotePath.Tooltip = Resources.Activity_MoveItem_Property_RemotePath_Description;
            RemotePath.EditPlaceholder = Resources.Activity_MoveItem_Property_RemotePath_Placeholder;
            RemotePath.IsRequired = true;
            RemotePath.IsPrincipal = true;
            RemotePath.OrderIndex = orderIndex++;
            RemotePath.Category = Resources.Input;

            NewPath.DisplayName = Resources.Activity_MoveItem_Property_NewPath_Name;
            NewPath.Tooltip = Resources.Activity_MoveItem_Property_NewPath_Description;
            NewPath.EditPlaceholder = Resources.Activity_MoveItem_Property_NewPath_Placeholder;
            NewPath.IsRequired = true;
            NewPath.IsPrincipal = true;
            NewPath.OrderIndex = orderIndex++;
            NewPath.Category = Resources.Input;

            Overwrite.DisplayName = Resources.Activity_MoveItem_Property_Overwrite_Name;
            Overwrite.Tooltip = Resources.Activity_MoveItem_Property_Overwrite_Description;
            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Options;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ConfigureContinueOnError(ref orderIndex);
        }
    }
}
