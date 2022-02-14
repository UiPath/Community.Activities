cd C:/Users/vladn/AppData/Local/UiPath/app-21.4.3
SET inputFile="HashFilesWithKey.xaml"
SET key_encryption="0591d56f0f600c7f"
SET filePath="C:/Users/vladn/Documents/UiPath/CryptographyNet5/HashFilesWithKey.xaml"

UIRobot.exe execute --file %filePath% --input "{'inputFile':'%inputFile%','key_encryption':'%key_encryption%'}"
PAUSE
