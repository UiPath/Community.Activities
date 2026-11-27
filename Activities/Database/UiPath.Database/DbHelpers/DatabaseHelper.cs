using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using Microsoft.Data.Sqlite;
using UiPath.Database.Extensibility;

namespace UiPath.Database
{
    public static class DatabaseHelper
    {
        /// <summary>
        /// Registers the built-in providers. Extra providers (for example Oracle, from the extended package) are
        /// registered when a connection of that provider is created.
        /// </summary>
        public static void RegisterFactories(bool isWindows)
        {
            DbWorkarounds.RegisterNativeLibraryResolver();

            DbProviderFactories.RegisterFactory(DatabaseConstants.SqlServerProvider, SqlClientFactory.Instance);

            //OLEDB driver is Windows proprietary - there is no support for other OS
            if (isWindows)
                DbProviderFactories.RegisterFactory(DatabaseConstants.OleDbProvider, OleDbFactory.Instance);

            DbProviderFactories.RegisterFactory(DatabaseConstants.OdbcProvider, OdbcFactory.Instance);
            DbProviderFactories.RegisterFactory(DatabaseConstants.SQLiteProvider, SqliteFactory.Instance);
        }

        /// <summary>
        /// Registers the providers that installed packages bring (for example Oracle, from the extended package), so that
        /// designers can list them and create their connection string builders.
        /// </summary>
        public static void RegisterExtraFactories()
        {
            foreach (IDatabaseProvider provider in DatabaseProviders.GetAll())
            {
                provider.Initialize();
                DbProviderFactories.RegisterFactory(provider.ProviderName, provider.Factory);
            }
        }
    }
}
