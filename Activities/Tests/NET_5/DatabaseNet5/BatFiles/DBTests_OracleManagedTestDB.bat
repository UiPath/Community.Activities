cd C:/Users/vladn/AppData/Local/UiPath/app-21.5.0-alpha2062
SET filePath="C:/Users/vladn/Documents/UiPath/NET5_TESTS/DatabaseNet5/DatabaseManagedDriver.xaml"
SET ODBC_managed_connStr=/"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.140)(PORT=49154)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ORCLCDB)));User Id=system;Password=Oracdoc_db1;/"
SET ODBC_managed_provider=/"Oracle.ManagedDataAccess.Client/"

UIRobot.exe execute --file %filePath% --input "{'ODBC_managed_connStr':%ODBC_managed_connStr%,'ODBC_managed_provider':%ODBC_managed_provider%}"

PAUSE

