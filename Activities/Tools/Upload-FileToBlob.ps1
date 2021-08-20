param(
    [Parameter(Mandatory)]
    [string] $filePath,

    [Parameter(Mandatory)]
    [string] $storageAccountKey,

    [Parameter(Mandatory)]
    [string] $azureSubscriptionId,

    [Parameter(Mandatory)]
    [string] $azureApplicationId,

    [Parameter(Mandatory)]
    [string] $azureTenantId,

    [Parameter(Mandatory)]
    [string] $azurePassword,

    [string] $storageAccountName = "uipathdevtest",
    [string] $containerName = "test-storage",
    [string] $blobPrefix,
    [string] $azCopyDownloadUri = "https://uipathdevtest.blob.core.windows.net/binaries/AzCopy.zip",
    [string] $azCopyZipName = "AzCopy"
)

$ErrorActionPreference = "Stop"

if (!(Get-Module "AzureRM")) {
    Import-Module "AzureRM"
}

$azureSecurePassword = $azurePassword | ConvertTo-SecureString -AsPlainText -Force
$azureCredential = New-Object System.Management.Automation.PSCredential($azureApplicationId, $azureSecurePassword)

Login-AzureRmAccount -Credential $azureCredential -TenantId $azureTenantId -ServicePrincipal -SubscriptionId $azureSubscriptionId

# delete any local journal files, so the "portable" version of AzCopy works
if (Test-Path "$($ENV:LOCALAPPDATA)\Microsoft\Azure\AzCopy") {
    Get-ChildItem "$($ENV:LOCALAPPDATA)\Microsoft\Azure\AzCopy\*.jnl" | Remove-Item -Force
}

# download "portable" AzCopy
$azCopyTempDirectory = Join-Path $ENV:TEMP "az$(Get-Date -f 'yyyyMMddhhmmssfff')"

New-Item -ItemType "Directory" -Path $azCopyTempDirectory

$azCopyZipFile = Join-Path $azCopyTempDirectory "AzCopy.zip"

$webClient = New-Object System.Net.WebClient
$webClient.DownloadFile($azCopyDownloadUri, $azCopyZipFile)

[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.Filesystem")

[System.IO.Compression.ZipFile]::ExtractToDirectory($azCopyZipFile, $azCopyTempDirectory)

$azCopyExe = Join-Path $azCopyTempDirectory "$azCopyZipName/AzCopy.exe"

$fileInfo = [System.IO.FileInfo]$filePath

$sourceDirectory = $fileInfo.Directory.FullName
$sourceFilePattern = $fileInfo.Name

$destinationUri = "https://$storageAccountName.blob.core.windows.net/$containerName/$blobPrefix"

& $azCopyExe /Source:"$sourceDirectory" /Dest:$destinationUri /DestKey:$storageAccountKey /Pattern:"$sourceFilePattern" /Y

Remove-Item -Path $azCopyTempDirectory -Force -Recurse
