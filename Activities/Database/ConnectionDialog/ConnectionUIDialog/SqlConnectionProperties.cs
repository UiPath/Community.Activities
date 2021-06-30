//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;

namespace Microsoft.Data.ConnectionUI
{
    public class SqlConnectionProperties : AdoDotNetConnectionProperties
    {
        public SqlConnectionProperties()
            : base("System.Data.SqlClient")
        {
            LocalReset();
        }

        public override void Reset()
        {
            base.Reset();
            LocalReset();
        }

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

        public override void Test()
        {
            string dataSource = ConnectionStringBuilder["Data Source"] as string;
            if (dataSource == null || dataSource.Length == 0)
            {
                throw new InvalidOperationException(Strings.SqlConnectionProperties_MustSpecifyDataSource);
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
                    throw new InvalidOperationException(Strings.SqlConnectionProperties_CannotTestNonExistentDatabase);
                }
                else
                {
                    throw;
                }
            }
        }

        protected override PropertyDescriptor DefaultProperty
        {
            get
            {
                return GetProperties(new Attribute[0])["DataSource"];
            }
        }

        protected override string ToTestString()
        {
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

        protected override void Inspect(DbConnection connection)
        {
            if (connection.ServerVersion.StartsWith("07", StringComparison.Ordinal) ||
                connection.ServerVersion.StartsWith("08", StringComparison.Ordinal))
            {
                throw new NotSupportedException(Strings.SqlConnectionProperties_UnsupportedSqlVersion);
            }
        }

        private void LocalReset()
        {
            // We always start with integrated security turned on
            this["Integrated Security"] = true;
        }

        private const int SqlError_CannotOpenDatabase = 4060;
    }

    public class SqlFileConnectionProperties : SqlConnectionProperties
    {
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

        public override void Reset()
        {
            base.Reset();
            LocalReset();
        }

        public override bool IsComplete
        {
            get
            {
                if (!base.IsComplete)
                {
                    return false;
                }
                if (!(ConnectionStringBuilder["AttachDbFilename"] is string) ||
                    (ConnectionStringBuilder["AttachDbFilename"] as string).Length == 0)
                {
                    return false;
                }
                return true;
            }
        }

        public override void Test()
        {
            string attachDbFilename = ConnectionStringBuilder["AttachDbFilename"] as string;
            try
            {
                if (attachDbFilename == null || attachDbFilename.Length == 0)
                {
                    throw new InvalidOperationException(Strings.SqlFileConnectionProperties_NoFileSpecified);
                }
                ConnectionStringBuilder["AttachDbFilename"] = System.IO.Path.GetFullPath(attachDbFilename);
                if (!System.IO.File.Exists(ConnectionStringBuilder["AttachDbFilename"] as string))
                {
                    throw new InvalidOperationException(Strings.SqlFileConnectionProperties_CannotTestNonExistentMdf);
                }
                base.Test();
            }
            catch (SqlException e)
            {
                if (e.Number == -2) // timeout
                {
                    throw new ApplicationException(e.Errors[0].Message + Environment.NewLine + Strings.SqlFileConnectionProperties_TimeoutReasons);
                }
                throw;
            }
            finally
            {
                if (attachDbFilename != null && attachDbFilename.Length > 0)
                {
                    ConnectionStringBuilder["AttachDbFilename"] = attachDbFilename;
                }
            }
        }

        protected override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection descriptors = base.GetProperties(attributes);
            PropertyDescriptor dataSourceDescriptor = descriptors.Find("DataSource", true);
            if (dataSourceDescriptor != null)
            {
                int index = descriptors.IndexOf(dataSourceDescriptor);
                PropertyDescriptor[] descriptorArray = new PropertyDescriptor[descriptors.Count];
                descriptors.CopyTo(descriptorArray, 0);
                descriptorArray[index] = new DynamicPropertyDescriptor(dataSourceDescriptor, new TypeConverterAttribute(typeof(DataSourceConverter)));
                (descriptorArray[index] as DynamicPropertyDescriptor).CanResetValueHandler = new CanResetValueHandler(CanResetDataSource);
                (descriptorArray[index] as DynamicPropertyDescriptor).ResetValueHandler = new ResetValueHandler(ResetDataSource);
                descriptors = new PropertyDescriptorCollection(descriptorArray, true);
            }
            return descriptors;
        }

        private void LocalReset()
        {
            this["Data Source"] = _defaultDataSource;
            this["User Instance"] = true;
            this["Connection Timeout"] = 30;
        }

        private bool CanResetDataSource(object component)
        {
            return !(this["Data Source"] is string) || !(this["Data Source"] as string).Equals(_defaultDataSource, StringComparison.OrdinalIgnoreCase);
        }

        private void ResetDataSource(object component)
        {
            this["Data Source"] = _defaultDataSource;
        }

        private class DataSourceConverter : StringConverter
        {
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
                        Win32.RegistryKey key = Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL");
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

            private StandardValuesCollection _standardValues;
        }

        private string _defaultDataSource;
    }
}