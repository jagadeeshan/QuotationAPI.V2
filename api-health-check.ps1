# API Health Check Script
# This script tests all major API endpoints

$baseUrl = "http://localhost:7502/api"
$headers = @{"Content-Type" = "application/json"}
$failedTests = @()
$passedTests = @()

function Test-Endpoint {
    param(
        [string]$name,
        [string]$method,
        [string]$endpoint,
        [object]$body = $null,
        [string]$token = $null
    )
    
    try {
        $url = "$baseUrl$endpoint"
        $headers = @{"Content-Type" = "application/json"}
        
        if ($token) {
            $headers["Authorization"] = "Bearer $token"
        }
        
        $params = @{
            Uri = $url
            Method = $method
            Headers = $headers
            TimeoutSec = 10
        }
        
        if ($body) {
            $params["Body"] = $body | ConvertTo-Json
        }
        
        $response = Invoke-WebRequest @params -ErrorAction SilentlyContinue
        
        if ($response.StatusCode -in 200, 201, 204) {
            $passedTests += "$name: ✓ ($($response.StatusCode))"
            Write-Host "✓ $name (Status: $($response.StatusCode))" -ForegroundColor Green
        }
        else {
            $failedTests += "$name: ✗ (Status: $($response.StatusCode))"
            Write-Host "✗ $name (Status: $($response.StatusCode))" -ForegroundColor Red
        }
    }
    catch {
        $failedTests += "$name: ✗ (Error: $($_.Exception.Message))"
        Write-Host "✗ $name (Error: $($_.Exception.Message))" -ForegroundColor Red
    }
}

# Start testing
Write-Host "`n====== API Health Check ======`n" -ForegroundColor Cyan

# Auth endpoints
Write-Host "Testing Auth Endpoints..." -ForegroundColor Yellow
Test-Endpoint "Auth - Login" "POST" "/auth/login" @{username="test"; password="test"}
Test-Endpoint "Auth - Register" "POST" "/auth/register" @{username="testuser"; email="test@example.com"; password="Test123!"; firstName="Test"; lastName="User"}

# LOV endpoints
Write-Host "`nTesting LOV Endpoints..." -ForegroundColor Yellow
Test-Endpoint "LOV - GetAll" "GET" "/list-of-values"

# Quotations endpoints
Write-Host "`nTesting Quotations Endpoints..." -ForegroundColor Yellow
Test-Endpoint "Quotations - GetAll" "GET" "/quotations"
Test-Endpoint "Quotations - Create" "POST" "/quotations" @{customerName="Test Customer"; email="test@example.com"; amount=1000; description="Test Quotation"}

# QuotationCalcRecords endpoints
Write-Host "`nTesting QuotationCalcRecords Endpoints..." -ForegroundColor Yellow
Test-Endpoint "QuotationCalc - GetAll" "GET" "/quotation-calc-records"

# InvoiceCalcRecords endpoints
Write-Host "`nTesting InvoiceCalcRecords Endpoints..." -ForegroundColor Yellow
Test-Endpoint "InvoiceCalc - GetAll" "GET" "/invoice-calc-records"

# Accounts endpoints
Write-Host "`nTesting Accounts Endpoints..." -ForegroundColor Yellow
Test-Endpoint "Accounts - GetAll" "GET" "/accounts/bank-cash-balances"

# Employees endpoints
Write-Host "`nTesting Employees Endpoints..." -ForegroundColor Yellow
Test-Endpoint "Employees - GetAll" "GET" "/employees"

# Configuration endpoints
Write-Host "`nTesting Configuration Endpoints..." -ForegroundColor Yellow
Test-Endpoint "Configuration - GetAll" "GET" "/configuration"

# Summary
Write-Host "`n====== SUMMARY ======`n" -ForegroundColor Cyan
Write-Host "✓ Passed: $($passedTests.Count)" -ForegroundColor Green
Write-Host "✗ Failed: $($failedTests.Count)" -ForegroundColor Red

if ($failedTests.Count -gt 0) {
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $failedTests | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
}

Write-Host ""
