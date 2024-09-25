using Oracle.ManagedDataAccess.Client;
using System;
using System.Activities.Presentation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Data.ConnectionUI.Dialog.Controls;
using UiPath.Data.ConnectionUI.Dialog.Properties;

namespace UiPath.Data.ConnectionUI.Dialog
{
    public class DataProvider
    {
        private string _name;
        private string _displayName;
        private string _shortDisplayName;
        private string _description;
        private Type _targetConnectionType;
        private IDictionary<string, string> _dataSourceDescriptions;
        private IDictionary<string, Type> _connectionUIControlTypes;
        private IDictionary<string, Type> _connectionPropertiesTypes;
        private static DataProvider _sqlDataProvider;
        private static DataProvider _oleDBDataProvider;
        private static DataProvider _odbcDataProvider;
        private static DataProvider _oracleManagedDataAcessProvider;

        #region Public Properties
        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string DisplayName
        {
            get
            {
                return (_displayName != null) ? _displayName : _name;
            }
        }

        public string ShortDisplayName
        {
            get
            {
                return _shortDisplayName;
            }
        }

        public string Description
        {
            get
            {
                return GetDescription(null);
            }
        }

        public static DataProvider SqlDataProvider
        {
            get
            {
                if (_sqlDataProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.SqlDataSource.Name, Resources.DataProvider_Sql_DataSource_Description },
                        { DataSource.MicrosoftSqlServerFileName, Resources.DataProvider_Sql_FileDataSource_Description }
                    };

                    Dictionary<string, Type> uiControls = new Dictionary<string, Type>
                    {
                        { DataSource.SqlDataSource.Name, typeof(SqlConnectionUIControl) },
                        { DataSource.MicrosoftSqlServerFileName, typeof(SqlFileConnectionUIControl) },
                        { string.Empty, typeof(SqlConnectionUIControl) }
                    };

                    Dictionary<string, Type> properties = new Dictionary<string, Type>
                    {
                        { DataSource.MicrosoftSqlServerFileName, typeof(SqlFileConnectionProperties) },
                        { string.Empty, typeof(SqlConnectionProperties) }
                    };

                    _sqlDataProvider = new DataProvider(
                        "Microsoft.Data.SqlClient",
                        Resources.DataProvider_Sql,
                        Resources.DataProvider_Sql_Short,
                        Resources.DataProvider_Sql_Description,
                        typeof(Microsoft.Data.SqlClient.SqlConnection),
                        descriptions,
                        uiControls,
                        properties);
                }
                return _sqlDataProvider;
            }
        }

        public static DataProvider OleDBDataProvider
        {
            get
            {
                if (_oleDBDataProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.SqlDataSource.Name, Resources.DataProvider_OleDB_SqlDataSource_Description },
                        { DataSource.AccessDataSource.Name, Resources.DataProvider_OleDB_AccessDataSource_Description }
                    };

                    Dictionary<string, Type> uiControls = new Dictionary<string, Type>
                    {
                        { DataSource.SqlDataSource.Name, typeof(SqlConnectionUIControl) },
                        { DataSource.AccessDataSource.Name, typeof(AccessConnectionUIControl) },
                        { string.Empty, typeof(OleDBConnectionUIControl) }
                    };

                    Dictionary<string, Type> properties = new Dictionary<string, Type>
                    {
                        { DataSource.SqlDataSource.Name, typeof(OleDBSqlConnectionProperties) },
                        { DataSource.AccessDataSource.Name, typeof(OleDBAccessConnectionProperties) },
                        { string.Empty, typeof(OleDBConnectionProperties) }
                    };

                    _oleDBDataProvider = new DataProvider(
                        "System.Data.OleDb",
                        Resources.DataProvider_OleDB,
                        Resources.DataProvider_OleDB_Short,
                        Resources.DataProvider_OleDB_Description,
                        typeof(System.Data.OleDb.OleDbConnection),
                        descriptions,
                        uiControls,
                        properties);
                }
                return _oleDBDataProvider;
            }
        }

        public static DataProvider OdbcDataProvider
        {
            get
            {
                if (_odbcDataProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.OdbcDataSource.Name, Resources.DataProvider_Odbc_DataSource_Description }
                    };

                    Dictionary<string, Type> uiControls = new Dictionary<string, Type>
                    {
                        { string.Empty, typeof(OdbcConnectionUIControl) }
                    };

                    _odbcDataProvider = new DataProvider(
                        "System.Data.Odbc",
                        Resources.DataProvider_Odbc,
                        Resources.DataProvider_Odbc_Short,
                        Resources.DataProvider_Odbc_Description,
                        typeof(System.Data.Odbc.OdbcConnection),
                        descriptions,
                        uiControls,
                        typeof(OdbcConnectionProperties));
                }
                return _odbcDataProvider;
            }
        }

        public static DataProvider OracleManagedDataAccessProvider
        {
            get
            {
                if (_oracleManagedDataAcessProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.OracleManagedDataAccessSource.Name, Resources.DataProvider_Oracle_DataSource_Description }
                    };

                    Dictionary<string, Type> uiControls = new Dictionary<string, Type>
                    {
                        { string.Empty, typeof(OracleConnectionUIControl) }
                    };

                    _oracleManagedDataAcessProvider = new DataProvider(
                        "Oracle.ManagedDataAccess.Client",
                        Resources.DataProvider_Oracle,
                        Resources.DataProvider_Oracle_Short,
                        Resources.DataProvider_Oracle_Description,
                        typeof(OracleConnection),
                        descriptions,
                        uiControls,
                        typeof(OracleConnectionProperties)
                        );
                }
                return _oracleManagedDataAcessProvider;
            }
        }

        #endregion

        public DataProvider(string name, string displayName, string shortDisplayName, string description, Type targetConnectionType)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            _name = name;
            _displayName = displayName;
            _shortDisplayName = shortDisplayName;
            _description = description;
            _targetConnectionType = targetConnectionType;
        }

        public DataProvider(string name, string displayName, string shortDisplayName, string description, Type targetConnectionType, Type connectionPropertiesType)
            : this(name, displayName, shortDisplayName, description, targetConnectionType)
        {
            if (connectionPropertiesType == null)
            {
                throw new ArgumentNullException("connectionPropertiesType");
            }

            _connectionPropertiesTypes = new Dictionary<string, Type>
            {
                { string.Empty, connectionPropertiesType }
            };
        }

        public DataProvider(string name, string displayName, string shortDisplayName, string description, Type targetConnectionType, IDictionary<string, Type> connectionUIControlTypes, Type connectionPropertiesType)
            : this(name, displayName, shortDisplayName, description, targetConnectionType, connectionPropertiesType)
        {
            _connectionUIControlTypes = connectionUIControlTypes;
        }

        public DataProvider(string name, string displayName, string shortDisplayName, string description, Type targetConnectionType, IDictionary<string, string> dataSourceDescriptions, IDictionary<string, Type> connectionUIControlTypes, Type connectionPropertiesType)
        : this(name, displayName, shortDisplayName, description, targetConnectionType, connectionUIControlTypes, connectionPropertiesType)
        {
            _dataSourceDescriptions = dataSourceDescriptions;
        }

        public DataProvider(string name, string displayName, string shortDisplayName, string description, Type targetConnectionType, IDictionary<string, string> dataSourceDescriptions, IDictionary<string, Type> connectionUIControlTypes, IDictionary<string, Type> connectionPropertiesTypes)
           : this(name, displayName, shortDisplayName, description, targetConnectionType)
        {
            _dataSourceDescriptions = dataSourceDescriptions;
            _connectionUIControlTypes = connectionUIControlTypes;
            _connectionPropertiesTypes = connectionPropertiesTypes;
        }

        public virtual string GetDescription(DataSource dataSource)
        {
            if (_dataSourceDescriptions != null && dataSource != null && dataSource.Name != null &&
                _dataSourceDescriptions.ContainsKey(dataSource.Name))
            {
                return _dataSourceDescriptions[dataSource.Name];
            }
            else
            {
                return _description;
            }
        }

        public virtual WorkflowElementDialog CreateConnectionUIControl(DataSource dataSource, IDataConnectionProperties properties)
        {
            string key = null;
            if (_connectionUIControlTypes != null &&
                (dataSource != null && dataSource.Name != null && _connectionUIControlTypes.ContainsKey(key = dataSource.Name)) ||
                _connectionUIControlTypes.ContainsKey(key = string.Empty))
            {
                WorkflowElementDialog uiInterface = (WorkflowElementDialog)Activator.CreateInstance(_connectionUIControlTypes[key]);
                ((IDataConnectionUIControl)uiInterface).Initialize(properties);
                return uiInterface;
            }
            else
            {
                return null;
            }
        }

        public IDataConnectionProperties CreateConnectionProperties()
        {
            return CreateConnectionProperties(null);
        }

        public virtual IDataConnectionProperties CreateConnectionProperties(DataSource dataSource)
        {
            string key = null;
            if (_connectionPropertiesTypes != null &&
                ((dataSource != null && dataSource.Name!=null && _connectionPropertiesTypes.ContainsKey(key = dataSource.Name)) ||
                _connectionPropertiesTypes.ContainsKey(key = string.Empty)))
            {
                return Activator.CreateInstance(_connectionPropertiesTypes[key]) as IDataConnectionProperties;
            }
            else
            {
                return null;
            }
        }
    }
}
