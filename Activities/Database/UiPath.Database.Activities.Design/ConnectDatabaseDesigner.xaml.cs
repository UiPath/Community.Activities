namespace UiPath.Database.Activities.Design
{
    // Interaction logic for ConnectDatabaseDesigner.xaml
    public partial class ConnectDatabaseDesigner
    {
        public ConnectDatabaseDesigner()
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
