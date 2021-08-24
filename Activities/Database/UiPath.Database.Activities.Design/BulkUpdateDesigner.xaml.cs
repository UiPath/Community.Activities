using System.Activities.Presentation.Model;

namespace UiPath.Database.Activities.Design
{
    /// <summary>
    /// Interaction logic for BulkUpdateDesigner.xaml
    /// </summary>
    public partial class BulkUpdateDesigner
    {
        public BulkUpdateDesigner()
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
