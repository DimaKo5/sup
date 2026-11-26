param([int]$Port = 5006)
$ErrorActionPreference = 'Stop'
$apiDir = Join-Path $PSScriptRoot 'Api'
$desktopDir = Join-Path $PSScriptRoot 'Desktop'
$dataDir = Join-Path $env:LOCALAPPDATA 'CollegeSupport/portable'
New-Item -Path $dataDir -ItemType Directory -Force | Out-Null
$env:LocalDemo = 'true'
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:COLLEGESUPPORT_API_URL = "http://localhost:$Port/"
$env:ConnectionStrings__Default = 'Data Source=' + (Join-Path $dataDir 'collegesupport.db')
$api = Start-Process -FilePath (Join-Path $apiDir 'CollegeSupport.Api.exe') -WorkingDirectory $apiDir -ArgumentList @('--urls', $env:ASPNETCORE_URLS) -WindowStyle Hidden -PassThru
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 40 -and !$api.HasExited; $attempt++) {
        try {
            $ready = (Invoke-WebRequest -UseBasicParsing -Uri ($env:COLLEGESUPPORT_API_URL + 'health') -TimeoutSec 1).StatusCode -eq 200
        } catch { Start-Sleep -Milliseconds 250 }
        if ($ready) { break }
    }
    if (!$ready -or $api.HasExited) { throw 'API не запустился. Проверьте порт и журнал в Api/logs.' }
    Start-Process -FilePath (Join-Path $desktopDir 'CollegeSupport.Desktop.exe') -WorkingDirectory $desktopDir -Wait
} finally {
    if (!$api.HasExited) { Stop-Process -Id $api.Id }
}
