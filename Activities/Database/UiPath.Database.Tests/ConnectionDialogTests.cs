#if WINDOWS
using System;
using System.ComponentModel;
using System.Linq;
using UiPath.Data.ConnectionUI.Dialog;
using UiPath.Database;
using UiPath.Database.Extended.Design;
using Xunit;

namespace UiPath.Database.Tests
{
    /// <summary>Tests of the logic of the connection dialog (no WPF control is created).</summary>
    public class AdoDotNetConnectionPropertiesTests
    {
        private const string DataSourceKey = "Data Source";

        private static AdoDotNetConnectionProperties NewSqlite() => new AdoDotNetConnectionProperties(DatabaseConstants.SQLiteProvider);

        [Fact]
        public void NewProperties_AreEmptyCompleteAndExtensible()
        {
            var properties = NewSqlite();

            Assert.False(properties.HasErrors);
            Assert.True(properties.IsComplete);
            Assert.NotNull(properties.ConnectionStringBuilder);
            Assert.Empty(properties.ErrorProperties);
            Assert.Equal(properties.ConnectionStringBuilder.IsFixedSize, !properties.IsExtensible);
        }

        [Fact]
        public void AddContainsRemoveReset_RaisePropertyChanged()
        {
            // The ODBC builder accepts any key, so Add/Remove really add and remove.
            var properties = new AdoDotNetConnectionProperties(DatabaseConstants.OdbcProvider);
            const string key = "Custom Key";
            int changes = 0;
            properties.PropertyChanged += (s, e) => changes++;

            properties.Add(key);
            properties.Add(key);
            Assert.True(properties.Contains(key));
            Assert.Equal(1, changes);

            properties.Remove(key);
            properties.Remove(key);
            Assert.False(properties.Contains(key));
            Assert.Equal(2, changes);

            properties.Add(key);
            properties.Reset(key);
            properties.Reset(key);
            Assert.False(properties.Contains(key));
            Assert.Equal(4, changes);

            properties.Add(key);
            properties.Reset();
            Assert.False(properties.Contains(key));
            Assert.Equal(6, changes);
        }

        [Fact]
        public void Parse_ReadsAConnectionString()
        {
            var properties = NewSqlite();
            int changes = 0;
            properties.PropertyChanged += (s, e) => changes++;

            properties.Parse("Data Source=:memory:");

            Assert.Equal(":memory:", properties[DataSourceKey]);
            Assert.Contains(":memory:", properties.ToFullString());
            Assert.Contains(":memory:", properties.ToDisplayString());
            Assert.Equal(1, changes);
        }

        [Fact]
        public void Indexer_ReturnsNullForUnknownAndDbNullForAnUnsetKnownProperty()
        {
            var properties = NewSqlite();

            Assert.Null(properties["No Such Property"]);
            Assert.Throws<ArgumentNullException>(() => properties[null]);
            Assert.Throws<ArgumentNullException>(() => properties[null] = "x");
        }

        [Fact]
        public void Indexer_SetsAndClearsAValue()
        {
            var properties = NewSqlite();
            int changes = 0;
            properties.PropertyChanged += (s, e) => changes++;

            properties[DataSourceKey] = "file.db";
            Assert.Equal("file.db", properties[DataSourceKey]);

            properties[DataSourceKey] = DBNull.Value;
            Assert.False(properties.ConnectionStringBuilder.ShouldSerialize(DataSourceKey));
            Assert.Equal(2, changes);
        }

        [Fact]
        public void Indexer_ValueEqualToTheDefaultIsRemovedAgain()
        {
            var properties = NewSqlite();

            // "Mode" has a default; setting the default value leaves the property in the reset state.
            object defaultMode = properties.ConnectionStringBuilder.TryGetValue("Mode", out object value) ? value : null;
            properties["Mode"] = defaultMode;

            Assert.False(properties.ConnectionStringBuilder.ShouldSerialize("Mode"));
        }

