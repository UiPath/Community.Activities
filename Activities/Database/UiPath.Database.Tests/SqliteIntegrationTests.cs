using Microsoft.Data.Sqlite;
using System;
using System.Activities;
using System.Activities.Statements;
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

        [Theory, TestPriority(8)]
        [InlineData(true)]
        [InlineData(false)]
        public void DatabaseTransaction_WithExistingConnection_AllowsFollowUpQuery(bool useTransaction)
        {
            int insertedId = CountRows() + 1000;
            const string insertedName = "TransactionActivity";
            const int insertedAge = 42;

            var transactionActivity = new DatabaseTransaction
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                UseTransaction = useTransaction,
                Body = new ExecuteNonQuery
                {
                    ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                    Sql = new InArgument<string>($"INSERT INTO {TableName} (Id, Name, Age) VALUES (@id, @name, @age)"),
                    Parameters = new Dictionary<string, Argument>
                    {
                        { "@id", new InArgument<int>(insertedId) },
                        { "@name", new InArgument<string>(insertedName) },
                        { "@age", new InArgument<int>(insertedAge) }
                    },
                    AffectedRecords = new OutArgument<int>()
                }
            };

            WorkflowInvoker.Invoke(transactionActivity, TimeSpan.FromSeconds(30));

            var queryActivity = new ExecuteQuery
            {
                ExistingDbConnection = new InArgument<DatabaseConnection>(_ => _fixture.Connection),
                Sql = new InArgument<string>($"SELECT Name, Age FROM {TableName} WHERE Id = @id"),
                Parameters = new Dictionary<string, Argument>
                {
                    { "@id", new InArgument<int>(insertedId) }
                },
                DataTable = new OutArgument<DataTable>()
            };

            var outputs = WorkflowInvoker.Invoke(queryActivity, TimeSpan.FromSeconds(30));
            var table = (DataTable)outputs[nameof(ExecuteQuery.DataTable)];

            Assert.Single(table.Rows);
            Assert.Equal(insertedName, table.Rows[0]["Name"]);
            Assert.Equal((long)insertedAge, table.Rows[0]["Age"]);
        }

        [Fact, TestPriority(9)]
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

        [Fact, TestPriority(10)]
        public void DatabaseTransaction_InternalConnection_OutputBound_IsNotDisposed()
        {
            // Internally-created connection + the DatabaseConnection output is bound to a variable
            // => the user captured it to reuse, so the activity must NOT dispose it.
            var dbPath = NewTempDbPath();
            try
            {
                var connVar = new Variable<DatabaseConnection>();
                var capture = new CaptureConnection { Input = new InArgument<DatabaseConnection>(connVar) };

                var workflow = new Sequence
                {
                    Variables = { connVar },
                    Activities =
                    {
                        new DatabaseTransaction
                        {
                            ConnectionString = new InArgument<string>($"Data Source={dbPath}"),
                            ProviderName = new InArgument<string>(DatabaseConstants.SQLiteProvider),
                            DatabaseConnection = new OutArgument<DatabaseConnection>(connVar) // bound output
                        },
                        capture
                    }
                };

                WorkflowInvoker.Invoke(workflow, TimeSpan.FromSeconds(30));

                Assert.NotNull(capture.Value);
                Assert.Equal(ConnectionState.Open, capture.Value.State);

                // The documented contract: the returned connection can be used for further operations.
                var (table, _) = capture.Value.ExecuteQuery("SELECT 1", new Dictionary<string, ParameterInfo>(), TimeSpan.Zero);
                Assert.Single(table.Rows);

                capture.Value.Dispose(); // we own it now
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact, TestPriority(11)]
        public void DatabaseTransaction_InternalConnection_OutputNotBound_IsDisposed()
        {
            // Internally-created connection + the DatabaseConnection output is NOT bound (no expression)
            // => nobody can reference it after the scope, so the activity owns and disposes it.
            var dbPath = NewTempDbPath();
            try
            {
                var activity = new DatabaseTransaction
                {
                    ConnectionString = new InArgument<string>($"Data Source={dbPath}"),
                    ProviderName = new InArgument<string>(DatabaseConstants.SQLiteProvider),
                    DatabaseConnection = new OutArgument<DatabaseConnection>() // not bound (no expression)
                };

                var outputs = WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30));
                var conn = (DatabaseConnection)outputs[nameof(DatabaseTransaction.DatabaseConnection)];

                Assert.NotNull(conn);
                Assert.Equal(ConnectionState.Closed, conn.State);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        private static string NewTempDbPath()
            => Path.Combine(Path.GetTempPath(), $"uipath_sqlite_txn_{Guid.NewGuid():N}.db");

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

        /// <summary>
        /// Reads a <see cref="DatabaseConnection"/> argument at the end of a workflow body and exposes
        /// it to the test, so the connection's post-scope state can be asserted.
        /// </summary>
        private sealed class CaptureConnection : CodeActivity
        {
            public InArgument<DatabaseConnection> Input { get; set; }
            public DatabaseConnection Value { get; private set; }

            protected override void Execute(CodeActivityContext context)
            {
                Value = Input.Get(context);
            }
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
