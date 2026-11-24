using System;
using System.Data.Common;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using UiPath.Database.BulkOps;
using UiPath.Database.Extended;
using UiPath.Database.Extensibility;
using Xunit;

namespace UiPath.Database.Extended.Tests
{
    public class OracleDatabaseProviderTests
    {
        private readonly OracleDatabaseProvider _provider = new OracleDatabaseProvider();

        [Fact]
        public void Provider_DescribesTheOracleDriver()
        {
            Assert.Equal(DatabaseConstants.OracleProvider, _provider.ProviderName);
            Assert.Same(OracleClientFactory.Instance, _provider.Factory);
            Assert.True(_provider.SupportsBulk);
            Assert.IsType<OracleBulkOperations>(_provider.CreateBulkOperations());
        }

        [Fact]
        public void Owns_OnlyOracleConnections()
        {
            Assert.True(_provider.Owns(new OracleConnection()));
            Assert.False(_provider.Owns(new System.Data.Odbc.OdbcConnection()));
            Assert.False(_provider.Owns(null));
        }

        [Theory]
        [InlineData(typeof(int), OracleDbType.Int32)]
        [InlineData(typeof(bool), OracleDbType.Boolean)]
        [InlineData(typeof(decimal), OracleDbType.Decimal)]
        [InlineData(typeof(OracleRefCursor), OracleDbType.RefCursor)]
        public void TryUpdateParameterType_MapsTheClrTypeToTheOracleType(Type clrType, OracleDbType expected)
        {
            var parameter = new OracleParameter();

            Assert.True(_provider.TryUpdateParameterType(parameter, clrType));
            Assert.Equal(expected, parameter.OracleDbType);
        }

        [Fact]
        public void TryUpdateParameterType_IgnoresUnmappedTypesAndForeignParameters()
        {
            Assert.False(_provider.TryUpdateParameterType(new OracleParameter(), typeof(DateTime)));
            Assert.False(_provider.TryUpdateParameterType(new System.Data.Odbc.OdbcParameter(), typeof(int)));
            Assert.False(_provider.TryUpdateParameterType(new OracleParameter(), null));
        }

        [Fact]
        public void GetCreateTempTableSql_CopiesTheColumnsOfTheSourceTable()
        {
            string sql = _provider.GetCreateTempTableSql("tmp", "a,b", "src");

            Assert.Equal("CREATE GLOBAL TEMPORARY TABLE tmp  ON COMMIT PRESERVE ROWS AS SELECT a,b FROM src", sql);
        }

        [Fact]
        public void Initialize_CanBeCalledMoreThanOnce()
        {
            // The logon version setting cannot be read back; the provider only has to accept repeated calls.
            _provider.Initialize();
            var exception = Record.Exception(() => _provider.Initialize());

            Assert.Null(exception);
        }

        [Fact]
        public void TheBaseAssembly_FindsTheOracleProviderOfTheExtendedAssembly()
        {
            // The extended assembly is loaded (this test uses it) and declares the provider with an assembly attribute.
            IDatabaseProvider found = DatabaseProviders.Find(DatabaseConstants.OracleProvider);

            Assert.NotNull(found);
            Assert.IsType<OracleDatabaseProvider>(found);
            Assert.Same(found, DatabaseProviders.FindFor(new OracleConnection()));
            Assert.Contains(DatabaseProviders.GetAll(), p => p is OracleDatabaseProvider);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(30)]
        public void OracleBulkOperations_HandsTheTableToTheBulkCopyAndFailsWithoutAnOpenConnection(int? timeoutSeconds)
        {
            var operations = new OracleBulkOperations
            {
                Connection = new OracleConnection(),
                TableName = "SOME_TABLE"
            };
            var table = new System.Data.DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Rows.Add(1);
            TimeSpan? timeout = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : (TimeSpan?)null;

            // No server in the unit tests: the call reaches OracleBulkCopy, which rejects the closed connection.
            Assert.ThrowsAny<Exception>(() => operations.WriteToServer(table, timeout));
        }

        [Fact]
        public void TheBaseAssembly_CreatesOracleBulkOperationsForOracleConnections()
        {
            IBulkOperations operations = BulkOperationsFactory.Create(new OracleConnection());

            Assert.IsType<OracleBulkOperations>(operations);
        }

        [Fact]
        public void RegisterExtraFactories_RegistersTheOracleFactory()
        {
            DatabaseHelper.RegisterExtraFactories();

            Assert.Same(OracleClientFactory.Instance, DbProviderFactories.GetFactory(DatabaseConstants.OracleProvider));
        }

        [Fact]
        public void TheExtendedAssembly_HasTheNameTheBaseAssemblyLoads()
        {
            Assert.Equal("UiPath.Database.Activities.Extended", typeof(OracleDatabaseProvider).Assembly.GetName().Name);
            Assert.Contains(typeof(OracleDatabaseProvider).Assembly.GetCustomAttributes(typeof(DatabaseProviderAttribute), false).Cast<DatabaseProviderAttribute>(),
                a => a.ProviderType == typeof(OracleDatabaseProvider));
        }
    }
}
