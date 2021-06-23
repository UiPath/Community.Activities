using System.Activities.Presentation.Model;

namespace UiPath.Database.Activities.Design
{
    /// <summary>
    /// Interaction logic for BulkInsertDesigner.xaml
    /// </summary>
    public partial class BulkInsertDesigner
    {
        public BulkInsertDesigner()
        {
            InitializeComponent();
        }

        private void ConfigureButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ConnectionDialog connDialog = new ConnectionDialog(ModelItem);
            connDialog.Show();
        }
    }
}