$ErrorActionPreference = 'Stop'
$base = 'http://localhost:7502/api'
$report = [ordered]@{ positive = @(); negative = @(); timestamp = (Get-Date).ToString('s') }

function Add-Positive($name, $ok, $details) {
  $script:report.positive += [pscustomobject]@{ test = $name; passed = $ok; details = $details }
}

function Add-Negative($name, $ok, $details) {
  $script:report.negative += [pscustomobject]@{ test = $name; passed = $ok; details = $details }
}

function Get-HttpErrorText($err) {
  try {
    if ($err.Exception.Response -and $err.Exception.Response.GetResponseStream) {
      $stream = $err.Exception.Response.GetResponseStream()
      if ($stream) {
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        if (-not [string]::IsNullOrWhiteSpace($body)) {
          return $body
        }
      }
    }
  }
  catch {
    # Fall through to generic message.
  }

  if ($err.ErrorDetails -and $err.ErrorDetails.Message) {
    return $err.ErrorDetails.Message
  }

  return $err.Exception.Message
}

try {
  $customer = Invoke-RestMethod -Uri "$base/customer-masters" -Method Post -ContentType 'application/json' -Body (@{
    name = 'Delta Test Customer'
    phone = '9876543210'
    address = 'Chennai'
    customerType = 'Retail'
    status = 'Active'
    openingBalance = 500
  } | ConvertTo-Json)
  Add-Positive 'Create customer master' $true "CustomerId=$($customer.id)"

  Invoke-RestMethod -Uri "$base/accounts/initial-balance" -Method Post -ContentType 'application/json' -Body (@{
    type = 'cash'
    amount = 10000
    description = 'Seed cash'
  } | ConvertTo-Json) | Out-Null

  Invoke-RestMethod -Uri "$base/accounts/initial-balance" -Method Post -ContentType 'application/json' -Body (@{
    type = 'bank'
    amount = 5000
    description = 'Seed bank'
  } | ConvertTo-Json) | Out-Null
  Add-Positive 'Set initial balances' $true 'cash=10000, bank=5000'

  $income = Invoke-RestMethod -Uri "$base/accounts/income-entries" -Method Post -ContentType 'application/json' -Body (@{
    description = 'Seed income'
    amount = 2500
    type = 'cash'
    incomeType = 'independent'
    date = (Get-Date).ToString('yyyy-MM-dd')
    category = 'Other'
  } | ConvertTo-Json)
  Add-Positive 'Create income entry' $true "IncomeId=$($income.id)"

  $expense = Invoke-RestMethod -Uri "$base/accounts/expense-entries" -Method Post -ContentType 'application/json' -Body (@{
    description = 'Seed expense'
    amount = 700
    type = 'cash'
    date = (Get-Date).ToString('yyyy-MM-dd')
    category = 'Other'
    status = 'active'
  } | ConvertTo-Json)
  Add-Positive 'Create expense entry' $true "ExpenseId=$($expense.id)"

  $transfer = Invoke-RestMethod -Uri "$base/accounts/cash-transfers" -Method Post -ContentType 'application/json' -Body (@{
    fromAccount = 'cash'
    toAccount = 'bank'
    amount = 300
    transferDate = (Get-Date).ToString('yyyy-MM-dd')
    description = 'Seed transfer'
  } | ConvertTo-Json)
  Add-Positive 'Create cash transfer' $true "TransferId=$($transfer.id)"

  $material = Invoke-RestMethod -Uri "$base/inventory/material-prices" -Method Post -ContentType 'application/json' -Body (@{
    material = 'Duplex'
    gsm = 180
    bf = 16
    price = 62
    unit = 'kg'
    effectiveDate = (Get-Date).ToString('yyyy-MM-dd')
    supplier = 'SeedSupplier'
    status = 'active'
  } | ConvertTo-Json)
  Add-Positive 'Create material price' $true "MaterialPriceId=$($material.id)"

  $waste = Invoke-RestMethod -Uri "$base/sales/waste" -Method Post -ContentType 'application/json' -Body (@{
    customerId = $customer.id
    customerName = $customer.name
    weightKg = 120
    unitPrice = 14
    description = 'Seed waste sale'
    saleDate = (Get-Date).ToString('yyyy-MM-dd')
  } | ConvertTo-Json)
  Add-Positive 'Create waste sale' $true "WasteSaleId=$($waste.id)"

  $employee = Invoke-RestMethod -Uri "$base/employees" -Method Post -ContentType 'application/json' -Body (@{
    employeeCode = 'EMP001'
    fullName = 'Seed Employee'
    phone = '9999999999'
    designation = 'Operator'
    joiningDate = (Get-Date).ToString('yyyy-MM-dd')
    monthlySalary = 18000
    status = 'active'
    department = 'Production'
  } | ConvertTo-Json)
  Add-Positive 'Create employee' $true "EmployeeId=$($employee.id)"

  $holiday = Invoke-RestMethod -Uri "$base/employees/holidays" -Method Post -ContentType 'application/json' -Body (@{
    year = (Get-Date).Year
    date = (Get-Date).ToString('yyyy-MM-dd')
    name = 'Seed Holiday'
    description = 'Test holiday'
  } | ConvertTo-Json)
  Add-Positive 'Create holiday' $true "HolidayId=$($holiday.id)"

  $salaryMaster = Invoke-RestMethod -Uri "$base/employees/salary-masters" -Method Post -ContentType 'application/json' -Body (@{
    employeeId = $employee.id
    salaryType = 'monthly'
    basicSalary = 15000
    hra = 2000
    allowance = 1000
    deduction = 500
    otMultiplier = 1.5
    effectiveFrom = (Get-Date).ToString('yyyy-MM-dd')
    description = 'Seed salary master'
  } | ConvertTo-Json)
  Add-Positive 'Create salary master' $true "SalaryMasterId=$($salaryMaster.id)"

  $advance = Invoke-RestMethod -Uri "$base/employees/salary-advances" -Method Post -ContentType 'application/json' -Body (@{
    employeeId = $employee.id
    amount = 1000
    requestDate = (Get-Date).ToString('yyyy-MM-dd')
    reason = 'Medical'
    status = 'approved'
  } | ConvertTo-Json)
  Add-Positive 'Create salary advance' $true "SalaryAdvanceId=$($advance.id)"

  $customersCount = (Invoke-RestMethod -Uri "$base/customer-masters").Count
  $incomeCount = (Invoke-RestMethod -Uri "$base/accounts/income-entries").Count
  $expenseCount = (Invoke-RestMethod -Uri "$base/accounts/expense-entries").Count
  $inventoryCount = (Invoke-RestMethod -Uri "$base/inventory/material-prices").Count
  $employeesCount = (Invoke-RestMethod -Uri "$base/employees").Count
  Add-Positive 'Read-back validation' ($customersCount -ge 1 -and $incomeCount -ge 1 -and $expenseCount -ge 1 -and $inventoryCount -ge 1 -and $employeesCount -ge 1) "customers=$customersCount income=$incomeCount expense=$expenseCount inventory=$inventoryCount employees=$employeesCount"
}
catch {
  Add-Positive 'Seed pipeline execution' $false $_.Exception.Message
}

