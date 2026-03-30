# Insert

`UiPath.Database.Activities.InsertDataTable`

Inserts a compatible DataTable in an existing database table. Returns the number of rows affected.

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ExistingDbConnection` | Existing connection | Property | `object` | Yes |  | Use the output of the Connect to Database activity | An already opened database connection obtained from the Connect to Database activity. |
| `DataTable` | Input data table | InArgument | `DataTable` | Yes |  | Provide the DataTable variable | The DataTable variable that will be inserted into the Table. The DataTable columns' name and description must match the ones from the database table. |
| `TableName` | Target table name | InArgument | `string` | Yes |  | Provide the target database table name | The target database table in which the data is to be inserted |
| `ContinueOnError` | Continue on error | Property | `object` |  |  |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `AffectedRecords` | Affected rows count | OutArgument | `int` | Number of affected rows. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:InsertDataTable DisplayName="Insert" ExistingDbConnection="[value]" DataTable="[value]" TableName="[value]" />
</Activity>
```
