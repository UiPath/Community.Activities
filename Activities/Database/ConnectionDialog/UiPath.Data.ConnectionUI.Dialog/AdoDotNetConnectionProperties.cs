using Oracle.ManagedDataAccess.Client;
using System.Diagnostics;
using System.Data.Common;
using System.ComponentModel;
using System;
using UiPath.Data.ConnectionUI.Dialog.Properties;
using System.Collections.Generic;
using System.Collections;
using System.Data.OleDb;
using System.Reflection;
using System.Activities.Presentation.Metadata;
using System.Windows;
using System.Configuration;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace UiPath.Data.ConnectionUI.Dialog
{
    
    public class AdoDotNetConnectionProperties : IDataConnectionProperties, ICustomTypeDescriptor
    {
        private string _providerName;
        private DbConnectionStringBuilder _connectionStringBuilder;
        private HashSet<string> _errorProperties = new HashSet<string>();
        public event EventHandler PropertyChanged;
        public event EventHandler PropertyValueChanged;
        public event EventHandler ErrorValidating;
        private bool _hasErrors;

        #region Public Properties
        public bool HasErrors
        {
            get
            {
                return _hasErrors;
            }
        }

        public DbConnectionStringBuilder ConnectionStringBuilder
        {
            get
            {
                return _connectionStringBuilder;
            }
        }

        public virtual bool IsExtensible
        {
            get
            {
                return !_connectionStringBuilder.IsFixedSize;
            }
        }

        public virtual bool IsComplete
        {
            get
            {
                return true;
            }
        }

        public HashSet<string> ErrorProperties
        {
            get
            {
                return _errorProperties;
            }
        }

        #endregion

        protected virtual PropertyDescriptor DefaultProperty
        {
            get
            {
                return TypeDescriptor.GetDefaultProperty(_connectionStringBuilder, true);
            }
        }

        public AdoDotNetConnectionProperties(string providerName)
        {
            Debug.Assert(providerName != null);
            _providerName = providerName;

#if NETCOREAPP
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("System.Data.OleDb", System.Data.OleDb.OleDbFactory.Instance);
            DbProviderFactories.RegisterFactory("System.Data.Odbc", System.Data.Odbc.OdbcFactory.Instance);
            DbProviderFactories.RegisterFactory("Oracle.ManagedDataAccess.Client", Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance);
#endif
            //SqlConnectionStringBuilder
            // Create an underlying connection string builder object
            DbProviderFactory factory = null;
            try
            {
                factory = DbProviderFactories.GetFactory(providerName);
            }
            catch
            {
#if NETFRAMEWORK
                if (!LegacyProviders.GetLegacyFactory(providerName, out factory))
                    throw;
#else
                throw;
#endif
            }
            Debug.Assert(factory != null);
            _connectionStringBuilder = factory.CreateConnectionStringBuilder();
            Debug.Assert(_connectionStringBuilder != null);
            _connectionStringBuilder.BrowsableConnectionString = false;
        }
        
        public virtual void Reset()
        {
            _connectionStringBuilder.Clear();
            OnPropertyChanged(EventArgs.Empty);
        }

        public virtual void Reset(string propertyName)
        {
            if (_connectionStringBuilder.ContainsKey(propertyName))
            {
                _connectionStringBuilder.Remove(propertyName);
                OnPropertyChanged(EventArgs.Empty);
            }
        }

        public virtual void Add(string propertyName)
        {
            if (!_connectionStringBuilder.ContainsKey(propertyName))
            {
                _connectionStringBuilder.Add(propertyName, string.Empty);
                OnPropertyChanged(EventArgs.Empty);
            }
        }

        public virtual bool Contains(string propertyName)
        {
            return _connectionStringBuilder.ContainsKey(propertyName);
        }

        public virtual void Parse(string s)
        {
            _connectionStringBuilder.ConnectionString = s;
            OnPropertyChanged(EventArgs.Empty);
        }

        public virtual void Remove(string propertyName)
        {
            if (_connectionStringBuilder.ContainsKey(propertyName))
            {
                _connectionStringBuilder.Remove(propertyName);
                OnPropertyChanged(EventArgs.Empty);
            }
        }

        public virtual void Test()
        {
            string testString = ToTestString();

            // If the connection string is empty, don't even bother testing
            if (testString == null || testString.Length == 0)
            {
                throw new InvalidOperationException(Resources.AdoDotNetConnectionProperties_NoProperties);
            }
            // Create a connection object
            DbConnection connection = null;
            DbProviderFactory factory = null;
            switch (_providerName)
            {
                case "Oracle.ManagedDataAccess.Client":
                    connection = new OracleConnection(testString);
                    break;
                case "Microsoft.Data.SqlClient":
                    connection = new SqlConnection(testString);
                    break;
                default:
                    factory = DbProviderFactories.GetFactory(_providerName);
                    Debug.Assert(factory != null);
                    connection = factory.CreateConnection();
                    break;
            }
                
            Debug.Assert(connection != null);
            // Try to open it
            try
            {
                connection.ConnectionString = testString;
                connection.Open();
                Inspect(connection);
            }
            finally
            {
                connection.Dispose();
            }
        }
        
        public virtual string ToDisplayString()
        {
            PropertyDescriptorCollection sensitiveProperties = TypeDescriptor.GetProperties(_connectionStringBuilder, new Attribute[] { PasswordPropertyTextAttribute.Yes });
            List<KeyValuePair<string, object>> savedValues = new List<KeyValuePair<string, object>>();
            foreach (PropertyDescriptor sensitiveProperty in sensitiveProperties)
            {
                string propertyName = sensitiveProperty.DisplayName;
                if (ConnectionStringBuilder.ShouldSerialize(propertyName))
                {
                    savedValues.Add(new KeyValuePair<string, object>(propertyName, ConnectionStringBuilder[propertyName]));
                    ConnectionStringBuilder.Remove(propertyName);
                }
            }
            try
            {
                return ConnectionStringBuilder.ConnectionString;
            }
            finally
            {
                foreach (KeyValuePair<string, object> savedValue in savedValues)
                {
                    if (savedValue.Value != null)
                    {
                        ConnectionStringBuilder[savedValue.Key] = savedValue.Value;
                    }
                }
            }
        }

        public virtual string ToFullString()
        {
            return _connectionStringBuilder.ConnectionString;
        }

        public virtual object this[string propertyName]
        {
            get
            {
                // Property name must not be null
                if (propertyName == null)
                {
                    throw new ArgumentNullException("propertyName");
                }

                // If property doesn't exist, just return null
                if (!_connectionStringBuilder.TryGetValue(propertyName, out object testValue))
                {
                    return null;
                }

                // If property value has been set, return this value
                if (_connectionStringBuilder.ShouldSerialize(propertyName))
                {
                    return _connectionStringBuilder[propertyName];
                }

                // Get the property's default value (if any)
                object val = _connectionStringBuilder[propertyName];

                // If a default value exists, return it
                if (val != null)
                {
                    return val;
                }

                // No value has been set and no default value exists, so return DBNull.Value
                return DBNull.Value;
            }
            set
            {
                // Property name must not be null
                if (propertyName == null)
                {
                    throw new ArgumentNullException("propertyName");
                }

                // Remove the value
                _connectionStringBuilder.Remove(propertyName);

                // Handle cases where value is DBNull.Value
                if (value == DBNull.Value)
                {
                    // Leave the property in the reset state
                    OnPropertyChanged(EventArgs.Empty);
                    return;
                }

                // Get the property's default value (if any)
                _connectionStringBuilder.TryGetValue(propertyName, out object val);

                // Set the value
                _connectionStringBuilder[propertyName] = value;

                // If the value is equal to the default, remove it again
                if (Equals(val, value))
                {
                    _connectionStringBuilder.Remove(propertyName);
                }

                OnPropertyChanged(EventArgs.Empty);
            }
        }

        protected virtual void Inspect(DbConnection connection)
        {
        }

        protected virtual string ToTestString()
        {
            return _connectionStringBuilder.ConnectionString;
        }

        protected virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var baseP = TypeDescriptor.GetProperties(_connectionStringBuilder, attributes);

            var props = new List<PropertyDescriptor>();
            int i = 0;
            foreach (PropertyDescriptor p in baseP)
            {
                bool isSensitive = false;
                foreach (Attribute pwd in p.Attributes)
                    if (pwd is PasswordPropertyTextAttribute)
                        isSensitive = true;
                if (isSensitive)
                    continue;
                Attribute[] a = new Attribute[p.Attributes.Count];
                p.Attributes.CopyTo(a, 0);
                props.Add(new KeyValuePropertyDescriptor(p, a));
            }

            //we clear the duplicates - an issue on .NET Core where we have a merge of keys and properties
            var duplicates = new List<PropertyDescriptor>();
            foreach (var prop in props)
            {
                var name = prop.Name;
                var trimmedName = name.Replace(" ", "");
                if (trimmedName == name && (props.Where(x => x.Name.Replace(" ", "") == trimmedName).Count() > 1))
                    duplicates.Add(prop);
            }
            foreach(var prop in duplicates)
                props.Remove(prop);
            return new PropertyDescriptorCollection(props.ToArray());
        }
        
        protected virtual void OnPropertyChanged(EventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        
        protected virtual void OnPropertyValueChanged(EventArgs e)
        {
            PropertyValueChanged?.Invoke(this, e);
        }

        protected virtual void OnErrorValidating(EventArgs e)
        {
            ErrorValidating?.Invoke(this, e);
        }

        #region ICustomTypeDescriptor implementation

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(_connectionStringBuilder);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(_connectionStringBuilder);
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(_connectionStringBuilder);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(_connectionStringBuilder, editorBaseType);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(_connectionStringBuilder);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return DefaultProperty;
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(new Attribute[1] { new BrowsableAttribute(true) });
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return GetProperties(attributes);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(_connectionStringBuilder);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(_connectionStringBuilder);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(_connectionStringBuilder, attributes);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return _connectionStringBuilder;
            
        }
        #endregion ICustomTypeDescriptor implementation

#if NETFRAMEWORK
        private static class LegacyProviders
        {
            private static readonly Lazy<Dictionary<string, DataRow>> _LegacyProviders = new Lazy<Dictionary<string, DataRow>>(() => InitLegacyProviders());

            private static Dictionary<string, DataRow> InitLegacyProviders()
            {
                var legacyConfig = new Dictionary<string, DataRow>();

                var dataSet = ConfigurationManager.GetSection("system.data") as System.Data.DataSet;
                var dr = dataSet.Tables[0].NewRow();
                dr[0] = "ODP.NET, Managed Driver";
                dr[1] = ".Net Framework Data Provider for Oracle";
                dr[2] = "Oracle.ManagedDataAccess.Client";
                dr[3] = typeof(OracleClientFactory).AssemblyQualifiedName;
                legacyConfig.Add((string)dr[2], dr);

                dr = dataSet.Tables[0].NewRow();
                dr[0] = ".Net Framework Data Provider";
                dr[1] = ".Net Framework Data Provider";
                dr[2] = "Microsoft.Data.SqlClient";
                dr[3] = typeof(SqlClientFactory).AssemblyQualifiedName;
                legacyConfig.Add((string)dr[2], dr);

                return legacyConfig;
            }
            internal static bool GetLegacyFactory(string providerName, out DbProviderFactory factory)
            {
                factory = null;
                if (_LegacyProviders.Value.TryGetValue(providerName, out var factoryConfig))
                {
                    factory = DbProviderFactories.GetFactory(factoryConfig);
                    return true;
                }
                else
                    return false;
            }
        }
#endif

        private class KeyValuePropertyDescriptor : PropertyDescriptor
        {
            private PropertyDescriptor m_Property;

            #region Public Properties
            public override string Name
            {
                get
                {
                    return m_Property.Name;
                }
            }

            public override Type ComponentType
            {
                get
                {
                    return m_Property.ComponentType;
                }
            }

            public override string Description
            {
                get
                {
                    return m_Property.Description;
                }
            }
            
            public object Value
            {
                get
                {
                    return GetValue(this);
                }
            }

            public override string Category
            {
                get
                {
                    return m_Property.Category;
                }
            }

            public override string DisplayName
            {
                get
                {
                    return m_Property.DisplayName;
                }

            }
            
            public override bool IsReadOnly
            {
                get
                {
                    return m_Property.IsReadOnly;
                }
            }

            public override Type PropertyType
            {

                get
                {
                    if ((m_Property != null) && (m_Property.PropertyType != null))
                    {
                        return m_Property.PropertyType;
                    }
                    else
                    {
                        return System.Type.Missing.GetType();

                    }
                }
            }
            #endregion

            public KeyValuePropertyDescriptor(PropertyDescriptor myProperty, Attribute[] attrs)
            : base(myProperty.Name, attrs)
            {
                m_Property = myProperty;
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override object GetValue(object component)
            {
                var conProp = (AdoDotNetConnectionProperties)component;
                var connStrB = conProp.ConnectionStringBuilder;
                connStrB.TryGetValue(m_Property.DisplayName, out var val);
                try
                {
                    if(m_Property.Converter.CanConvertTo(m_Property.PropertyType))
                        val = m_Property.Converter.ConvertTo(val, m_Property.PropertyType);
                }
                catch (Exception){ }
                conProp._errorProperties.Remove(m_Property.DisplayName);
                conProp.OnErrorValidating(EventArgs.Empty);
                return val;
            }
            
            public override void ResetValue(object component)
            {
                //Have to implement
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }

            public override void SetValue(object component, object value)
            {
                AdoDotNetConnectionProperties conProp = (AdoDotNetConnectionProperties)component;
                var connStrB = conProp.ConnectionStringBuilder;
                
                try
                {
                    conProp._hasErrors = false;
                    
                    if (m_Property.Converter.CanConvertFrom(value.GetType()))
                        value = m_Property.Converter.ConvertFrom(value);
                    if (value is string && string.IsNullOrEmpty((string)value))
                        value = null;

                    connStrB[m_Property.DisplayName] = value;
                }
                catch(Exception ex) {
                    conProp._errorProperties.Add(m_Property.DisplayName);
                    conProp._hasErrors = true;
                    conProp.OnErrorValidating(EventArgs.Empty);
                }
                conProp.OnPropertyValueChanged(EventArgs.Empty);
            }
        }
    }
}
