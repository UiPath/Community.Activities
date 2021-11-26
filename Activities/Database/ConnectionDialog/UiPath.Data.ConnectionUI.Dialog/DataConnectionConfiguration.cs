using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UiPath.Data.ConnectionUI.Dialog
{
    public class DataConnectionConfiguration 
    {
        // Available data sources:
        private IDictionary<string, DataSource> dataSources;

        // Available data providers: 
        private IDictionary<string, DataProvider> dataProviders;

        public XElement RootElement { get; set; }

        public IDictionary<string, DataSource> DataSources
        {
            get { return dataSources; }
        }

        public IDictionary<string, DataProvider> Providers
        {
            get { return dataProviders; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Configuration file path.</param>
        public DataConnectionConfiguration()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            dataSources = new Dictionary<string, DataSource>
            {
                { DataSource.AccessDataSource.Name, DataSource.AccessDataSource },
                { DataSource.SqlDataSource.Name, DataSource.SqlDataSource },
                { DataSource.SqlFileDataSource.Name, DataSource.SqlFileDataSource },
                { DataSource.OdbcDataSource.Name, DataSource.OdbcDataSource },
                { DataSource.OracleManagedDataAccessSource.Name, DataSource.OracleManagedDataAccessSource },
                { DataSource.UnspecifiedDataSource.DisplayName, DataSource.UnspecifiedDataSource }
            };

            dataProviders = new Dictionary<string, DataProvider>
            {
                { DataProvider.SqlDataProvider.Name, DataProvider.SqlDataProvider },
                { DataProvider.OleDBDataProvider.Name, DataProvider.OleDBDataProvider },
                { DataProvider.OdbcDataProvider.Name, DataProvider.OdbcDataProvider },
                { DataProvider.OracleManagedDataAccessProvider.Name, DataProvider.OracleManagedDataAccessProvider }
            };
        }
    }
}
