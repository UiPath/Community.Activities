param(
    [Parameter(Mandatory = $true)]
    [string] $authToken,

    [Parameter(Mandatory = $true)]
    [string] $buildId,

    [Parameter(Mandatory = $true)]
    [string] $artifactsDirectory,

    [string] $organizationName = "uipath",
    [string] $projectName = "Activities",
    [string] $activityBuildTagPattern = "ActivityPackage=(\w+)",

    [switch] $deploySymbols
)

$ErrorActionPreference = "Stop"

function Main {

    Set-ScriptConstants

    $build = Get-Build
    
    $buildTags = $build.tags

    if (!$buildTags -or $buildTags.Length -eq 0) {
        Write-Host "No build tags specifying Activity packages to deploy were found on build wit hid $buildId. All packages will be deployed"
        Exit 0
    }

    $packagesToDeploy = @()
    $packagesToDeploySpec = ""

    foreach ($tag in $buildTags) {

        if ($tag -match $activityBuildTagPattern) {

            $packageToDeploy = $tag -replace $activityBuildTagPattern,"`$1"

            Write-Verbose "Found matching build tag '$tag', which references Activity '$packageToDeploy'"
            if ($packageToDeploy.Length -gt 1) {
                $packageToDeploy = $packageToDeploy[0].ToString().ToUpper() + $packageToDeploy.Substring(1)
            }
            
            # Make first letter uppercase in case branch name is lowercase
            Write-Verbose "Formatted activity name to: $packageToDeploy"
            
            $packageFilePattern = "UiPath.$($packageToDeploy).*.nupkg"
            $symbolsFilePattern = "UiPath.$($packageToDeploy).*.symbols.nupkg"

            $matchingPackage = Get-ChildItem $artifactsDirectory | `
                Where-Object { $_.Name -like $packageFilePattern } | `
                Select-Object -First 1

            if ($matchingPackage) {

                Write-Verbose "Found package matching '$packageFilePattern' at path '$matchingPackage'"

                $packagesToDeploy += $packageToDeploy
                $packagesToDeploySpec += (Join-Path $artifactsDirectory $packageFilePattern) + ";"

                if (!$deploySymbols) {
                    $packagesToDeploySpec += "!" + (Join-Path $artifactsDirectory $symbolsFilePattern) + ";"
                }

            } else {

                Write-Verbose "No package matching '$packageFilePattern' was found in the artifacts directory '$artifactsDirectory'"
            }
        }
    }

    Write-Host "##vso[task.setvariable variable=PackagesToDeploy;]$packagesToDeploySpec"

    Write-Output (@{
        PackagesToDeploySpec = $packagesToDeploySpec;
        PackagesToDeploy = $packagesToDeploy;
    })
}

function Set-ScriptConstants {

    $authorizationHeaderBytes = [System.Text.ASCIIEncoding]::ASCII.GetBytes(("{0}:{1}" -f "",$authToken))
    $authorizationHeaderBase64 = [System.Convert]::ToBase64String($authorizationHeaderBytes)

    $script:headers = @{
        Accept = "application/json";
        Authorization = "Basic $authorizationHeaderBase64";
    }
}

function Get-Build {

    $getBuildUri = "https://dev.azure.com/$organizationName/$projectName/_apis/build/builds/$($buildId)?api-version=4.1"

    $buildResponse = Invoke-RestMethod -Uri $getBuildUri -Method "Get" -Headers $script:headers

    return $buildResponse
}

Main
