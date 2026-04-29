# Database — Coded Workflow API

`UiPath.Database.Activities`

Provides coded workflow operations for querying and modifying relational databases via ADO.NET. Supports SQL Server, Oracle, SQLite, OLE DB, and ODBC data sources.

**Service accessor:** `database` (type `IDatabaseService`)
**Required package:** `"UiPath.Database.Activities": "*"` in project.json dependencies

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
System
System.Collections.Generic
System.Data
UiPath.Database
UiPath.Database.Activities
UiPath.Database.Activities.API
UiPath.Database.Activities.API.Models
```

## Service Overview

The `database` service opens a connection via `UseConnection` and returns a `DatabaseConnection` on which all operations are called directly — there are no extension methods on a separate handle type.

`UseConnection` is **synchronous** (no `Task`, no `await`). Use a standard `using` statement to ensure the connection is closed when the scope exits.

```csharp
using var conn = database.UseConnection(new DatabaseScopeOptions { ... });
// call conn.ExecuteQuery(...), conn.Execute(...), etc.
```

> **Note:** `DatabaseConnection` is `IDisposable` but not `IAsyncDisposable`. Use `using` (not `await using`).

---

## Opening a Connection

### `DatabaseConnection UseConnection(DatabaseScopeOptions options)`

Opens a database connection configured with the given options.

**Parameters:**
- `options` (`DatabaseScopeOptions`) — Provider name and connection string

**Returns:** `DatabaseConnection` — An open, disposable connection. Always use inside a `using` statement.

---

## `DatabaseConnection` — Methods & Properties

All database operations are called directly on the `DatabaseConnection` object returned by `UseConnection`.

### Property

| Property | Type | Description |
|----------|------|-------------|
| `State` | `ConnectionState?` | Current state of the underlying ADO.NET connection (e.g., `Open`, `Closed`). `null` if the connection has not been initialized. |

### Method Summary

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ExecuteQuery(string sql, Dictionary<string, ParameterInfo> parameters, TimeSpan? commandTimeout, CommandType commandType = CommandType.Text)` | `(DataTable ResultTable, DataSet ResultDataSet)` | Executes a query and returns all result sets. |
| `Execute(string sql, Dictionary<string, ParameterInfo> parameters, TimeSpan? commandTimeout, CommandType commandType = CommandType.Text)` | `int` | Executes a non-query statement and returns the number of rows affected. |
| `InsertDataTable(string tableName, DataTable dataTable, TimeSpan? commandTimeout = null)` | `int` | Inserts all rows of a `DataTable` into a database table. |
| `BulkInsertDataTable(string tableName, DataTable dataTable, TimeSpan? commandTimeout = null, IExecutorRuntime executorRuntime = null)` | `long` | Bulk-inserts all rows using native bulk copy (SQL Server / Oracle). Falls back to `InsertDataTable` for other providers. |
| `BulkUpdateDataTable(bool bulkBatch, string tableName, DataTable dataTable, string[] columnNames, TimeSpan? commandTimeout = null, IExecutorRuntime executorRuntime = null)` | `long` | Updates rows matched by `columnNames` using native bulk merge (SQL Server / Oracle) or row-by-row UPDATE. |
| `SupportsBulk()` | `bool` | Returns `true` if the current provider supports native bulk operations (SQL Server and Oracle only). |
| `BeginTransaction()` | `void` | Begins a database transaction. Only one transaction can be active at a time. |
| `Commit()` | `void` | Commits the active transaction and releases it. |
| `Rollback()` | `void` | Rolls back the active transaction and releases it. |
| `Dispose()` | `void` | Closes the connection and releases all resources. Called automatically by `using`. |

---

## Method Reference

### `(DataTable ResultTable, DataSet ResultDataSet) ExecuteQuery(string sql, Dictionary<string, ParameterInfo> parameters, TimeSpan? commandTimeout, CommandType commandType = CommandType.Text)`

Executes a SQL query and returns results. `ResultTable` contains the first result set as a `DataTable`. `ResultDataSet` contains all result sets — useful when executing stored procedures that return multiple result sets (up to 100).

Output and input-output parameter values are written back into the `parameters` dictionary after execution.

