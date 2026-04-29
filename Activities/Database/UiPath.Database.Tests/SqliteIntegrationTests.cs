using Microsoft.Data.Sqlite;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UiPath.Database.Activities;
using UiPath.Database.Tests.Utils;
using Xunit;
using UiPath.Database;

namespace UiPath.Database.Tests
{
    /// <summary>
    /// Integration tests that exercise all database activities against a real SQLite database.
    /// Tests are ordered so each step builds on the state left by the previous one.
    /// </summary>
    [TestCaseOrderer("UiPath.Database.Tests.Utils.PriorityOrderer", "UiPath.Database.Tests")]
    public class SqliteIntegrationTests : IClassFixture<SqliteFixture>
    {
        private const string TableName = "People";

        private readonly SqliteFixture _fixture;

        public SqliteIntegrationTests(SqliteFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact, TestPriority(1)]
        public void CreateTable_ExecuteNonQuery_CreatesTableSuccessfully()
        {
            var sql = "CREATE TABLE IF NOT EXISTS " + TableName + " (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Age INTEGER NOT NULL)";

            var activity = new ExecuteNonQuery
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                Sql = new InArgument<string>(sql),
                AffectedRecords = new OutArgument<int>()
            };

            WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30));

            // Verify table exists by querying it
            var tableParams = new Dictionary<string, ParameterInfo>
            {
                { "@tableName", new ParameterInfo { Value = TableName } }
            };
            var (table, _) = _fixture.Connection.ExecuteQuery("SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName", tableParams, TimeSpan.Zero);
            Assert.Single(table.Rows);
        }

        [Fact, TestPriority(2)]
        public void InsertDataTable_InsertsRowsSuccessfully()
        {
            var dataTable = BuildPeopleTable(
                (1, "Alice", 30),
                (2, "Bob", 25));

            var activity = new InsertDataTable
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                TableName = new InArgument<string>(TableName),
                DataTable = new InArgument<DataTable>(_ => dataTable),
                AffectedRecords = new OutArgument<int>()
            };

            var outputs = WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30));

            var affected = (int)outputs[nameof(InsertDataTable.AffectedRecords)];
            Assert.Equal(2, affected);
        }

        [Fact, TestPriority(3)]
        public void BulkInsert_InsertsRowsSuccessfully()
        {
            var dataTable = BuildPeopleTable(
                (3, "Charlie", 40),
                (4, "Diana", 35));

            var activity = new BulkInsert
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                TableName = new InArgument<string>(TableName),
                DataTable = new InArgument<DataTable>(_ => dataTable),
                AffectedRecords = new OutArgument<long>()
            };

            var outputs = WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30));

            var affected = (long)outputs[nameof(BulkInsert.AffectedRecords)];
            Assert.Equal(2, affected);
        }

        [Fact, TestPriority(4)]
        public void ExecuteQuery_ReturnsAllInsertedRows()
        {
            var activity = new ExecuteQuery
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                Sql = new InArgument<string>("SELECT * FROM " + TableName + " ORDER BY Id"),
                DataTable = new OutArgument<DataTable>()
            };

            var outputs = WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30));

            var table = (DataTable)outputs[nameof(ExecuteQuery.DataTable)];
            Assert.Equal(4, table.Rows.Count);
            Assert.Equal("Alice", table.Rows[0]["Name"]);
            Assert.Equal("Bob", table.Rows[1]["Name"]);
            Assert.Equal("Charlie", table.Rows[2]["Name"]);
            Assert.Equal("Diana", table.Rows[3]["Name"]);
        }

        [Fact, TestPriority(5)]
        public void ExecuteNonQuery_WithParameters_UpdatesRow()
        {
            var activity = new ExecuteNonQuery
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                Sql = new InArgument<string>("UPDATE " + TableName + " SET Age = @newAge WHERE Name = @name"),
                Parameters = new Dictionary<string, Argument>
                {
                    { "@newAge", new InArgument<int>(99) },
                    { "@name",   new InArgument<string>("Alice") }
                },
                AffectedRecords = new OutArgument<int>()
            };

            var outputs = WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30));

            var affected = (int)outputs[nameof(ExecuteNonQuery.AffectedRecords)];
            Assert.Equal(1, affected);

            // Verify the value was actually updated
            var verifyParams = new Dictionary<string, ParameterInfo>
            {
                { "@name", new ParameterInfo { Value = "Alice" } }
            };
            var (table, _) = _fixture.Connection.ExecuteQuery("SELECT Age FROM " + TableName + " WHERE Name=@name", verifyParams, TimeSpan.Zero);
            Assert.Equal(99L, table.Rows[0]["Age"]);
        }

        [Fact, TestPriority(6)]
        public void Transaction_Rollback_DoesNotPersistRows()
        {
            int countBefore = CountRows();

            _fixture.Connection.BeginTransaction();
            var dataTable = BuildPeopleTable((5, "RolledBack", 1));
            _fixture.Connection.InsertDataTable(TableName, dataTable);

            _fixture.Connection.Rollback();

            int countAfter = CountRows();
            Assert.Equal(countBefore, countAfter);
        }

        [Fact, TestPriority(7)]
        public void Transaction_Commit_PersistsRows()
        {
            int countBefore = CountRows();

            _fixture.Connection.BeginTransaction();
            var dataTable = BuildPeopleTable((5, "Committed", 50));
            _fixture.Connection.InsertDataTable(TableName, dataTable);

            _fixture.Connection.Commit();

            int countAfter = CountRows();
            Assert.Equal(countBefore + 1, countAfter);
        }

        [Fact, TestPriority(8)]
        public void ConnectAndDisconnect_WithConnectionString_OpensAndClosesConnection()
        {
            DatabaseConnection connection = null;

            var connectActivity = new DatabaseConnect
            {
                ConnectionString = new InArgument<string>($"Data Source={_fixture.DbPath}"),
                ProviderName = new InArgument<string>(DatabaseConstants.SQLiteProvider),
                DatabaseConnection = new OutArgument<DatabaseConnection>()
            };

            var connectOutputs = WorkflowInvoker.Invoke(connectActivity, TimeSpan.FromSeconds(30));
            connection = (DatabaseConnection)connectOutputs[nameof(DatabaseConnect.DatabaseConnection)];

            Assert.NotNull(connection);
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);

            var disconnectActivity = new DatabaseDisconnect
            {
                DatabaseConnection = new InArgument<DatabaseConnection>(_ => connection)
            };

            WorkflowInvoker.Invoke(disconnectActivity, TimeSpan.FromSeconds(30));

            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
        }

        private int CountRows()
        {
            var (table, _) = _fixture.Connection.ExecuteQuery("SELECT COUNT(*) AS Cnt FROM " + TableName, null, TimeSpan.Zero);
            return Convert.ToInt32(table.Rows[0]["Cnt"]);
        }

        private static DataTable BuildPeopleTable(params (int Id, string Name, int Age)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(long));

            foreach (var (id, name, age) in rows)
                table.Rows.Add((long)id, name, (long)age);

            return table;
        }
    }

    public sealed class SqliteFixture : IDisposable
    {
        private readonly string _dbPath;

        public string DbPath => _dbPath;

        public DatabaseConnection Connection { get; }

        public SqliteFixture()
        {
            // Use a temp file so the database is created fresh on first access and cleaned up after.
            _dbPath = Path.Combine(Path.GetTempPath(), $"uipath_sqlite_test_{Guid.NewGuid():N}.db");
            Connection = new DatabaseConnection().Initialize($"Data Source={_dbPath}", DatabaseConstants.SQLiteProvider);
            // Create the schema upfront so tests that depend on the table don't fail when run
            // out of order or in isolation (test ordering is best-effort, not guaranteed).
            Connection.Execute(
                "CREATE TABLE IF NOT EXISTS People (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Age INTEGER NOT NULL)",
                new Dictionary<string, ParameterInfo>(), TimeSpan.Zero);
        }

        public void Dispose()
        {
            Connection.Dispose();
            // Clear the connection pool so SQLite releases its file lock before deletion.
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
    }
}
