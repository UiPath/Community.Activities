using System;
using System.Activities.Presentation;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    /// <summary>
    /// Interaction logic for OleDBConnectionUIControl.xaml
    /// </summary>
    public partial class OleDBConnectionUIControl : WorkflowElementDialog, IDataConnectionUIControl
	{
		List<ProviderType> _providers = null;
		private List<string> _databases;
		private IDataConnectionProperties _connectionProperties;

        #region Public Properties
        public List<String> Databases
		{
			get { return _databases; }
		}

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
		
		public string OleDbProvider
		{
			get
			{
				return (string)_connectionProperties["Provider"];
			}
			set
			{
				_connectionProperties["Provider"] = value;
			}
		}

		public string Datasource
		{
			get
			{
				return (string)_connectionProperties["Data Source"];
			}
			set
			{
				_connectionProperties["Data Source"] = value;
			}
		}

		public string Location
		{
			get
			{
				return (string)_connectionProperties["Location"];
			}
			set
			{
				_connectionProperties["Location"] = value;
			}
		}

		public bool UseWindowsAuthentication
		{
			get
			{
				return _connectionProperties.Contains("Integrated Security") &&
						   _connectionProperties["Integrated Security"] is string &&
						   (_connectionProperties["Integrated Security"] as string).Length>0;
			}
			set
			{
				string val = (value) ? "SSPI" : null;
				if (value)
				{
					_connectionProperties.Reset("User ID");
					_connectionProperties.Reset("Password");
					_connectionProperties.Reset("Persist Security Info");
				}
				_connectionProperties["Integrated Security"] = val;
			}
		}

		public bool SavePassword
		{
			get
			{
				if (_connectionProperties.Contains("Persist Security Info") &&
				_connectionProperties["Persist Security Info"] is bool)
					return (bool)_connectionProperties["Persist Security Info"];
				return false;
			}
			set
			{
				_connectionProperties["Persist Security Info"] = value;
			}
		}

		public string InitialCatalog
		{
			get
			{
				return (string)_connectionProperties["Initial Catalog"];
			}
			set
			{
				_connectionProperties["Initial Catalog"] = value;
			}
		}

		public List<ProviderType> Providers
		{
			get { return _providers; }
		}
		#endregion

		public OleDBConnectionUIControl()
        {
			InitializeComponent();
        }

		public void Initialize(IDataConnectionProperties connectionProperties)
		{
			if (!(connectionProperties is OleDBConnectionProperties))
			{
				throw new ArgumentException(Properties.Resources.OleDBConnectionUIControl_InvalidConnectionProperties);
			}
			EnumerateProviders();
			_connectionProperties = connectionProperties;
		}

		private void DataLink_Click(object sender, RoutedEventArgs e)
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
					_connectionProperties.ToFullString(),
					ref NativeMethods.IID_IUnknown,
					ref dataSource);

				// Get IDBPromptInitialize interface from data links object
				NativeMethods.IDBPromptInitialize promptInitialize = (NativeMethods.IDBPromptInitialize)dataInitialize;

				// Display the data links dialog using this data source
				promptInitialize.PromptDataSource(
					null,
					 new WindowInteropHelper(Window.GetWindow(this)).Handle,
					NativeMethods.DBPROMPTOPTIONS_PROPERTYSHEET | NativeMethods.DBPROMPTOPTIONS_DISABLE_PROVIDER_SELECTION,
					0,
					IntPtr.Zero,
					null,
					ref NativeMethods.IID_IUnknown,
					ref dataSource);

				// Retrieve the new connection string from the data source
				dataInitialize.GetInitializationString(dataSource, true, out string newConnectionString);

				// Parse the new connection string into the connection properties object
				_connectionProperties.Parse(newConnectionString);

				// Reload the control with the modified connection properties
				VisualTreeHelpers.RefreshBindings(this);
				passwordTextbox.Password = Password;
			}
			catch (Exception ex)
			{
				COMException comex = ex as COMException;
				if (comex == null || comex.ErrorCode != NativeMethods.DB_E_CANCELED)
				{
					MessageBox.Show(ex.Message, Properties.Resources.Error_Label, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
			Password = ((PasswordBox)sender).Password;
		}

        private void ProviderCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			locationLabel.IsEnabled = false;
			locationTextBox.IsEnabled = false;
			sqlAuthentication.IsChecked = true;
			integraredSecRadioButton.IsEnabled = false;
			sqlAuthentication.IsEnabled = false;
			usernameTextbox.IsEnabled = false;
			usernameLabel.IsEnabled = false;
			passwordLabel.IsEnabled = false;
			passwordTextbox.IsEnabled = false;
			savepasswordCheckbox.IsEnabled = false;
			initialCatalogGroup.IsEnabled = false;
			sqlAuthentication.IsChecked = true;

			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(_connectionProperties);
			PropertyDescriptor propertyDescriptor = null;

			if ((propertyDescriptor = propertyDescriptors["Location"]) != null &&
				propertyDescriptor.IsBrowsable)
			{
				locationLabel.IsEnabled = true;
				locationTextBox.IsEnabled = true;
			}
			if ((propertyDescriptor = propertyDescriptors["Integrated Security"]) != null &&
				propertyDescriptor.IsBrowsable)
			{
				integraredSecRadioButton.IsEnabled = true;
			}
			if ((propertyDescriptor = propertyDescriptors["User ID"]) != null &&
				propertyDescriptor.IsBrowsable)
			{
				usernameTextbox.IsEnabled = true;
				usernameLabel.IsEnabled = true;
				sqlAuthentication.IsEnabled = true;
			}
			if (_connectionProperties["Password"] != null)
			{
				passwordLabel.IsEnabled = true;
				passwordTextbox.IsEnabled = true;
				sqlAuthentication.IsEnabled = true;
			}
			if (_connectionProperties["Password"] != null &&
					(propertyDescriptor = propertyDescriptors["PersistSecurityInfo"]) != null &&
					propertyDescriptor.IsBrowsable)
			{
				savepasswordCheckbox.IsEnabled = true;
			}
			if ((propertyDescriptor = propertyDescriptors["Initial Catalog"]) != null &&
					propertyDescriptor.IsBrowsable)
			{
				initialCatalogGroup.IsEnabled = true;
			}
			VisualTreeHelpers.RefreshBindings(this);
		}

		private void DatabaseCombobox_DropDownOpened(object sender, EventArgs e)
		{
			EnumerateCatalogs();
		}

		private void EnumerateProviders()
		{
			OleDbDataReader dr = null;
			try
			{
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
				_providers = new List<ProviderType>();
				Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("CLSID");
				using (key)
				{
					foreach (KeyValuePair<string, string> source in sources)
					{
						Microsoft.Win32.RegistryKey subKey = key.OpenSubKey(source.Key + "\\ProgID");
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
					_providers.Add(new ProviderType() { Name = entry.Key, DisplayName = sources[entry.Value] });

				}
				_providers = _providers.OrderBy(x => x.DisplayName).ToList<ProviderType>();
			}
			finally
			{
				if (dr != null)
				{
					dr.Dispose();
				}
			}
		}

		private void EnumerateCatalogs()
		{
			// Perform the enumeration
			DataTable dataTable = null;
			OleDbConnection connection = null;
			try
			{
				// Create a connection string without initial catalog
				OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder(_connectionProperties.ToFullString());
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
			_databases = new List<string>();
			for (int i = 0; i < dataTable.Rows.Count; i++)
			{
				_databases.Add(dataTable.Rows[i]["CATALOG_NAME"] as string);
			}
			databaseCombobox.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
		}

		public struct ProviderType
		{
			public string Name { get; set; }
			public string DisplayName { get; set; }
		}
    }
}