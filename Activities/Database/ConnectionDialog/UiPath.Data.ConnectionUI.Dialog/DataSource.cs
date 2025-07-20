using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Data.ConnectionUI.Dialog.Properties;

namespace UiPath.Data.ConnectionUI.Dialog
{
    public class DataSource
    {
        private string _name;
        private string _displayName;
        private DataProvider _defaultProvider;
        private ICollection<DataProvider> _providers;
        private static DataSource _accessDataSource;
        private static DataSource _sqlDataSource;
        private static DataSource _sqlFileDataSource;
        private static DataSource _odbcDataSource;
        private static DataSource _oracleManagedDataAccessaSource;
        private static DataSource _unspecifiedDataSource;

        public const string MicrosoftSqlServerFileName = "MicrosoftSqlServerFile";

        #region Public Properties
        public static DataSource AccessDataSource
        {
            get
            {
                if (_accessDataSource == null)
                {
                    _accessDataSource = new DataSource("MicrosoftAccess", Resources.DataSource_MicrosoftAccess);
                    _accessDataSource.Providers.Add(DataProvider.OleDBDataProvider);
                }
                return _accessDataSource;
            }
        }
        
        public static DataSource SqlDataSource
        {
            get
            {
                if (_sqlDataSource == null)
                {
                    _sqlDataSource = new DataSource("MicrosoftSqlServer", Resources.DataSource_MicrosoftSqlServer);
                    _sqlDataSource.Providers.Add(DataProvider.SqlDataProvider);
                    _sqlDataSource.Providers.Add(DataProvider.OleDBDataProvider);
                    _sqlDataSource.DefaultProvider = DataProvider.SqlDataProvider;
                }
                return _sqlDataSource;
            }
        }

        public static DataSource SqlFileDataSource
        {
            get
            {
                if (_sqlFileDataSource == null)
                {
                    _sqlFileDataSource = new DataSource("MicrosoftSqlServerFile", Resources.DataSource_MicrosoftSqlServerFile);
                    _sqlFileDataSource.Providers.Add(DataProvider.SqlDataProvider);
                }
                return _sqlFileDataSource;
            }
        }

        public static DataSource OdbcDataSource
        {
            get
            {
                if (_odbcDataSource == null)
                {
                    _odbcDataSource = new DataSource("OdbcDsn", Resources.DataSource_MicrosoftOdbcDsn);
                    _odbcDataSource.Providers.Add(DataProvider.OdbcDataProvider);
                }
                return _odbcDataSource;
            }
        }

        public static DataSource OracleManagedDataAccessSource
        {
            get
            {
                if (_oracleManagedDataAccessaSource == null)
                {
                    _oracleManagedDataAccessaSource = new DataSource("OracleManagedDataAccess", Resources.DataProvider_Oracle);
                    _oracleManagedDataAccessaSource.Providers.Add(DataProvider.OracleManagedDataAccessProvider);
                }
                return _oracleManagedDataAccessaSource;
            }
        }

        public static DataSource UnspecifiedDataSource
        {
            get
            {
                if (_unspecifiedDataSource == null)
                {
                    _unspecifiedDataSource = DataSource.CreateUnspecified();
                    _unspecifiedDataSource.Providers.Add(DataProvider.OdbcDataProvider);
                    _unspecifiedDataSource.Providers.Add(DataProvider.OleDBDataProvider);
                    _unspecifiedDataSource.Providers.Add(DataProvider.SqlDataProvider);
                    _unspecifiedDataSource.Providers.Add(DataProvider.OracleManagedDataAccessProvider);
                }
                return _unspecifiedDataSource;
            }
        }

        public ICollection<DataProvider> Providers
        {
            get
            {
                return _providers;
            }
        }

        public DataProvider DefaultProvider
        {
            get
            {
                switch (_providers.Count)
                {
                    case 0:
                        Debug.Assert(_defaultProvider == null);
                        return null;
                    case 1:
                        // If there is only one data provider, it must be the default
                        IEnumerator<DataProvider> e = _providers.GetEnumerator();
                        e.MoveNext();
                        return e.Current;
                    default:
                        return (_name != null) ? _defaultProvider : null;
                }
            }
            set
            {
                if (_providers.Count == 1 && _defaultProvider != value)
                {
                    throw new InvalidOperationException(Resources.DataSource_CannotChangeSingleDataProvider);
                }
                if (value != null && !_providers.Contains(value))
                {
                    throw new InvalidOperationException(Resources.DataSource_DataProviderNotFound);
                }
                _defaultProvider = value;
            }
        }

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
        #endregion

        public DataSource()
        {
            _displayName = Resources.DataSource_UnspecifiedDisplayName;
            _providers = new List<DataProvider>();
        }

        public DataSource(string name, string displayName)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            _name = name;
            _displayName = displayName;
            _providers = new List<DataProvider>();
        }
        
        internal static DataSource CreateUnspecified()
        {
            return new DataSource();
        }
    }
}
