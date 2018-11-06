//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

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
        private string fullFilePath = null;
        private XDocument xDoc = null;

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
            if (!String.IsNullOrEmpty(path))
            {
                fullFilePath = Path.GetFullPath(Path.Combine(path, configFileName));
            }
            else
            {
                fullFilePath = Path.Combine(System.Environment.CurrentDirectory, configFileName);
            }
            if (!String.IsNullOrEmpty(fullFilePath) && File.Exists(fullFilePath))
            {
                xDoc = XDocument.Load(fullFilePath);
            }
            else
            {
                xDoc = new XDocument();
                xDoc.Add(new XElement("ConnectionDialog", new XElement("DataSourceSelection")));
            }

            this.RootElement = xDoc.Root;
        }

        public XElement RootElement { get; set; }

        public void LoadConfiguration(DataConnectionDialog dialog)
        {
            dialog.DataSources.Add(DataSource.SqlDataSource);
            dialog.DataSources.Add(DataSource.SqlFileDataSource);
            //dialog.DataSources.Add(DataSource.OracleDataSource);
            dialog.DataSources.Add(DataSource.AccessDataSource);
            dialog.DataSources.Add(DataSource.OdbcDataSource);
            //dialog.DataSources.Add(SqlCe.SqlCeDataSource);

            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.SqlDataProvider);
            //dialog.UnspecifiedDataSource.Providers.Add(DataProvider.OracleDataProvider);
            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.OleDBDataProvider);
            dialog.UnspecifiedDataSource.Providers.Add(DataProvider.OdbcDataProvider);
            dialog.DataSources.Add(dialog.UnspecifiedDataSource);

            this.dataSources = new Dictionary<string, DataSource>();
            this.dataSources.Add(DataSource.SqlDataSource.Name, DataSource.SqlDataSource);
            this.dataSources.Add(DataSource.SqlFileDataSource.Name, DataSource.SqlFileDataSource);
            //this.dataSources.Add(DataSource.OracleDataSource.Name, DataSource.OracleDataSource);
            this.dataSources.Add(DataSource.AccessDataSource.Name, DataSource.AccessDataSource);
            this.dataSources.Add(DataSource.OdbcDataSource.Name, DataSource.OdbcDataSource);
            //this.dataSources.Add(SqlCe.SqlCeDataSource.Name, SqlCe.SqlCeDataSource);
            this.dataSources.Add(dialog.UnspecifiedDataSource.DisplayName, dialog.UnspecifiedDataSource);

            this.dataProviders = new Dictionary<string, DataProvider>();
            this.dataProviders.Add(DataProvider.SqlDataProvider.Name, DataProvider.SqlDataProvider);
            //this.dataProviders.Add(DataProvider.OracleDataProvider.Name, DataProvider.OracleDataProvider);
            this.dataProviders.Add(DataProvider.OleDBDataProvider.Name, DataProvider.OleDBDataProvider);
            this.dataProviders.Add(DataProvider.OdbcDataProvider.Name, DataProvider.OdbcDataProvider);
            //this.dataProviders.Add(SqlCe.SqlCeDataProvider.Name, SqlCe.SqlCeDataProvider);


            DataSource ds = null;
            string dsName = this.GetSelectedSource();
            if (!String.IsNullOrEmpty(dsName) && this.dataSources.TryGetValue(dsName, out ds))
            {
                dialog.SelectedDataSource = ds;
            }

            DataProvider dp = null;
            string dpName = this.GetSelectedProvider();
            if (!String.IsNullOrEmpty(dpName) && this.dataProviders.TryGetValue(dpName, out dp))
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
                        this.SaveSelectedSource(ds.DisplayName);
                    }
                    else
                    {
                        this.SaveSelectedSource(ds.Name);
                    }
                }
                DataProvider dp = dcd.SelectedDataProvider;
                if (dp != null)
                {
                    this.SaveSelectedProvider(dp.Name);
                }

                xDoc.Save(fullFilePath);
            }
        }

        public string GetSelectedSource()
        {
            try
            {
                XElement xElem = this.RootElement.Element("DataSourceSelection");
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
                XElement xElem = this.RootElement.Element("DataSourceSelection");
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
            if (!String.IsNullOrEmpty(source))
            {
                try
                {
                    XElement xElem = this.RootElement.Element("DataSourceSelection");
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
            if (!String.IsNullOrEmpty(provider))
            {
                try
                {
                    XElement xElem = this.RootElement.Element("DataSourceSelection");
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
