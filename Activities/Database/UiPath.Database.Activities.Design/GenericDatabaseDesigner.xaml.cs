using System.Activities.Presentation.Model;

namespace UiPath.Database.Activities.Design
{
    // Interaction logic for GenericDatabaseDesigner.xaml
    public partial class GenericDatabaseDesigner
    {
        public GenericDatabaseDesigner()
        {
            InitializeComponent();
        }

        private void SqlButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (ModelEditingScope editingScope = ModelItem.BeginEdit())
            {
                SqlEditorDialog sqlDialog = new SqlEditorDialog(ModelItem);
                if (sqlDialog.ShowOkCancel())
                {
                    editingScope.Complete();
                }
                else
                {
                    editingScope.Revert();
                }
            }
        }

        private void ConfigureButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ConnectionDialog connDialog = new ConnectionDialog(ModelItem);
            connDialog.Show();
        }
    }
}
