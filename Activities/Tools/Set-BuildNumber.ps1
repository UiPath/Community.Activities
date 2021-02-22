[CmdletBinding()]
param(
    [AllowNull()]
    [AllowEmptyString()]
    [string] $affectedActivities,

    [string] $packagesOutputDirectory
)

$ErrorActionPreference = "Stop"

function Main {

    $buildNumber = ""

    if ($affectedActivities) {

        $affectedActivitiesList = $affectedActivities -split "," | Where-Object { !([string]::IsNullOrWhiteSpace($_.ToString())) }
        $affectedActivitiesVersions = @()

        foreach ($affectedActivity in $affectedActivitiesList) {

            Write-Verbose "Computing version for affected Activity '$affectedActivity'"

            $nugetPackages = Get-ChildItem $packagesOutputDirectory

            $affectedActivityPackagePath = $nugetPackages | `
                Where-Object { $_.Name.ToLower().StartsWith("uipath.$($affectedActivity.ToLower())") } | `
                Select-Object -ExpandProperty "FullName" -First 1

            if (!$affectedActivityPackagePath) {
                Write-Warning "No Activity package was found for affected Activity '$affectedActivity', in directory '$packagesOutputDirectory'"
            } else {

                $affectedActivityPackageName = [System.IO.Path]::GetFileNameWithoutExtension((([System.IO.FileInfo]$affectedActivityPackagePath).Name))
                if($affectedActivityPackageName.Contains("Activities")) {
                    $affectedActivityVersion = $affectedActivityPackageName -replace "^UiPath\.$affectedActivity\.Activities\.",""
                    $affectedActivityName = $affectedActivityPackageName -replace "^UiPath\.($affectedActivity)\.Activities\..+`$","`$1"
                }
                else {
                    $affectedActivityVersion = $affectedActivityPackageName -replace "^UiPath\.$affectedActivity\.",""
                    $affectedActivityName = $affectedActivityPackageName -replace "^UiPath\.($affectedActivity)\..+`$","`$1"                    
                }

                $affectedActivitiesVersions += "$affectedActivityName.$affectedActivityVersion"
            }
        }

        if ($affectedActivitiesVersions.Length -eq 0) {

            Write-Verbose "No affected Activities have matching packages in directory '$packagesOutputDirectory'. Generating an MSBuild-like version"

            $buildNumber = Generate-MsBuildVersion

        } else {

            $buildNumber = ($affectedActivitiesVersions -join ";")
        }

    } else {

        Write-Verbose "No affected Activities list provided. Generating an MSBuild-like version"

        $buildNumber = Generate-MsBuildVersion
    }

    # changed limit to 128 chars due to DVTS-1672; change back to 255 if GitHub fixes this
    $buildNumber = $buildNumber.SubString(0, [System.Math]::Min(128, $buildNumber.Length)) #vsts buildNumber cannot be more than 255 chars
    Write-Host "##vso[build.updatebuildnumber]$buildNumber"

    Write-Output $buildNumber
}

function Generate-MsBuildVersion {

    $now = Get-Date
    $y2k = Get-Date -Year 2000 -Month 1 -Day 1
    $build = [int]($now - $y2k).TotalDays
    $revision = [int]($now.TimeOfDay.TotalSeconds / 2)

    return "1.0.$($build).$($revision)"
}

Main
