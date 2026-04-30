using System;
using System.Activities.Presentation;
using System.Windows;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for SqliteConnectionUIControl.xaml
    /// </summary>
    public partial class SqliteConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private IDataConnectionProperties _connectionProperties;

        public string DataSource
        {
            get => _connectionProperties?["Data Source"] as string;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _connectionProperties["Data Source"] = value.Trim();
                else
                    _connectionProperties.Reset("Data Source");
            }
        }

        public SqliteConnectionUIControl()
        {
            InitializeComponent();
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if (connectionProperties is not SqliteConnectionProperties)
                throw new ArgumentException($"Expected {nameof(SqliteConnectionProperties)}.", nameof(connectionProperties));

            _connectionProperties = connectionProperties;
            DataContext = this;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".db",
                Filter = Properties.UiPath_Data_ConnectionUI_Dialog.SqliteFileBrowseFilter
            };

            if (dlg.ShowDialog() == true)
                fileNameTextBox.Text = dlg.FileName;
        }
    }
}
