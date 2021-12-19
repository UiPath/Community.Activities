using System;
using System.Activities.Presentation;
using System.Windows;
using System.Windows.Controls;
using Res=UiPath.Data.ConnectionUI.Dialog.Properties;


namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for SqlFileConnectionUIControl.xaml
    /// </summary>
    public partial class SqlFileConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private IDataConnectionProperties _connectionProperties;

        #region Public Properties
        public string Password
        {
            get
            {
                return (string)_connectionProperties["Password"];
            }
            set
            {
                _connectionProperties["Password"] = value;
            }
        }

        public string UserName
        {
            get
            {
                return (string)_connectionProperties["User ID"];
            }
            set
            {
                _connectionProperties["User ID"] = value;
            }
        }

        public bool SavePassword
        {
            get
            {
                return (bool)_connectionProperties["Persist Security Info"];
            }
            set
            {
                _connectionProperties["Persist Security Info"] = value;
            }
        }

        public string DatabaseFile
        {
            get
            {
                return (string)_connectionProperties["AttachDbFilename"];
            }
            set
            {
                _connectionProperties["AttachDbFilename"] = value;
            }
        }

        public bool UseWindowsAuthentication
        {
            get
            {
                return (bool)_connectionProperties["Integrated Security"];
            }
            set
            {
                _connectionProperties["Integrated Security"] = value;
                if (value)
                {
                    usernameTextbox.Clear();
                    passwordTextbox.Clear();
                    saveCheckbox.IsChecked = false;
                }
            }
        }
        #endregion

        public SqlFileConnectionUIControl()
        {
            InitializeComponent();
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if (!(connectionProperties is SqlFileConnectionProperties))
            {
                throw new ArgumentException(Res.Resources.SqlFileConnectionUIControl_InvalidConnectionProperties);
            }
            _connectionProperties = connectionProperties;
            UseWindowsAuthentication = true;
            SavePassword = false;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".mdf";
            dlg.Filter = Properties.Resources.DatabaseFileBrowseFilter;

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                fileNameTextBox.Text = filename;
            }
        }

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = ((PasswordBox)sender).Password;
        } 
    }
}
