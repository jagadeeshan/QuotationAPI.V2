$baseUrl = "http://localhost:7502/api"
$results = @()

function Test-Endpoint {
    param([string]$name, [string]$method, [string]$endpoint, [object]$body = $null)
    
    try {
        $url = "$baseUrl$endpoint"
        $params = @{Uri = $url; Method = $method; Headers = @{"Content-Type" = "application/json"}; TimeoutSec = 10}
        if ($body) { $params["Body"] = $body | ConvertTo-Json }
        $response = Invoke-WebRequest @params -ErrorAction SilentlyContinue
        $status = if ($response.StatusCode -in 200, 201, 204) { "PASS" } else { "FAIL" }
        $results += "$status | $name | $($response.StatusCode)"
        Write-Host "$status - $name ($($response.StatusCode))"
    }
    catch {
        $results += "FAIL | $name | Error: $($_.Exception.Message.Substring(0, 50))"
        Write-Host "FAIL - $name (Error)"
    }
}

Write-Host "API Health Check Started`n"

# Auth
Write-Host "=== Auth Endpoints ===" 
Test-Endpoint "Auth-Login" "POST" "/auth/login" @{username="test"; password="test"}

# LOV
Write-Host "`n=== LOV Endpoints ===" 
Test-Endpoint "LOV-GetAll" "GET" "/list-of-values"

# Quotations
Write-Host "`n=== Quotations Endpoints ===" 
Test-Endpoint "Quotations-GetAll" "GET" "/quotations"

# QuotationCalc
Write-Host "`n=== QuotationCalc Endpoints ===" 
Test-Endpoint "QuotCalc-GetAll" "GET" "/quotation-calc-records"

# InvoiceCalc
Write-Host "`n=== InvoiceCalc Endpoints ===" 
Test-Endpoint "InvCalc-GetAll" "GET" "/invoice-calc-records"

# Accounts
Write-Host "`n=== Accounts Endpoints ===" 
Test-Endpoint "Accounts-GetAll" "GET" "/accounts/bank-cash-balances"

# Employees
Write-Host "`n=== Employees Endpoints ===" 
Test-Endpoint "Employees-GetAll" "GET" "/employees"

# Configuration
Write-Host "`n=== Configuration Endpoints ===" 
Test-Endpoint "Config-GetAll" "GET" "/configuration"

Write-Host "`n=== SUMMARY ===" 
$passed = ($results | Select-String "PASS").Count
$failed = ($results | Select-String "FAIL").Count
Write-Host "Passed: $passed`nFailed: $failed"
