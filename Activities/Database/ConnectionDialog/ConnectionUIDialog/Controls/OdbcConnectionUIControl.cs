//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Drawing;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Microsoft.Data.ConnectionUI
{
    public partial class OdbcConnectionUIControl : UserControl, IDataConnectionUIControl
    {
        public OdbcConnectionUIControl()
        {
            InitializeComponent();
            RightToLeft = RightToLeft.Inherit;

            // WinForms automatically sets the accessible name for text boxes based on
            // a label previous to it, but does not do the same when it is proceeded
            // by a radio button.  So, simulate that behavior here.
            dataSourceNameComboBox.AccessibleName = TextWithoutMnemonics(useDataSourceNameRadioButton.Text);
            connectionStringTextBox.AccessibleName = TextWithoutMnemonics(useConnectionStringRadioButton.Text);

            _uiThread = Thread.CurrentThread;
        }

        public void Initialize(IDataConnectionProperties connectionProperties)
        {
            if (!(connectionProperties is OdbcConnectionProperties))
            {
                throw new ArgumentException(Strings.OdbcConnectionUIControl_InvalidConnectionProperties);
            }

            _connectionProperties = connectionProperties;
        }

        public void LoadProperties()
        {
            _loading = true;

            EnumerateDataSourceNames();

            if (Properties.ToFullString().Length == 0 ||
                (Properties["Dsn"] is string && (Properties["Dsn"] as string).Length > 0))
            {
                useDataSourceNameRadioButton.Checked = true;
            }
            else
            {
                useConnectionStringRadioButton.Checked = true;
            }
            UpdateControls();

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
                LayoutUtils.MirrorControl(useDataSourceNameRadioButton);
                LayoutUtils.MirrorControl(dataSourceNameTableLayoutPanel);
                LayoutUtils.MirrorControl(useConnectionStringRadioButton);
                LayoutUtils.MirrorControl(connectionStringTableLayoutPanel);
            }
            else
            {
                LayoutUtils.UnmirrorControl(connectionStringTableLayoutPanel);
                LayoutUtils.UnmirrorControl(useConnectionStringRadioButton);
                LayoutUtils.UnmirrorControl(dataSourceNameTableLayoutPanel);
                LayoutUtils.UnmirrorControl(useDataSourceNameRadioButton);
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
            if (ActiveControl == useDataSourceNameRadioButton &&
                (keyData & Keys.KeyCode) == Keys.Down)
            {
                useConnectionStringRadioButton.Focus();
                return true;
            }
            if (ActiveControl == useConnectionStringRadioButton &&
                (keyData & Keys.KeyCode) == Keys.Down)
            {
                useDataSourceNameRadioButton.Focus();
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

        private void SetDataSourceOption(object sender, EventArgs e)
        {
            if (useDataSourceNameRadioButton.Checked)
            {
                dataSourceNameTableLayoutPanel.Enabled = true;
                if (!_loading)
                {
                    string dsn = Properties["Dsn"] as string;
                    string uid = (Properties.Contains("uid")) ? Properties["uid"] as string : null;
                    string pwd = (Properties.Contains("pwd")) ? Properties["pwd"] as string : null;
                    Properties.Parse(string.Empty);
                    Properties["Dsn"] = dsn;
                    Properties["uid"] = uid;
                    Properties["pwd"] = pwd;
                }
                UpdateControls();
                connectionStringTableLayoutPanel.Enabled = false;
            }
            else /* if (useConnectionStringRadioButton.Checked) */
            {
                dataSourceNameTableLayoutPanel.Enabled = false;
                if (!_loading)
                {
                    string dsn = Properties["Dsn"] as string;
                    string uid = (Properties.Contains("uid")) ? Properties["uid"] as string : null;
                    string pwd = (Properties.Contains("pwd")) ? Properties["pwd"] as string : null;
                    Properties.Parse(connectionStringTextBox.Text);
                    Properties["Dsn"] = dsn;
                    Properties["uid"] = uid;
                    Properties["pwd"] = pwd;
                }
                UpdateControls();
                connectionStringTableLayoutPanel.Enabled = true;
            }
        }

        private void HandleComboBoxDownKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                EnumerateDataSourceNames(sender, e);
            }
        }
#if NOTUSED
		private void SettingDataSourceName(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Dsn"] = (dataSourceNameComboBox.Text.Trim().Length > 0) ? dataSourceNameComboBox.Text.Trim() : null;
				if (dataSourceNameComboBox.Items.Count == 0 && _dataSourceNameEnumerationThread == null)
				{
					// Start an enumeration of data source names
					_dataSourceNameEnumerationThread = new Thread(new ThreadStart(EnumerateDataSourceNames));
					_dataSourceNameEnumerationThread.Start();
				}
			}
		}
#endif
        private void EnumerateDataSourceNames(object sender, EventArgs e)
        {
            if (dataSourceNameComboBox.Items.Count == 0)
            {
                Cursor currentCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
#if NOTUSED
					if (_dataSourceNameEnumerationThread == null ||
						_dataSourceNameEnumerationThread.ThreadState == ThreadState.Stopped)
					{
#endif
                    EnumerateDataSourceNames();
#if NOTUSED
					}
					else if (_dataSourceNameEnumerationThread.ThreadState == ThreadState.Running)
					{
						// Wait for the asynchronous enumeration to finish
						_dataSourceNameEnumerationThread.Join();

						// Populate the combo box now, rather than waiting for
						// the asynchronous call to be marshaled back to the UI
						// thread
						PopulateDataSourceNameComboBox();
					}
#endif
                }
                finally
                {
                    Cursor.Current = currentCursor;
                }
            }
        }

        private void SetDataSourceName(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties["Dsn"] = (dataSourceNameComboBox.Text.Length > 0) ? dataSourceNameComboBox.Text : null;
            }
            UpdateControls();
        }

        private void RefreshDataSourceNames(object sender, EventArgs e)
        {
            dataSourceNameComboBox.Items.Clear();
            EnumerateDataSourceNames(sender, e);
        }

        private void SetConnectionString(object sender, EventArgs e)
        {
            if (!_loading)
            {
                string pwd = (Properties.Contains("pwd")) ? Properties["pwd"] as string : null;
                try
                {
                    Properties.Parse(connectionStringTextBox.Text.Trim());
                }
                catch (ArgumentException ex)
                {
                    IUIService uiService = null;
                    if (ParentForm != null && ParentForm.Site != null)
                    {
                        uiService = ParentForm.Site.GetService(typeof(IUIService)) as IUIService;
                    }
                    if (uiService != null)
                    {
                        uiService.ShowError(ex);
                    }
                    else
                    {
                        RTLAwareMessageBox.Show(null, ex.Message, MessageBoxIcon.Exclamation);
                    }
                }
                if (connectionStringTextBox.Text.Trim().Length > 0 &&
                    !Properties.Contains("pwd") && pwd != null)
                {
                    Properties["pwd"] = pwd;
                }
                connectionStringTextBox.Text = Properties.ToDisplayString();
            }
            UpdateControls();
        }

        private void BuildConnectionString(object sender, EventArgs e)
        {
            IntPtr henv = IntPtr.Zero;
            IntPtr hdbc = IntPtr.Zero;
            short result = 0;
            try
            {
                result = NativeMethods.SQLAllocEnv(out henv);
                if (!NativeMethods.SQL_SUCCEEDED(result))
                {
                    throw new ApplicationException(Strings.OdbcConnectionUIControl_SQLAllocEnvFailed);
                }

                result = NativeMethods.SQLAllocConnect(henv, out hdbc);
                if (!NativeMethods.SQL_SUCCEEDED(result))
                {
                    throw new ApplicationException(Strings.OdbcConnectionUIControl_SQLAllocConnectFailed);
                }

                string currentConnectionString = Properties.ToFullString();
                System.Text.StringBuilder newConnectionString = new System.Text.StringBuilder(1024);
                result = NativeMethods.SQLDriverConnect(hdbc, ParentForm.Handle, currentConnectionString, (short)currentConnectionString.Length, newConnectionString, 1024, out short newConnectionStringLength, NativeMethods.SQL_DRIVER_PROMPT);
                if (!NativeMethods.SQL_SUCCEEDED(result) && result != NativeMethods.SQL_NO_DATA)
                {
                    // Try again without the current connection string, in case it was invalid
                    result = NativeMethods.SQLDriverConnect(hdbc, ParentForm.Handle, null, 0, newConnectionString, 1024, out newConnectionStringLength, NativeMethods.SQL_DRIVER_PROMPT);
                }
                if (!NativeMethods.SQL_SUCCEEDED(result) && result != NativeMethods.SQL_NO_DATA)
                {
                    throw new ApplicationException(Strings.OdbcConnectionUIControl_SQLDriverConnectFailed);
                }
                else
                {
                    NativeMethods.SQLDisconnect(hdbc);
                }

                if (newConnectionStringLength > 0)
                {
                    RefreshDataSourceNames(sender, e);
                    Properties.Parse(newConnectionString.ToString());
                    UpdateControls();
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

        private void SetUserName(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties["uid"] = (userNameTextBox.Text.Trim().Length > 0) ? userNameTextBox.Text.Trim() : null;
            }
            UpdateControls();
        }

        private void SetPassword(object sender, EventArgs e)
        {
            if (!_loading)
            {
                Properties["pwd"] = (passwordTextBox.Text.Length > 0) ? passwordTextBox.Text : null;
                passwordTextBox.Text = passwordTextBox.Text; // forces reselection of all text
            }
            UpdateControls();
        }

        private void TrimControlText(object sender, EventArgs e)
        {
            Control c = sender as Control;
            c.Text = c.Text.Trim();
            UpdateControls();
        }

        private void UpdateControls()
        {
            if (Properties["Dsn"] is string &&
                (Properties["Dsn"] as string).Length > 0 &&
                dataSourceNameComboBox.Items.Contains(Properties["Dsn"]))
            {
                dataSourceNameComboBox.Text = Properties["Dsn"] as string;
            }
            else
            {
                dataSourceNameComboBox.Text = null;
            }
            connectionStringTextBox.Text = Properties.ToDisplayString();
            if (Properties.Contains("uid"))
            {
                userNameTextBox.Text = Properties["uid"] as string;
            }
            else
            {
                userNameTextBox.Text = null;
            }
            if (Properties.Contains("pwd"))
            {
                passwordTextBox.Text = Properties["pwd"] as string;
            }
            else
            {
                passwordTextBox.Text = null;
            }
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
            _dataSourceNames = new object[dataTable.Rows.Count];
            for (int i = 0; i < _dataSourceNames.Length; i++)
            {
                _dataSourceNames[i] = dataTable.Rows[i]["SOURCES_NAME"] as string;
            }

            // Sort the list
            Array.Sort(_dataSourceNames);

            // Populate the server combo box items (must occur on the UI thread)
            if (Thread.CurrentThread == _uiThread)
            {
                PopulateDataSourceNameComboBox();
            }
            else if (IsHandleCreated)
            {
                BeginInvoke(new ThreadStart(PopulateDataSourceNameComboBox));
            }
        }

        private void PopulateDataSourceNameComboBox()
        {
            if (dataSourceNameComboBox.Items.Count == 0)
            {
                if (_dataSourceNames.Length > 0)
                {
                    dataSourceNameComboBox.Items.AddRange(_dataSourceNames);
                }
                else
                {
                    dataSourceNameComboBox.Items.Add(string.Empty);
                }
            }
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

        private IDataConnectionProperties Properties
        {
            get
            {
                return _connectionProperties;
            }
        }

        private bool _loading;
        private object[] _dataSourceNames;
        private Thread _uiThread;
        //		private Thread _dataSourceNameEnumerationThread;
        private IDataConnectionProperties _connectionProperties;
    }
}
