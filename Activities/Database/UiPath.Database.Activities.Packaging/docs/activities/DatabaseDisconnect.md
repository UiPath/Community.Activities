# Disconnect from Database

`UiPath.Database.Activities.DatabaseDisconnect`

Closes a connection to a database

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `DatabaseConnection` | Existing connection | InArgument | `DatabaseConnection` | Yes |  | Use the output of the Connect to Database activity | An already opened database connection obtained from the Connect to Database activity. |

### Input/Output

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:DatabaseDisconnect DisplayName="Disconnect from Database" DatabaseConnection="[value]" />
</Activity>
```
