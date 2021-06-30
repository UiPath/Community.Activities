//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Data.OleDb;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms.Design;
using System.Runtime.InteropServices;

using ThreadState = System.Threading.ThreadState;

namespace Microsoft.Data.ConnectionUI
{
	public partial class OleDBConnectionUIControl : UserControl, IDataConnectionUIControl
	{
		public OleDBConnectionUIControl()
		{
			InitializeComponent();
			RightToLeft = RightToLeft.Inherit;
			_uiThread = Thread.CurrentThread;
		}

		public void Initialize(IDataConnectionProperties connectionProperties)
		{
			Initialize(connectionProperties, false);
		}

		public void Initialize(IDataConnectionProperties connectionProperties, bool disableProviderSelection)
		{
			if (!(connectionProperties is OleDBConnectionProperties))
			{
				throw new ArgumentException(Strings.OleDBConnectionUIControl_InvalidConnectionProperties);
			}

			EnumerateProviders();
			providerComboBox.Enabled = !disableProviderSelection;
			dataLinksButton.Enabled = false;
			dataSourceGroupBox.Enabled = false;
			logonGroupBox.Enabled = false;
			initialCatalogLabel.Enabled = false;
			initialCatalogComboBox.Enabled = false;

			_connectionProperties = connectionProperties;
		}

		public void LoadProperties()
		{
			_loading = true;

			string provider = Properties["Provider"] as string;
			if (provider != null && provider.Length > 0)
			{
				object candidate = null;
				foreach (ProviderStruct providerStruct in providerComboBox.Items)
				{
					if (providerStruct.ProgId.Equals(provider))
					{
						candidate = providerStruct;
						break;
					}
					if (providerStruct.ProgId.StartsWith(provider + ".", StringComparison.OrdinalIgnoreCase))
					{
						if (candidate == null ||
							providerStruct.ProgId.CompareTo(((ProviderStruct)candidate).ProgId) > 0)
						{
							candidate = providerStruct;
						}
					}
				}
				providerComboBox.SelectedItem = candidate;
			}
			else
			{
				providerComboBox.SelectedItem = null;
			}

			if (Properties.Contains("Data Source") &&
				Properties["Data Source"] is string)
			{
				dataSourceTextBox.Text = Properties["Data Source"] as string;
			}
			else
			{
				dataSourceTextBox.Text = null;
			}
			if (Properties.Contains("Location") &&
				Properties["Location"] is string)
			{
				locationTextBox.Text = Properties["Location"] as string;
			}
			else
			{
				locationTextBox.Text = null;
			}
			if (Properties.Contains("Integrated Security") &&
				Properties["Integrated Security"] is string &&
				(Properties["Integrated Security"] as string).Length > 0)
			{
				integratedSecurityRadioButton.Checked = true;
			}
			else
			{
				nativeSecurityRadioButton.Checked = true;
			}
			if (Properties.Contains("User ID") &&
				Properties["User ID"] is string)
			{
				userNameTextBox.Text = Properties["User ID"] as string;
			}
			else
			{
				userNameTextBox.Text = null;
			}
			if (Properties.Contains("Password") &&
				Properties["Password"] is string)
			{
				passwordTextBox.Text = Properties["Password"] as string;
				blankPasswordCheckBox.Checked = (passwordTextBox.Text.Length == 0);
			}
			else
			{
				passwordTextBox.Text = null;
				blankPasswordCheckBox.Checked = false;
			}
			if (Properties.Contains("Persist Security Info") &&
				Properties["Persist Security Info"] is bool)
			{
				allowSavingPasswordCheckBox.Checked = (bool)Properties["Persist Security Info"];
			}
			else
			{
				allowSavingPasswordCheckBox.Checked = false;
			}
			if (Properties.Contains("Initial Catalog") &&
				Properties["Initial Catalog"] is string)
			{
				initialCatalogComboBox.Text = Properties["Initial Catalog"] as string;
			}
			else
			{
				initialCatalogComboBox.Text = null;
			}

			_loading = false;
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			Size preferredSize = base.GetPreferredSize(proposedSize);

			// We have these "Blank Password" and "Allow Saving Password"
			// check boxes on the same line.  In long text languages, this
			// doesn't always fit.  Tweak the preferred size to account for
			// this.
			int preferredWidth =
				logonGroupBox.Padding.Left +
				loginTableLayoutPanel.Margin.Left +
				blankPasswordCheckBox.Margin.Left +
				blankPasswordCheckBox.Width +
				blankPasswordCheckBox.Margin.Right +
				allowSavingPasswordCheckBox.Margin.Left +
				allowSavingPasswordCheckBox.Width +
				allowSavingPasswordCheckBox.Margin.Right +
				loginTableLayoutPanel.Margin.Right +
				logonGroupBox.Padding.Right;
			if (preferredWidth > preferredSize.Width)
			{
				preferredSize = new Size(preferredWidth, preferredSize.Height);
			}

			return preferredSize;
		}

