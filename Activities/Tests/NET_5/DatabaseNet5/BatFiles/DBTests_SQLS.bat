cd C:\Users\vladn\AppData\Local\UiPath\app-21.5.0-alpha2062
SET filePath="C:/Users/vladn/Documents/UiPath/NET5_TESTS/DatabaseNet5/DatabaseSQLS.xaml"
SET SQLServer_Connection=\"Data Source=192.168.1.140;User ID=sa;Password=@mParola123\"
SET SQLServer_db_provider=\"System.Data.SqlClient\"


UIRobot.exe execute --file %filePath% --input "{'SQLServer_Connection':%SQLServer_Connection%,'SQLServer_db_provider':%SQLServer_db_provider%}"
PAUSE
