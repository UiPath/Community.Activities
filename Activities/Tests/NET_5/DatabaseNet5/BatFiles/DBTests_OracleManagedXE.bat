cd C:\Users\vladn\AppData\Local\UiPath\app-21.5.0-alpha2062
SET filePath="C:/Users/vladn/Documents/UiPath/DatabaseTests/DatabaseTestsOracleManaged.xaml"
SET ODBC_managed_connStr=\"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.westeurope.azurecontainer.io)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=XE)));User Id=system;Password=oracle;\"
SET ODBC_managed_provider=\"Oracle.ManagedDataAccess.Client\"

UIRobot.exe execute --file %filePath% --input "{'ODBC_managed_connStr':%ODBC_managed_connStr%,'ODBC_managed_provider':%ODBC_managed_provider%}"

PAUSE