		// Simulate RTL mirroring
		protected override void OnRightToLeftChanged(EventArgs e)
		{
			base.OnRightToLeftChanged(e);
			if (ParentForm != null &&
				ParentForm.RightToLeftLayout == true &&
				RightToLeft == RightToLeft.Yes)
			{
				LayoutUtils.MirrorControl(providerLabel, providerTableLayoutPanel);
				LayoutUtils.MirrorControl(integratedSecurityRadioButton);
				LayoutUtils.MirrorControl(nativeSecurityRadioButton);
				LayoutUtils.MirrorControl(loginTableLayoutPanel);
				LayoutUtils.MirrorControl(initialCatalogLabel, initialCatalogComboBox);
			}
			else
			{
				LayoutUtils.UnmirrorControl(initialCatalogLabel, initialCatalogComboBox);
				LayoutUtils.UnmirrorControl(loginTableLayoutPanel);
				LayoutUtils.UnmirrorControl(nativeSecurityRadioButton);
				LayoutUtils.UnmirrorControl(integratedSecurityRadioButton);
				LayoutUtils.UnmirrorControl(providerLabel, providerTableLayoutPanel);
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

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (Parent == null)
			{
				OnFontChanged(e);
			}
		}

		private void EnumerateProviders()
		{
			Cursor currentCursor = Cursor.Current;
			OleDbDataReader dr = null;
			try
			{
				Cursor.Current = Cursors.WaitCursor;

				// Get the sources rowset for the root OLE DB enumerator
				dr = OleDbEnumerator.GetEnumerator(Type.GetTypeFromCLSID(NativeMethods.CLSID_OLEDB_ENUMERATOR));

				// Get the CLSIDs and descriptions of each data source (not binders or enumerators)
				Dictionary<string, string> sources = new Dictionary<string, string>(); // avoids duplicate entries
				while (dr.Read())
				{
					int type = dr.GetInt32(dr.GetOrdinal("SOURCES_TYPE"));
					if (type == NativeMethods.DBSOURCETYPE_DATASOURCE_TDP ||
						type == NativeMethods.DBSOURCETYPE_DATASOURCE_MDP)
					{
						string clsId = dr["SOURCES_CLSID"].ToString();
						string description = dr["SOURCES_DESCRIPTION"].ToString();
						sources[clsId] = description;
					}
				}

				// Get the full ProgID for each data source
				Dictionary<string, string> sourceProgIds = new Dictionary<string, string>(sources.Count);
                Win32.RegistryKey key = Win32.Registry.ClassesRoot.OpenSubKey("CLSID");
				using (key)
				{
					foreach (KeyValuePair<string, string> source in sources)
					{
                        Win32.RegistryKey subKey = key.OpenSubKey(source.Key + "\\ProgID");
						if (subKey != null)
						{
							using (subKey)
							{
								string progId = key.OpenSubKey(source.Key + "\\ProgID").GetValue(null) as string;
								if (progId != null &&
									!progId.Equals("MSDASQL", StringComparison.OrdinalIgnoreCase) &&
									!progId.StartsWith("MSDASQL.", StringComparison.OrdinalIgnoreCase) &&
									!progId.Equals("Microsoft OLE DB Provider for ODBC Drivers"))
								{
									sourceProgIds[progId] = source.Key;
								}
							} // subKey is disposed here
						}
					}
				} // key is disposed here

				// Populate the combo box
				foreach (KeyValuePair<string, string> entry in sourceProgIds)
				{
					providerComboBox.Items.Add(new ProviderStruct(entry.Key, sources[entry.Value]));
				}
			}
			finally
			{
				if (dr != null)
				{
					dr.Dispose();
				}

				Cursor.Current = currentCursor;
			}
		}

		private void SetProvider(object sender, EventArgs e)
		{
			if (providerComboBox.SelectedItem is ProviderStruct)
			{
				// Set the provider to initialize the correct set of properties
				if (!_loading)
				{
					Properties["Provider"] = ((ProviderStruct)providerComboBox.SelectedItem).ProgId;
				}

				// Remove all miscellaneous properties because they just get in the way
				foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(Properties))
				{
					if (descriptor.Category.Equals(CategoryAttribute.Default.Category, StringComparison.CurrentCulture))
					{
						Properties.Remove(descriptor.DisplayName);
					}
				}

				// Enable data links button
				dataLinksButton.Enabled = true;

				// Enable all container controls
				dataSourceGroupBox.Enabled = true;
				logonGroupBox.Enabled = true;
				loginTableLayoutPanel.Enabled = true;
				initialCatalogLabel.Enabled = true;
				initialCatalogComboBox.Enabled = true;

				// Initially disable all end user controls
				dataSourceLabel.Enabled = false;
				dataSourceTextBox.Enabled = false;
				locationLabel.Enabled = false;
				locationTextBox.Enabled = false;
				integratedSecurityRadioButton.Enabled = false;
				nativeSecurityRadioButton.Enabled = false;
				userNameLabel.Enabled = false;
				userNameTextBox.Enabled = false;
				passwordLabel.Enabled = false;
				passwordTextBox.Enabled = false;
				blankPasswordCheckBox.Enabled = false;
				allowSavingPasswordCheckBox.Enabled = false;
				initialCatalogLabel.Enabled = false;
				initialCatalogComboBox.Enabled = false;

				// Now selectively enable those that are supported
				PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(Properties);
				PropertyDescriptor propertyDescriptor = null;
				if ((propertyDescriptor = propertyDescriptors["DataSource"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					dataSourceLabel.Enabled = true;
					dataSourceTextBox.Enabled = true;
				}
				if ((propertyDescriptor = propertyDescriptors["Location"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					locationLabel.Enabled = true;
					locationTextBox.Enabled = true;
				}
				dataSourceGroupBox.Enabled = (dataSourceTextBox.Enabled || locationTextBox.Enabled);
				if ((propertyDescriptor = propertyDescriptors["Integrated Security"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					integratedSecurityRadioButton.Enabled = true;
				}
				if ((propertyDescriptor = propertyDescriptors["User ID"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					userNameLabel.Enabled = true;
					userNameTextBox.Enabled = true;
				}
				if ((propertyDescriptor = propertyDescriptors["Password"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					passwordLabel.Enabled = true;
					passwordTextBox.Enabled = true;
					blankPasswordCheckBox.Enabled = true;
				}
				if (passwordTextBox.Enabled &&
					(propertyDescriptor = propertyDescriptors["PersistSecurityInfo"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					allowSavingPasswordCheckBox.Enabled = true;
				}
				loginTableLayoutPanel.Enabled = (userNameTextBox.Enabled || passwordTextBox.Enabled);
				nativeSecurityRadioButton.Enabled = loginTableLayoutPanel.Enabled;
				logonGroupBox.Enabled = (integratedSecurityRadioButton.Enabled || nativeSecurityRadioButton.Enabled);
				if ((propertyDescriptor = propertyDescriptors["Initial Catalog"]) != null &&
					propertyDescriptor.IsBrowsable)
				{
					initialCatalogLabel.Enabled = true;
					initialCatalogComboBox.Enabled = true;
				}
			}
			else
			{
				if (!_loading)
				{
					Properties["Provider"] = null;
				}

				// Disable data links button
				dataLinksButton.Enabled = false;

				// Disable all container controls
				dataSourceGroupBox.Enabled = false;
				logonGroupBox.Enabled = false;
				initialCatalogLabel.Enabled = false;
				initialCatalogComboBox.Enabled = false;
			}

			if (!_loading)
			{
				LoadProperties();
			}

			initialCatalogComboBox.Items.Clear(); // a provider change requires a refresh here
		}

		private void SetProviderDropDownWidth(object sender, EventArgs e)
		{
			if (providerComboBox.Items.Count > 0)
			{
				int largestWidth = 0;
				using (Graphics g = Graphics.FromHwnd(providerComboBox.Handle))
				{
					foreach (ProviderStruct providerStruct in providerComboBox.Items)
					{
						int width = TextRenderer.MeasureText(
							g,
							providerStruct.Description,
							providerComboBox.Font,
							new Size(int.MaxValue, int.MaxValue),
							TextFormatFlags.WordBreak
						).Width;
						if (width > largestWidth)
						{
							largestWidth = width;
						}
					}
				}
				providerComboBox.DropDownWidth = largestWidth + 3; // give a little extra margin
				if (providerComboBox.Items.Count > providerComboBox.MaxDropDownItems)
				{
					providerComboBox.DropDownWidth += SystemInformation.VerticalScrollBarWidth;
				}
			}
			else
			{
				providerComboBox.DropDownWidth = providerComboBox.Width;
			}
		}

		private void ShowDataLinks(object sender, EventArgs e)
		{
			try
			{
				// Create data links object as IDataInitialize
				Type dataLinksType = Type.GetTypeFromCLSID(NativeMethods.CLSID_DataLinks);
				NativeMethods.IDataInitialize dataInitialize = Activator.CreateInstance(dataLinksType) as NativeMethods.IDataInitialize;

				// Create data source object from connection string
				object dataSource = null;
				dataInitialize.GetDataSource(null,
					NativeMethods.CLSCTX_INPROC_SERVER,
					Properties.ToFullString(),
					ref NativeMethods.IID_IUnknown,
					ref dataSource);

				// Get IDBPromptInitialize interface from data links object
				NativeMethods.IDBPromptInitialize promptInitialize = (NativeMethods.IDBPromptInitialize)dataInitialize;

				// Display the data links dialog using this data source
				promptInitialize.PromptDataSource(
					null,
					ParentForm.Handle,
					NativeMethods.DBPROMPTOPTIONS_PROPERTYSHEET | NativeMethods.DBPROMPTOPTIONS_DISABLE_PROVIDER_SELECTION,
					0,
					IntPtr.Zero,
					null,
					ref NativeMethods.IID_IUnknown,
					ref dataSource);

                // Retrieve the new connection string from the data source
                dataInitialize.GetInitializationString(dataSource, true, out string newConnectionString);

                // Parse the new connection string into the connection properties object
                Properties.Parse(newConnectionString);

				// Reload the control with the modified connection properties
				LoadProperties();
			}
			catch (Exception ex)
			{
				COMException comex = ex as COMException;
				if (comex == null || comex.ErrorCode != NativeMethods.DB_E_CANCELED)
				{
					IUIService uiService = GetService(typeof(IUIService)) as IUIService;
					if (uiService != null)
					{
						uiService.ShowError(ex);
					}
					else
					{
						RTLAwareMessageBox.Show(null, ex.Message, MessageBoxIcon.Exclamation);
					}
				}
			}
		}

		private void SetDataSource(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Data Source"] = (dataSourceTextBox.Text.Trim().Length > 0) ? dataSourceTextBox.Text.Trim() : null;
			}
			initialCatalogComboBox.Items.Clear(); // a server change requires a refresh here
		}

		private void SetLocation(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Location"] = locationTextBox.Text;
			}
			initialCatalogComboBox.Items.Clear(); // a server change requires a refresh here
		}

		private void SetSecurityOption(object sender, EventArgs e)
		{
			if (!_loading)
			{
				if (integratedSecurityRadioButton.Checked)
				{
					Properties["Integrated Security"] = "SSPI";
					Properties.Reset("User ID");
					Properties.Reset("Password");
					Properties.Reset("Persist Security Info");
				}
				else
				{
					Properties.Reset("Integrated Security");
					SetUserName(sender, e);
					SetPassword(sender, e);
					SetBlankPassword(sender, e);
					SetAllowSavingPassword(sender, e);
				}
			}
			loginTableLayoutPanel.Enabled = !integratedSecurityRadioButton.Checked;
			initialCatalogComboBox.Items.Clear(); // an authentication change requires a refresh here
		}

		private void SetUserName(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["User ID"] = (userNameTextBox.Text.Trim().Length > 0) ? userNameTextBox.Text.Trim() : null;
			}
			initialCatalogComboBox.Items.Clear(); // a user name change requires a refresh here
		}

		private void SetPassword(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Password"] = (passwordTextBox.Text.Length > 0) ? passwordTextBox.Text : null;
				if (passwordTextBox.Text.Length == 0)
				{
					Properties.Remove("Password");
				}
				passwordTextBox.Text = passwordTextBox.Text; // forces reselection of all text
			}
			initialCatalogComboBox.Items.Clear(); // a password change requires a refresh here
		}

		private void SetBlankPassword(object sender, EventArgs e)
		{
			if (blankPasswordCheckBox.Checked)
			{
				if (!_loading)
				{
					Properties["Password"] = string.Empty;
				}
				passwordLabel.Enabled = false;
				passwordTextBox.Enabled = false;
			}
			else
			{
				if (!_loading)
				{
					SetPassword(sender, e);
				}
				passwordLabel.Enabled = true;
				passwordTextBox.Enabled = true;
			}
		}

		private void SetAllowSavingPassword(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Persist Security Info"] = allowSavingPasswordCheckBox.Checked;
			}
		}

		private void HandleComboBoxDownKey(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down)
			{
				EnumerateCatalogs(sender, e);
			}
		}

		private void SetInitialCatalog(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Initial Catalog"] = (initialCatalogComboBox.Text.Trim().Length > 0) ? initialCatalogComboBox.Text.Trim() : null;
				if (initialCatalogComboBox.Items.Count == 0 && _catalogEnumerationThread == null)
				{
					// Start an enumeration of initial catalogs
					_catalogEnumerationThread = new Thread(new ThreadStart(EnumerateCatalogs));
					_catalogEnumerationThread.Start();
				}
			}
		}

		private void EnumerateCatalogs(object sender, EventArgs e)
		{
			if (initialCatalogComboBox.Items.Count == 0)
			{
				Cursor currentCursor = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				try
				{
					if (_catalogEnumerationThread == null ||
						_catalogEnumerationThread.ThreadState == ThreadState.Stopped)
					{
						EnumerateCatalogs();
					}
					else if (_catalogEnumerationThread.ThreadState == ThreadState.Running)
					{
						// Wait for the asynchronous enumeration to finish
						_catalogEnumerationThread.Join();

						// Populate the combo box now, rather than waiting for
						// the asynchronous call to be marshaled back to the UI
						// thread
						PopulateInitialCatalogComboBox();
					}
				}
				finally
				{
					Cursor.Current = currentCursor;
				}
			}
		}

		private void TrimControlText(object sender, EventArgs e)
		{
			Control c = sender as Control;
			c.Text = c.Text.Trim();
		}

		private void EnumerateCatalogs()
		{
			// Perform the enumeration
			DataTable dataTable = null;
			OleDbConnection connection = null;
			try
			{
				// Create a connection string without initial catalog
				OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder(Properties.ToFullString());
				builder.Remove("Initial Catalog");

				// Create a connection
				connection = new OleDbConnection(builder.ConnectionString);

				// Open the connection
				connection.Open();

				// Try to get the DBSCHEMA_CATALOGS schema rowset
				dataTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Catalogs, null);
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
				if (connection != null)
				{
					connection.Dispose();
				}
			}

			// Create the object array of catalog names
			_catalogs = new object[dataTable.Rows.Count];
			for (int i = 0; i < _catalogs.Length; i++)
			{
				_catalogs[i] = dataTable.Rows[i]["CATALOG_NAME"];
			}

			// Populate the initial catalog combo box items (must occur on the UI thread)
			if (Thread.CurrentThread == _uiThread)
			{
				PopulateInitialCatalogComboBox();
			}
			else if (IsHandleCreated)
			{
				BeginInvoke(new ThreadStart(PopulateInitialCatalogComboBox));
			}
		}

		private void PopulateInitialCatalogComboBox()
		{
			if (initialCatalogComboBox.Items.Count == 0)
			{
				if (_catalogs.Length > 0)
				{
					initialCatalogComboBox.Items.AddRange(_catalogs);
				}
				else
				{
					initialCatalogComboBox.Items.Add(string.Empty);
				}
			}
			_catalogEnumerationThread = null;
		}

		private IDataConnectionProperties Properties
		{
			get
			{
				return _connectionProperties;
			}
		}

		private struct ProviderStruct
		{
			public ProviderStruct(string progId, string description)
			{
				_progId = progId;
				_description = description;
			}

			public string ProgId
			{
				get
				{
					return _progId;
				}
			}

			public string Description
			{
				get
				{
					return _description;
				}
			}

			public override string ToString()
			{
				return _description;
			}

			private string _progId;
			private string _description;
		}

		private bool _loading;
		private object[] _catalogs;
		private Thread _uiThread;
		private Thread _catalogEnumerationThread;
		private IDataConnectionProperties _connectionProperties;
	}
}
