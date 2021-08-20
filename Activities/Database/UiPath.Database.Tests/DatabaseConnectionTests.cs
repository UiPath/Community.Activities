using Microsoft.Activities.UnitTesting;
using Moq;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Dynamic;
using UiPath.Database.Activities;
using UiPath.Database.BulkOps;
using Xunit;

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
            var host = new WorkflowInvokerTest(connectActivity);
            var ex = Record.Exception(() => host.TestActivity());
            Assert.Null(ex);
        }

        [Fact]
        public void TestDisconect()
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = false;
            dbConnection.Setup(con => con.Dispose()).Callback(() => executed = true);
            dynamic arguments = new ExpandoObject();
            arguments.DatabaseConnection = dbConnection.Object;
            var host = new WorkflowInvokerTest(new DatabaseDisconnect(), arguments);
            var ex = Record.Exception(() => host.TestActivity());
            Assert.Null(ex);
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
            var dbTransactionActivity = new DatabaseTransaction { UseTransaction = useTransaction };
            var host = new WorkflowInvokerTest(dbTransactionActivity, arguments);
            var ex = Record.Exception(() => host.TestActivity());
            Assert.Null(ex);
            Assert.True(executed == useTransaction);
        }

        [Theory]
        [InlineData("System.Data.Odbc")]
        [InlineData("System.Data.Oledb")]
        [InlineData("System.Data.OracleClient")]
        [InlineData("System.Data.SqlClient")]
        [InlineData("Oracle.DataAccess.Client")]
        [InlineData("Oracle.ManagedDataAccess.Client")]
        [InlineData("Mysql.Data.MysqlClient")]
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
            var parameters = new Dictionary<string, Tuple<object, ArgumentDirection>>() { { "param1", new Tuple<object, ArgumentDirection>("", ArgumentDirection.Out) } };
            databaseConnection.ExecuteQuery("TestProcedure", parameters, 0);
            if (provider.ToLower().Contains("oracle"))
                Assert.True(param.Object.Size == 1000000);
            if (!provider.ToLower().Contains("oracle"))
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