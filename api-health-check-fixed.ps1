$baseUrl = "http://localhost:7502/api"
$results = @()

function Test-Endpoint {
    param([string]$name, [string]$method, [string]$endpoint, [object]$body = $null)
    
    try {
        $url = "$baseUrl$endpoint"
        $params = @{Uri = $url; Method = $method; Headers = @{"Content-Type" = "application/json"}; TimeoutSec = 10}
        if ($body) { $params["Body"] = $body | ConvertTo-Json }
        $response = Invoke-WebRequest @params -ErrorAction SilentlyContinue -UseBasicParsing
        $status = if ($response.StatusCode -in 200, 201, 204) { "PASS" } else { "FAIL" }
        $results += "$status | $name | $($response.StatusCode)"
        Write-Host "$status - $name ($($response.StatusCode))"
    }
    catch {
        $msg = $_.Exception.Message.Substring(0, [Math]::Min(50, $_.Exception.Message.Length))
        $results += "FAIL | $name | $msg"
        Write-Host "FAIL - $name ($msg)"
    }
}

Write-Host "API Health Check Started`n" -ForegroundColor Cyan

# Auth
Write-Host "=== Auth Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "Auth-Register" "POST" "/auth/register" @{username="testuser1"; email="test1@example.com"; password="Test1234!"; firstName="Test"; lastName="User"}
Test-Endpoint "Auth-Login" "POST" "/auth/login" @{username="testuser1"; password="Test1234!"}

# LOV
Write-Host "`n=== LOV Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "LOV-GetAll" "GET" "/list-of-values"
Test-Endpoint "LOV-GetByCategory" "GET" "/list-of-values/by-category/ITEM_CATEGORY"

# Quotations
Write-Host "`n=== Quotations Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "Quotations-GetAll" "GET" "/quotations"
Test-Endpoint "Quotations-Create" "POST" "/quotations" @{customerName="Test Corp"; email="test@corp.com"; amount=5000; description="Test Quotation"}

# QuotationCalc
Write-Host "`n=== QuotationCalc Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "QuotCalc-GetAll" "GET" "/quotation-calc-records"

# InvoiceCalc
Write-Host "`n=== InvoiceCalc Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "InvCalc-GetAll" "GET" "/invoice-calc-records"

# Accounts (CORRECTED PATHS)
Write-Host "`n=== Accounts Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "Accounts-Balances" "GET" "/accounts/balances"
Test-Endpoint "Accounts-CurrentBalances" "GET" "/accounts/balances/current"
Test-Endpoint "Accounts-TotalIncome" "GET" "/accounts/income/total"
Test-Endpoint "Accounts-TotalExpenses" "GET" "/accounts/expenses/total"

# Employees
Write-Host "`n=== Employees Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "Employees-GetAll" "GET" "/employees"

# Configuration (CORRECTED PATHS)
Write-Host "`n=== Configuration Endpoints ===" -ForegroundColor Yellow
Test-Endpoint "Config-SystemSettings" "GET" "/configuration/system-settings"
Test-Endpoint "Config-ModelConstants" "GET" "/configuration/model-constants"

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
$passed = ($results | Select-String "^PASS").Count
$failed = ($results | Select-String "^FAIL").Count
Write-Host "Total Tests: $($results.Count)" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red

if ($failed -gt 0) {
    Write-Host "`nFailed Endpoints:" -ForegroundColor Red
    $results | Select-String "^FAIL" | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
}
