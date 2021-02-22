<#
.PARAMETER Action
    Valid options: instrument/cleanup
.PARAMETER OutputFolder
    Valid absolute path containing uninstrumented dlls for 'instrument' action or
    instrumented dlls for 'cleanup' action
#>
param (
    [Parameter(Mandatory=$true)]
    [string] $Action,

    [Parameter(Mandatory=$true)]
    [string] $RootFolder,

    [Parameter(Mandatory=$true)]
    [string] $OutputFolder
)

[string[]] $filterList = "UiPath.DocumentProcessing.Contracts.csproj",
                         "UiPath.CEFSharpBundle.csproj",
                         "UiPath.Platform.csproj"

function Get-ListOfFilesWithExtension {
    param(
        [Parameter(Mandatory=$true)]
        [string] $RootFolder,

        [Parameter(Mandatory=$true)]
        [string] $OutputFolder,

        [Parameter(Mandatory=$true)]
        [string] $Extension
    )
    $Dir = Get-ChildItem $RootFolder -recurse
    
    $List = $Dir | Where-Object {$_.extension -eq $Extension} `
                 | Where-Object {$_.FullName -notmatch ".*Tests.*"} `
                 | Where-Object {$_.FullName -notmatch ".*Storage.*"} `
                 | Where-Object {$filterList -notcontains $_.Name}

    $files = New-Object System.Collections.Generic.List[Object]

    foreach ($file in $List) {
        $dll = $file.Name.replace("csproj", "dll")
        
        $paths = Get-ChildItem -Path $OutputFolder -recurse -filter $dll -File
        if ($paths.count -gt 0) {
            $files.Add($paths[0])
        }
    }

    return $files
}

<#
.SYNOPSIS
    For a given folder and its subfolders create instrumented dlls
    using original ones and pdb files with symbols and rename initial file
    adding .orig extension
.PARAMETER RootFolder
    The base directory which contains the dlls and symbols
.PARAMETER VsinstPath
    The absolute path to the vsinstr.exe file
#>
function Create-InstrumentedDll {
    param(
        [Parameter(Mandatory=$true)]
        [string] $RootFolder,

        [Parameter(Mandatory=$true)]
        [string] $OutputFolder,

        [Parameter(Mandatory=$true)]
        [string] $VsinstrPath
    )

    try {
        Push-Location $RootFolder

        if (Test-Path "$($OutputFolder)\InstrumentedDlls") {
            Remove-Item -Recurse -Force -Path "$($OutputFolder)\InstrumentedDlls\*"
        } else {
            New-Item -ItemType Directory -Force -Path "$($OutputFolder)\InstrumentedDlls"
        }

        $files = Get-ListOfFilesWithExtension -RootFolder $RootFolder -OutputFolder $OutputFolder -Extension ".csproj"
        foreach ($file in $files) {
            & $VsinstrPath /coverage $file.FullName

            Copy-Item $file.FullName "$($OutputFolder)\InstrumentedDlls\$($file.Name)"
            if (Test-Path "$($file.FullName).orig") {
                Copy-Item "$($file.FullName).orig" "$($OutputFolder)\InstrumentedDlls\$($file.Name).orig"
            }
            else {
                Write-Host "Skipped copying the original assembly for $($file.FullName), because the assembly could not be instrumented."
            }

            $pdbPath = $file.FullName.replace(".dll", ".pdb")
            $pdbFileName = $file.Name.replace(".dll", ".pdb")
            Copy-Item $pdbPath "$($OutputFolder)\InstrumentedDlls\$($pdbFileName)"

            $pdbPath = $file.FullName.replace(".dll", ".instr.pdb")

            if (Test-Path $pdbPath) {
                $pdbFileName = $file.Name.replace(".dll", ".instr.pdb")
                Copy-Item $pdbPath "$($OutputFolder)\InstrumentedDlls\$($pdbFileName)"    
            }
            else {
                Write-Host "Skipped copying the instrumented PDB for $($file.FullName), because the assembly could not be instrumented."
            }
        }
    }
    finally {
        Pop-Location
    }
}

<#
.SYNOPSIS
    For a given folder and its subfolder delete instrumented dlls
    and rename initial file removing .orig extension
.PARAMETER RootFolder
    The base directory which contains the instrumented dlls
#>
function CleanUp-InstrumentedDlls {
    param(
        [Parameter(Mandatory=$true)]
        [string] $RootFolder,

        [Parameter(Mandatory=$true)]
        [string] $OutputFolder
    )

    $files = Get-ListOfFilesWithExtension -RootFolder $RootFolder -OutputFolder $OutputFolder -Extension ".orig"
    if ($files.count -le 0) {
        throw "No files to clean-up found. Please check that the input arguments are correctly configured."
    }
    foreach ($file in $files) {
        Write-Host "Cleaning up $($file.FullName)"
        $dll = $file.FullName
        $dll = $dll.substring(0, $dll.length - 5)
        Remove-Item $dll -Force

        $pdb = $dll.replace("dll", "instr.pdb")
        Remove-Item $pdb -Force

        $filename = $dll.split("\")
        $filename = $filename[$filename.length - 1]
        Rename-Item -Path $file -NewName $filename
    }
}

function Extract-Vsinstr-ExePath {
    try {
        $vsInstrRootPath = "C:\Program Files (x86)\Microsoft Visual Studio\201[0-9]\*\Team Tools\Performance Tools\"

        if ((Test-Path $vsInstrRootPath) -eq $false) {
            $vsInstrRootpath = "C:\Program Files\Microsoft Visual Studio\201[0-9]\*\Team Tools\Performance Tools\"
        
            if ((Test-Path $vsInstrRootpath) -eq $False) {
                $vsInstrRootPath = "C:\Program Files (x86)\Microsoft Visual Studio\"

                if ((Test-Path $vsInstrRootpath) -eq $False) {
                    throw "Visual Studio does not exist"
                }
            }
        }

        $Time = Get-Date
        $Time = $Time.ToUniversalTime()
        Write-Host [$Time] "Start Searching vsinstr.exe file"

        $results = (Get-ChildItem -recurse -filter "vsinstr.exe" -Path $vsInstrRootpath 2>"stderr.txt" | Select-Object FullName)
        $path = ($results | Where-Object {$_ -notlike "*x64*" } | Sort-Object -Descending)[0].FullName

        $Time = Get-Date
        $Time = $Time.ToUniversalTime()
        Write-Host [$Time] $path

        return $path
    }
    catch {
        return $null
    }
}

function Main {
    if ($Action -eq 'instrument') {
        if (Test-Path $RootFolder) {
            $VsinstrPath = Extract-Vsinstr-ExePath
            if ([string]::IsNullOrEmpty($VsinstrPath)) {
                throw "'Vsinst.exe' file not found on this machine"
            }

            $VsinstrPath
            Create-InstrumentedDll -RootFolder $RootFolder -OutputFolder $OutputFolder -VsinstrPath $VsinstrPath
        } else {
            throw "Invalid value for 'RootFolder' parameter"
        }
    }
    else {
        if ($Action -eq 'cleanup') {
            if (Test-Path $RootFolder) {
                CleanUp-InstrumentedDlls -RootFolder $RootFolder -OutputFolder $OutputFolder
            } else {
                throw "Invalid value for 'RootFolder' parameter"
            }
        } else {
            throw "Invalid value for 'Action' parameter. Must use one of the following options: instrument/cleanup"
        }
    }
}

Main