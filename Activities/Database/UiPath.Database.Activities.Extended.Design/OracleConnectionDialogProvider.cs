using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using UiPath.Data.ConnectionUI.Dialog;
using UiPath.Data.ConnectionUI.Dialog.Controls;
using UiPath.Data.ConnectionUI.Dialog.Properties;
using UiPath.Database;

namespace UiPath.Database.Extended.Design
{
    /// <summary>
    /// Offers Oracle in the connection dialog. It lives in the extended package together with the Oracle driver.
    /// </summary>
    public sealed class OracleConnectionDialogProvider : IConnectionDialogProvider
    {
        private const string DataSourceName = "OracleManagedDataAccess";

        private static readonly DataProvider Provider = CreateProvider();
        private static readonly DataSource Source = CreateSource();

        public DataProvider DataProvider => Provider;

        public DataSource DataSource => Source;

        private static DataProvider CreateProvider()
        {
            var descriptions = new Dictionary<string, string>
            {
                { DataSourceName, Resources.DataProvider_Oracle_DataSource_Description }
            };

            var uiControls = new Dictionary<string, System.Type>
            {
                { string.Empty, typeof(OracleConnectionUIControl) }
            };

            return new DataProvider(
                DatabaseConstants.OracleProvider,
                Resources.DataProvider_Oracle,
                Resources.DataProvider_Oracle_Short,
                Resources.DataProvider_Oracle_Description,
                typeof(OracleConnection),
                descriptions,
                uiControls,
                typeof(OracleConnectionProperties));
        }

        private static DataSource CreateSource()
        {
            var source = new DataSource(DataSourceName, Resources.DataProvider_Oracle);
            source.Providers.Add(Provider);
            return source;
        }
    }
}
