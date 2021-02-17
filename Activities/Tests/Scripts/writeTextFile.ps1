$text = 'Hello World'

# Create file:

$text | Set-Content 'file_powershell.txt'
#or
$text | Out-File 'file_powershell.txt'
#or
$text > 'file_powershell.txt'

# Append to file:

$text | Add-Content 'file_powershell.txt'
#or
$text | Out-File 'file_powershell.txt' -Append
#or
$text >> 'file_powershell.txt'