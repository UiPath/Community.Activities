# Bulk Update

`UiPath.Database.Activities.BulkUpdate`

Updates a compatible DataTable in an existing database table. The activity also updates all the columns that are not in the collection of column names used as a primary key. Returns the number of rows affected.

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ExistingDbConnection` | Existing connection | Property | `object` | Yes |  | Use the output of the Connect to Database activity | An already opened database connection obtained from the Connect to Database activity. |
| `DataTable` | Input data table | InArgument | `DataTable` | Yes |  | Provide the DataTable variable | The DataTable variable that will be used to update the database table. The DataTable columns' name and description must match the columns from the database table and be a subset of them. |
| `TableName` | Target table name | InArgument | `string` | Yes |  | Provide the target database table name | The target database table in which the data is to be updated. |
| `BulkUpdateFlag` | Bulk/batch update | Property | `bool` |  |  |  | Check this box to enable the creation of a temp table using Bulk insert and to update using join between tables. Otherwise, bulk updates are issued in batch. |
| `ColumnNames` | Columns used for matching rows | InArgument | `string[]` | Yes |  | Column names used for row matching | The collection of column names used for row matching. These column names will not be changed by the Bulk Update activity. |
| `ContinueOnError` | Continue on error | Property | `object` |  |  |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `AffectedRecords` | Affected rows count | OutArgument | `long` | Number of affected rows. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:BulkUpdate DisplayName="Bulk Update" ExistingDbConnection="[value]" DataTable="[value]" TableName="[value]" ColumnNames="[value]" />
</Activity>
```
