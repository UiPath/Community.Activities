# Run Query

`UiPath.Database.Activities.ExecuteQuery`

Executes a query on a database and returns the query result as a Data Table and optionally as a Data Set containing all result sets

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ExistingDbConnection` | Existing connection | Property | `DatabaseConnection` | Yes |  | Use the output of the Connect to Database activity | An already opened database connection obtained from the Connect to Database activity. |
| `CommandType` | Command type | Property | `CommandType` | Yes |  |  | Specifies how a command string is interpreted |
| `Sql` | SQL query | InArgument | `string` | Yes |  | Provide an SQL statement that corresponds to the selected command type | An SQL query to be executed. This property must be completed according to the selection from the Command type property |
| `Parameters` | Parameters | Property | `Dictionary<string, Argument>` |  |  |  | A dictionary of named parameters that are bound to the SQL command. The binding is done by specifying the '@parameterName' statement in the SQL command. At runtime the parameterName will be replaced with its value from the dictionary. |
| `TimeoutMS` | Timeout | Property | `int` |  |  |  | Specifies the amount of time (in milliseconds) to wait for the SQL command to run before an error is thrown. If not set, the connection-level timeout is used; if no connection-level timeout is configured, defaults to 30 seconds. Must be greater than or equal to 0. |
| `ContinueOnError` | Continue on error | Property | `bool` |  |  |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `DataTable` | Data table | OutArgument | `DataTable` | The output of the SQL command wrapped in a DataTable variable. Contains the first result set. |
| `DataSet` | Data set | OutArgument | `DataSet` | The output of the SQL command wrapped in a DataSet variable. Contains all result sets returned by the query. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:ExecuteQuery DisplayName="Run Query" ExistingDbConnection="[value]" CommandType="[value]" Sql="[value]" />
</Activity>
```
