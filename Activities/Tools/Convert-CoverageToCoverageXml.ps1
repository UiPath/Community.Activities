param(
    [string] $inputPath,
    [Parameter(Mandatory = $true)]
    [string] $outputPath)

function Main {
    # You can test locally by setting VsTestToolsInstallerInstalledToolLocation to
    # your VS tools path, e.g. C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Team Tools.
    # We might need to differentiate depending on whether our binaries are x86 or x64 in the future.

    New-item -ItemType Directory -Path $outputPath -Force

    $exePaths = (Get-ChildItem -Path $env:VsTestToolsInstallerInstalledToolLocation -Recurse -Include CodeCoverage.exe)
    if(0 -eq $exePaths.Count){
        Write-Warning "Cannot find CodeCoverage.exe in $env:VsTestToolsInstallerInstalledToolLocation"
        $outputFile = [System.IO.Path]::Combine($outputPath,"noCoverageFile.xml");
        New-item -ItemType File -Path $outputFile -Force
        Set-Content -Path $outputFile -Value '<?xml version="1.0" encoding="UTF-8" ?><results></results>'
        return
    }

    $codeCoverageExePath = $exePaths[0].FullName;

    $coverageFiles = (Get-ChildItem -Path $inputPath -Recurse -Include "*.coverage")
    if ($coverageFiles -eq $null) {
        $outputFile = [System.IO.Path]::Combine($outputPath,"noCoverageFile.xml");
        New-item -ItemType File -Path $outputFile -Force
        Set-Content -Path $outputFile -Value '<?xml version="1.0" encoding="UTF-8" ?><results></results>'
        return
    }

    foreach ($file in $coverageFiles) {
        $coverageFilePath = $file.FullName;
        $outputFile = [System.IO.Path]::Combine($outputPath, $file.Name + "xml");

        New-item -ItemType File -Path $outputFile -Force
        Remove-Item -Path $outputFile

        $params = @("analyze" , "/output:$outputFile", """$coverageFilePath""")

        Write-Host "$codeCoverageExePath" $params

        & "$codeCoverageExePath" $params

        Write-Host "Saved $coverageFilePath as $outputFile"
    }
}

Main