using System;
using System.Activities.Presentation;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.Sql;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UiPath.Data.ConnectionUI.Dialog;
using Res = UiPath.Data.ConnectionUI.Dialog.Properties;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for SqlConnectionUIControl.xaml
    /// </summary>
    public partial class SqlConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
    {
        private List<string> _servers;
        private List<string> _databases;
        private ControlProperties _controlProperties;
        private string currentOleDBProvider;

        #region Public Properties
        public List<String> Servers
        {
            get { return _servers; }
        }

        public List<String> Databases
        {
            get { return _databases; }
        }

        public ControlProperties Properties
        {
            get
            {
                return _controlProperties;
            }
        }
        #endregion
        
        public SqlConnectionUIControl()
        {
            InitializeComponent();
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if (connectionProperties == null)
            {
                throw new ArgumentNullException("connectionProperties");
            }

            if (!(connectionProperties is SqlConnectionProperties) &&
                !(connectionProperties is OleDBSqlConnectionProperties))
            {
                throw new ArgumentException("invalid");
            }

            if (connectionProperties is OleDBSqlConnectionProperties)
            {
                currentOleDBProvider = connectionProperties["Provider"] as string;
            }
            _controlProperties = new ControlProperties(connectionProperties);
        }

        private void ServerList_DropDownOpened(object sender, EventArgs e)
        {
            if (_servers == null)
            {
                EnumerateServers();
            }
        }

        private void RefreshServersButton_Click(object sender, RoutedEventArgs e)
        {
            EnumerateServers();
        }

        private void DatabaseList_DropDownOpened(object sender, EventArgs e)
        {
            EnumerateDatabases();
        }

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Properties.Password = ((PasswordBox)sender).Password;
        }

        private void SqlAuthentication_Unchecked(object sender, RoutedEventArgs e)
        {
            usernameTextbox.Text = string.Empty;
            passwordTextbox.Password = string.Empty;
            savepasswordCheckbox.IsChecked = false;
        }

        private void DatabaseOption_Click(object sender, RoutedEventArgs e)
        {
            attachedDatabaseTextbox.Clear();
            logicalNameTextbox.Clear();
        }

        private void AttachOption_Click(object sender, RoutedEventArgs e)
        {
            databaseList.Text = string.Empty;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".mdf";
            dlg.Filter = Res.Resources.DatabaseFileBrowseFilter;

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                attachedDatabaseTextbox.Text = filename;
            }
        }

        private void EnumerateServers()
        {
            // Perform the enumeration
            DataTable dataTable = null;
            try
            {
#if NETFRAMEWORK
                dataTable = SqlDataSourceEnumerator.Instance.GetDataSources();
#endif
#if NETCOREAPP
                dataTable = SqlServerScanner.GetList();
#endif
            }
            catch
            {
                dataTable = new DataTable
                {
                    Locale = System.Globalization.CultureInfo.InvariantCulture
                };
            }

            // Create the object array of server names (with instances appended)
            _servers = new List<string>();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                string name = dataTable.Rows[i]["ServerName"].ToString();
                string instance = dataTable.Rows[i]["InstanceName"].ToString();
                if (instance.Length == 0)
                {
                    _servers.Add(name);
                }
                else
                {
                    _servers.Add(string.Format("{0}\\{1}", name, instance));
                }
            }
            _servers = _servers.Distinct().ToList<string>();
            // Sort the list
            _servers.Sort();
            serverList.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
        }

        private void EnumerateDatabases()
        {
            // Perform the enumeration
            DataTable dataTable = null;
            IDbConnection connection = null;
            IDataReader reader = null;
            try
            {
                // Get a basic connection
                connection = Properties.GetBasicConnection();

                // Create a command to check if the database is on SQL AZure.
                IDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT CASE WHEN SERVERPROPERTY(N'EDITION') = 'SQL Data Services' OR SERVERPROPERTY(N'EDITION') = 'SQL Azure' THEN 1 ELSE 0 END";

                // Open the connection
                connection.Open();

                // SQL AZure doesn't support HAS_DBACCESS at this moment.
                // Change the command text to get database names accordingly
                if ((int)(command.ExecuteScalar()) == 1)
                {
                    command.CommandText = "SELECT name FROM master.dbo.sysdatabases ORDER BY name";
                }
                else
                {
                    command.CommandText = "SELECT name FROM master.dbo.sysdatabases WHERE HAS_DBACCESS(name) = 1 ORDER BY name";
                }

                // Execute the command
                reader = command.ExecuteReader();

                // Read into the data table
                dataTable = new DataTable
                {
                    Locale = System.Globalization.CultureInfo.CurrentCulture
                };
                dataTable.Load(reader);
            }
            catch (Exception ex)
            {
                dataTable = new DataTable
                {
                    Locale = System.Globalization.CultureInfo.InvariantCulture
                };
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                if (connection != null)
                {
                    connection.Dispose();
                }
            }

            // Create the object array of database names
            _databases = new List<string>();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                _databases.Add((string)dataTable.Rows[i]["name"]);
            }
            databaseList.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
        }

        public class ControlProperties
        {
            private IDataConnectionProperties _properties;

            public ControlProperties(IDataConnectionProperties properties)
            {
                properties["Integrated Security"] = true;
                _properties = properties;
                UseWindowsAuthentication = true;
            }

            public string Provider
            {
                get
                {
                    if (_properties is OleDBSqlConnectionProperties)
                    {
                        return _properties["Provider"] as string;
                    }
                    return null;
                }
            }

            public string ServerName
            {
                get
                {
                    return _properties[ServerNameProperty] as string;
                }
                set
                {
                    if (value != null && value.Trim().Length > 0)
                    {
                        _properties[ServerNameProperty] = value.Trim();
                    }
                    else
                    {
                        _properties.Reset(ServerNameProperty);
                    }
                }
            }

            public bool UserInstance
            {
                get
                {
                    if (_properties is SqlConnectionProperties)
                    {
                        return (bool)_properties["User Instance"];
                    }
                    return false;
                }
            }

            public bool UseWindowsAuthentication
            {
                get
                {
                    if (_properties is SqlConnectionProperties)
                    {
                        return (bool)_properties["Integrated Security"];
                    }
                    if (_properties is OleDBConnectionProperties)
                    {
                        return _properties.Contains("Integrated Security") &&
                            _properties["Integrated Security"] is string &&
                            (_properties["Integrated Security"] as string).Equals("SSPI", StringComparison.OrdinalIgnoreCase);
                    }
                    if (_properties is OdbcConnectionProperties)
                    {
                        return _properties.Contains("Trusted_Connection") &&
                            _properties["Trusted_Connection"] is string &&
                            (_properties["Trusted_Connection"] as string).Equals("Yes", StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }
                set
                {
                    if (_properties is SqlConnectionProperties)
                    {
                        if (value)
                        {
                            _properties["Integrated Security"] = value;
                        }
                        else
                        {
                            _properties.Reset("Integrated Security");
                        }
                    }
                    if (_properties is OleDBConnectionProperties)
                    {
                        if (value)
                        {
                            _properties["Integrated Security"] = "SSPI";
                        }
                        else
                        {
                            _properties.Reset("Integrated Security");
                        }
                    }
                    if (_properties is OdbcConnectionProperties)
                    {
                        if (value)
                        {
                            _properties["Trusted_Connection"] = "Yes";
                        }
                        else
                        {
                            _properties.Remove("Trusted_Connection");
                        }
                    }
                }
            }

            public string UserName
            {
                get
                {
                    return _properties[UserNameProperty] as string;
                }
                set
                {
                    if (value != null && value.Trim().Length > 0)
                    {
                        _properties[UserNameProperty] = value.Trim();
                    }
                    else
                    {
                        _properties.Reset(UserNameProperty);
                    }
                }
            }

            public string Password
            {
                get
                {
                    return _properties[PasswordProperty] as string;
                }
                set
                {
                    if (value != null && value.Length > 0)
                    {
                        _properties[PasswordProperty] = value;
                    }
                    else
                    {
                        _properties.Reset(PasswordProperty);
                    }
                }
            }

            public bool SavePassword
            {
                get
                {
                    if (_properties is OdbcConnectionProperties)
                    {
                        return false;
                    }
                    return (bool)_properties["Persist Security Info"];
                }
                set
                {
                    Debug.Assert(!(_properties is OdbcConnectionProperties));
                    if (value)
                    {
                        _properties["Persist Security Info"] = value;
                    }
                    else
                    {
                        _properties.Reset("Persist Security Info");
                    }
                }
            }

            public string DatabaseName
            {
                get
                {
                    if (!UseDBFile)
                        return _properties[DatabaseNameProperty] as string;
                    else
                        return "";
                }
                set
                {
                    {
                        if (value != null && value.Trim().Length > 0)
                        {
                            _properties[DatabaseNameProperty] = value.Trim();
                        }
                        else
                        {
                            _properties.Reset(DatabaseNameProperty);
                        }
                    }
                }
            }

            public string DatabaseFile
            {
                get
                {
                    return _properties[DatabaseFileProperty] as string;
                }
                set
                {
                    if (value != null && value.Trim().Length > 0)
                    {
                        _properties[DatabaseFileProperty] = value.Trim();
                    }
                    else
                    {
                        _properties.Reset(DatabaseFileProperty);
                    }
                }
            }

            public string LogicalDatabaseName
            {
                get
                {
                    if (UseDBFile)
                        return _properties[DatabaseNameProperty] as string;
                    else
                        return "";
                }
                set
                {
                    if (value != null && value.Trim().Length > 0)
                    {
                        _properties[DatabaseNameProperty] = value.Trim();
                    }
                    else
                    {
                        _properties.Reset(DatabaseNameProperty);
                    }

                }
            }

            public bool UseDBFile
            {
                get { return !string.IsNullOrEmpty(DatabaseFile); }

            }

            private string ServerNameProperty
            {
                get
                {
                    return
                        (_properties is SqlConnectionProperties) ? "Data Source" :
                        (_properties is OleDBConnectionProperties) ? "Data Source" :
                        (_properties is OdbcConnectionProperties) ? "SERVER" : null;
                }
            }

            private string UserNameProperty
            {
                get
                {
                    return
                        (_properties is SqlConnectionProperties) ? "User ID" :
                        (_properties is OleDBConnectionProperties) ? "User ID" :
                        (_properties is OdbcConnectionProperties) ? "UID" : null;
                }
            }

            private string PasswordProperty
            {
                get
                {
                    return
                        (_properties is SqlConnectionProperties) ? "Password" :
                        (_properties is OleDBConnectionProperties) ? "Password" :
                        (_properties is OdbcConnectionProperties) ? "PWD" : null;
                }
            }

            private string DatabaseNameProperty
            {
                get
                {
                    return
                        (_properties is SqlConnectionProperties) ? "Initial Catalog" :
                        (_properties is OleDBConnectionProperties) ? "Initial Catalog" :
                        (_properties is OdbcConnectionProperties) ? "DATABASE" : null;
                }
            }

            private string DatabaseFileProperty
            {
                get
                {
                    return
                        (_properties is SqlConnectionProperties) ? "AttachDbFilename" :
                        (_properties is OleDBConnectionProperties) ? "Initial File Name" :
                        (_properties is OdbcConnectionProperties) ? "AttachDBFileName" : null;
                }
            }

            public IDbConnection GetBasicConnection()
            {
                IDbConnection connection = null;

                string connectionString = string.Empty;
                if (_properties is SqlConnectionProperties || _properties is OleDBConnectionProperties)
                {
                    if (_properties is OleDBConnectionProperties)
                    {
                        connectionString += "Provider=" + _properties["Provider"].ToString() + ";";
                    }
                    connectionString += "Data Source='" + ServerName.Replace("'", "''") + "';";
                    if (UserInstance)
                    {
                        connectionString += "User Instance=true;";
                    }
                    if (UseWindowsAuthentication)
                    {
                        connectionString += "Integrated Security=" + _properties["Integrated Security"].ToString() + ";";
                    }
                    else
                    {
                        connectionString += "User ID='" + UserName.Replace("'", "''") + "';";
                        connectionString += "Password='" + Password.Replace("'", "''") + "';";
                    }
                    if (_properties is SqlConnectionProperties)
                    {
                        connectionString += "Pooling=False;Encrypt=false";
                    }
                }
                if (_properties is OdbcConnectionProperties)
                {
                    connectionString += "DRIVER={SQL Server};";
                    connectionString += "SERVER={" + ServerName.Replace("}", "}}") + "};";
                    if (UseWindowsAuthentication)
                    {
                        connectionString += "Trusted_Connection=Yes;";
                    }
                    else
                    {
                        connectionString += "UID={" + UserName.Replace("}", "}}") + "};";
                        connectionString += "PWD={" + Password.Replace("}", "}}") + "};";
                    }
                }

                if (_properties is SqlConnectionProperties)
                {
                    connection = new SqlConnection(connectionString);
                }
                if (_properties is OleDBConnectionProperties)
                {
                    connection = new OleDbConnection(connectionString);
                }
                if (_properties is OdbcConnectionProperties)
                {
                    connection = new OdbcConnection(connectionString);
                }

                return connection;
            }
        }
    }
}
