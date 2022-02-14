cd C:\Users\vladn\AppData\Local\UiPath\app-21.5.0-alpha2062
SET filePath="C:/Users/vladn/Documents/UiPath/DatabaseTests/DatabaseTestsODBC.xaml"
SET ODBC_db_connStr=\""Dsn=EXTPROC_FOR_XE;uid=system;pwd=oracle"\"
SET ODBC_db_provider=\"System.Data.Odbc\"

UIRobot.exe execute --file %filePath% --input "{'ODBC_db_connStr':%ODBC_db_connStr%,'ODBC_db_provider':%ODBC_db_provider%}"

PAUSE

