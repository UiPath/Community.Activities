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

            dataReader.Setup(r => r.FieldCount).Returns(0);
            dataReader.Setup(r => r.Read()).Returns(false);
            dataReader.Setup(r => r.NextResult()).Returns(false);

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
        public void ExecuteQuery_SingleResultSet_ReturnsDataSetWithOneTable()
        {
            var con = new Mock<DbConnection>();
            var cmd = new Mock<DbCommand>();
            var dbParameterCollection = new Mock<DbParameterCollection>();
            var iEnum = new List<DbParameter>();
            var dataReader = new Mock<DbDataReader>();

            con.SetReturnsDefault(cmd.Object);
            cmd.SetReturnsDefault(dbParameterCollection.Object);
            cmd.SetReturnsDefault(dataReader.Object);
            dbParameterCollection.Setup(x => x.GetEnumerator()).Returns(iEnum.GetEnumerator());

            bool readerClosed = false;

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r.GetName(0)).Returns("Col1");
            dataReader.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
            dataReader.Setup(r => r.Read()).Returns(false);
            dataReader.Setup(r => r.IsClosed).Returns(() => readerClosed);
            dataReader.Setup(r => r.Close()).Callback(() => readerClosed = true);
            dataReader.Setup(r => r.NextResult()).Returns(() => { readerClosed = true; return false; });

            var databaseConnection = new DatabaseConnection().Initialize(con.Object);
            var parameters = new Dictionary<string, ParameterInfo>();

            var (resultTable, resultDataSet) = databaseConnection.ExecuteQuery("SELECT 1", parameters, 0);

            Assert.NotNull(resultDataSet);
            Assert.Single(resultDataSet.Tables);
            Assert.Same(resultDataSet.Tables[0], resultTable);
        }

        [Fact]
        public void ExecuteQuery_MultipleResultSets_ReturnsDataSetWithMultipleTables()
        {
            var con = new Mock<DbConnection>();
            var cmd = new Mock<DbCommand>();
            var dbParameterCollection = new Mock<DbParameterCollection>();
            var iEnum = new List<DbParameter>();
            var dataReader = new Mock<DbDataReader>();

            con.SetReturnsDefault(cmd.Object);
            cmd.SetReturnsDefault(dbParameterCollection.Object);
            cmd.SetReturnsDefault(dataReader.Object);
            dbParameterCollection.Setup(x => x.GetEnumerator()).Returns(iEnum.GetEnumerator());

            // State tracking across result sets
            bool inSecondResultSet = false;
            bool secondRowRead = false;
            bool readerClosed = false;

            // Both result sets have 1 column; names differ to distinguish them
            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r.GetName(0)).Returns(() => inSecondResultSet ? "SecondTableCol" : "FirstTableCol");
            dataReader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
            dataReader.Setup(r => r.Read()).Returns(() =>
            {
                if (inSecondResultSet && !secondRowRead)
                {
                    secondRowRead = true;
                    return true;
                }
                return false;
            });
            dataReader.Setup(r => r.GetValues(It.IsAny<object[]>())).Returns((object[] vals) =>
            {
                vals[0] = "TestValue";
                return 1;
            });

            // DataTable.Load calls NextResult after consuming first result set.
            // First call: returns true (more results), advances to second result set.
            // Second call: returns false (no more), reader is done.
            int nextResultCalls = 0;
            dataReader.Setup(r => r.NextResult()).Returns(() =>
            {
                nextResultCalls++;
                if (nextResultCalls == 1) { inSecondResultSet = true; return true; }
                readerClosed = true;
                return false;
            });

            dataReader.Setup(r => r.IsClosed).Returns(() => readerClosed);
            dataReader.Setup(r => r.Close()).Callback(() => readerClosed = true);

            var databaseConnection = new DatabaseConnection().Initialize(con.Object);
            var parameters = new Dictionary<string, ParameterInfo>();

            var (resultTable, resultDataSet) = databaseConnection.ExecuteQuery("SELECT 1; SELECT 2", parameters, 0);

            Assert.NotNull(resultDataSet);
            Assert.Equal(2, resultDataSet.Tables.Count);
            Assert.Same(resultDataSet.Tables[0], resultTable);
            Assert.Single(resultDataSet.Tables[1].Columns);
            Assert.Equal("SecondTableCol", resultDataSet.Tables[1].Columns[0].ColumnName);
            Assert.Single(resultDataSet.Tables[1].Rows);
            Assert.Equal("TestValue", resultDataSet.Tables[1].Rows[0][0]);
        }

        [Fact]
        public void ExecuteQuery_DuplicateColumnNames_DeduplicatesInSubsequentResultSets()
        {
            var con = new Mock<DbConnection>();
            var cmd = new Mock<DbCommand>();
            var dbParameterCollection = new Mock<DbParameterCollection>();
            var iEnum = new List<DbParameter>();
            var dataReader = new Mock<DbDataReader>();

            con.SetReturnsDefault(cmd.Object);
            cmd.SetReturnsDefault(dbParameterCollection.Object);
            cmd.SetReturnsDefault(dataReader.Object);
            dbParameterCollection.Setup(x => x.GetEnumerator()).Returns(iEnum.GetEnumerator());

            bool inSecondResultSet = false;
            bool readerClosed = false;

            // First result set: 1 column. Second: 2 columns with same name "Col".
            dataReader.Setup(r => r.FieldCount).Returns(() => inSecondResultSet ? 2 : 1);
            dataReader.Setup(r => r.GetName(It.IsAny<int>())).Returns("Col");
            dataReader.Setup(r => r.GetFieldType(It.IsAny<int>())).Returns(typeof(string));
            dataReader.Setup(r => r.Read()).Returns(false);

            int nextResultCalls = 0;
            dataReader.Setup(r => r.NextResult()).Returns(() =>
            {
                nextResultCalls++;
                if (nextResultCalls == 1) { inSecondResultSet = true; return true; }
                readerClosed = true;
                return false;
            });

            dataReader.Setup(r => r.IsClosed).Returns(() => readerClosed);
            dataReader.Setup(r => r.Close()).Callback(() => readerClosed = true);

            var databaseConnection = new DatabaseConnection().Initialize(con.Object);
            var parameters = new Dictionary<string, ParameterInfo>();

            var (_, resultDataSet) = databaseConnection.ExecuteQuery("SELECT 1; SELECT 2", parameters, 0);

            Assert.Equal(2, resultDataSet.Tables.Count);
            var secondTable = resultDataSet.Tables[1];
            Assert.Equal(2, secondTable.Columns.Count);
            Assert.Equal("Col", secondTable.Columns[0].ColumnName);
            Assert.Equal("Col1", secondTable.Columns[1].ColumnName);
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