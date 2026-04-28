using System;
using System.Activities.Presentation;
using System.Windows;
using System.Windows.Controls;
using Res=UiPath.Data.ConnectionUI.Dialog.Properties;
using UiPath.Database;

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
                return (string)_connectionProperties[DatabaseConstants.Password];
            }
            set
            {
                _connectionProperties[DatabaseConstants.Password] = value;
            }
        }

        public string UserName
        {
            get
            {
                return (string)_connectionProperties[DatabaseConstants.User_ID];
            }
            set
            {
                _connectionProperties[DatabaseConstants.User_ID] = value;
            }
        }

        public bool SavePassword
        {
            get
            {
                return (bool)_connectionProperties[DatabaseConstants.Persist_Security_Info];
            }
            set
            {
                _connectionProperties[DatabaseConstants.Persist_Security_Info] = value;
            }
        }

        public string DatabaseFile
        {
            get
            {
                return (string)_connectionProperties[DatabaseConstants.AttachDbFilename];
            }
            set
            {
                _connectionProperties[DatabaseConstants.AttachDbFilename] = value;
            }
        }

        public bool UseWindowsAuthentication
        {
            get
            {
                return (bool)_connectionProperties[DatabaseConstants.Integrated_Security];
            }
            set
            {
                _connectionProperties[DatabaseConstants.Integrated_Security] = value;
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
                throw new ArgumentException(Res.Resources.SqlFileConnectionUIControl_InvalidConnectionProperties, nameof(connectionProperties));
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
