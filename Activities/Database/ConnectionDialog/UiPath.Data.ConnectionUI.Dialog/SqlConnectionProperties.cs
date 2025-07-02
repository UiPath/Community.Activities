using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UiPath.Data.ConnectionUI.Dialog.Properties;
using UiPath.Database;

namespace UiPath.Data.ConnectionUI.Dialog
{
    public class SqlConnectionProperties : AdoDotNetConnectionProperties
    {
        private const int SqlError_CannotOpenDatabase = 4060;

        public override bool IsComplete
        {
            get
            {
                if (!(ConnectionStringBuilder[DatabaseConstants.Data_Source] is string) ||
                    (ConnectionStringBuilder[DatabaseConstants.Data_Source] as string).Length == 0)
                {
                    return false;
                }
                if (!(bool)ConnectionStringBuilder[DatabaseConstants.Integrated_Security] &&
                    (!(ConnectionStringBuilder[DatabaseConstants.User_ID] is string) ||
                    (ConnectionStringBuilder[DatabaseConstants.User_ID] as string).Length == 0))
                {
                    return false;
                }
                return true;
            }
        }

        protected override PropertyDescriptor DefaultProperty
        {
            get
            {
                return GetProperties(new Attribute[0])["DataSource"];
            }
        }

        public SqlConnectionProperties()
            : base(DatabaseConstants.SqlServerProvider)
        {
            LocalReset();
        }

        public override void Reset()
        {
            base.Reset();
            LocalReset();
        }

        private void LocalReset()
        {
            // We always start with integrated security turned on
            this[DatabaseConstants.Integrated_Security] = true;
        }

        public override void Test()
        {
            string dataSource = ConnectionStringBuilder[DatabaseConstants.Data_Source] as string;
            if (dataSource == null || dataSource.Length == 0)
            {
                throw new InvalidOperationException(Resources.SqlConnectionProperties_MustSpecifyDataSource);
            }
            string database = ConnectionStringBuilder["Initial Catalog"] as string;
            try
            {
                base.Test();
            }
            catch (SqlException e)
            {
                if (e.Number == SqlError_CannotOpenDatabase && database != null && database.Length > 0)
                {
                    throw new InvalidOperationException(Resources.SqlConnectionProperties_CannotTestNonExistentDatabase);
                }
                else
                {
                    throw;
                }
            }
        }

        public override string ToFullString()
        {
            AddEncryptIfNeeded();
            return base.ToFullString();
        }

        public override string ToDisplayString()
        {
            AddEncryptIfNeeded();
            return base.ToDisplayString();
        }

        protected override string ToTestString()
        {
            AddEncryptIfNeeded();
            bool savedPooling = (bool)ConnectionStringBuilder[DatabaseConstants.Pooling];
            bool wasDefault = !ConnectionStringBuilder.ShouldSerialize(DatabaseConstants.Pooling);
            ConnectionStringBuilder[DatabaseConstants.Pooling] = false;
            string testString = ConnectionStringBuilder.ConnectionString;
            ConnectionStringBuilder[DatabaseConstants.Pooling] = savedPooling;
            if (wasDefault)
            {
                ConnectionStringBuilder.Remove(DatabaseConstants.Pooling);
            }
            return testString;
        }

        private void AddEncryptIfNeeded()
        {
            bool wasDefault = !ConnectionStringBuilder.ShouldSerialize("Encrypt");
            if (wasDefault)
                ConnectionStringBuilder["Encrypt"] = false;
        }
    }

    public class SqlFileConnectionProperties : SqlConnectionProperties
    {
        private string _defaultDataSource;

        public SqlFileConnectionProperties()
            : this(null)
        {
        }

        public SqlFileConnectionProperties(string defaultInstanceName)
        {
            _defaultDataSource = ".";
            if (defaultInstanceName != null && defaultInstanceName.Length > 0)
            {
                _defaultDataSource += "\\" + defaultInstanceName;
            }
            else
            {
                DataSourceConverter conv = new DataSourceConverter();
                TypeConverter.StandardValuesCollection coll = conv.GetStandardValues(null);
                if (coll.Count > 0)
                {
                    _defaultDataSource = coll[0] as string;
                }
            }
            LocalReset();
        }
        
        private void LocalReset()
        {
            this[DatabaseConstants.Data_Source] = _defaultDataSource;
            this[DatabaseConstants.User_Instance] = true;
            this["Connection Timeout"] = 30;
        }
        
        private class DataSourceConverter : StringConverter
        {

            private StandardValuesCollection _standardValues;

            public DataSourceConverter()
            {
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                if (_standardValues == null)
                {
                    string[] dataSources = null;

                    if (HelpUtils.IsWow64())
                    {
                        List<string> dataSourceList = new List<string>();
                        // Read 64 registry key of SQL Server Instances Names.
                        dataSourceList.AddRange(HelpUtils.GetValueNamesWow64("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL", NativeMethods.KEY_WOW64_64KEY | NativeMethods.KEY_QUERY_VALUE));
                        // Read 32 registry key of SQL Server Instances Names.
                        dataSourceList.AddRange(HelpUtils.GetValueNamesWow64("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL", NativeMethods.KEY_WOW64_32KEY | NativeMethods.KEY_QUERY_VALUE));
                        dataSources = dataSourceList.ToArray();
                    }
                    else
                    {
                        // Look in the registry for all local SQL Server instances
                        Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL");
                        if (key != null)
                        {
                            using (key)
                            {
                                dataSources = key.GetValueNames();
                            } // key is Disposed here
                        }
                    }

                    if (dataSources != null)
                    {
                        for (int i = 0; i < dataSources.Length; i++)
                        {
                            if (string.Equals(dataSources[i], "MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                            {
                                dataSources[i] = ".";
                            }
                            else
                            {
                                dataSources[i] = ".\\" + dataSources[i];
                            }
                        }
                        _standardValues = new StandardValuesCollection(dataSources);
                    }
                    else
                    {
                        _standardValues = new StandardValuesCollection(new string[0]);
                    }
                }
                return _standardValues;
            }
        }
    }
}
