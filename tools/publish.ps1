param([switch]$SkipNuGetAudit)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$package = Join-Path $artifacts ('publish-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $package -Force | Out-Null
$publishOptions = @('-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-m:1')
if ($SkipNuGetAudit) { $publishOptions += '-p:NuGetAudit=false' }
foreach ($component in 'Api', 'Desktop') {
    $project = Join-Path $root "CollegeSupport.$component/CollegeSupport.$component.csproj"
    dotnet publish $project @publishOptions -o (Join-Path $package $component)
    if ($LASTEXITCODE -ne 0) { throw "Сбой публикации $component." }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start-CollegeSupport.ps1') -Destination $package
$zip = Join-Path $artifacts 'CollegeSupport-1.0.0-win-x64.zip'
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
Write-Output $zip
