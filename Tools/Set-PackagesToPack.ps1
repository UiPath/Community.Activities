[CmdletBinding()]
param(
    [string] $dir = 'E:\Work\Activities',

    [AllowNull()]
    [AllowEmptyString()]
    [string] $affectedActivities = 'Platform,UIAutomation,System,GSuite,MicrosoftOffice365'
)


function Main
{
    Write-Verbose $affectedActivities
    $solutions = @()
    if($affectedActivities)
    {
        $solutions = $affectedActivities -split "," | Where-Object { !([string]::IsNullOrWhiteSpace($_.ToString())) }
        $solutions = Get-ChildItem -Path $dir *.sln | Where-Object { Is-ActivitySolution $solutions $_  }
        $solutions | ForEach-Object { Write-Verbose $_.Name }
    }

    $files = Get-ChildItem -Path $dir *.csproj -Recurse -ErrorAction SilentlyContinue -ErrorVariable err
    Validate-Long-Paths $err

    if($solutions.Length -eq 0)
    {
        return;
    }

    $packFiles = $files | Where-Object { Is-Package $solutions $_};
    $pattern = ''
    foreach($file in $packFiles) 
    {
        Write-Verbose $file
        $pattern += "**\$file;" 
    }
    if($pattern)
    {
        Write-Verbose $pattern;
        Write-Host \"##vso[task.setvariable variable=PackagesToPack;]$pattern";
        Write-Host \"##vso[task.setvariable variable=HasNuspecPackages;]true";
    }

    $skdFiles = $files | Where-Object { Is-SdkPackage $solutions $_};
    $pattern = ''
    foreach($file in $skdFiles) 
    {
        Write-Verbose $file
        $pattern += "**\$file;" 
    }
    if($pattern)
    {
        Write-Verbose $pattern;
        Write-Host \"##vso[task.setvariable variable=SdkPackagesToPack;]$pattern";
        Write-Host \"##vso[task.setvariable variable=HasSdkPackages;]true";
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
    $nuspec = $projFile.FullName -replace '.csproj','.nuspec';
    if(Test-Path $nuspec)
    {
        return $true;
    }
    return $false;
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
    $content = Get-Content $projFile.FullName;
    if(($content | Select-String "<PackageId>") -and ($content | Select-String "<VersionSuffix>"))
    {
        return $true;
    }
    return $false;
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