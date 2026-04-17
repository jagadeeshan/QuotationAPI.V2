param(
    [string]$SqlFile = "$PSScriptRoot\seed-expense-category-lov-rerunnable.sql",
    [string]$ConnectionString = $null
)

function Get-LocalConnectionString {
    $configFile = Join-Path $PSScriptRoot '..\appsettings.Development.json'
    if (Test-Path $configFile) {
        try {
            $content = Get-Content $configFile -Raw | ConvertFrom-Json
            return $content.ConnectionStrings.DefaultConnection
        }
        catch {
            throw "Unable to parse local appsettings.Development.json: $_"
        }
    }
    throw "Local appsettings.Development.json not found at $configFile"
}

if (-not $ConnectionString) {
    if ($env:ConnectionStrings__DefaultConnection) {
        $ConnectionString = $env:ConnectionStrings__DefaultConnection
    }
    elseif ($env:SUPABASE_POOLER_CONNECTION_STRING) {
        $ConnectionString = $env:SUPABASE_POOLER_CONNECTION_STRING
    }
    elseif ($env:SUPABASE_DB_URL) {
        $ConnectionString = $env:SUPABASE_DB_URL
    }
    elseif ($env:DATABASE_URL) {
        $ConnectionString = $env:DATABASE_URL
    }
    else {
        $ConnectionString = Get-LocalConnectionString
    }
}

if (-not $ConnectionString) {
    throw "No PostgreSQL connection string was provided. Set -ConnectionString, ConnectionStrings__DefaultConnection, SUPABASE_POOLER_CONNECTION_STRING, SUPABASE_DB_URL, or DATABASE_URL."
}

if (-not (Test-Path $SqlFile)) {
    throw "SQL file not found: $SqlFile"
}

$assemblyPath = Join-Path $PSScriptRoot '..\bin\Debug\net8.0\Npgsql.dll'
if (-not (Test-Path $assemblyPath)) {
    throw "Npgsql assembly not found at $assemblyPath. Build the project first or update the assembly path."
}

Add-Type -Path $assemblyPath

$scriptText = Get-Content $SqlFile -Raw

$connection = New-Object Npgsql.NpgsqlConnection($ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $scriptText
    $command.ExecuteNonQuery() | Out-Null
    Write-Host "Successfully executed SQL file against PostgreSQL database." -ForegroundColor Green
}
catch {
    Write-Host "Failed to execute SQL file: $_" -ForegroundColor Red
    throw
}
finally {
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