**Parameters:**
- `sql` (`string`) — SQL query text, stored procedure name, or table name
- `parameters` (`Dictionary<string, ParameterInfo>`) — Named parameters to bind. Pass an empty dictionary when no parameters are needed. Updated in-place with output/inout values after execution.
- `commandTimeout` (`TimeSpan?`) — Maximum execution time. When `null`, defaults to 30 seconds.
- `commandType` (`CommandType`) — Interpretation of the `sql` argument. Default: `CommandType.Text`.

**Returns:** `(DataTable ResultTable, DataSet ResultDataSet)` — Tuple with the first result set and all result sets.

---

### `int Execute(string sql, Dictionary<string, ParameterInfo> parameters, TimeSpan? commandTimeout, CommandType commandType = CommandType.Text)`

Executes a non-query SQL statement (INSERT, UPDATE, DELETE, DDL, or stored procedure with no result set). Output and input-output parameter values are written back into the `parameters` dictionary after execution.

**Parameters:**
- `sql` (`string`) — SQL statement, stored procedure name, or table name
- `parameters` (`Dictionary<string, ParameterInfo>`) — Named parameters to bind. Pass an empty dictionary when no parameters are needed. Updated in-place with output/inout values after execution.
- `commandTimeout` (`TimeSpan?`) — Maximum execution time. When `null`, defaults to 30 seconds.
- `commandType` (`CommandType`) — Interpretation of the `sql` argument. Default: `CommandType.Text`.

**Returns:** `int` — Number of rows affected.

---

### `int InsertDataTable(string tableName, DataTable dataTable, TimeSpan? commandTimeout = null)`

Inserts all rows of a `DataTable` into the named database table. Column names in the `DataTable` must match the column names in the target table. Works with all supported providers.

**Parameters:**
- `tableName` (`string`) — Target table name (may include schema, e.g., `"dbo.Orders"`)
- `dataTable` (`DataTable`) — Data to insert; column names must match the database table
- `commandTimeout` (`TimeSpan?`) — Per-command timeout. When `null`, defaults to 30 seconds.

**Returns:** `int` — Number of rows inserted.

---

### `long BulkInsertDataTable(string tableName, DataTable dataTable, TimeSpan? commandTimeout = null, IExecutorRuntime executorRuntime = null)`

Inserts all rows using the provider's native bulk-copy API for maximum throughput. Natively supported on **SQL Server** (`SqlBulkCopy`) and **Oracle** (`OracleBulkCopy`). For all other providers, automatically falls back to `InsertDataTable` and logs a warning if `executorRuntime` is provided.

**Parameters:**
- `tableName` (`string`) — Target table name
- `dataTable` (`DataTable`) — Data to insert; column names, types, and count must match the database table exactly
- `commandTimeout` (`TimeSpan?`) — Per-operation timeout. When `null`, defaults to 30 seconds.
- `executorRuntime` (`IExecutorRuntime`) — Optional UiPath executor runtime used to log a warning when the provider does not support native bulk operations. Pass `null` to suppress the warning.

**Returns:** `long` — Number of rows inserted (computed as the post-insert row count minus the pre-insert row count).

---

### `long BulkUpdateDataTable(bool bulkBatch, string tableName, DataTable dataTable, string[] columnNames, TimeSpan? commandTimeout = null, IExecutorRuntime executorRuntime = null)`

Updates existing rows in the target table. `columnNames` identifies the key columns used in the `WHERE` clause to match rows; all other columns in the `DataTable` are updated.

When `bulkBatch` is `true` and the provider supports bulk operations (SQL Server or Oracle), uses a MERGE statement via a temporary table. Otherwise falls back to a row-by-row `UPDATE` statement.

**Parameters:**
- `bulkBatch` (`bool`) — When `true`, attempts native bulk MERGE; falls back to row-by-row UPDATE for unsupported providers
- `tableName` (`string`) — Target table name
- `dataTable` (`DataTable`) — Data to update; must include the key columns specified in `columnNames` plus the columns to update
- `columnNames` (`string[]`) — Column names that form the match key (used in the `WHERE` / `ON` clause)
- `commandTimeout` (`TimeSpan?`) — Per-operation timeout. When `null`, defaults to 30 seconds.
- `executorRuntime` (`IExecutorRuntime`) — Optional runtime for logging; pass `null` to suppress warnings.