        [Fact]
        public void ToDisplayString_LeavesOutSensitiveValuesButKeepsThemInTheFullString()
        {
            var properties = new AdoDotNetConnectionProperties(DatabaseConstants.SqlServerProvider);
            properties.Parse("Data Source=file.db;Password=secret");

            Assert.DoesNotContain("secret", properties.ToDisplayString());
            Assert.Contains("secret", properties.ToFullString());
            Assert.Contains("secret", properties.ConnectionStringBuilder.ConnectionString);
        }

        [Fact]
        public void Test_OpensAnInMemoryDatabase()
        {
            var properties = NewSqlite();
            properties.Parse("Data Source=:memory:");

            properties.Test();
        }

        [Fact]
        public void Test_RejectsAnEmptyConnectionString()
        {
            Assert.Throws<InvalidOperationException>(() => NewSqlite().Test());
        }

        [Fact]
        public void Test_UsesTheSqlServerConnectionForTheSqlServerProvider()
        {
            var properties = new AdoDotNetConnectionProperties(DatabaseConstants.SqlServerProvider);
            properties.Parse("Data Source=.;Connect Timeout=1;Initial Catalog=x");

            // No server answers: the point is that a SqlConnection is created and opened.
            Assert.ThrowsAny<Exception>(() => properties.Test());
        }

        [Fact]
        public void CustomTypeDescriptor_ListsBrowsablePropertiesWithoutSensitiveOnes()
        {
            ICustomTypeDescriptor descriptor = new AdoDotNetConnectionProperties(DatabaseConstants.SqlServerProvider);

            PropertyDescriptorCollection properties = descriptor.GetProperties();

            Assert.NotEmpty(properties);
            Assert.DoesNotContain(properties.Cast<PropertyDescriptor>(), p => p.Name == "Password");
            Assert.NotNull(descriptor.GetClassName());
            Assert.NotNull(descriptor.GetAttributes());
            Assert.NotNull(descriptor.GetConverter());
            descriptor.GetDefaultProperty();
            Assert.NotNull(descriptor.GetEvents());
            Assert.NotNull(descriptor.GetEvents(new Attribute[0]));
            Assert.NotNull(descriptor.GetPropertyOwner(null));
            Assert.NotNull(descriptor.GetProperties(new Attribute[] { new BrowsableAttribute(true) }));
            descriptor.GetComponentName();
        }

        [Fact]
        public void PropertyDescriptors_ReadAndWriteThroughTheBuilder()
        {
            var properties = NewSqlite();
            ICustomTypeDescriptor descriptor = properties;
            PropertyDescriptor dataSource = descriptor.GetProperties().Cast<PropertyDescriptor>().First(p => p.DisplayName == DataSourceKey);
            int validations = 0;
            properties.ErrorValidating += (s, e) => validations++;
            int valueChanges = 0;
            properties.PropertyValueChanged += (s, e) => valueChanges++;

            dataSource.SetValue(properties, "file.db");
            object read = dataSource.GetValue(properties);

            Assert.Equal("file.db", read);
            Assert.Equal(1, valueChanges);
            Assert.True(validations >= 1);
            Assert.False(properties.HasErrors);
            Assert.False(dataSource.CanResetValue(properties));
            Assert.False(dataSource.ShouldSerializeValue(properties));
            Assert.NotNull(dataSource.ComponentType);
            Assert.NotNull(dataSource.PropertyType);
            Assert.Equal(DataSourceKey, dataSource.DisplayName);
            Assert.NotNull(dataSource.Category);
            Assert.NotNull(dataSource.Description);
            Assert.False(dataSource.IsReadOnly);
            dataSource.ResetValue(properties);

            dataSource.SetValue(properties, string.Empty);
            Assert.False(properties.ConnectionStringBuilder.ShouldSerialize(DataSourceKey));
        }

