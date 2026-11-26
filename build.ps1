param([switch]$SkipNuGetAudit)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $restoreOptions = @()
    if ($SkipNuGetAudit) { $restoreOptions += '-p:NuGetAudit=false' }
    dotnet build CollegeSupport.slnx -c Release -m:1 @restoreOptions
    if ($LASTEXITCODE -ne 0) { throw 'Сборка завершилась ошибкой.' }
    dotnet test CollegeSupport.Tests/CollegeSupport.Tests.csproj -c Release --no-build --collect 'XPlat Code Coverage'
    if ($LASTEXITCODE -ne 0) { throw 'Тесты завершились ошибкой.' }
} finally {
    Pop-Location
}
