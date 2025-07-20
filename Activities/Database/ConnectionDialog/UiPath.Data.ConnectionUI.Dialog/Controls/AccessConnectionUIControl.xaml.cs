using System;
using System.Activities.Presentation;
using System.Windows;
using System.Windows.Controls;
using UiPath.Database;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for AccessConnectionUIControl.xaml
    /// </summary>
    public partial class AccessConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private IDataConnectionProperties _connectionProperties;

        #region Private Properties
        private string DatabaseFileProperty
        {
            get
            {
                if (!(_connectionProperties is OdbcConnectionProperties))
                {
                    return DatabaseConstants.Data_Source;
                }
                else
                {
                    return DatabaseConstants.DBQ;
                }
            }
        }

        private string UserNameProperty
        {
            get
            {
                if (!(_connectionProperties is OdbcConnectionProperties))
                {
                    return DatabaseConstants.User_ID;
                }
                else
                {
                    return DatabaseConstants.UID;
                }
            }
        }

        private string PasswordProperty
        {
            get
            {
                if (!(_connectionProperties is OdbcConnectionProperties))
                {
                    return "Jet OLEDB:Database Password";
                }
                else
                {
                    return "PWD";
                }
            }
        }
        #endregion

        #region Public Properties
        public string Password
        {
            get
            {
                return (string)_connectionProperties[PasswordProperty];
            }
            set
            {
                _connectionProperties[PasswordProperty] = value;
            }
        }
        
        public string UserName
        {
            get
            {
                return (string)_connectionProperties[UserNameProperty];
            }
            set
            {
                _connectionProperties[UserNameProperty] = value;
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
                return (string)_connectionProperties[DatabaseFileProperty];
            }
            set
            {
                _connectionProperties[DatabaseFileProperty] = value;
            }
        }
        #endregion

        public AccessConnectionUIControl()
        {
            InitializeComponent();
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if (connectionProperties == null)
            {
                throw new ArgumentNullException("connectionProperties");
            }

            if (!(connectionProperties is OleDBAccessConnectionProperties))
            {
                throw new ArgumentException(Properties.Resources.AccessConnectionUIControl_InvalidConnectionProperties);
            }

            if (connectionProperties is OdbcConnectionProperties)
            {
                // ODBC does not support saving the password
                saveCheckbox.IsEnabled = false;
            }

            _connectionProperties = connectionProperties;
        }

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = ((PasswordBox)sender).Password;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = Properties.Resources.AccessConnectionUIControl_BrowseFileDefaultExt;
            dlg.Filter = Properties.Resources.AccessConnectionUIControl_BrowseFileFilter;
            dlg.Title = Properties.Resources.AccessConnectionUIControl_BrowseFileTitle;
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;

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
    }
}
