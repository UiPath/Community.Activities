[CmdletBinding()]
param(
    [string] $dir = 'E:\Work\Activities',

    [AllowNull()]
    [AllowEmptyString()]
    [string] $affectedActivities = 'Platform,UIAutomation,System,GSuite,MicrosoftOffice365,MLServices'
)


function Main
{
    Write-Host "dir: $dir"
    Write-Host "affectedActivities: $affectedActivities"

    $solutions = @()
    if($affectedActivities)
    {
        $solutions = $affectedActivities -split "," | Where-Object { !([string]::IsNullOrWhiteSpace($_.ToString())) }
        $solutions = Get-ChildItem -Path $dir *.sln | Where-Object { Is-ActivitySolution $solutions $_  }
        $solutions | ForEach-Object { Write-Host "solution: $($_.Name)" }
    }

    if($solutions.Length -eq 0)
    {
        Write-Host "No solutions found"
        return;
    }
    Write-Host "solutions:" $solutions


    $files = Get-ChildItem -Path $dir *.csproj -Recurse -ErrorAction SilentlyContinue -ErrorVariable err
    Validate-Long-Paths $err
    Write-Host "files: $files"

    $packFiles = $files | Where-Object { Is-Package $solutions $_};
    $pattern = ''
    foreach($file in $packFiles) 
    {
        Write-Host "pack file:" $file
        $pattern += "**\$file;" 
    }
    if($pattern)
    {
        Write-Host "pack pattern:" $pattern;
        Write-Host "##vso[task.setvariable variable=PackagesToPack;]$pattern";
        Write-Host "##vso[task.setvariable variable=HasNuspecPackages;]true";
    }

    $skdFiles = $files | Where-Object { Is-SdkPackage $solutions $_};
    $pattern = ''
    foreach($file in $skdFiles) 
    {
        Write-Host "sdk file:" $file
        $pattern += "**\$file;" 
    }
    if($pattern)
    {
        Write-Host "sdk pattern:" $pattern;
        Write-Host "##vso[task.setvariable variable=SdkPackagesToPack;]$pattern";
        Write-Host "##vso[task.setvariable variable=HasSdkPackages;]true";
    }
}

function Is-ActivitySolution($actvs, $sln)
{
    foreach($actv in $actvs)
    {
        if($sln.Name.EndsWith(".$actv.sln"))
        {
            return $true;
        }
    }
    return $false;
}

function Is-Package($solutions, $projFile)
{
    $inSolution = $false;
    foreach($sln in $solutions)
    {
        if(Get-Content $sln.FullName | Select-String $projFile.Name)
        {
            $inSolution = $true;
            break;
        }
    }
    if(!$inSolution)
    {
        return $false;
    }
    Write-Host "found projFile:" $projFile.FullName;
    $nuspec = $projFile.FullName -replace '.csproj','.nuspec';
    return (Test-Path $nuspec)
}

function Is-SdkPackage($solutions, $projFile)
{
    $inSolution = $false;
    foreach($sln in $solutions)
    {
        if(Get-Content $sln.FullName | Select-String $projFile.Name)
        {
            $inSolution = $true;
            break;
        }
    }
    if(!$inSolution)
    {
        return $false;
    }
    Write-Host "found sdk projFile:" $projFile.FullName;
    $content = Get-Content $projFile.FullName;
    return ($content | Select-String "<PackageId>")
}

function Validate-Long-Paths($err)
{
    if (-Not $err) 
    {
        return
    }

    foreach ($errorRecord in $err)
    {
        # /node_modules/ should not contain any nuspec files. ignore the error in that case
        if ($errorRecord.TargetObject -match '\\node_modules\\')
        {
                Write-Warning "Possible path too long in path '$($errorRecord.TargetObject)'."
                Write-Warning $errorRecord.Exception
        }
        else
        {
            throw $errorRecord.Exception
        }
    }
}

Main