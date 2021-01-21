param(
    [string] $rootDirectory = "E:\Work\Activities",
    [string] $prereleaseTag = "alpha",
    [string] $commitSha,
    [string] $buildId = "42"
)

$ErrorActionPreference = "Stop"

function Main 
{
    if($prereleaseTag -eq "final")
    {
        $buildId = "";
    }
    $prereleaseTag = Get-PrereleaseTag $prereleaseTag $commitSha;

    Write-Host "Patching nuspecs with prerelease tag from folder $rootDirectory"
    # Patch all .nuspec files in the solution
    Get-ChildItem (Join-Path $rootDirectory "**\*.nuspec") -Recurse | ForEach-Object {
        $xmlDoc = New-Object System.Xml.XmlDocument
        Write-Host "$_.FullName";
        $xmlDoc.Load($_.FullName);
        if ($prereleaseTag -and ($prereleaseTag -ne "final")) 
        {
            Patch-PrereleaseTag $xmlDoc $_.FullName
        }

        Patch-ReleaseNotes $xmlDoc $_.FullName
    
        $xmlDoc.Save($_.FullName)
    }

    Patch-Sdk-Projects
    Patch-Sdk-BuildProps
}

function Patch-Sdk-BuildProps()
{
    Get-ChildItem (Join-Path $rootDirectory "**\*.build.props") -Recurse | ForEach-Object {
        $xmlDoc = New-Object System.Xml.XmlDocument
        Write-Host $_.FullName;
        $xmlDoc.Load($_.FullName);
        if ($prereleaseTag -and ($prereleaseTag -ne "final")) 
        {
            Patch-Sdk-BuildProp $xmlDoc $_.FullName
        }
    }
}

function Patch-Sdk-BuildProp([System.Xml.XmlDocument] $xmlDoc, [string] $projectPath) 
{
    $versionSuffix = Get-Suffix

    $versionNode = $xmlDoc.SelectSingleNode("/Project/PropertyGroup/VersionSuffix")
    if(!$versionNode)
    {
        return;
    }
    Write-Host "Patching $projectPath with prerelease tag $versionPostfix"
    $versionNode.InnerText = $versionSuffix;
    $xmlDoc.Save($projectPath);
}

function Patch-Sdk-Projects()
{
    Get-ChildItem (Join-Path $rootDirectory "**\*.csproj") -Recurse | ForEach-Object {
        $xmlDoc = New-Object System.Xml.XmlDocument
        Write-Host $_.FullName;
        $xmlDoc.Load($_.FullName);
        if ($prereleaseTag -and ($prereleaseTag -ne "final")) 
        {
            Patch-Sdk-Project $xmlDoc $_.FullName
        }
    }
}

function Patch-Sdk-Project([System.Xml.XmlDocument] $xmlDoc, [string] $projectPath) 
{
    $versionSuffix = Get-Suffix

    $versionNode = $xmlDoc.SelectSingleNode("/Project/PropertyGroup/VersionSuffix")
    if(!$versionNode)
    {
        return;
    }
    Write-Host "Patching $projectPath with prerelease tag $versionPostfix"
    $versionNode.InnerText = $versionSuffix;
    $xmlDoc.Save($projectPath);
}

function Get-PrereleaseTag([string] $prereleaseTag, [string] $commitSha)
{
    if(($prereleaseTag -ne "final") -or (-not $commitSha))
    {
        return $prereleaseTag;
    }
    #if the commit message is a merge from a tagged release branch use that tag
    #eg. a merge from release/UIAutomation/v19.8.0-ce should apply the 'ce' tag
    [string]$commitMessage = Get-CommitMessage $commitSha
    Write-Host "$commitMessage"
    $commitMessagePattern = "Merge branch '(.+)'.*";
    if($commitMessage -match $commitMessagePattern) 
    {
        $mergedBranchName = $commitMessage -replace $commitMessagePattern, "`$1";
        Write-Host "This is a merge from a release branch. Branch name '$mergedBranchName'";
        $parts = $mergedBranchName.Split('/');
        $lastPart = $parts[$parts.Length - 1];
        if($lastPart.StartsWith("v")) #if the last part is version then we take the tag from it
        {
            $idx = $lastPart.LastIndexOf('-');
            if($idx -gt 0)
            {
                $prereleaseTag = $lastPart.Substring($idx + 1).TrimEnd();
                Write-Host "This is a tagged release. Tag = '$prereleaseTag'";
                return $prereleaseTag;
            }
        }
    }
    return $prereleaseTag;
}

function Get-CommitMessage([string] $sha) 
{
    $message = git show -s --format=%B $sha
    if ($message.GetType().Name -ne "String") {
        # message is an array of objects, each representing a line in the commit message
        $message = $message -join [System.Environment]::NewLine
    }

    return $message
}

function Get-Suffix()
{
    $suffix = if ($buildId) 
    {
        "$prereleaseTag.$buildId"
    }
    else 
    {
        $prereleaseTag
    }
    return $suffix
}

function Patch-PrereleaseTag([System.Xml.XmlDocument] $xmlDoc, [string] $nuspecPath) 
{
    $versionPostfix = Get-Suffix

    $versionNode = $xmlDoc.SelectSingleNode("/package/metadata/version")
    if(!$versionNode)
    {
        return;
    }
    Write-Host "Patching $nuspecPath with prerelease tag $versionPostfix"
    $versionNode.InnerText = $versionNode.InnerText + "-$versionPostfix"
}

function Patch-ReleaseNotes([System.Xml.XmlDocument] $xmlDoc, [string] $nuspecPath) {

    $activityPackDirectory = ([System.IO.FileInfo]$_.FullName).Directory.FullName

    $releaseNotesFileName = "ReleaseNotes.txt"
    $releaseNotesFilePath = Join-Path $activityPackDirectory $releaseNotesFileName

    if (!(Test-Path $releaseNotesFilePath)) {
        # TODO Remove this
        New-Item -Path $releaseNotesFilePath -ItemType File
        Write-Host "No release notes file found in directory $activityPackDirectory"
        return
    } else {
        Write-Host "Patching $nuspecPath with release notes from file $releaseNotesFilePath"
    }

    $releaseNotes = [System.IO.File]::ReadAllText($releaseNotesFilePath).Trim()

    if ($releaseNotes.Length -eq 0) {
        Write-Host "Release notes file $releaseNotesFilePath is empty or contains only whitespaces"
        return
    }

    # the good ol' JavaScript not-not hack that turns anything into a boolean
    $releaseNotesNodeExists = !!$xmlDoc.SelectSingleNode("/package/metadata/releaseNotes")
    
    $releaseNotesNode = if ($releaseNotesNodeExists) {
        $xmlDoc.SelectSingleNode("/package/metadata/releaseNotes")
    } else {
        $xmlDoc.CreateElement("releaseNotes")
    }

    $releaseNotesNode.InnerText = $releaseNotes

    if (!$releaseNotesNodeExists) {

        $metadataNode = $xmlDoc.SelectSingleNode("/package/metadata")

        $metadataNode.AppendChild($releaseNotesNode) | Out-Null
    }
}

Main
