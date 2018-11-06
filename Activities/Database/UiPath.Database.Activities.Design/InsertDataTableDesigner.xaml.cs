namespace UiPath.Database.Activities.Design
{
    // Interaction logic for InsertDataTableDesigner.xaml
    public partial class InsertDataTableDesigner
    {
        public InsertDataTableDesigner()
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
