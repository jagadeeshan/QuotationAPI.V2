param(
  [Parameter(Mandatory = $true)]
  [string]$InputPath,

  [Parameter(Mandatory = $false)]
  [string]$OutputPath = "F:\Company Project\QuotationAPI.V2\scripts\seed-adminsettings-lovitems-rerunnable.sql"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $InputPath)) {
  throw "Input SQL file not found: $InputPath"
}

$raw = Get-Content $InputPath -Raw

# Make the script rerunnable for Postgres.
$raw = $raw -replace 'DELETE FROM "LovItems";\s*SELECT setval\(pg_get_serial_sequence\(''""LovItems""'', ''Id''\), 1, false\);\s*DELETE FROM "AdminSystemSettings";', 'TRUNCATE TABLE "LovItems" RESTART IDENTITY CASCADE;`nTRUNCATE TABLE "AdminSystemSettings" RESTART IDENTITY CASCADE;'

# Replace all hard-coded timestamp literals with runtime current timestamp.
$raw = $raw -replace "TIMESTAMP\s*'[^']+'", 'CURRENT_TIMESTAMP'

# Optional cleanup for SQL Server leftovers if present.
$raw = $raw -replace '^\s*SET\s+NOCOUNT\s+ON\s*;\s*', ''
$raw = $raw -replace '\bdbo\.', ''

Set-Content -Path $OutputPath -Value $raw -Encoding UTF8

Write-Host "Normalized SQL generated: $OutputPath"
