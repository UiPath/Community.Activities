using System.Activities.Presentation;
using System.Windows;

namespace UiPath.Data.ConnectionUI.Dialog.Dialogs
{
    /// <summary>
    /// Interaction logic for DataConnectionAdvancedDialog.xaml
    /// </summary>
    public partial class DataConnectionAdvancedDialog : WorkflowElementDialog
    {
        public DataConnectionAdvancedDialog(IDataConnectionProperties connectionProperties)
        {
            InitializeComponent();
            this.WindowSizeToContent = SizeToContent.Manual;
            PropertyGrid1.SelectedObject = connectionProperties;
        }

        public void ToggleOKButton(bool state)
        {
            this.EnableOk(state);
        }
    }
}
