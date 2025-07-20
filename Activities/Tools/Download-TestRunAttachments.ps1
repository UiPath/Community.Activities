param(
    [Parameter(Mandatory = $true)]
    [string]$project,
    [Parameter(Mandatory = $true)]
    [string] $buildId,
    [Parameter(Mandatory = $true)]
    [string] $accessToken,
    [string] $organization = "UiPath",
    [Parameter(Mandatory = $true)]
    [string] $outputPath
)
function Main {

    New-item -ItemType Directory -Path $outputPath -Force

    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "", $accessToken)))

    $headers = @{Authorization = ("Basic {0}" -f $base64AuthInfo) }
    $uri = "https://dev.azure.com/$organization/$project/_apis/build/builds/$buildId"
    $uri = $uri + "?api-version=5.0"

    Write-Host "Get build details"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers

    $minLastUpdatedDate = $($response.queueTime)
    $maxLastUpdatedDate = Get-Date -Hour 23 -Minute 59 -Second 59 -Millisecond 0 -Format yyyy-MM-ddTHH:mm:ss.ff
    Write-Host "Get build test runs"
    $ProgressPreference = 'SilentlyContinue'
    $testRunsList = Invoke-RestMethod  "https://dev.azure.com/$organization/$project/_apis/test/runs?minLastUpdatedDate=$minLastUpdatedDate&maxLastUpdatedDate=$maxLastUpdatedDate&buildIds=$buildId&api-version=5.0" -Method Get  -Headers $headers
    if ($testRunsList -and $testRunsList.value) {
        foreach ($value in $testRunsList.value) {
            $testRunId = $($value.id)
            Write-Host "TestRun id: $testRunId"
            $uri = "https://dev.azure.com/$organization/$project/_apis/test/runs/$testRunId/attachments?api-version=5.0-preview.1"
            $testRunAttachments = Invoke-RestMethod -uri $uri   -Method Get  -Headers $headers
            if ($testRunAttachments -and $testRunAttachments.value) {
                foreach ($att in $testRunAttachments.value) {
                    $id = $($att.id)
                    $filename = "$testRunId-$($att.filename)"
                    Write-Host "Downloading filename: $id - $filename"
                    $uri = "https://dev.azure.com/$organization/$project/_apis/test/Runs/$testRunId/attachments/$id\?api-version=5.0-preview.1"

                    Invoke-WebRequest -Uri $uri -Method Get -Headers $headers -OutFile "$outputPath\$filename"
                }
            }
            else {
                Write-Host "No test run attachments"
            }
        }
    }
    else {
        Write-Host "No test runs found"
    }
}

Main
