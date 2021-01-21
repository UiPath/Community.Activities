param(
    [Parameter(Mandatory = $true)]
    [string] $githubAuthToken
)

Import-Module ([System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "\Utils-RemoteBranch.psm1")))

Set-Location -Path $env:BUILD_SOURCESDIRECTORY
Write-Host "Running in $env:BUILD_SOURCESDIRECTORY"

$sourceBranch = $env:SYSTEM_PULLREQUEST_SOURCEBRANCH
$targetBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH

$githubAuthorizationHeaderBase64 = [System.Convert]::ToBase64String([char[]]$githubAuthToken)
Write-Host "git -c http.extraheader=""AUTHORIZATION: basic AUTH_TOKEN_NOT_DISPLAYED_FOR_SECURITY_REASONS"" fetch origin $sourceBranch"
Invoke-Git "-c http.extraheader=""AUTHORIZATION: basic $githubAuthorizationHeaderBase64"" fetch origin $sourceBranch"

Write-Host "git -c http.extraheader=""AUTHORIZATION: basic AUTH_TOKEN_NOT_DISPLAYED_FOR_SECURITY_REASONS"" fetch origin $targetBranch"
Invoke-Git "-c http.extraheader=""AUTHORIZATION: basic $githubAuthorizationHeaderBase64"" fetch origin $targetBranch"

Write-Host "Re-creating merge commit without a ref, based on source branch and target branch commit ids"
Write-Host "git checkout $env:TargetBranchCommitId"
Invoke-Git "checkout $env:TargetBranchCommitId"

Invoke-Git "config user.email ""devtest-team@uipath.com"""
Invoke-Git "config user.name ""$env:BUILD_SOURCEVERSIONAUTHOR"""

Write-Host "git -c http.extraheader=""AUTHORIZATION: basic AUTH_TOKEN_NOT_DISPLAYED_FOR_SECURITY_REASONS"" merge $env:SourceBranchCommitId"
Invoke-Git "-c http.extraheader=""AUTHORIZATION: basic $githubAuthorizationHeaderBase64"" merge $env:SourceBranchCommitId"

$currentCommit = & git log -1
Write-Host "Last commit is $currentCommit"