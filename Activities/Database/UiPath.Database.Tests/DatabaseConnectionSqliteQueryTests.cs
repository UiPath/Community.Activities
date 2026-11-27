using Microsoft.Data.Sqlite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UiPath.Database;
using Xunit;

namespace UiPath.Database.Tests
{
    /// <summary>
    /// How <see cref="DatabaseConnection.ExecuteQuery"/> loads SQLite results (STUD-82629).
    /// DataTable.Load asks Microsoft.Data.Sqlite for GetSchemaTable, which reads the whole source table once per
    /// selected table column (SELECT typeof(col) ... GROUP BY typeof(col)) to pick the column type. SQLite results
    /// are now loaded without GetSchemaTable; these tests pin the resulting column types and values.
    /// </summary>
    public class DatabaseConnectionSqliteQueryTests : IDisposable
    {
        private readonly SqliteConnection _sqlite;
        private readonly DatabaseConnection _connection;
        private readonly List<string> _statements = new List<string>();

        public DatabaseConnectionSqliteQueryTests()
        {
            _sqlite = new SqliteConnection("Data Source=:memory:");
            _sqlite.Open();
            // Records every statement SQLite runs on this connection, including the ones the driver runs itself.
            raw.sqlite3_trace(_sqlite.Handle, (_, statement) => _statements.Add(statement.utf8_to_string()), null);
            _connection = new DatabaseConnection().Initialize(_sqlite);
        }

        public void Dispose()
        {
            _connection.Dispose();
            _sqlite.Dispose();
        }

        private void Execute(string sql) => _connection.Execute(sql, null, TimeSpan.Zero);

        private DataTable Query(string sql) => _connection.ExecuteQuery(sql, null, TimeSpan.Zero).ResultTable;

        private static object[] Column(DataTable table, string name) => table.Rows.Cast<DataRow>().Select(r => r[name]).ToArray();

        [Fact]
        public void ExecuteQuery_RunsOnlyTheUserStatement_WithoutFullTableTypeScans()
        {
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY, Name TEXT, Num INTEGER, C1 TEXT); CREATE INDEX IX_Name ON T(Name)");
            Execute("INSERT INTO T (Name, Num, C1) VALUES ('a', 1, 'x'), ('b', 2, 'y')");
            _statements.Clear();

            var table = Query("SELECT * FROM T WHERE Name = 'b'");

            Assert.Single(table.Rows);
            Assert.DoesNotContain(_statements, s => s.Contains("typeof(", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(new[] { "SELECT * FROM T WHERE Name = 'b'" }, _statements);
        }

        [Fact]
        public void ExecuteQuery_MapsTheStoredTypesOfEachColumn()
        {
            Execute("CREATE TABLE T (I INTEGER, R REAL, S TEXT, B BLOB)");
            Execute("INSERT INTO T VALUES (1, 1.5, 'a', x'0102'), (2, 2.5, 'b', x'03')");

            var table = Query("SELECT * FROM T");

            Assert.Equal(new[] { typeof(long), typeof(double), typeof(string), typeof(byte[]) }, table.Columns.Cast<DataColumn>().Select(c => c.DataType));
            Assert.Equal(new object[] { 1L, 2L }, Column(table, "I"));
            Assert.Equal(new object[] { 1.5, 2.5 }, Column(table, "R"));
            Assert.Equal(new object[] { "a", "b" }, Column(table, "S"));
            Assert.Equal(new byte[] { 1, 2 }, (byte[])table.Rows[0]["B"]);
        }

        [Fact]
        public void ExecuteQuery_MixedStoredTypes_UsesTheMostCommonTypeAmongTheResultRows()
        {
            // The first row holds an integer, the later rows text. Typing the column from the first row alone would
            // make it Int64 and throw "Couldn't store <2026-01-02> in D Column".
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY, D DATE, Price NUMERIC)");
            Execute("INSERT INTO T (D, Price) VALUES (20260101, 1), ('2026-01-02', 1.5), ('2026-01-03', 2.5)");

            var table = Query("SELECT D, Price FROM T ORDER BY Id");

            Assert.Equal(typeof(string), table.Columns["D"].DataType);
            Assert.Equal(new object[] { "20260101", "2026-01-02", "2026-01-03" }, Column(table, "D"));
            Assert.Equal(typeof(double), table.Columns["Price"].DataType);
            Assert.Equal(new object[] { 1.0, 1.5, 2.5 }, Column(table, "Price"));
        }

