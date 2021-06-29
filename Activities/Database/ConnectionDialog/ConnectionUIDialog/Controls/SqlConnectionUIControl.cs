//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using ThreadState = System.Threading.ThreadState;

namespace Microsoft.Data.ConnectionUI
{
    public partial class SqlConnectionUIControl : UserControl, IDataConnectionUIControl
    {
        public SqlConnectionUIControl()
        {
            InitializeComponent();
            RightToLeft = RightToLeft.Inherit;

            int requiredHeight = LayoutUtils.GetPreferredCheckBoxHeight(savePasswordCheckBox);
            if (savePasswordCheckBox.Height < requiredHeight)
            {
                savePasswordCheckBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
                loginTableLayoutPanel.Height += loginTableLayoutPanel.Margin.Bottom;
                loginTableLayoutPanel.Margin = new Padding(loginTableLayoutPanel.Margin.Left, loginTableLayoutPanel.Margin.Top, loginTableLayoutPanel.Margin.Right, 0);
            }

            // Apparently WinForms automatically sets the accessible name for text boxes
            // based on a label previous to it, but does not do the same when it is
            // proceeded by a radio button.  So, simulate that behavior here
            selectDatabaseComboBox.AccessibleName = TextWithoutMnemonics(selectDatabaseRadioButton.Text);
            attachDatabaseTextBox.AccessibleName = TextWithoutMnemonics(attachDatabaseRadioButton.Text);

            _uiThread = Thread.CurrentThread;
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
                throw new ArgumentException(Strings.SqlConnectionUIControl_InvalidConnectionProperties);
            }

            if (connectionProperties is OleDBSqlConnectionProperties)
            {
                currentOleDBProvider = connectionProperties["Provider"] as string;
            }

            if (connectionProperties is OdbcConnectionProperties)
            {
                // ODBC does not support saving the password
                savePasswordCheckBox.Enabled = false;
            }

            _controlProperties = new ControlProperties(connectionProperties);
        }

        public void LoadProperties()
        {
            _loading = true;

            if (currentOleDBProvider != Properties.Provider)
            {
                selectDatabaseComboBox.Items.Clear(); // a provider change requires a refresh here
                currentOleDBProvider = Properties.Provider;
            }

            serverComboBox.Text = Properties.ServerName;
            if (Properties.UseWindowsAuthentication)
            {
                windowsAuthenticationRadioButton.Checked = true;
            }
            else
            {
                sqlAuthenticationRadioButton.Checked = true;
            }
            if (currentUserInstanceSetting != Properties.UserInstance)
            {
                selectDatabaseComboBox.Items.Clear(); // this change requires a refresh here
            }
            currentUserInstanceSetting = Properties.UserInstance;
            userNameTextBox.Text = Properties.UserName;
            passwordTextBox.Text = Properties.Password;
            savePasswordCheckBox.Checked = Properties.SavePassword;
            if (Properties.DatabaseFile == null || Properties.DatabaseFile.Length == 0)
            {
                selectDatabaseRadioButton.Checked = true;
                selectDatabaseComboBox.Text = Properties.DatabaseName;
                attachDatabaseTextBox.Text = null;
                logicalDatabaseNameTextBox.Text = null;
            }
            else
            {
                attachDatabaseRadioButton.Checked = true;
                selectDatabaseComboBox.Text = null;
                attachDatabaseTextBox.Text = Properties.DatabaseFile;
                logicalDatabaseNameTextBox.Text = Properties.LogicalDatabaseName;
            }

            _loading = false;
        }

