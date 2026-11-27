# UiPath Database Activities

`UiPath.Database.Activities`

## Documentation

- [XAML Activities Reference](activities/) - Per-activity documentation for XAML workflows

## Activities

### Database

| Activity | Description |
|----------|-------------|
| [Bulk Insert](activities/BulkInsert.md) | Updates a database table via Bulk operations of the specific database driver. Falls back to Insert Data Table if the database driver does not support Bulk operations. |
| [Bulk Update](activities/BulkUpdate.md) | Updates a compatible DataTable in an existing database table. The activity also updates all the columns that are not in the collection of column names used as a primary key. Returns the number of rows affected. |
| [Connect to Database](activities/DatabaseConnect.md) | Connects to a database by using a standard connection string |
| [Disconnect from Database](activities/DatabaseDisconnect.md) | Closes a connection to a database |
| [Insert](activities/InsertDataTable.md) | Inserts a compatible DataTable in an existing database table. Returns the number of rows affected. |
| [Run Command](activities/ExecuteNonQuery.md) | Executes an SQL statement on a database. For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For all other types of statements, the return value is -1. |
| [Run Query](activities/ExecuteQuery.md) | Executes a query on a database and returns the query result as a Data Table |
