param(
    [string]$ConnectionString = $env:SUPABASE_POOLER_CONNECTION_STRING,
    [string]$Migration = "",
    [string]$OutputSql = "scripts/ef-migrations-idempotent.sql"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Connection string is required. Pass -ConnectionString or set SUPABASE_POOLER_CONNECTION_STRING."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
Set-Location $repoRoot

Write-Host "Running EF migration against Supabase..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($Migration)) {
    dotnet ef database update --connection "$ConnectionString"
} else {
    dotnet ef database update $Migration --connection "$ConnectionString"
}
if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef database update failed with exit code $LASTEXITCODE"
}

Write-Host "Generating idempotent SQL migration script..." -ForegroundColor Cyan
$env:ConnectionStrings__DefaultConnection = $ConnectionString
try {
    dotnet ef migrations script --idempotent --output "$OutputSql"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef migrations script failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
}

Write-Host "Migration complete." -ForegroundColor Green
Write-Host "SQL script generated at: $OutputSql" -ForegroundColor Green
