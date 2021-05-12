Param(
    [string]$projectPath = "E:\Work\Activities\Platform\UiPath.Platform\UiPath.Platform.csproj",
    [string]$outputPath = "E:\Work\Activities\Output\Activities\Packages\",
    [string]$suffix = "dev"
)

Write-Host $projectPath;
Write-Host $outputPath;
Write-Host $suffix;

if($suffix -eq "dev")
{
    $date = Get-Date;
    $day = [int]$date.DayOfYear;
    $seconds = [int]$date.TimeOfDay.TotalSeconds;
    $buildNo = ($day * 100) + $seconds;
    $suffix = "$suffix.$buildNo";
}

Write-Host $suffix;

if($suffix)
{
    & dotnet pack "$projectPath" --no-build --no-restore --output "$outputPath" --version-suffix "$suffix";
}
else 
{
    & dotnet pack "$projectPath" --no-build --no-restore --output "$outputPath";
}