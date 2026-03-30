# Run Query

`UiPath.Database.Activities.ExecuteQuery`

Executes a query on a database and returns the query result as a Data Table

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ExistingDbConnection` | Existing connection | Property | `object` | Yes |  | Use the output of the Connect to Database activity | An already opened database connection obtained from the Connect to Database activity. |
| `CommandType` | Command type | Property | `object` | Yes |  |  | Specifies how a command string is interpreted |
| `Sql` | SQL query | InArgument | `string` | Yes |  | Provide an SQL statement that corresponds to the selected command type | An SQL query to be executed. This property must be completed according to the selection from the Command type property |
| `Parameters` | Parameters | Property | `object` |  |  |  | A dictionary of named parameters that are bound to the SQL command. The binding is done by specifying the '@parameterName' statement in the SQL command. At runtime the parameterName will be replaced with its value from the dictionary. |
| `TimeoutMS` | Timeout | Property | `object` |  |  | Default is 30000 milliseconds | Specifies the amount of time (in millisecond) to wait for the sql command to run before an error is thrown. The default value is 30000 milliseconds (30 seconds) and must be greater than or equal to 0. |
| `ContinueOnError` | Continue on error | Property | `object` |  |  |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `DataTable` | Data table | OutArgument | `DataTable` | The output of the SQL command wrapped in a DataTable variable. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:ExecuteQuery DisplayName="Run Query" ExistingDbConnection="[value]" CommandType="[value]" Sql="[value]" />
</Activity>
```
