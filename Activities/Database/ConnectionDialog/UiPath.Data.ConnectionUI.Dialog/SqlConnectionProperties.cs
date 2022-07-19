using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Data.ConnectionUI.Dialog.Properties;

namespace UiPath.Data.ConnectionUI.Dialog
{
    public class SqlConnectionProperties : AdoDotNetConnectionProperties
    {
        private const int SqlError_CannotOpenDatabase = 4060;

        public override bool IsComplete
        {
            get
            {
                if (!(ConnectionStringBuilder["Data Source"] is string) ||
                    (ConnectionStringBuilder["Data Source"] as string).Length == 0)
                {
                    return false;
                }
                if (!(bool)ConnectionStringBuilder["Integrated Security"] &&
                    (!(ConnectionStringBuilder["User ID"] is string) ||
                    (ConnectionStringBuilder["User ID"] as string).Length == 0))
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
            : base("Microsoft.Data.SqlClient")
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
            this["Integrated Security"] = true;
        }

        public override void Test()
        {
            string dataSource = ConnectionStringBuilder["Data Source"] as string;
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
            bool savedPooling = (bool)ConnectionStringBuilder["Pooling"];
            bool wasDefault = !ConnectionStringBuilder.ShouldSerialize("Pooling");
            ConnectionStringBuilder["Pooling"] = false;
            string testString = ConnectionStringBuilder.ConnectionString;
            ConnectionStringBuilder["Pooling"] = savedPooling;
            if (wasDefault)
            {
                ConnectionStringBuilder.Remove("Pooling");
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
            this["Data Source"] = _defaultDataSource;
            this["User Instance"] = true;
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