        [Fact]
        public void ExecuteQuery_MixedExpression_FallsBackToStringWhenTheMostCommonTypeCannotHoldEveryValue()
        {
            // An expression has no source column, so 2.1.1 (DataTable.Load) typed it from the first row: String.
            // Int64 is the most common type here but cannot hold 'a', so the column falls back to String.
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY)");
            Execute("INSERT INTO T (Id) VALUES (1), (2), (3)");

            var table = Query("SELECT CASE WHEN Id = 1 THEN 'a' ELSE Id END AS V FROM T ORDER BY Id");

            Assert.Equal(typeof(string), table.Columns["V"].DataType);
            Assert.Equal(new object[] { "a", "2", "3" }, Column(table, "V"));
        }

        [Fact]
        public void ExecuteQuery_TiedTypes_FallBackToStringWhenTheFirstSeenTypeCannotHoldEveryValue()
        {
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY, D DATE)");
            Execute("INSERT INTO T (D) VALUES (20260101), ('2026-01-02')");

            var table = Query("SELECT D FROM T ORDER BY Id");

            Assert.Equal(typeof(string), table.Columns["D"].DataType);
            Assert.Equal(new object[] { "20260101", "2026-01-02" }, Column(table, "D"));
        }

        [Fact]
        public void ExecuteQuery_MinorityValuesThatConvert_KeepTheMostCommonType()
        {
            // As with DataTable.Load, a value of a less common type is converted when the column type can hold it.
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY, V)");
            Execute("INSERT INTO T (V) VALUES (5), (6), ('7')");

            var table = Query("SELECT V FROM T ORDER BY Id");

            Assert.Equal(typeof(long), table.Columns["V"].DataType);
            Assert.Equal(new object[] { 5L, 6L, 7L }, Column(table, "V"));
        }

        [Fact]
        public void ExecuteQuery_FirstRowNull_TakesTheTypeFromTheOtherRows()
        {
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY, V)");
            Execute("INSERT INTO T (V) VALUES (NULL), (5), (6)");

            var table = Query("SELECT V FROM T ORDER BY Id");

            Assert.Equal(typeof(long), table.Columns["V"].DataType);
            Assert.Equal(new object[] { DBNull.Value, 5L, 6L }, Column(table, "V"));
        }

        [Theory]
        [InlineData("SELECT * FROM T")]
        [InlineData("SELECT * FROM T WHERE 0")]
        public void ExecuteQuery_NoNonNullValues_UsesTheDeclaredColumnType(string sql)
        {
            Execute("CREATE TABLE T (I INTEGER, R REAL, S TEXT, B BLOB, N NUMERIC)");
            Execute("INSERT INTO T VALUES (NULL, NULL, NULL, NULL, NULL)");

            var table = Query(sql);

            // Same mapping the driver uses for a NULL value: the affinity of the declared type (NUMERIC maps to text).
            Assert.Equal(new[] { typeof(long), typeof(double), typeof(string), typeof(byte[]), typeof(string) }, table.Columns.Cast<DataColumn>().Select(c => c.DataType));
        }

        [Fact]
        public void ExecuteQuery_Expressions_MapLikeTheDriver()
        {
            var table = Query("SELECT 1 AS One, 'a' AS S, 1.5 AS R, NULL AS N");

            Assert.Equal(new[] { typeof(long), typeof(string), typeof(double), typeof(byte[]) }, table.Columns.Cast<DataColumn>().Select(c => c.DataType));
            Assert.Equal(new object[] { 1L, "a", 1.5, DBNull.Value }, table.Rows[0].ItemArray);
        }

        // Expected names are what DataTable.Load returned for the same queries (checked against Microsoft.Data.Sqlite 8.0.34).
        [Theory]
        [InlineData("SELECT Name, Name, name FROM T", "Name|Name1|Name2")] // SQLite reports the declared spelling of a bare column
        [InlineData("SELECT 1 AS a, 2 AS a, 3 AS a1", "a|a2|a1")]
        [InlineData("SELECT 1 AS a1, 2 AS a, 3 AS a", "a1|a|a2")]
        [InlineData("SELECT 1 AS x, 2 AS X, 3 AS x", "x|X1|x2")]
        [InlineData("SELECT 1 AS \"\", 2 AS \"\"", "Column1|Column2")]
        [InlineData("SELECT 1 AS Column1, 2 AS \"\"", "Column1|Column2")]
        public void ExecuteQuery_DuplicateOrEmptyColumnNames_GetTheNamesDataTableLoadGave(string sql, string expected)
        {
            Execute("CREATE TABLE T (Name TEXT)");
            Execute("INSERT INTO T VALUES ('a')");

            var table = Query(sql);

            Assert.Equal(expected.Split('|'), table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        }

        [Fact]
        public void ExecuteQuery_RowsAreUnchanged_LikeDataTableLoad()
        {
            Execute("CREATE TABLE T (V TEXT)");
            Execute("INSERT INTO T VALUES ('a'), ('b')");

            var table = Query("SELECT V FROM T");

            Assert.All(table.Rows.Cast<DataRow>(), r => Assert.Equal(DataRowState.Unchanged, r.RowState));
            Assert.Null(table.GetChanges());
        }

        [Fact]
        public void ExecuteQuery_DoesNotCopyTableConstraints()
        {
            // Documented difference from 2.1.1: DataTable.Load copied NOT NULL, UNIQUE, AUTOINCREMENT and the primary key
            // of WITHOUT ROWID tables from GetSchemaTable. Those are no longer read, so the DataTable has no constraints.
            Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Code TEXT UNIQUE)");
            Execute("CREATE TABLE K (K TEXT PRIMARY KEY, V TEXT) WITHOUT ROWID");
            Execute("INSERT INTO T (Name, Code) VALUES ('a', 'x'); INSERT INTO K VALUES ('k', 'v')");

            var people = Query("SELECT * FROM T");
            var keyed = Query("SELECT * FROM K");

            Assert.All(people.Columns.Cast<DataColumn>(), c =>
            {
                Assert.True(c.AllowDBNull);
                Assert.False(c.Unique);
                Assert.False(c.AutoIncrement);
            });
            Assert.Empty(keyed.PrimaryKey);
            Assert.Empty(people.Constraints);
        }

        [Fact]
        public void ExecuteQuery_MultipleResultSets_AreAllReturned()
        {
            Execute("CREATE TABLE T (V TEXT)");

            var (first, dataSet) = _connection.ExecuteQuery("SELECT 1 AS A; INSERT INTO T VALUES ('x'); SELECT V FROM T; SELECT 2.5 AS C", null, TimeSpan.Zero);

            Assert.Equal(3, dataSet.Tables.Count);
            Assert.Same(dataSet.Tables[0], first);
            Assert.Equal(1L, dataSet.Tables[0].Rows[0]["A"]);
            Assert.Equal("x", dataSet.Tables[1].Rows[0]["V"]);
            Assert.Equal(2.5, dataSet.Tables[2].Rows[0]["C"]);
        }

        [Fact]
        public void ExecuteQuery_StatementWithoutRows_ReturnsAnEmptyTable()
        {
            Execute("CREATE TABLE T (V TEXT)");

            var (table, dataSet) = _connection.ExecuteQuery("INSERT INTO T VALUES ('x')", null, TimeSpan.Zero);

            Assert.Single(dataSet.Tables);
            Assert.Empty(table.Columns);
            Assert.Empty(table.Rows);
        }

        [Fact]
        public void ExecuteQuery_WithParameters_ReturnsTheMatchingRow()
        {
            Execute("CREATE TABLE T (Id INTEGER, Name TEXT)");
            Execute("INSERT INTO T VALUES (1, 'a'), (2, 'b')");
            var parameters = new Dictionary<string, ParameterInfo> { { "@id", new ParameterInfo { Value = 2 } } };

            var table = _connection.ExecuteQuery("SELECT Name FROM T WHERE Id = @id", parameters, TimeSpan.Zero).ResultTable;

            Assert.Equal(new object[] { "b" }, Column(table, "Name"));
        }
    }
}
