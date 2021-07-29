//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.Data.ConnectionUI
{
    public interface IDataConnectionConfiguration
    {
        string GetSelectedSource();
        void SaveSelectedSource(string provider);

        string GetSelectedProvider();
        void SaveSelectedProvider(string provider);
    }

    /// <summary>
    /// Provide a default executeInsert for the storage of DataConnection Dialog UI configuration.
    /// </summary>
    public class DataConnectionConfiguration : IDataConnectionConfiguration
    {
        private const string configFileName = @"DataConnection.xml";
        private readonly string fullFilePath = null;
        private readonly XDocument xDoc = null;

        // Available data sources:
        private IDictionary<string, DataSource> dataSources;

        // Available data providers: 
        private IDictionary<string, DataProvider> dataProviders;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Configuration file path.</param>
        public DataConnectionConfiguration(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                fullFilePath = Path.GetFullPath(Path.Combine(path, configFileName));
            }
            else
            {
                fullFilePath = Path.Combine(System.Environment.CurrentDirectory, configFileName);
            }
            if (!string.IsNullOrEmpty(fullFilePath) && File.Exists(fullFilePath))
            {
                xDoc = XDocument.Load(fullFilePath);
            }
            else
            {
                xDoc = new XDocument();
                xDoc.Add(new XElement("ConnectionDialog", new XElement("DataSourceSelection")));
            }

            RootElement = xDoc.Root;
        }

        public XElement RootElement { get; set; }

        public void LoadConfiguration(DataConnectionDialog dialog)
        {
            dialog.DataSources.Add(DataSource.SqlDataSource);
            dialog.DataSources.Add(DataSource.SqlFileDataSource);
            dialog.DataSources.Add(DataSource.AccessDataSource);
            dialog.DataSources.Add(DataSource.OdbcDataSource);
            dialog.DataSources.Add(DataSource.OracleManagedDataAccessSource);

            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.SqlDataProvider);
            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.OleDBDataProvider);
            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.OdbcDataProvider);
            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.OracleManagedDataAccessProvider);
            dialog.DataSources.Add(dialog.UnspecifiedDataSource);

            dataSources = new Dictionary<string, DataSource>
            {
                { DataSource.SqlDataSource.Name, DataSource.SqlDataSource },
                { DataSource.SqlFileDataSource.Name, DataSource.SqlFileDataSource },
                { DataSource.AccessDataSource.Name, DataSource.AccessDataSource },
                { DataSource.OdbcDataSource.Name, DataSource.OdbcDataSource },
                { DataSource.OracleManagedDataAccessSource.Name, DataSource.OracleManagedDataAccessSource },
                { dialog.UnspecifiedDataSource.DisplayName, dialog.UnspecifiedDataSource }
            };

            dataProviders = new Dictionary<string, DataProvider>
            {
                { DataProvider.SqlDataProvider.Name, DataProvider.SqlDataProvider },
                { DataProvider.OleDBDataProvider.Name, DataProvider.OleDBDataProvider },
                { DataProvider.OdbcDataProvider.Name, DataProvider.OdbcDataProvider },
                { DataProvider.OracleManagedDataAccessProvider.Name, DataProvider.OracleManagedDataAccessProvider }
            };
            string dsName = GetSelectedSource();

            if (!string.IsNullOrEmpty(dsName) && dataSources.TryGetValue(dsName, out DataSource ds))
            {
                dialog.SelectedDataSource = ds;
            }

            string dpName = GetSelectedProvider();
            if (!string.IsNullOrEmpty(dpName) && dataProviders.TryGetValue(dpName, out DataProvider dp))
            {
                dialog.SelectedDataProvider = dp;
            }
        }

        public void SaveConfiguration(DataConnectionDialog dcd)
        {
            if (dcd.SaveSelection)
            {
                DataSource ds = dcd.SelectedDataSource;
                if (ds != null)
                {
                    if (ds == dcd.UnspecifiedDataSource)
                    {
                        SaveSelectedSource(ds.DisplayName);
                    }
                    else
                    {
                        SaveSelectedSource(ds.Name);
                    }
                }
                DataProvider dp = dcd.SelectedDataProvider;
                if (dp != null)
                {
                    SaveSelectedProvider(dp.Name);
                }

                xDoc.Save(fullFilePath);
            }
        }

        public string GetSelectedSource()
        {
            try
            {
                XElement xElem = RootElement.Element("DataSourceSelection");
                XElement sourceElem = xElem.Element("SelectedSource");
                if (sourceElem != null)
                {
                    return sourceElem.Value as string;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public string GetSelectedProvider()
        {
            try
            {
                XElement xElem = RootElement.Element("DataSourceSelection");
                XElement providerElem = xElem.Element("SelectedProvider");
                if (providerElem != null)
                {
                    return providerElem.Value as string;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public void SaveSelectedSource(string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    XElement xElem = RootElement.Element("DataSourceSelection");
                    XElement sourceElem = xElem.Element("SelectedSource");
                    if (sourceElem != null)
                    {
                        sourceElem.Value = source;
                    }
                    else
                    {
                        xElem.Add(new XElement("SelectedSource", source));
                    }
                }
                catch
                {
                }
            }

        }

        public void SaveSelectedProvider(string provider)
        {
            if (!string.IsNullOrEmpty(provider))
            {
                try
                {
                    XElement xElem = RootElement.Element("DataSourceSelection");
                    XElement sourceElem = xElem.Element("SelectedProvider");
                    if (sourceElem != null)
                    {
                        sourceElem.Value = provider;
                    }
                    else
                    {
                        xElem.Add(new XElement("SelectedProvider", provider));
                    }
                }
                catch
                {
                }
            }
        }
    }
}
