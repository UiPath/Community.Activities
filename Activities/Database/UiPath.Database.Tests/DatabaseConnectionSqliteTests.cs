using System;
using System.Data;
using UiPath.Database;
using Xunit;

namespace UiPath.Database.Tests
{
    /// <summary>Paths of <see cref="DatabaseConnection"/> that a real in-memory SQLite database can exercise.</summary>
    public class DatabaseConnectionSqliteTests : IDisposable
    {
        private readonly DatabaseConnection _connection;

        public DatabaseConnectionSqliteTests()
        {
            _connection = new DatabaseConnection().Initialize("Data Source=:memory:", DatabaseConstants.SQLiteProvider);
            _connection.Execute("CREATE TABLE People (Id INTEGER, Name TEXT, Age INTEGER)", null, TimeSpan.Zero);
        }

        public void Dispose() => _connection.Dispose();

        private static DataTable People(params (long Id, string Name, long Age)[] rows)
        {
            var table = new DataTable("People");
            table.Columns.Add("Id", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(long));
            foreach (var row in rows)
            {
                table.Rows.Add(row.Id, row.Name, row.Age);
            }

            return table;
        }

        private long CountRows() => Convert.ToInt64(_connection.ExecuteQuery("SELECT COUNT(*) AS C FROM People", null, TimeSpan.Zero).ResultTable.Rows[0]["C"]);

        [Fact]
        public void InsertDataTable_InsertsRowsOfAnEmptyAndANonEmptyTable()
        {
            Assert.Equal(0, _connection.InsertDataTable("People", People()));
            Assert.Equal(2, _connection.InsertDataTable("People", People((1, "a", 10), (2, "b", 20))));
            Assert.Equal(2, CountRows());
        }

        [Fact]
        public void InsertDataTable_ReportsBothFailuresWhenTheTableDoesNotExist()
        {
            var exception = Assert.Throws<AggregateException>(() => _connection.InsertDataTable("NoSuchTable", People((1, "a", 10))));

            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.Equal(0, CountRows());
        }

        [Fact]
        public void InsertDataTable_RollsBackTheLocalTransactionWhenARowFails()
        {
            _connection.Execute("CREATE TABLE Strict (Id INTEGER PRIMARY KEY, Name TEXT)", null, TimeSpan.Zero);
            var table = new DataTable("Strict");
            table.Columns.Add("Id", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1L, "a");
            table.Rows.Add(1L, "duplicate key");

            Assert.ThrowsAny<Exception>(() => _connection.InsertDataTable("Strict", table));

            Assert.Equal(0L, Convert.ToInt64(_connection.ExecuteQuery("SELECT COUNT(*) AS C FROM Strict", null, TimeSpan.Zero).ResultTable.Rows[0]["C"]));
        }

        [Fact]
        public void BulkInsertDataTable_FallsBackToInsertAndWarnsThroughTheRuntime()
        {
            var runtime = new Moq.Mock<UiPath.Robot.Activities.Api.IExecutorRuntime>();

            long inserted = _connection.BulkInsertDataTable("People", People((1, "a", 10), (2, "b", 20)), null, runtime.Object);

            Assert.Equal(2, inserted);
            runtime.Verify(r => r.LogMessage(Moq.It.IsAny<UiPath.Robot.Activities.Api.LogMessage>()), Moq.Times.Once);
        }

        [Fact]
        public void BeginTransaction_CannotBeNested()
        {
            _connection.BeginTransaction();

            Assert.Throws<InvalidOperationException>(() => _connection.BeginTransaction());
            _connection.Rollback();
        }

        [Fact]
        public void CommitAndRollback_WithoutATransactionDoNothing()
        {
            _connection.Commit();
            _connection.Rollback();

            _connection.BeginTransaction();
            _connection.Execute("INSERT INTO People VALUES (1, 'a', 1)", null, TimeSpan.Zero);
            _connection.Rollback();
            Assert.Equal(0, CountRows());
        }

        [Fact]
        public void State_IsOpenUntilTheConnectionIsDisposed()
        {
            Assert.Equal(ConnectionState.Open, _connection.State);
            _connection.Dispose();
            Assert.Equal(ConnectionState.Closed, _connection.State);
        }
    }
}
