using Microsoft.Activities.UnitTesting;
using Moq;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

        [Theory]
        [InlineData("System.Data.Odbc")]
        [InlineData("System.Data.Oledb")]
        [InlineData("System.Data.OracleClient")]
        [InlineData("System.Data.SqlClient")]
        [InlineData("Oracle.DataAccess.Client")]
        [InlineData("Oracle.ManagedDataAccess.Client")]
        [InlineData("Mysql.Data.MysqlClient")]
        public void BulkInsertTest(string provider)
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var dbDataTable = new Mock<DataTable>();
            var dbDataAdapter = new Mock<DbDataAdapter>();
            var bulkOps = new Mock<IBulkOperations>();
            var connection = new Mock<DbConnection>();
            var command = new Mock<DbCommand>();
            var executed = false;
            var fallback = false;

            bulkOps.Setup(x => x.WriteToServer(dbDataTable.Object)).Callback(() => executed = true);
            dbConnection.Setup(x => x.InsertDataTable("test", dbDataTable.Object, true)).Callback(() => fallback = true);

            dbConnection.Object.DoBulkInsert(provider, "test", dbDataTable.Object, ".", null, dbDataAdapter.Object, bulkOps.Object, command.Object, command.Object, out long affectedRecords);

            if (provider == "System.Data.SqlClient" || provider == "Oracle.ManagedDataAccess.Client")
            {
                Assert.True(executed || fallback);
            }
            else
            {
                Assert.False(executed);
            }
        }

        [Fact]
        public void BulkUpdateTest()
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var dbDataTable = new DataTable();
            var dbDataAdapter = new Mock<DbDataAdapter>();
            var connection = new Mock<DbConnection>();
            var transaction = new Mock<DbTransaction>();
            var command = new Mock<DbCommand>();
            var updateParam = new Mock<List<DbParameter>>();
            var whereParam = new Mock<List<DbParameter>>();
            var columns = new string[] {"C1"};

            dbDataTable.Columns.Add("C1");
            dbDataTable.Columns.Add("C2");
            dbDataTable.Rows.Add("1", "2");

            dbConnection
                .Setup(u => u.BuildParameter(It.IsAny<DbCommand>(), It.IsAny<string>(), It.IsAny<DataColumn>()))
                .Returns((DbCommand comm, string name, DataColumn column) =>
                    Mock.Of<DbParameter>(x => x.ParameterName == name && x.Value == column)
                );

            var result = dbConnection.Object.SetupBulkUpdateCommand("test", dbDataTable, columns, "{0}", connection.Object, transaction.Object, command.Object, updateParam.Object, whereParam.Object);

            var commandText = string.Format("UPDATE {0} SET {1} WHERE {2}", "test", result.Item1,result.Item2);

            Assert.Equal("UPDATE test SET  \"C2\"=p2 WHERE  \"C1\"=p1", commandText);
        }
    }
}