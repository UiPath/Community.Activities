param(
    [Parameter(Mandatory = $true)]
    [string] $branchRef,

    [Parameter(Mandatory = $true)]
    [string] $commitSha,

    [Parameter(Mandatory = $true)]
    [string] $githubAuthToken,

    [string] $pullRequestNumber,

    [string] $affectedActivities = "All",

    [string] $githubOrganizationName = "UiPath",

    [string] $githubProjectName = "Activities",

    [string] $activityLabelPattern = "C:(.+)",
    [string] $activityTagFormat = "ActivityPackage={0}"
)

$ErrorActionPreference = "Stop"

function Main {

    Set-ScriptConstants

    Write-Verbose "Provided branch is: $($script:branchName)"
    Write-Verbose "Provided commit is: $commitSha"
    Write-Verbose "Provided PR is: $pullRequestNumber"

    $affectedActivitiesList = @()

    if ($affectedActivities) {
        Write-Verbose "Affected Activities have been explicitly specified. Using the provided value: $affectedActivities"
        if ($affectedActivities -contains "All") {
            Write-Verbose "All specified"
        }
        else {
            $affectedActivitiesList = ($affectedActivities -split ",") | ForEach-Object { $_.Trim() }
        }
    }
    else {
        $affectedActivitiesList = Get-AffectedActivities
    }
    if ($affectedActivitiesList.length -eq 0) {
        $defaultActivity = "BuildSolution_Activities"
        Write-Host "No affected activities found; Setting default [$defaultActivity] to true"
        Write-Host "##vso[task.setvariable variable=$defaultActivity;]true"
    }
    else {
        foreach ($affectedActivity in $affectedActivitiesList) {
            $varName = "BuildSolution_$affectedActivity"
            Write-Verbose $varName
            Write-Host "##vso[task.setvariable variable=$varName;]true"
            Write-Host "##vso[build.addbuildtag]$($activityTagFormat -f $affectedActivity)"
        }
    }

    Write-Host "##vso[task.setvariable variable=AffectedActivities;]$($affectedActivitiesList -join ',')"

    Write-Host "Enable analysis: $env:EnableAnalysis"
    if ($env:EnableAnalysis -eq $true) {

        Write-Host "Build reason: ${env:Build_Reason}"
        $buildingActivities =  ($affectedActivitiesList.length -eq 0) -or ("Activities" -in $affectedActivitiesList)
        Write-Host "Building activities: $buildingActivities"

        if (${env:Build_Reason} -eq 'PullRequest') {
            Write-Host "Pipeline is building PR request. Setting RunAnalysis to true"
            Write-Host "##vso[task.setvariable variable=RunAnalysis;]true"
        }
        elseif ($buildingActivities) {
            Write-Host "Pipeline is building Activities. Setting RunAnalysis to true"
            Write-Host "##vso[task.setvariable variable=RunAnalysis;]true"
        }
    }
}

function Set-ScriptConstants {

    $githubAuthorizationHeaderBase64 = [System.Convert]::ToBase64String([char[]]$githubAuthToken)

    $script:githubHeaders = @{
        Authorization = "Basic $githubAuthorizationHeaderBase64"
    }

    $script:branchName = $branchRef -replace "^refs/heads/", ""

    $script:commitMessagePatterns = @(
        "Merge branch '(.+)' into $($script:branchName)",
        "Merge remote-tracking branch 'origin/(.+)'"
        "Merge remote-tracking branch 'origin/(.+)' into $($script:branchName)",
        "Merge pull request \#\d+ from UiPath/(.+)",
        "Merge branch '(.+)'"
    )
}

function Is-ReleaseBranch([string] $branch) {
    return $branch.StartsWith("release/")
}

function Is-FinalBranch([string] $branch) {
    return ($branch.StartsWith("masters/") -or $branch.StartsWith("support/"))
}

function Is-DevelopBranch([string] $branch) {
    return ($branch -eq "develop")
}

