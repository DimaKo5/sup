param([string]$ApiExe, [int]$Port = 5086)
$ErrorActionPreference = 'Stop'
if (!$ApiExe) { $ApiExe = Join-Path $PSScriptRoot '../CollegeSupport.Api/bin/Release/net10.0/CollegeSupport.Api.exe' }
$exe = (Resolve-Path -LiteralPath $ApiExe).Path
$work = Join-Path ([IO.Path]::GetTempPath()) ('CollegeSupport-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$env:LocalDemo = 'true'
$env:ConnectionStrings__Default = 'Data Source=' + (Join-Path $work 'smoke.db')
$env:Security__LoginAttemptsPerMinute = '20'
$base = "http://localhost:$Port"
$api = Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -ArgumentList @('--urls', $base) -RedirectStandardOutput (Join-Path $work 'stdout.txt') -RedirectStandardError (Join-Path $work 'stderr.txt') -WindowStyle Hidden -PassThru

function Request($Method, $Path, $Code, $Token, $Body) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $request = @{Method=$Method; Uri="$base$Path"; Headers=$headers; SkipHttpErrorCheck=$true; TimeoutSec=8}
    if ($null -ne $Body) {
        $request.ContentType = 'application/json; charset=utf-8'
        $request.Body = ConvertTo-Json -InputObject $Body -Compress
    }
    $response = Invoke-WebRequest @request
    if ($response.StatusCode -ne $Code) { throw "$Method $Path expected $Code, received $($response.StatusCode)" }
    Write-Host "$($response.StatusCode) $Method $Path"
    if ($response.Content -and $response.Headers['Content-Type'] -match 'application/json') {
        return ,(ConvertFrom-Json -InputObject $response.Content -NoEnumerate)
    }
    return $response
}
function Check($Condition, $Description) {
    if (!$Condition) { throw $Description }
}
try {
    $ready = $false
    for ($i = 0; $i -lt 40 -and !$api.HasExited; ++$i) {
        try {
            $ready = (Invoke-WebRequest "$base/health" -TimeoutSec 1).StatusCode -eq 200
        } catch { Start-Sleep -Milliseconds 250 }
        if ($ready) { break }
    }
    Check ($ready -and !$api.HasExited) "API failed to start. Logs: $work"
    $null = Request GET '/health' 200
    $null = Request GET '/api/appeals' 401
    $null = Request POST '/api/auth/login' 401 '' @{login='dispatcher'; password='incorrect'}
    $dispatcher = Request POST '/api/auth/login' 200 '' @{login='dispatcher'; password='Dispatcher123'}
    $admin = Request POST '/api/auth/login' 200 '' @{login='admin'; password='Admin123'}
    $tech = Request POST '/api/auth/login' 200 '' @{login='technician1'; password='Tech123'}
    $other = Request POST '/api/auth/login' 200 '' @{login='technician2'; password='Tech123'}
    Check ($admin.user.role -eq 'Admin' -and $dispatcher.user.role -eq 'Dispatcher') 'Unexpected demo roles'
    $all = Request GET '/api/appeals' 200 $dispatcher.token
    Check ($all.Count -eq 7) 'Expected seven demo appeals'
    $mine = Request GET '/api/appeals' 200 $tech.token
    Check ($mine.Count -eq 4 -and !($mine | Where-Object technicianId -ne $tech.user.id)) 'Technician list scope'
    $null = Request GET "/api/appeals/$($mine[0].id)" 403 $other.token
    $null = Request GET '/api/users' 200 $admin.token
    $null = Request GET '/api/users' 403 $dispatcher.token
    $stats = Request GET '/api/appeals/statistics' 200 $dispatcher.token
    Check ($stats.total -eq 7 -and $stats.open -eq 3 -and $stats.completed -eq 3 -and $stats.revenue -eq 10000) 'Invalid statistics'
    $stats = Request GET '/api/appeals/statistics' 200 $tech.token
    Check ($stats.total -eq $mine.Count) 'Statistics scope'
    $appeal = Request POST '/api/appeals' 201 $dispatcher.token @{
        employeeId=$all[0].employeeId; cabinet='101'; equipment='Smoke-Projector'; problemDescription='Проектор не показывает изображение'
    }
    $path = "/api/appeals/$($appeal.id)"
    $null = Request PUT "$path/status" 400 $dispatcher.token @{status='InProgress'}
    $null = Request PUT "$path/technician" 200 $dispatcher.token @{technicianId=$tech.user.id}
    $null = Request PUT "$path/status" 403 $other.token @{status='InProgress'}
    $null = Request PUT "$path/status" 200 $tech.token @{status='InProgress'}
    $filtered = Request GET '/api/appeals?status=InProgress&search=Smoke-Projector' 200 $dispatcher.token
    Check ($filtered.Count -eq 1 -and $filtered[0].id -eq $appeal.id) 'Search and status filter'
    $null = Request PUT "$path/status" 200 $tech.token @{status='WaitingUser'}
    $null = Request PUT "$path/status" 400 $tech.token @{status='Resolved'; cost=0}
    $null = Request PUT "$path/status" 200 $tech.token @{status='Resolved'; cost=150}
    $closed = Request PUT "$path/status" 200 $dispatcher.token @{status='Closed'}
    Check ($closed.status -eq 'Closed' -and $closed.completedAt) 'Appeal lifecycle'
    $import = Request POST '/api/exchange/employees/import' 200 $admin.token @{
        content="ФИО;Телефон;Email;Кабинет`nУчебный сотрудник;+79009994411;;`nУчебный сотрудник;+79009994411;;301"
    }
    Check ($import.imported -eq 1 -and $import.skipped -eq 1 -and $import.errors[0] -match '2') 'CSV error row or valid employee missing'
    foreach ($format in 'csv','json','xml') {
        $result = Request GET "/api/exchange/appeals/export?format=$format" 200 $admin.token
        if ($format -eq 'json') {
            Check ($result.Count -eq 8 -and ($result | Where-Object equipment -eq 'Smoke-Projector')) 'JSON content'
        } elseif ($format -eq 'xml') {
            [xml]$xml = $result.Content
            Check ($xml.DocumentElement.InnerText -match 'Smoke-Projector') 'XML content'
        } else {
            $csv = [Text.Encoding]::UTF8.GetString($result.RawContentStream.ToArray())
            Check ($csv -match 'Smoke-Projector') 'CSV content'
        }
    }
    Write-Output "Live API smoke passed. Isolated data: $work"
} finally {
    if (!$api.HasExited) { Stop-Process -Id $api.Id }
}