**Returns:** `long` — Number of rows affected.

---

### `bool SupportsBulk()`

Returns `true` if the active provider supports native bulk operations. Currently `true` for SQL Server and Oracle; `false` for SQLite, OLE DB, and ODBC.

**Returns:** `bool`

---

### `void BeginTransaction()`

Starts a database transaction. All subsequent `Execute`, `ExecuteQuery`, `InsertDataTable`, `BulkInsertDataTable`, and `BulkUpdateDataTable` calls participate in the transaction until `Commit()` or `Rollback()` is called.

Throws `InvalidOperationException` if a transaction is already active.

---

### `void Commit()`

Commits the active transaction and disposes it. After calling `Commit`, the connection continues operating in auto-commit mode until `BeginTransaction()` is called again.

---

### `void Rollback()`

Rolls back the active transaction and disposes it. After calling `Rollback`, the connection continues operating in auto-commit mode.

---

## Options & Configuration

### `DatabaseScopeOptions`

Options for configuring a database connection passed to `UseConnection`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProviderName` | `string` | — | ADO.NET provider invariant name. See the [Supported Providers](#supported-providers) table below. |
| `ConnectionString` | `string` | — | Database connection string. Format depends on the provider. |

---

## Supported Providers

| Database | `ProviderName` | Notes |
|----------|---------------|-------|
| SQL Server | `Microsoft.Data.SqlClient` | Supports native bulk operations. |
| Oracle | `Oracle.ManagedDataAccess.Client` | Supports native bulk operations. |
| SQLite | `Microsoft.Data.Sqlite` | No native bulk; uses parameterised INSERT fallback. |
| OLE DB sources | `System.Data.OleDb` | Access, Excel, CSV, legacy SQL Server. Windows only. |
| ODBC sources | `System.Data.Odbc` | Generic ODBC driver bridge. Windows only. |

---

## Supporting Types

### `ParameterInfo`

Describes a single SQL parameter passed to `Execute` or `ExecuteQuery`.

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `object` | Parameter value. Set to `DBNull.Value` or `null` for SQL NULL. |
| `Type` | `Type` | Optional explicit .NET type. Used for Oracle-specific type mapping (e.g., `typeof(OracleRefCursor)` for ref cursors). |
| `Direction` | `ArgumentDirection` | Parameter direction: `In`, `Out`, or `InOut`. Default: `In`. |

> **Output parameters:** After `Execute` or `ExecuteQuery` returns, output and inout parameter values are written back into the `parameters` dictionary. Read updated values from the dictionary using the original parameter name.

### `CommandType` (from `System.Data`)

| Value | Description |
|-------|-------------|
| `Text` | SQL text statement (default). |
| `StoredProcedure` | Stored procedure name. |
| `TableDirect` | Table name (OLE DB only). |

### `ArgumentDirection` (from `System.Activities`)

| Value | Description |
|-------|-------------|
| `In` | Input parameter. |
| `Out` | Output parameter. |
| `InOut` | Bidirectional parameter. |

---

## Common Patterns

### Query a SQL Server database

```csharp
[Workflow]
public void Execute()
{
    using var conn = database.UseConnection(new DatabaseScopeOptions
    {
        ProviderName = "Microsoft.Data.SqlClient",
        ConnectionString = "Server=myserver;Database=mydb;Integrated Security=True;"
    });

    var parameters = new Dictionary<string, ParameterInfo>();
    var (table, _) = conn.ExecuteQuery(
        "SELECT OrderId, CustomerName, Total FROM Orders WHERE Status = 'Pending'",
        parameters,
        commandTimeout: TimeSpan.FromSeconds(30));

    foreach (DataRow row in table.Rows)
        Log($"Order {row["OrderId"]}: {row["CustomerName"]} — {row["Total"]}");
}
```

### Execute a parameterised UPDATE

```csharp
[Workflow]
public void Execute()
{
    using var conn = database.UseConnection(new DatabaseScopeOptions
    {
        ProviderName = "Microsoft.Data.SqlClient",
        ConnectionString = "Server=myserver;Database=mydb;Integrated Security=True;"
    });

    var parameters = new Dictionary<string, ParameterInfo>
    {
        ["@Status"]  = new ParameterInfo { Value = "Processed", Direction = ArgumentDirection.In },
        ["@OrderId"] = new ParameterInfo { Value = 12345,       Direction = ArgumentDirection.In }
    };

    int rows = conn.Execute(
        "UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId",
        parameters,
        commandTimeout: TimeSpan.FromSeconds(15));

    Log($"{rows} row(s) updated.");
}
```

### Call a stored procedure with an output parameter

```csharp
[Workflow]
public void Execute()
{
    using var conn = database.UseConnection(new DatabaseScopeOptions
    {
        ProviderName = "Microsoft.Data.SqlClient",
        ConnectionString = "Server=myserver;Database=mydb;Integrated Security=True;"
    });

    var parameters = new Dictionary<string, ParameterInfo>
    {
        ["@CustomerId"] = new ParameterInfo { Value = 42, Direction = ArgumentDirection.In },
        ["@TotalOrders"] = new ParameterInfo { Value = 0, Direction = ArgumentDirection.Out }
    };

    conn.Execute("GetCustomerOrderCount", parameters, null, CommandType.StoredProcedure);

    // Output values are written back into the dictionary after execution
    var totalOrders = (int)parameters["@TotalOrders"].Value;
    Log($"Total orders: {totalOrders}");
}
```

### Bulk-insert a DataTable into SQL Server

```csharp
[Workflow]
public void Execute()
{
    using var conn = database.UseConnection(new DatabaseScopeOptions
    {
        ProviderName = "Microsoft.Data.SqlClient",
        ConnectionString = "Server=myserver;Database=mydb;Integrated Security=True;"
    });

    var table = new DataTable();
    table.Columns.Add("ProductId", typeof(int));
    table.Columns.Add("Name",      typeof(string));
    table.Columns.Add("Price",     typeof(decimal));
    table.Rows.Add(1, "Widget A", 9.99m);
    table.Rows.Add(2, "Widget B", 14.99m);

    long inserted = conn.BulkInsertDataTable("dbo.Products", table);
    Log($"{inserted} row(s) inserted.");
}
```

### Use a transaction with rollback on failure

```csharp
[Workflow]
public void Execute()
{
    using var conn = database.UseConnection(new DatabaseScopeOptions
    {
        ProviderName = "Microsoft.Data.SqlClient",
        ConnectionString = "Server=myserver;Database=mydb;Integrated Security=True;"
    });

    conn.BeginTransaction();
    try
    {
        var p1 = new Dictionary<string, ParameterInfo>
        {
            ["@Amount"]      = new ParameterInfo { Value = 500m, Direction = ArgumentDirection.In },
            ["@FromAccount"] = new ParameterInfo { Value = 101,  Direction = ArgumentDirection.In }
        };
        conn.Execute("UPDATE Accounts SET Balance = Balance - @Amount WHERE AccountId = @FromAccount", p1, null);

        var p2 = new Dictionary<string, ParameterInfo>
        {
            ["@Amount"]    = new ParameterInfo { Value = 500m, Direction = ArgumentDirection.In },
            ["@ToAccount"] = new ParameterInfo { Value = 202,  Direction = ArgumentDirection.In }
        };
        conn.Execute("UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountId = @ToAccount", p2, null);

        conn.Commit();
        Log("Transfer committed.");
    }
    catch (Exception ex)
    {
        conn.Rollback();
        Log($"Transfer rolled back: {ex.Message}");
        throw;
    }
}
```

### Connect to SQLite and insert rows

```csharp
[Workflow]
public void Execute()
{
    using var conn = database.UseConnection(new DatabaseScopeOptions
    {
        ProviderName = "Microsoft.Data.Sqlite",
        ConnectionString = "Data Source=C:\\Data\\inventory.db;"
    });

    var table = new DataTable();
    table.Columns.Add("ItemName", typeof(string));
    table.Columns.Add("Quantity", typeof(int));
    table.Rows.Add("Bolt M6", 200);
    table.Rows.Add("Nut M6",  200);

    int inserted = conn.InsertDataTable("Inventory", table);
    Log($"{inserted} row(s) inserted.");
}
```
