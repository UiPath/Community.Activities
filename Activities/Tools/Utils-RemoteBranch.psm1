<#
.Synopsis
    Invoke git, handling its quirky stderr that isn't error

.Outputs
    Git messages, and lastly the exit code

.Example
    Invoke-Git push

.Example
    Invoke-Git "add ."
#>
function Invoke-Git
{
param(
[Parameter(Mandatory)]
[string] $Command )

    try {

        $exit = 0
        $stdErrPath = [System.IO.Path]::GetTempFileName()
        $stdOutPath = [System.IO.Path]::GetTempFileName()

        Invoke-Expression "git $Command 2>$stdErrPath 1>$stdOutPath"
        $exit = $LASTEXITCODE
        if ( $exit -gt 0 )
        {
            Write-Error (Get-Content $stdErrPath -Raw).ToString()
            Get-Content $stdOutPath
        }
        else
        {
            $errContent = Get-Content $stdErrPath -Raw
            if ($null -ne $errContent)
            {
                ($errContent -split "At line")[0].Trim().Substring(6)
            }
            Get-Content $stdOutPath
        }
        
        if ($exit -ne 0) {
            Write-Error "Exit code: $exit"
        }
    }
    catch
    {
        Write-Host "Error: $_`n$($_.ScriptStackTrace)"
    }
    finally
    {
        if ( Test-Path $stdOutPath )
        {
            Remove-Item $stdOutPath
        }

        if ( Test-Path $stdErrPath )
        {
            Remove-Item $stdErrPath
        }
    }
}