# Connect to Database

`UiPath.Database.Activities.DatabaseConnect`

Connects to a database by using a standard connection string

**Package:** `UiPath.Database.Activities`
**Category:** Database

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `ProviderName` | Provider name | InArgument | `string` | Yes |  | Please provide the database provider name | The name of the database provider used to access the database. Please see the documentation for more examples. |
| `ConnectionString` | Connection string | InArgument | `string` | Yes |  | Please provide the database connection string | The connection string used to establish a database connection. Please see the documentation for more examples. |
| `ConnectionSecureString` | Secure connection string | InArgument | `SecureString` | Yes |  | Please provide the database connection SecureString variable | The connection string used to establish a database connection provided as a SecureString. Please see the documentation for more examples. |

### Input/Output

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `DatabaseConnection` | Database connection | OutArgument | `DatabaseConnection` | The database connection used for the operations within this activity. |

## XAML Example

```xml
<Activity mc:Ignorable="sap sap2010" xmlns:db="clr-namespace:UiPath.Database.Activities;assembly=UiPath.Database.Activities">
  <db:DatabaseConnect DisplayName="Connect to Database" ProviderName="[value]" ConnectionString="[value]" ConnectionSecureString="[value]" />
</Activity>
```
