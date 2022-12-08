using System;
using System.Activities.Presentation;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for OdbcConnectionUIControl.xaml
    /// </summary>
    public partial class OdbcConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private IDataConnectionProperties _connectionProperties;
        private string[] _dataSourceNames;

        #region Public Properties
        public string DataSourceLabel
        {
            get { return string.Format(Properties.Resources.DataSourceSpec_Label, (IntPtr.Size == 8) ? "64 bit" : "32 bit"); }
        }

        public string[] DataSources
        {
            get { return _dataSourceNames; }
        }

        public string DataSource
        {
            get { return _connectionProperties["Dsn"] as string; }
            set 
            { 
                _connectionProperties["Dsn"] = value.Trim();
                UpdateConnectionString();
            }
        }

        public string ConString
        {
            get { return _connectionProperties.ToDisplayString(); }
        }

        public string Password
        {
            get
            {
                return (string)_connectionProperties["pwd"];
            }
            set
            {
                _connectionProperties["pwd"] = value;
            }
        }

        public string UserName
        {
            get
            {
                return (string)_connectionProperties["uid"];
            }
            set
            {
                _connectionProperties["uid"] = value;
                UpdateConnectionString();
            }
        }
        #endregion

        public OdbcConnectionUIControl()
        {
            EnumerateDataSourceNames();
            InitializeComponent();
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if (!(connectionProperties is OdbcConnectionProperties))
            {
                throw new ArgumentException(Properties.Resources.OdbcConnectionUIControl_InvalidConnectionProperties);
            }

            _connectionProperties = connectionProperties;
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            IntPtr henv = IntPtr.Zero;
            IntPtr hdbc = IntPtr.Zero;
            short result = 0;
            try
            {
                result = NativeMethods.SQLAllocEnv(out henv);
                if (!NativeMethods.SQL_SUCCEEDED(result))
                {
                    throw new ApplicationException(Properties.Resources.OdbcConnectionUIControl_SQLAllocEnvFailed);
                }

                result = NativeMethods.SQLAllocConnect(henv, out hdbc);
                if (!NativeMethods.SQL_SUCCEEDED(result))
                {
                    throw new ApplicationException(Properties.Resources.OdbcConnectionUIControl_SQLAllocConnectFailed);
                }

                string currentConnectionString = _connectionProperties.ToFullString();
                System.Text.StringBuilder newConnectionString = new System.Text.StringBuilder(1024);
                result = NativeMethods.SQLDriverConnect(hdbc, new WindowInteropHelper(Window.GetWindow(this)).Handle, currentConnectionString, (short)currentConnectionString.Length, newConnectionString, 1024, out short newConnectionStringLength, NativeMethods.SQL_DRIVER_PROMPT);
                if (!NativeMethods.SQL_SUCCEEDED(result) && result != NativeMethods.SQL_NO_DATA)
                {
                    // Try again without the current connection string, in case it was invalid
                    result = NativeMethods.SQLDriverConnect(hdbc, new WindowInteropHelper(Window.GetWindow(this)).Handle, null, 0, newConnectionString, 1024, out newConnectionStringLength, NativeMethods.SQL_DRIVER_PROMPT);
                }
                if (!NativeMethods.SQL_SUCCEEDED(result) && result != NativeMethods.SQL_NO_DATA)
                {
                    throw new ApplicationException(Properties.Resources.OdbcConnectionUIControl_SQLDriverConnectFailed);
                }
                else
                {
                    NativeMethods.SQLDisconnect(hdbc);
                }

                if (newConnectionStringLength > 0)
                {
                    Refresh_Click(sender, e);
                    _connectionProperties.Parse(newConnectionString.ToString());
                    VisualTreeHelpers.RefreshBindings(this);
                    passwordTextbox.Password = Password;
                }
            }
            finally
            {
                if (hdbc != IntPtr.Zero)
                {
                    NativeMethods.SQLFreeConnect(hdbc);
                }
                if (henv != IntPtr.Zero)
                {
                    NativeMethods.SQLFreeEnv(henv);
                }
            }
        }

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = ((PasswordBox)sender).Password;
        }

        private void ConStrTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                _connectionProperties.Parse(conStrTextBox.Text.Trim());
                VisualTreeHelpers.RefreshBindings(this);
                passwordTextbox.Password = Password;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error_Label, MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateConnectionString();
            }
        }
        
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            EnumerateDataSourceNames();
            dsncomboBox.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
        }

        private void EnumerateDataSourceNames()
        {
            // Perform the enumeration
            DataTable dataTable = new DataTable
            {
                Locale = System.Globalization.CultureInfo.InvariantCulture
            };
            try
            {
                // Use the MSDAORA enumerator
                System.Data.OleDb.OleDbDataReader reader = System.Data.OleDb.OleDbEnumerator.GetEnumerator(Type.GetTypeFromCLSID(NativeMethods.CLSID_MSDASQL_ENUMERATOR));
                using (reader)
                {
                    dataTable.Load(reader);
                }
            }
            catch
            {
            }

            // Create the object array of data source names (with instances appended)
            _dataSourceNames = new string[dataTable.Rows.Count];
            for (int i = 0; i < _dataSourceNames.Length; i++)
            {
                _dataSourceNames[i] = dataTable.Rows[i]["SOURCES_NAME"] as string;
            }

            // Sort the list
            Array.Sort(_dataSourceNames);
        }

        private void UpdateConnectionString()
        {
            conStrTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        } 
    }
}
