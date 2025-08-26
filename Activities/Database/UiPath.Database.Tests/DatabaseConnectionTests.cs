using Moq;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using UiPath.Database.Activities;
using Xunit;
using UiPath.Database;

namespace UiPath.Database.Tests
{
    public class DatabaseConnectionTests
    {
        [Fact]
        public void TestConnect()
        {
            var factory = new Mock<IDBConnectionFactory>();
            factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>()))
                                .Returns(() => new DatabaseConnection());
            var connectActivity = new DatabaseConnect(factory.Object)
            {
                ConnectionString = new InArgument<string>("alpha"),
                ProviderName = new InArgument<string>("beta")
            };

            WorkflowInvoker.Invoke(connectActivity, TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void TestDisconect()
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = false;
            dbConnection.Setup(con => con.Dispose()).Callback(() => executed = true);
            dynamic arguments = new ExpandoObject();
            arguments.DatabaseConnection = dbConnection.Object;
            var disconnectActivity = new DatabaseDisconnect()
            {
                DatabaseConnection = new InArgument<DatabaseConnection>(ctx => dbConnection.Object),
            };
            
            WorkflowInvoker.Invoke(disconnectActivity, TimeSpan.FromSeconds(30));

            Assert.True(executed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestDatabaseTransactionWithEmptyBody(bool useTransaction)
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = false;
            dbConnection.Setup(con => con.BeginTransaction()).Callback(() => executed = true);
            dynamic arguments = new ExpandoObject();
            arguments.ExistingDbConnection = dbConnection.Object;
            var dbTransactionActivity = new DatabaseTransaction 
            { 
                UseTransaction = useTransaction,
                ExistingDbConnection = new InArgument<DatabaseConnection>(ctx => dbConnection.Object)
            };

            WorkflowInvoker.Invoke(dbTransactionActivity, TimeSpan.FromSeconds(30));
            Assert.True(executed == useTransaction);
        }

        [Theory]
        [InlineData(DatabaseConstants.OdbcProvider)]
        [InlineData(DatabaseConstants.OleDbProvider)]
        [InlineData(DatabaseConstants.SqlServerProvider)]
        [InlineData(DatabaseConstants.OracleProvider)]
        [InlineData("Mysql.Data.MysqlClient")] //Legacy
        [InlineData("System.Data.OracleClient")] //Legacy
        [InlineData("Oracle.DataAccess.Client")] //Legacy
        public void TestSize(string provider)
        {
            var con = new Mock<DbConnection>();
            var cmd = new Mock<DbCommand>();
            var dbParameterCollection = new Mock<DbParameterCollection>();
            var iEnum = new List<DbParameter>();
            var param = new Mock<DbParameter>();
            var dataReader = new Mock<DbDataReader>();

            con.SetReturnsDefault(cmd.Object);
            con.SetReturnsDefault(provider);

            cmd.SetReturnsDefault(dbParameterCollection.Object);
            cmd.SetReturnsDefault(param.Object);
            cmd.SetReturnsDefault(dataReader.Object);
            dbParameterCollection.Setup(x => x.GetEnumerator()).Returns(iEnum.GetEnumerator());
            param.SetupAllProperties();
            param.SetReturnsDefault(ParameterDirection.InputOutput);

            var databaseConnection = new DatabaseConnection().Initialize(con.Object);
            var parameters = new Dictionary<string, ParameterInfo>() { 
                { "param1", new ParameterInfo() {Value = "", Direction = ArgumentDirection.Out}
                }
            };
            databaseConnection.ExecuteQuery("TestProcedure", parameters, 0);
            if (provider.Contains(DatabaseConstants.OraclePattern, StringComparison.OrdinalIgnoreCase))
                Assert.True(param.Object.Size == 1000000);
            if (!provider.ToLower().Contains(DatabaseConstants.OraclePattern, StringComparison.OrdinalIgnoreCase))
                Assert.True(param.Object.Size == -1);
        }

        [Fact]
        public void BulkInsertTest()
        {
            var dbDataTable = new DataTable();
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = false;
            dbConnection.Setup(con => con.SupportsBulk()).Callback(() => executed = true);
            dbConnection.Object.BulkInsertDataTable("test", dbDataTable);

            Assert.True(executed == true);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BulkUpdateTest(bool supportBulk)
        {
            var dbDataTable = new DataTable();
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = !supportBulk;
            string[] pars = { "Id1", "Id2" };
            dbConnection.Setup(con => con.SupportsBulk()).Callback(() => executed = true);
            dbConnection.Object.BulkUpdateDataTable(supportBulk, "test", dbDataTable,pars);

            Assert.True(executed == true);
        }
    }
}