        [Fact]
        public void PropertyDescriptors_RecordAValueThatCannotBeSet()
        {
            var properties = NewSqlite();
            ICustomTypeDescriptor descriptor = properties;
            PropertyDescriptor mode = descriptor.GetProperties().Cast<PropertyDescriptor>().First(p => p.DisplayName == "Mode");
            int validations = 0;
            properties.ErrorValidating += (s, e) => validations++;

            mode.SetValue(properties, "not-a-valid-mode");

            Assert.True(properties.HasErrors);
            Assert.Contains("Mode", properties.ErrorProperties);
            Assert.True(validations >= 1);
        }

        [Fact]
        public void SqliteAndOdbcProperties_KnowWhenTheyAreComplete()
        {
            var sqlite = new SqliteConnectionProperties();
            Assert.False(sqlite.IsComplete);
            sqlite[DataSourceKey] = "file.db";
            Assert.True(sqlite.IsComplete);
            Assert.Contains("file.db", sqlite.ToFullString());
            sqlite.Test();
        }

        [Fact]
        public void Test_RefusesAnEmptyConnectionString()
        {
            Assert.Throws<InvalidOperationException>(() => NewSqlite().Test());
        }

        [Fact]
        public void CustomTypeDescriptor_DelegatesToTheUnderlyingBuilder()
        {
            ICustomTypeDescriptor descriptor = NewSqlite();

            Assert.NotNull(descriptor.GetAttributes());
            Assert.NotNull(descriptor.GetConverter());
            Assert.NotNull(descriptor.GetEvents());
            Assert.NotNull(descriptor.GetEvents(new Attribute[0]));
            Assert.NotNull(descriptor.GetProperties());
            descriptor.GetEditor(typeof(object));
            descriptor.GetDefaultEvent();
            descriptor.GetDefaultProperty();
            descriptor.GetClassName();
            descriptor.GetComponentName();
        }
    }

    public class DataProviderAndDataSourceTests
    {
        [Fact]
        public void BuiltInProviders_HaveNamesAndDescriptions()
        {
            Assert.Equal(DatabaseConstants.SqlServerProvider, DataProvider.SqlDataProvider.Name);
            Assert.Equal(DatabaseConstants.OleDbProvider, DataProvider.OleDBDataProvider.Name);
            Assert.Equal(DatabaseConstants.OdbcProvider, DataProvider.OdbcDataProvider.Name);
            Assert.Equal(DatabaseConstants.SQLiteProvider, DataProvider.SqliteDataProvider.Name);

            foreach (DataProvider provider in new[] { DataProvider.SqlDataProvider, DataProvider.OleDBDataProvider, DataProvider.OdbcDataProvider, DataProvider.SqliteDataProvider })
            {
                Assert.Same(provider, provider.Name == DatabaseConstants.SqlServerProvider ? DataProvider.SqlDataProvider : provider);
                Assert.False(string.IsNullOrEmpty(provider.DisplayName));
                Assert.NotNull(provider.ShortDisplayName);
                Assert.False(string.IsNullOrEmpty(provider.Description));
                Assert.Equal(provider.Description, provider.GetDescription(null));
            }
        }

        [Fact]
        public void GetDescription_UsesTheDescriptionOfTheDataSourceWhenThereIsOne()
        {
            string forSource = DataProvider.SqlDataProvider.GetDescription(DataSource.SqlDataSource);

            Assert.NotEqual(DataProvider.SqlDataProvider.Description, forSource);
            Assert.Equal(DataProvider.SqlDataProvider.Description, DataProvider.SqlDataProvider.GetDescription(DataSource.AccessDataSource));
        }

        [Fact]
        public void CreateConnectionProperties_PicksTheTypeOfTheDataSourceOrTheDefault()
        {
            Assert.IsType<SqlConnectionProperties>(DataProvider.SqlDataProvider.CreateConnectionProperties());
            Assert.IsType<SqlConnectionProperties>(DataProvider.SqlDataProvider.CreateConnectionProperties(DataSource.SqlDataSource));
            Assert.IsType<SqlFileConnectionProperties>(DataProvider.SqlDataProvider.CreateConnectionProperties(DataSource.SqlFileDataSource));
            Assert.IsType<SqliteConnectionProperties>(DataProvider.SqliteDataProvider.CreateConnectionProperties());
            Assert.IsType<OdbcConnectionProperties>(DataProvider.OdbcDataProvider.CreateConnectionProperties(DataSource.OdbcDataSource));
        }