function Get-AffectedActivities 
{
    $affectedActivities = @()

    # if the current branch is a release branch, the affected Activity lies in the branch name (release/activity/version)
    if ((Is-ReleaseBranch $script:branchName) -or (Is-FinalBranch $script:branchName)) 
    {
        $affectedActivity = Get-AffectedActivityFromBranchName $script:branchName

        Write-Verbose "The current branch is a release branch. Found affected Activity name in branch: $affectedActivity"

        $affectedActivities += $affectedActivity
    }
    else 
    {
        $affectedActivities = Get-AffectedActivitiesFromPRMerge
    }

    return $affectedActivities
}

function Get-AffectedActivitiesFromPRMerge {
    $affectedActivities = @()

    # if it's a PR, get the labels directly, else,
    # determine which PR has been merged in the current branch and get the labels from there
    if ($pullRequestNumber) {
        Write-Verbose "Determining affected Activities from PR labels"

        $affectedActivities = Get-AffectedActivitiesFromPRLabels $pullRequestNumber

    }
    else {
        Write-Verbose "Determining affected Activities from the provided commit's message ($commitSha)"

        $commitMessage = Get-CommitMessage $commitSha
        $mergePrMessagePattern = "pull request \#(\d+) from UiPath/.+"

        if ($commitMessage -match $mergePrMessagePattern) {
            $prNumber = $Matches[1]

            Write-Verbose "Commit $commitSha with message '$commitMessage' is a PR merge commit, which references PR #$prNumber"

            $affectedActivities = Get-AffectedActivitiesFromPRLabels $prNumber

        }
        else {
            Write-Verbose "Commit $commitSha with message '$commitMessage' is not a PR merge commit"
        }
    }

    return $affectedActivities
}

function Get-AffectedActivitiesFromReleaseMerge {
    $mergedBranchName = Get-MergedBranchName $commitSha

    $affectedActivity = (Get-AffectedActivityFromBranchName $mergedBranchName).Trim()
    Write-Verbose $affectedActivity
    return @($affectedActivity)
}

function Get-MergedBranchName([string] $sha) {
    $commitMessage = Get-CommitMessage $sha

    Write-Verbose "Parsing commit message '$commitMessage'"

    $mergedBranchName = $null

    foreach ($commitMessagePattern in $script:commitMessagePatterns) {
        if ($commitMessage -match $commitMessagePattern) {
            $mergedBranchName = $commitMessage -replace $commitMessagePattern, "`$1"

            if ($mergedBranchName) {
                Write-Verbose "Found merged branch name '$mergedBranchName', matched by pattern '$commitMessagePattern'"
                break
            }
        }
    }

    return $mergedBranchName
}

function Get-AffectedActivityFromBranchName([string] $branch) {
    return (($branch -replace "^release/(.+)/v.+", "`$1") -replace "^masters/(.+)", "`$1") -replace "^support/(.+)/v.+", "`$1"
}

function Get-AffectedActivitiesFromPRLabels([string] $prNumber) {
    $affectedActivities = @()
    $pullRequest = Get-PullRequest $prNumber

    if (!$pullRequest) {
        Write-Verbose "Pull request #($prNumber) was not found or an error has occurred during the GitHub web request"
    }

    if ($pullRequest -and $pullRequest.labels) {
        foreach ($label in $pullRequest.labels) {
            if ($label.name -match $activityLabelPattern) {
                $affectedActivity = $label.name -replace $activityLabelPattern, "`$1"

                Write-Verbose "Found matching PR label '$($label.name)', which references Activity '$affectedActivity'"

                $affectedActivities += $affectedActivity
            }
        }
    }

    if ($affectedActivities.Length -eq 0) {
        Write-Verbose "No labels matching $activityLabelPattern were found on PR #$prNumber"
    }

    return $affectedActivities
}

function Get-CommitMessage([string] $sha) {
    $message = git show -s --format=%B $sha
    if ($message.GetType().Name -ne "String") {
        # message is an array of objects, each representing a line in the commit message
        $message = $message -join [System.Environment]::NewLine
    }

    return $message
}

function Get-PullRequest([string] $prNumber) {
    $getPrUrl = "https://api.github.com/repos/$githubOrganizationName/$githubProjectName/pulls/$prNumber"

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    $pullRequest = $null

    try {
        $pullRequest = Invoke-RestMethod -Method Get -Uri $getPrUrl -Headers $script:githubHeaders
    }
    catch {
        Write-Error $_
    }

    return $pullRequest
}

Main
