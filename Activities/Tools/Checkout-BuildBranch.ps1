Import-Module ([System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "\Utils-RemoteBranch.psm1")))

Set-Location -Path $env:BUILD_SOURCESDIRECTORY
Write-Host "Running in $env:BUILD_SOURCESDIRECTORY"

$sourceBranch = $env:SYSTEM_PULLREQUEST_SOURCEBRANCH
$targetBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH

Write-Host "git fetch origin $sourceBranch"
Invoke-Git "fetch origin $sourceBranch"

Write-Host "git fetch origin $targetBranch"
Invoke-Git "fetch origin $targetBranch"

Write-Host "Re-creating merge commit without a ref, based on source branch and target branch commit ids"
Write-Host "git checkout $env:TargetBranchCommitId"
Invoke-Git "checkout $env:TargetBranchCommitId"

Invoke-Git "config user.email ""devtest-team@uipath.com"""
Invoke-Git "config user.name ""$env:BUILD_SOURCEVERSIONAUTHOR"""

Write-Host "git merge $env:SourceBranchCommitId"
Invoke-Git "merge $env:SourceBranchCommitId"

$currentCommit = & git log -1
Write-Host "Last commit is $currentCommit"