        [Fact]
        public void CreateConnectionProperties_IsNullWhenTheProviderHasNoPropertiesType()
        {
            var provider = new DataProvider("Some.Provider", "Some", "S", "Description", typeof(object));

            Assert.Null(provider.CreateConnectionProperties());
            Assert.Null(provider.CreateConnectionUIControl(null, null));
            Assert.Equal("Description", provider.Description);
            Assert.Equal("Some", provider.DisplayName);
        }

        [Fact]
        public void DisplayName_FallsBackToTheName()
        {
            var provider = new DataProvider("Some.Provider", null, null, null, typeof(object));

            Assert.Equal("Some.Provider", provider.DisplayName);
        }

        [Fact]
        public void Constructors_RejectMissingArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new DataProvider(null, "d", "s", "x", typeof(object)));
            Assert.Throws<ArgumentNullException>(() => new DataProvider("n", "d", "s", "x", typeof(object), (Type)null));
            Assert.Throws<ArgumentNullException>(() => new DataSource(null, "d"));
        }

        [Fact]
        public void DataSources_ListTheirProviders()
        {
            // Compared by name: the lazy statics of DataProvider and DataSource refer to each other, so the first access can
            // create a second instance of the same provider.
            Assert.Contains(DataSource.SqlDataSource.Providers, p => p.Name == DatabaseConstants.SqlServerProvider);
            Assert.Contains(DataSource.SqlDataSource.Providers, p => p.Name == DatabaseConstants.OleDbProvider);
            Assert.Equal(DatabaseConstants.SqlServerProvider, DataSource.SqlDataSource.DefaultProvider.Name);
            Assert.Equal(DatabaseConstants.OleDbProvider, DataSource.AccessDataSource.DefaultProvider.Name);
            Assert.Equal(DatabaseConstants.SqlServerProvider, DataSource.SqlFileDataSource.DefaultProvider.Name);
            Assert.Equal(DatabaseConstants.OdbcProvider, DataSource.OdbcDataSource.DefaultProvider.Name);
            Assert.Equal(DatabaseConstants.SQLiteProvider, DataSource.SqliteDataSource.DefaultProvider.Name);
            Assert.Equal("MicrosoftSqlServer", DataSource.SqlDataSource.Name);
            Assert.False(string.IsNullOrEmpty(DataSource.SqlDataSource.DisplayName));
        }

        [Fact]
        public void DefaultProvider_FollowsTheNumberOfProviders()
        {
            var empty = new DataSource("empty", null);
            Assert.Null(empty.DefaultProvider);
            Assert.Equal("empty", empty.DisplayName);

            var many = new DataSource("many", "Many");
            many.Providers.Add(DataProvider.SqlDataProvider);
            many.Providers.Add(DataProvider.OdbcDataProvider);
            Assert.Null(many.DefaultProvider);
            many.DefaultProvider = DataProvider.OdbcDataProvider;
            Assert.Same(DataProvider.OdbcDataProvider, many.DefaultProvider);
            Assert.Throws<InvalidOperationException>(() => many.DefaultProvider = DataProvider.SqliteDataProvider);

            var single = new DataSource("single", "Single");
            single.Providers.Add(DataProvider.SqlDataProvider);
            Assert.Throws<InvalidOperationException>(() => single.DefaultProvider = DataProvider.OdbcDataProvider);

            var unnamed = new DataSource();
            unnamed.Providers.Add(DataProvider.SqlDataProvider);
            unnamed.Providers.Add(DataProvider.OdbcDataProvider);
            Assert.Null(unnamed.DefaultProvider);
        }

        [Fact]
        public void UnspecifiedDataSource_ListsTheBuiltInAndTheInstalledExtraProviders()
        {
            DataSource unspecified = DataSource.UnspecifiedDataSource;

            Assert.Contains(unspecified.Providers, p => p.Name == DatabaseConstants.SqlServerProvider);
            Assert.Contains(unspecified.Providers, p => p.Name == DatabaseConstants.SQLiteProvider);
            Assert.Contains(unspecified.Providers, p => p.Name == DatabaseConstants.OracleProvider);
            // Looking again does not add the extra providers twice.
            Assert.Equal(unspecified.Providers.Count, DataSource.UnspecifiedDataSource.Providers.Count);
        }

        [Fact]
        public void Configuration_ListsTheBuiltInAndTheInstalledExtraProviders()
        {
            var configuration = new DataConnectionConfiguration();

            Assert.Contains(DatabaseConstants.SqlServerProvider, configuration.Providers.Keys);
            Assert.Contains(DatabaseConstants.SQLiteProvider, configuration.Providers.Keys);
            Assert.Contains(DatabaseConstants.OracleProvider, configuration.Providers.Keys);
            Assert.Contains(DataSource.SqlDataSource.Name, configuration.DataSources.Keys);
            Assert.Contains("OracleManagedDataAccess", configuration.DataSources.Keys);
            Assert.Contains(DataSource.UnspecifiedDataSource.DisplayName, configuration.DataSources.Keys);
            Assert.Null(configuration.RootElement);
        }
    }

    public class OracleDialogProviderTests
    {
        [Fact]
        public void TheDialogCatalogFindsTheOracleProviderOfTheExtendedDesignAssembly()
        {
            Assert.Contains(ConnectionDialogProviders.GetAll(), p => p is OracleConnectionDialogProvider);
            Assert.Equal(
                "UiPath.Database.Activities.Extended.Design",
                typeof(OracleConnectionDialogProvider).Assembly.GetName().Name);
        }

        [Fact]
        public void TheOracleDialogProvider_OffersTheOracleProviderAndSource()
        {
            var provider = new OracleConnectionDialogProvider();

            Assert.Equal(DatabaseConstants.OracleProvider, provider.DataProvider.Name);
            Assert.Contains(provider.DataProvider, provider.DataSource.Providers);
            Assert.Equal("OracleManagedDataAccess", provider.DataSource.Name);
            Assert.IsType<OracleConnectionProperties>(provider.DataProvider.CreateConnectionProperties());
            Assert.False(string.IsNullOrEmpty(provider.DataProvider.Description));
        }

        [Fact]
        public void OracleConnectionProperties_AreCompleteWithADataSourceAndAUser()
        {
            var properties = new OracleConnectionProperties();
            Assert.False(properties.IsComplete);

            properties[DatabaseConstants.Data_Source] = "host/service";
            Assert.False(properties.IsComplete);

            properties[DatabaseConstants.User_ID] = "scott";
            Assert.True(properties.IsComplete);
        }

        [Fact]
        public void OracleConnectionProperties_BuildTheTestStringWithoutChangingPooling()
        {
            var properties = new OracleConnectionProperties();
            properties[DatabaseConstants.Data_Source] = "host/service";
            properties[DatabaseConstants.User_ID] = "scott";
            properties[DatabaseConstants.Password] = "tiger";

            string testString = (string)typeof(OracleConnectionProperties)
                .GetMethod("ToTestString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(properties, null);

            Assert.Contains("host/service", testString);
            Assert.Contains("Pooling=", testString);
            Assert.False(properties.ConnectionStringBuilder.ShouldSerialize(DatabaseConstants.Pooling));
            Assert.DoesNotContain("\"", properties.ToFullString());

            properties.Reset();
            Assert.False(properties.ConnectionStringBuilder.ShouldSerialize(DatabaseConstants.Data_Source));
        }
    }
}
#endif
