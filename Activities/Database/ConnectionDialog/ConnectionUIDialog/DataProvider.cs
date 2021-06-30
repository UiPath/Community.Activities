//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Data.ConnectionUI
{
    public class DataProvider
    {
        public DataProvider(string name, string displayName, string shortDisplayName)
            : this(name, displayName, shortDisplayName, null, null)
        {
        }

        public DataProvider(string name, string displayName, string shortDisplayName, string description)
            : this(name, displayName, shortDisplayName, description, null)
        {
        }

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

        public DataProvider(string name, string displayName, string shortDisplayName, string description, Type targetConnectionType, Type connectionUIControlType, Type connectionPropertiesType)
            : this(name, displayName, shortDisplayName, description, targetConnectionType, connectionPropertiesType)
        {
            if (connectionUIControlType == null)
            {
                throw new ArgumentNullException("connectionUIControlType");
            }

            _connectionUIControlTypes = new Dictionary<string, Type>
            {
                { string.Empty, connectionUIControlType }
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

        public static DataProvider SqlDataProvider
        {
            get
            {
                if (_sqlDataProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.SqlDataSource.Name, Strings.DataProvider_Sql_DataSource_Description },
                        { DataSource.MicrosoftSqlServerFileName, Strings.DataProvider_Sql_FileDataSource_Description }
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
                        "System.Data.SqlClient",
                        Strings.DataProvider_Sql,
                        Strings.DataProvider_Sql_Short,
                        Strings.DataProvider_Sql_Description,
                        typeof(System.Data.SqlClient.SqlConnection),
                        descriptions,
                        uiControls,
                        properties);
                }
                return _sqlDataProvider;
            }
        }

        private static DataProvider _sqlDataProvider;

        public static DataProvider OleDBDataProvider
        {
            get
            {
                if (_oleDBDataProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.SqlDataSource.Name, Strings.DataProvider_OleDB_SqlDataSource_Description },
                        { DataSource.AccessDataSource.Name, Strings.DataProvider_OleDB_AccessDataSource_Description }
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
                        Strings.DataProvider_OleDB,
                        Strings.DataProvider_OleDB_Short,
                        Strings.DataProvider_OleDB_Description,
                        typeof(System.Data.OleDb.OleDbConnection),
                        descriptions,
                        uiControls,
                        properties);
                }
                return _oleDBDataProvider;
            }
        }

        private static DataProvider _oleDBDataProvider;

        public static DataProvider OdbcDataProvider
        {
            get
            {
                if (_odbcDataProvider == null)
                {
                    Dictionary<string, string> descriptions = new Dictionary<string, string>
                    {
                        { DataSource.OdbcDataSource.Name, Strings.DataProvider_Odbc_DataSource_Description }
                    };

                    Dictionary<string, Type> uiControls = new Dictionary<string, Type>
                    {
                        { string.Empty, typeof(OdbcConnectionUIControl) }
                    };

                    _odbcDataProvider = new DataProvider(
                        "System.Data.Odbc",
                        Strings.DataProvider_Odbc,
                        Strings.DataProvider_Odbc_Short,
                        Strings.DataProvider_Odbc_Description,
                        typeof(System.Data.Odbc.OdbcConnection),
                        descriptions,
                        uiControls,
                        typeof(OdbcConnectionProperties));
                }
                return _odbcDataProvider;
            }
        }

        private static DataProvider _odbcDataProvider;

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

        public Type TargetConnectionType
        {
            get
            {
                return _targetConnectionType;
            }
        }

        public virtual string GetDescription(DataSource dataSource)
        {
            if (_dataSourceDescriptions != null && dataSource != null &&
                _dataSourceDescriptions.ContainsKey(dataSource.Name))
            {
                return _dataSourceDescriptions[dataSource.Name];
            }
            else
            {
                return _description;
            }
        }

        public IDataConnectionUIControl CreateConnectionUIControl()
        {
            return CreateConnectionUIControl(null);
        }

        public virtual IDataConnectionUIControl CreateConnectionUIControl(DataSource dataSource)
        {
            string key = null;
            if (_connectionUIControlTypes != null &&
                (dataSource != null && _connectionUIControlTypes.ContainsKey(key = dataSource.Name)) ||
                _connectionUIControlTypes.ContainsKey(key = string.Empty))
            {
                return Activator.CreateInstance(_connectionUIControlTypes[key]) as IDataConnectionUIControl;
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
                ((dataSource != null && _connectionPropertiesTypes.ContainsKey(key = dataSource.Name)) ||
                _connectionPropertiesTypes.ContainsKey(key = string.Empty)))
            {
                return Activator.CreateInstance(_connectionPropertiesTypes[key]) as IDataConnectionProperties;
            }
            else
            {
                return null;
            }
        }

        private string _name;
        private string _displayName;
        private string _shortDisplayName;
        private string _description;
        private Type _targetConnectionType;
        private IDictionary<string, string> _dataSourceDescriptions;
        private IDictionary<string, Type> _connectionUIControlTypes;
        private IDictionary<string, Type> _connectionPropertiesTypes;
    }
}