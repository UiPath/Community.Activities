using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;

namespace UiPath.Database
{
    public static class DatabaseHelper
    {
        private static bool _sqlNetAllowedLogonVersionClientSet;
        public static void RegisterFactories(bool isWindows)
        {
            DbProviderFactories.RegisterFactory(DatabaseConstants.SqlServerProvider, SqlClientFactory.Instance);

            //OLEDB driver is Windows propietary - there is no support for other OS
            if (isWindows)
                DbProviderFactories.RegisterFactory(DatabaseConstants.OleDbProvider, OleDbFactory.Instance);

            DbProviderFactories.RegisterFactory(DatabaseConstants.OdbcProvider, OdbcFactory.Instance);
            DbProviderFactories.RegisterFactory(DatabaseConstants.OracleProvider, OracleClientFactory.Instance);

            SetSqlNetAllowedLogonVersionClientSetForOracle();
        }

        public static void SetSqlNetAllowedLogonVersionClientSetForOracle()
        {
            if (_sqlNetAllowedLogonVersionClientSet)
                return;

            OracleConfiguration.SqlNetAllowedLogonVersionClient = OracleAllowedLogonVersionClient.Version8;
            _sqlNetAllowedLogonVersionClientSet = true;
        }
    }
}
