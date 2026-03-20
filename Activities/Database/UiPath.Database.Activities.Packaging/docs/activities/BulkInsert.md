# Bulk Insert

`UiPath.Database.Activities.BulkInsert`

Updates a database table via Bulk operations of the specific database driver. Falls back to Insert Data Table if the database driver does not support Bulk operations.

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

### Input/Output

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `AffectedRecords` | Affected rows count | OutArgument | `long` | Number of affected rows. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:BulkInsert DisplayName="Bulk Insert" ExistingDbConnection="[value]" DataTable="[value]" TableName="[value]" />
</Activity>
```
