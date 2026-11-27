# Start Transaction

`UiPath.Database.Activities.DatabaseTransaction`

Connects to a database and opens a scope in which multiple database operations can be performed. Returns a `DatabaseConnection` variable for use by Database activities nested inside the scope. When the scope completes successfully and `UseTransaction` is `true`, all enclosed operations are committed as a single transaction; if any operation inside the scope fails, the transaction is rolled back automatically. See [Connection Lifecycle](#connection-lifecycle) for when the connection is closed.

> **Note:** This activity is available on Studio Desktop only (not Studio Web).

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ExistingDbConnection` | Existing connection | InArgument | `DatabaseConnection` | | | | An already-open `DatabaseConnection` to reuse. When provided, `ProviderName`, `ConnectionString`, and `ConnectionSecureString` must all be empty. Can be obtained from the output of a Connect to Database activity. |
| `ProviderName` | Provider name | InArgument | `string` | | | | The ADO.NET provider invariant name (e.g. `Microsoft.Data.SqlClient`). Required when `ExistingDbConnection` is not provided. |
| `ConnectionString` | Connection string | InArgument | `string` | | | | The connection string used to open the database connection. Provide either `ConnectionString` or `ConnectionSecureString`, not both. |
| `ConnectionSecureString` | Secure connection string | InArgument | `SecureString` | | | | The connection string as a `SecureString`. Provide either `ConnectionString` or `ConnectionSecureString`, not both. |
| `UseTransaction` | Use transaction | Property | `bool` | | `true` | | When `true`, all operations inside the scope are wrapped in a single database transaction that is committed on success or rolled back on any fault. When `false`, each operation is committed individually. |
| `ContinueOnError` | Continue on error | InArgument | `bool` | | `false` | | When `true`, the workflow continues even if an error occurs inside the scope. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `DatabaseConnection` | Database connection | OutArgument | `DatabaseConnection` | The connection used by this scope. Pass this variable to Database activities nested inside the scope. If you bind this output to a variable, the connection is left open after the scope so it can be reused by subsequent activities (you then own it and must close it — e.g. with Disconnect from Database). See [Connection Lifecycle](#connection-lifecycle). |

## Valid Property Combinations

This activity opens a connection in one of two ways:

| Mode | Required properties |
|------|-------------------|
| New connection | `ProviderName` + exactly one of `ConnectionString` or `ConnectionSecureString` |
| Existing connection | `ExistingDbConnection` only — `ProviderName`, `ConnectionString`, and `ConnectionSecureString` must all be empty |

Providing both `ExistingDbConnection` and any connection-string property throws an `ArgumentException` at runtime.

## Connection Lifecycle

Whether the connection is closed when the scope exits depends on how it was obtained and whether you capture the output:

| How the connection was obtained | `DatabaseConnection` output bound? | On scope exit |
|---|---|---|
| Created by this activity (`ProviderName` + connection string) | No | **Closed** — the activity owns it and disposes it. |
| Created by this activity (`ProviderName` + connection string) | Yes | **Left open** — you captured it to reuse, so you now own it and must close it (e.g. with Disconnect from Database). |
| Supplied via `ExistingDbConnection` | Either | **Left open** — the caller owns it; the activity never closes a connection it did not create. |

In all cases, when `UseTransaction` is `true` the transaction itself is committed (on success) or rolled back (on fault) before the scope exits; a connection left open afterwards continues in auto-commit mode with no active transaction.

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:DatabaseTransaction DisplayName="Start Transaction" ProviderName="[value]" ConnectionString="[value]" DatabaseConnection="[dbConn]">
    <db:DatabaseTransaction.Body>
      <Sequence DisplayName="Do">
        <!-- nested database activities reference DatabaseConnection="[dbConn]" -->
      </Sequence>
    </db:DatabaseTransaction.Body>
  </db:DatabaseTransaction>
</Activity>
```
