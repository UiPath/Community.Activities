# Run Command

`UiPath.Database.Activities.ExecuteNonQuery`

Executes an SQL statement on a database. For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For all other types of statements, the return value is -1.

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ExistingDbConnection` | Existing connection | Property | `DatabaseConnection` | Yes |  | Use the output of the Connect to Database activity | An already opened database connection obtained from the Connect to Database activity. |
| `CommandType` | Command type | Property | `CommandType` | Yes |  |  | Specifies how a command string is interpreted |
| `Sql` | SQL command | InArgument | `string` | Yes |  | Provide an SQL statement that corresponds to the selected command type | An SQL command to be executed. This property must be completed according to the selection from the Command type property |
| `Parameters` | Parameters | Property | `Dictionary<string, Argument>` |  |  |  | A dictionary of named parameters that are bound to the SQL command. The binding is done by specifying the '@parameterName' statement in the SQL command. At runtime the parameterName will be replaced with its value from the dictionary. |
| `TimeoutMS` | Timeout | Property | `int` |  |  |  | Specifies the amount of time (in milliseconds) to wait for the SQL command to run before an error is thrown. If not set, the connection-level timeout is used; if no connection-level timeout is configured, defaults to 30 seconds. Must be greater than or equal to 0. |
| `ContinueOnError` | Continue on error | Property | `bool` |  |  |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `AffectedRecords` | Affected rows count | OutArgument | `int` | The result of the execution of the SQL command. For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For all other types of statements, the return value is -1. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:ExecuteNonQuery DisplayName="Run Command" ExistingDbConnection="[value]" CommandType="[value]" Sql="[value]" />
</Activity>
```