        // Simulate RTL mirroring
        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            if (ParentForm != null &&
                ParentForm.RightToLeftLayout == true &&
                RightToLeft == RightToLeft.Yes)
            {
                LayoutUtils.MirrorControl(serverLabel, serverTableLayoutPanel);
                LayoutUtils.MirrorControl(windowsAuthenticationRadioButton);
                LayoutUtils.MirrorControl(sqlAuthenticationRadioButton);
                LayoutUtils.MirrorControl(loginTableLayoutPanel);
                LayoutUtils.MirrorControl(selectDatabaseRadioButton);
                LayoutUtils.MirrorControl(selectDatabaseComboBox);
                LayoutUtils.MirrorControl(attachDatabaseRadioButton);
                LayoutUtils.MirrorControl(attachDatabaseTableLayoutPanel);
                LayoutUtils.MirrorControl(logicalDatabaseNameLabel);
                LayoutUtils.MirrorControl(logicalDatabaseNameTextBox);
            }
            else
            {
                LayoutUtils.UnmirrorControl(logicalDatabaseNameTextBox);
                LayoutUtils.UnmirrorControl(logicalDatabaseNameLabel);
                LayoutUtils.UnmirrorControl(attachDatabaseTableLayoutPanel);
                LayoutUtils.UnmirrorControl(attachDatabaseRadioButton);
                LayoutUtils.UnmirrorControl(selectDatabaseComboBox);
                LayoutUtils.UnmirrorControl(selectDatabaseRadioButton);
                LayoutUtils.UnmirrorControl(loginTableLayoutPanel);
                LayoutUtils.UnmirrorControl(sqlAuthenticationRadioButton);
                LayoutUtils.UnmirrorControl(windowsAuthenticationRadioButton);
                LayoutUtils.UnmirrorControl(serverLabel, serverTableLayoutPanel);
            }
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            Size baseSize = Size;
            MinimumSize = Size.Empty;
            base.ScaleControl(factor, specified);
            MinimumSize = new Size(
                (int)Math.Round(baseSize.Width * factor.Width),
                (int)Math.Round(baseSize.Height * factor.Height));
        }

        [UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows)]
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (ActiveControl == selectDatabaseRadioButton &&
                (keyData & Keys.KeyCode) == Keys.Down)
            {
                attachDatabaseRadioButton.Focus();
                return true;
            }
            if (ActiveControl == attachDatabaseRadioButton &&
                (keyData & Keys.KeyCode) == Keys.Down)
            {
                selectDatabaseRadioButton.Focus();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent == null)
            {
                OnFontChanged(e);
            }
        }

        private void HandleComboBoxDownKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (sender == serverComboBox)
                {
                    EnumerateServers(sender, e);
                }
                if (sender == selectDatabaseComboBox)
                {
                    EnumerateDatabases(sender, e);
                }
            }
        }

        private void EnumerateServers(object sender, EventArgs e)
        {
            if (serverComboBox.Items.Count == 0)
            {
                Cursor currentCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    if (_serverEnumerationThread == null ||
                        _serverEnumerationThread.ThreadState == ThreadState.Stopped)
                    {
                        EnumerateServers();
                    }
                    else if (_serverEnumerationThread.ThreadState == ThreadState.Running)
                    {
                        // Wait for the asynchronous enumeration to finish
                        _serverEnumerationThread.Join();

                        // Populate the combo box now, rather than waiting for
                        // the asynchronous call to be marshaled back to the UI
                        // thread
                        PopulateServerComboBox();
                    }
                }
                finally
                {
                    Cursor.Current = currentCursor;
                }
            }
        }

        private void SetServer(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties.ServerName = serverComboBox.Text;
                if (serverComboBox.Items.Count == 0 && _serverEnumerationThread == null)
                {
                    // Start an enumeration of servers
                    _serverEnumerationThread = new Thread(new ThreadStart(EnumerateServers));
                    _serverEnumerationThread.Start();
                }
            }
            SetDatabaseGroupBoxStatus(sender, e);
            selectDatabaseComboBox.Items.Clear(); // a server change requires a refresh here
        }

        private void RefreshServers(object sender, EventArgs e)
        {
            serverComboBox.Items.Clear();
            EnumerateServers(sender, e);
        }

        private void SetAuthenticationOption(object sender, EventArgs e)
        {
            if (windowsAuthenticationRadioButton.Checked)
            {
                if (!_loading)
                {
                    Properties.UseWindowsAuthentication = true;
                    Properties.UserName = null;
                    Properties.Password = null;
                    Properties.SavePassword = false;
                }
                loginTableLayoutPanel.Enabled = false;
            }
            else /* if (sqlAuthenticationRadioButton.Checked) */
            {
                if (!_loading)
                {
                    Properties.UseWindowsAuthentication = false;
                    SetUserName(sender, e);
                    SetPassword(sender, e);
                    SetSavePassword(sender, e);
                }
                loginTableLayoutPanel.Enabled = true;
            }
            SetDatabaseGroupBoxStatus(sender, e);
            selectDatabaseComboBox.Items.Clear(); // an authentication change requires a refresh here
        }

        private void SetUserName(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties.UserName = userNameTextBox.Text;
            }
            SetDatabaseGroupBoxStatus(sender, e);
            selectDatabaseComboBox.Items.Clear(); // a user name change requires a refresh here
        }

        private void SetPassword(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties.Password = passwordTextBox.Text;
                passwordTextBox.Text = passwordTextBox.Text; // forces reselection of all text
            }
            selectDatabaseComboBox.Items.Clear(); // a password change requires a refresh here
        }

        private void SetSavePassword(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties.SavePassword = savePasswordCheckBox.Checked;
            }
        }

        private void SetDatabaseGroupBoxStatus(object sender, EventArgs e)
        {
            if (serverComboBox.Text.Trim().Length > 0 &&
                (windowsAuthenticationRadioButton.Checked ||
                userNameTextBox.Text.Trim().Length > 0))
            {
                databaseGroupBox.Enabled = true;
            }
            else
            {
                databaseGroupBox.Enabled = false;
            }
        }

        private void SetDatabaseOption(object sender, EventArgs e)
        {
            if (selectDatabaseRadioButton.Checked)
            {
                SetDatabase(sender, e);
                SetAttachDatabase(sender, e);
                selectDatabaseComboBox.Enabled = true;
                attachDatabaseTableLayoutPanel.Enabled = false;
                logicalDatabaseNameLabel.Enabled = false;
                logicalDatabaseNameTextBox.Enabled = false;
            }
            else /* if (attachDatabaseRadioButton.Checked) */
            {
                SetAttachDatabase(sender, e);
                SetLogicalFilename(sender, e);
                selectDatabaseComboBox.Enabled = false;
                attachDatabaseTableLayoutPanel.Enabled = true;
                logicalDatabaseNameLabel.Enabled = true;
                logicalDatabaseNameTextBox.Enabled = true;
            }
        }

        private void SetDatabase(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties.DatabaseName = selectDatabaseComboBox.Text;
                if (selectDatabaseComboBox.Items.Count == 0 && _databaseEnumerationThread == null)
                {
                    // Start an enumeration of databases
                    _databaseEnumerationThread = new Thread(new ThreadStart(EnumerateDatabases));
                    _databaseEnumerationThread.Start();
                }
            }
        }

        private void EnumerateDatabases(object sender, EventArgs e)
        {
            if (selectDatabaseComboBox.Items.Count == 0)
            {
                Cursor currentCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    if (_databaseEnumerationThread == null ||
                        _databaseEnumerationThread.ThreadState == ThreadState.Stopped)
                    {
                        EnumerateDatabases();
                    }
                    else if (_databaseEnumerationThread.ThreadState == ThreadState.Running)
                    {
                        // Wait for the asynchronous enumeration to finish
                        _databaseEnumerationThread.Join();

                        // Populate the combo box now, rather than waiting for
                        // the asynchronous call to be marshaled back to the UI
                        // thread
                        PopulateDatabaseComboBox();
                    }
                }
                finally
                {
                    Cursor.Current = currentCursor;
                }
            }
        }

        private void SetAttachDatabase(object sender, EventArgs e)
        {
            if (!_loading)
            {
                if (selectDatabaseRadioButton.Checked)
                {
                    Properties.DatabaseFile = null;
                }
                else /* if (attachDatabaseRadioButton.Checked) */
                {
                    Properties.DatabaseFile = attachDatabaseTextBox.Text;
                }
            }
        }

        private void SetLogicalFilename(object sender, EventArgs e)
        {
            if (!_loading)
            {
                if (selectDatabaseRadioButton.Checked)
                {
                    Properties.LogicalDatabaseName = null;
                }
                else /* if (attachDatabaseRadioButton.Checked) */
                {
                    Properties.LogicalDatabaseName = logicalDatabaseNameTextBox.Text;
                }
            }
        }

        private void Browse(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = Strings.SqlConnectionUIControl_BrowseFileTitle,
                Multiselect = false,
                RestoreDirectory = true,
                Filter = Strings.SqlConnectionUIControl_BrowseFileFilter,
                DefaultExt = Strings.SqlConnectionUIControl_BrowseFileDefaultExt
            };
            if (Container != null)
            {
                Container.Add(fileDialog);
            }
            try
            {
                DialogResult result = fileDialog.ShowDialog(ParentForm);
                if (result == DialogResult.OK)
                {
                    attachDatabaseTextBox.Text = fileDialog.FileName.Trim();
                }
            }
            finally
            {
                if (Container != null)
                {
                    Container.Remove(fileDialog);
                }
                fileDialog.Dispose();
            }
        }

        private void TrimControlText(object sender, EventArgs e)
        {
            Control c = sender as Control;
            c.Text = c.Text.Trim();
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
            _servers = new object[dataTable.Rows.Count];
            for (int i = 0; i < _servers.Length; i++)
            {
                string name = dataTable.Rows[i]["ServerName"].ToString();
                string instance = dataTable.Rows[i]["InstanceName"].ToString();
                if (instance.Length == 0)
                {
                    _servers[i] = name;
                }
                else
                {
                    _servers[i] = name + "\\" + instance;
                }
            }
            _servers = _servers.Distinct().ToArray();
            // Sort the list
            Array.Sort(_servers);

            // Populate the server combo box items (must occur on the UI thread)
            if (Thread.CurrentThread == _uiThread)
            {
                PopulateServerComboBox();
            }
            else if (IsHandleCreated)
            {
                BeginInvoke(new ThreadStart(PopulateServerComboBox));
            }
        }

        private void PopulateServerComboBox()
        {
            if (serverComboBox.Items.Count == 0)
            {
                if (_servers.Length > 0)
                {
                    serverComboBox.Items.AddRange(_servers);
                }
                else
                {
                    serverComboBox.Items.Add(string.Empty);
                }
            }
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
            catch
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
            _databases = new object[dataTable.Rows.Count];
            for (int i = 0; i < _databases.Length; i++)
            {
                _databases[i] = dataTable.Rows[i]["name"];
            }

            selectDatabaseComboBox.Items.Clear();
            // Populate the database combo box items (must occur on the UI thread)
            if (Thread.CurrentThread == _uiThread)
            {
                PopulateDatabaseComboBox();
            }
            else if (IsHandleCreated)
            {
                BeginInvoke(new ThreadStart(PopulateDatabaseComboBox));
            }
        }

        private void PopulateDatabaseComboBox()
        {
            if (selectDatabaseComboBox.Items.Count == 0)
            {
                if (_databases.Length > 0)
                {
                    selectDatabaseComboBox.Items.AddRange(_databases);
                }
                else
                {
                    selectDatabaseComboBox.Items.Add(string.Empty);
                }
            }
            _databaseEnumerationThread = null;
        }

        private static string TextWithoutMnemonics(string text)
        {
            if (text == null)
            {
                return null;
            }

            int index = text.IndexOf('&');
            if (index == -1)
            {
                return text;
            }

            System.Text.StringBuilder str = new System.Text.StringBuilder(text.Substring(0, index));
            for (; index < text.Length; ++index)
            {
                if (text[index] == '&')
                {
                    // Skip this & and copy the next character instead
                    index++;
                }
                if (index < text.Length)
                {
                    str.Append(text[index]);
                }
            }

            return str.ToString();
        }

        private ControlProperties Properties
        {
            get
            {
                return _controlProperties;
            }
        }

        private class ControlProperties
        {
            public ControlProperties(IDataConnectionProperties properties)
            {
                _properties = properties;
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
                    return _properties[DatabaseNameProperty] as string;
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
                    return DatabaseName;
                }
                set
                {
                    DatabaseName = value;
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
                        connectionString += "Pooling=False;";
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

            private IDataConnectionProperties _properties;
        }

        private bool _loading;
        private object[] _servers;
        private object[] _databases;
        private readonly Thread _uiThread;
        private Thread _serverEnumerationThread;
        private Thread _databaseEnumerationThread;
        private string currentOleDBProvider;
        private bool currentUserInstanceSetting;
        private ControlProperties _controlProperties;
    }
}