# Negative cases
try {
  Invoke-RestMethod -Uri "$base/accounts/reset-all" -Method Post -TimeoutSec 10 | Out-Null
  Add-Negative 'Reset accounts without confirm should fail' $false 'Unexpected success'
}
catch {
  $msg = Get-HttpErrorText $_
  $ok = $msg -match 'confirm=YES|confirm data reset'
  Add-Negative 'Reset accounts without confirm should fail' $ok $msg
}

try {
  Invoke-RestMethod -Uri "$base/accounts/cash-transfers" -Method Post -ContentType 'application/json' -Body (@{
    fromAccount = 'cash'
    toAccount = 'cash'
    amount = 10
    transferDate = (Get-Date).ToString('yyyy-MM-dd')
    description = 'Invalid transfer'
  } | ConvertTo-Json) | Out-Null
  Add-Negative 'Cash transfer with same accounts should fail' $false 'Unexpected success'
}
catch {
  $msg = Get-HttpErrorText $_
  $ok = $msg -match 'opposite bank/cash'
  Add-Negative 'Cash transfer with same accounts should fail' $ok $msg
}

try {
  Invoke-RestMethod -Uri "$base/zoho-books/pull-delta" -Method Post -TimeoutSec 10 | Out-Null
  Add-Negative 'Zoho pull without config should fail' $false 'Unexpected success'
}
catch {
  $msg = Get-HttpErrorText $_
  $ok = $msg -match 'ZohoBooks configuration missing'
  Add-Negative 'Zoho pull without config should fail' $ok $msg
}

$report | ConvertTo-Json -Depth 5 | Set-Content -Path "f:\Company Project\QuotationAPI\QuotationAPI.V2\API_TESTING_REPORT.json" -Encoding UTF8
Write-Output ($report | ConvertTo-Json -Depth 5)
