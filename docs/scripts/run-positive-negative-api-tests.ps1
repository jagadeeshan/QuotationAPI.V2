param(
  [string]$BaseUrl = 'http://localhost:7502',
  [string]$OutputJson = 'scripts/positive-negative-api-results.json',
  [string]$OutputHtml = 'scripts/positive-negative-impact-report.html'
)

$ErrorActionPreference = 'Stop'

$results = [System.Collections.Generic.List[object]]::new()
$runTag = 'RUN-' + (Get-Date -Format 'yyyyMMddHHmmss')
$today = Get-Date -Format 'yyyy-MM-dd'
$month = Get-Date -Format 'yyyy-MM'

function Invoke-Api {
  param(
    [Parameter(Mandatory = $true)][string]$Method,
    [Parameter(Mandatory = $true)][string]$Path,
    [object]$Body
  )

  $uri = "$BaseUrl$Path"
  try {
    if ($PSBoundParameters.ContainsKey('Body')) {
      $json = $Body | ConvertTo-Json -Depth 20
      $resp = Invoke-RestMethod -Method $Method -Uri $uri -Body $json -ContentType 'application/json'
    } else {
      $resp = Invoke-RestMethod -Method $Method -Uri $uri
    }

    return [pscustomobject]@{ ok = $true; status = 200; body = $resp }
  }
  catch {
    $status = 0
    $message = $_.Exception.Message
    if ($_.Exception.Response) {
      try { $status = [int]$_.Exception.Response.StatusCode } catch {}
    }
    if ($_.ErrorDetails.Message) {
      $message = $_.ErrorDetails.Message
    }
    return [pscustomobject]@{ ok = $false; status = $status; body = $message }
  }
}

function Add-Case {
  param(
    [string]$Module,
    [string]$CaseType,
    [string]$Scenario,
    [string]$Endpoint,
    [string]$UiScreen,
    [string]$ImpactedModules,
    [string]$ImpactExplanation,
    [int[]]$ExpectedStatuses,
    [object]$Response,
    [object]$InsertedKeys
  )

  $passed = $false
  if ($Response.ok -and ($ExpectedStatuses -contains 200 -or $ExpectedStatuses -contains 201)) {
    $passed = $true
  }
  if (-not $Response.ok -and $ExpectedStatuses -contains $Response.status) {
    $passed = $true
  }

  $results.Add([pscustomobject]@{
    runTag = $runTag
    module = $Module
    caseType = $CaseType
    scenario = $Scenario
    endpoint = $Endpoint
    uiScreen = $UiScreen
    impactedModules = $ImpactedModules
    impactExplanation = $ImpactExplanation
    expectedStatuses = ($ExpectedStatuses -join ',')
    actualStatus = $Response.status
    passed = $passed
    insertedKeys = $InsertedKeys
    responsePreview = ($Response.body | ConvertTo-Json -Depth 8 -Compress)
  }) | Out-Null
}

Write-Host 'Running positive/negative API test insertions...'

# Positive: auth register
$authUsername = "user_$runTag"
$authEmail = "$authUsername@example.com"
$authRegisterResp = Invoke-Api -Method Post -Path '/api/auth/register' -Body @{
  username = $authUsername
  email = $authEmail
  firstName = 'Api'
  lastName = 'Tester'
  password = 'Secret@123'
}
Add-Case -Module 'Auth' -CaseType 'Positive' -Scenario 'Register user' -Endpoint 'POST /api/auth/register' -UiScreen 'Auth > Register' -ImpactedModules 'Auth, Role-based access' -ImpactExplanation 'Creates a user account and role mapping used for application access.' -ExpectedStatuses @(200,201) -Response $authRegisterResp -InsertedKeys @{ username = $authUsername }

# Negative: auth login invalid password
$authBadLoginResp = Invoke-Api -Method Post -Path '/api/auth/login' -Body @{
  username = $authUsername
  password = 'WrongPassword'
}
Add-Case -Module 'Auth' -CaseType 'Negative' -Scenario 'Login with invalid password' -Endpoint 'POST /api/auth/login' -UiScreen 'Auth > Login' -ImpactedModules 'Auth' -ImpactExplanation 'Rejects invalid credential login and prevents unauthorized access.' -ExpectedStatuses @(401) -Response $authBadLoginResp -InsertedKeys @{}

# Positive: admin user group
$adminGroupResp = Invoke-Api -Method Post -Path '/api/admin-module/user-groups' -Body @{
  name = "Group $runTag"
  description = 'API test admin group'
  permissions = @('quotation.read', 'accounts.read')
  members = @()
}
$adminGroupId = $adminGroupResp.body.id
Add-Case -Module 'Admin' -CaseType 'Positive' -Scenario 'Create admin user group' -Endpoint 'POST /api/admin-module/user-groups' -UiScreen 'Admin > User Groups' -ImpactedModules 'Admin permissions matrix' -ImpactExplanation 'Creates a group used for feature-permission assignments and audit trails.' -ExpectedStatuses @(200,201) -Response $adminGroupResp -InsertedKeys @{ groupId = $adminGroupId }

# Negative: admin get unknown user
$adminUnknownUserResp = Invoke-Api -Method Get -Path '/api/admin-module/users/user-unknown'
Add-Case -Module 'Admin' -CaseType 'Negative' -Scenario 'Fetch unknown admin user' -Endpoint 'GET /api/admin-module/users/{id}' -UiScreen 'Admin > User Management' -ImpactedModules 'Admin' -ImpactExplanation 'Unknown admin user id should return not found without mutating admin state.' -ExpectedStatuses @(404) -Response $adminUnknownUserResp -InsertedKeys @{}

# Positive: customer
$customerResp = Invoke-Api -Method Post -Path '/api/customer-masters' -Body @{
  name = "Customer $runTag"
  phone = '9876543210'
  email = "customer.$runTag@example.com"
  address = 'Test Address'
  customerType = 'Retail'
  status = 'Active'
  openingBalance = 0
}
$customerId = $customerResp.body.id
Add-Case -Module 'Customer' -CaseType 'Positive' -Scenario 'Create customer master' -Endpoint 'POST /api/customer-masters' -UiScreen 'Customer > Customer List' -ImpactedModules 'Quotation, Invoice, Sales, Accounts' -ImpactExplanation 'Created customer appears in master list and becomes selectable in dependent forms.' -ExpectedStatuses @(200,201) -Response $customerResp -InsertedKeys @{ customerId = $customerId }

# Negative: GST invalid
$gstResp = Invoke-Api -Method Get -Path '/api/customer-masters/gst/lookup?gstNumber=123'
Add-Case -Module 'Customer' -CaseType 'Negative' -Scenario 'GST lookup invalid number' -Endpoint 'GET /api/customer-masters/gst/lookup' -UiScreen 'Customer > GST Lookup' -ImpactedModules 'Customer' -ImpactExplanation 'Invalid GST should be rejected and no customer data should be modified.' -ExpectedStatuses @(400) -Response $gstResp -InsertedKeys @{}

# Initialize balances for downstream purchase/expense scenarios.
$null = Invoke-Api -Method Post -Path '/api/accounts/initial-balance' -Body @{
  type = 'bank'
  amount = 250000
  description = "Seed bank balance $runTag"
}

$null = Invoke-Api -Method Post -Path '/api/accounts/initial-balance' -Body @{
  type = 'cash'
  amount = 150000
  description = "Seed cash balance $runTag"
}

# Positive: purchase ledger
$purchaseResp = Invoke-Api -Method Post -Path '/api/accounts/ledger/purchase-sales' -Body @{
  customerId = $customerId
  customerName = "Customer $runTag"
  paymentType = 'bank'
  totalAmountPaid = 15000
  taxPercent = 18
  voucherDate = $today
}
$voucherNo = $purchaseResp.body.voucherNumber
Add-Case -Module 'Accounts' -CaseType 'Positive' -Scenario 'Create purchase voucher' -Endpoint 'POST /api/accounts/ledger/purchase-sales' -UiScreen 'Accounts > Purchase/Sales Ledger' -ImpactedModules 'Inventory, Tax Payment, Financial Year Performance' -ImpactExplanation 'Purchase voucher can be linked by inventory stock entries and contributes to tax calculations.' -ExpectedStatuses @(200,201) -Response $purchaseResp -InsertedKeys @{ voucherNumber = $voucherNo }

# Positive: reel stock linked to purchase
$reelResp = Invoke-Api -Method Post -Path '/api/inventory/reel-stocks' -Body @{
  stockType = 'reel'
  material = 'Kraft'
  rollSize = '40-inch'
  gsm = 120
  bf = 18
  quantity = 10
  unitCost = 65
  weight = 100
  amount = 6500
  purchaseVoucherNumber = $voucherNo
  purchaseInvoiceNumber = "PI-$runTag"
  receivedDate = $today
  remarks = 'API seed stock'
  taxPercent = 18
  taxAmount = 1170
  finalAmount = 7670
  currentStock = 100
  reorderLevel = 20
  unit = 'kg'
  status = 'active'
}
Add-Case -Module 'Inventory' -CaseType 'Positive' -Scenario 'Create reel stock with purchase linkage' -Endpoint 'POST /api/inventory/reel-stocks' -UiScreen 'Inventory > Add Reel Stock' -ImpactedModules 'Inventory Dashboard, Material Prices' -ImpactExplanation 'Stock row is added and auto-upserts material price reference data for pricing lookup.' -ExpectedStatuses @(200,201) -Response $reelResp -InsertedKeys @{ reelId = $reelResp.body.id; reelNumber = $reelResp.body.reelNumber }

# Negative: reel stock without purchase voucher
$badReelResp = Invoke-Api -Method Post -Path '/api/inventory/reel-stocks' -Body @{
  stockType = 'reel'
  material = 'Kraft'
  quantity = 1
  unitCost = 10
  weight = 10
}
Add-Case -Module 'Inventory' -CaseType 'Negative' -Scenario 'Create stock without purchase voucher' -Endpoint 'POST /api/inventory/reel-stocks' -UiScreen 'Inventory > Add Reel Stock' -ImpactedModules 'Inventory' -ImpactExplanation 'Request must fail; no invalid stock row should be created.' -ExpectedStatuses @(400) -Response $badReelResp -InsertedKeys @{}

# Positive: expense record approved -> impacts expense entry + balances
$expRecResp = Invoke-Api -Method Post -Path '/api/expense-records' -Body @{
  category = 'Transport'
  amount = 950
  expenseDate = $today
  paidBy = 'tester'
  paymentMethod = 'cash'
  remarks = 'Seed expense record'
  status = 'approved'
}
Add-Case -Module 'Expense' -CaseType 'Positive' -Scenario 'Create approved expense record' -Endpoint 'POST /api/expense-records' -UiScreen 'Expense > Expense Form/List' -ImpactedModules 'Accounts Dashboard, Expense Ledger' -ImpactExplanation 'Approved expense creates/updates linked expense-entry and reduces matching balance type.' -ExpectedStatuses @(200,201) -Response $expRecResp -InsertedKeys @{ expenseRecordId = $expRecResp.body.id }

# Negative: fetch unknown expense record
$badExpGet = Invoke-Api -Method Get -Path '/api/expense-records/EXP-UNKNOWN'
Add-Case -Module 'Expense' -CaseType 'Negative' -Scenario 'Get unknown expense record' -Endpoint 'GET /api/expense-records/{id}' -UiScreen 'Expense > Expense List' -ImpactedModules 'Expense' -ImpactExplanation 'Unknown record should return not found and not alter accounting tables.' -ExpectedStatuses @(404) -Response $badExpGet -InsertedKeys @{}

# Positive: waste sale
$wasteResp = Invoke-Api -Method Post -Path '/api/sales/waste' -Body @{
  customerId = $customerId
  customerName = "Customer $runTag"
  weightKg = 250
  unitPrice = 12
  description = 'Seed waste sale'
  saleDate = $today
}
Add-Case -Module 'Sales' -CaseType 'Positive' -Scenario 'Create waste sale' -Endpoint 'POST /api/sales/waste' -UiScreen 'Sales > Waste Sale' -ImpactedModules 'Accounts Customer Outstanding, Sales Summary' -ImpactExplanation 'Waste sale creates revenue record and auto-creates customer outstanding.' -ExpectedStatuses @(200,201) -Response $wasteResp -InsertedKeys @{ wasteSaleId = $wasteResp.body.id; outstandingId = $wasteResp.body.outstandingId }

# Negative: roll sale missing customer name
$badRollResp = Invoke-Api -Method Post -Path '/api/sales/roll' -Body @{
  customerId = $customerId
  weightKg = 100
  unitPrice = 20
  paperPricePerKg = 12
  saleDate = $today
}
Add-Case -Module 'Sales' -CaseType 'Negative' -Scenario 'Create roll sale without customer name' -Endpoint 'POST /api/sales/roll' -UiScreen 'Sales > Roll Sale' -ImpactedModules 'Sales' -ImpactExplanation 'Missing required customer name must fail validation.' -ExpectedStatuses @(400) -Response $badRollResp -InsertedKeys @{}

# Positive: employee
$empResp = Invoke-Api -Method Post -Path '/api/employees' -Body @{
  fullName = "Employee $runTag"
  phone = '9000000001'
  designation = 'Operator'
  joiningDate = $today
  monthlySalary = 25000
  status = 'active'
  department = 'Production'
}
$employeeId = $empResp.body.id
Add-Case -Module 'Employee' -CaseType 'Positive' -Scenario 'Create employee' -Endpoint 'POST /api/employees' -UiScreen 'Employee > Employee Master' -ImpactedModules 'Attendance, Salary, Payslip' -ImpactExplanation 'Employee becomes available for attendance tracking and salary calculations.' -ExpectedStatuses @(200,201) -Response $empResp -InsertedKeys @{ employeeId = $employeeId; employeeCode = $empResp.body.employeeCode }

# Negative: attendance before joining date
$yesterday = (Get-Date).AddDays(-1).ToString('yyyy-MM-dd')
$attendanceNegative = Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{
  employeeId = $employeeId
  date = '2000-01-01'
  status = 'present'
  attendanceHours = 8
  notes = 'Invalid historical attendance'
}
Add-Case -Module 'Employee' -CaseType 'Negative' -Scenario 'Attendance before joining date' -Endpoint 'POST /api/employees/attendance' -UiScreen 'Employee > Attendance' -ImpactedModules 'Attendance, Salary' -ImpactExplanation 'Invalid attendance must be rejected to prevent salary distortion.' -ExpectedStatuses @(400) -Response $attendanceNegative -InsertedKeys @{}

# Positive: quotation calc record
$qCalcResp = Invoke-Api -Method Post -Path '/api/quotation-calc-records' -Body @{
  companyName = "Company $runTag"
  description = 'Quotation calc seed'
  amount = 25000
  dataJson = '{"item":{"itemName":"Box A","customerName":"Customer"},"price":{"actualAmount":25000,"profit":3500}}'
}
Add-Case -Module 'Quotation' -CaseType 'Positive' -Scenario 'Create quotation calc record' -Endpoint 'POST /api/quotation-calc-records' -UiScreen 'Quotation > Quotation Form/List' -ImpactedModules 'Items, Analytics' -ImpactExplanation 'Quotation record contributes to item aggregation and quotation analytics.' -ExpectedStatuses @(200,201) -Response $qCalcResp -InsertedKeys @{ quotationCalcId = $qCalcResp.body.id }

# Negative: quotation with invalid email
$qBadResp = Invoke-Api -Method Post -Path '/api/quotations' -Body @{
  customerName = "Customer $runTag"
  email = 'not-an-email'
  amount = 1000
  description = 'Invalid quotation'
  validityDays = 30
  lineItems = @(@{ itemDescription = 'Item'; quantity = 1; unitPrice = 1000 })
}
Add-Case -Module 'Quotation' -CaseType 'Negative' -Scenario 'Create quotation with invalid email' -Endpoint 'POST /api/quotations' -UiScreen 'Quotation > Quotation Form' -ImpactedModules 'Quotation' -ImpactExplanation 'Validation should prevent invalid quotation persistence.' -ExpectedStatuses @(400) -Response $qBadResp -InsertedKeys @{}

# Positive: invoice calc record
$iCalcResp = Invoke-Api -Method Post -Path '/api/invoice-calc-records' -Body @{
  companyName = "Company $runTag"
  description = 'Invoice calc seed'
  amount = 27000
  dataJson = '{"item":{"itemName":"Order A","customerName":"Customer"},"price":{"actualAmount":27000,"profit":4200}}'
}
Add-Case -Module 'Invoice' -CaseType 'Positive' -Scenario 'Create invoice calc record' -Endpoint 'POST /api/invoice-calc-records' -UiScreen 'Invoice > Invoice Form/List' -ImpactedModules 'Items, Financial Year Performance' -ImpactExplanation 'Invoice record contributes to order totals, profit, and FY reporting.' -ExpectedStatuses @(200,201) -Response $iCalcResp -InsertedKeys @{ invoiceCalcId = $iCalcResp.body.id }

# Negative: unknown invoice calc id
$badInvoiceGet = Invoke-Api -Method Get -Path '/api/invoice-calc-records/999999999'
Add-Case -Module 'Invoice' -CaseType 'Negative' -Scenario 'Get unknown invoice calc record' -Endpoint 'GET /api/invoice-calc-records/{id}' -UiScreen 'Invoice > Invoice List' -ImpactedModules 'Invoice' -ImpactExplanation 'Unknown invoice ID should return not found without side effects.' -ExpectedStatuses @(404) -Response $badInvoiceGet -InsertedKeys @{}

# Positive: LOV item
$lovResp = Invoke-Api -Method Post -Path '/api/list-of-values' -Body @{
  parentname = 'expensecategory'
  parentvalue = $null
  name = "seed-lov-$runTag"
  value = 1
  description = 'seed lov'
  itemtype = 'value'
  displayorder = 1
  isactive = 'Y'
}
Add-Case -Module 'LOV' -CaseType 'Positive' -Scenario 'Create LOV item' -Endpoint 'POST /api/list-of-values' -UiScreen 'Admin > Expense Category Management, Quotation/Invoice/Expense dropdowns' -ImpactedModules 'Expense, Quotation, Invoice, Sales' -ImpactExplanation 'LOV insertion updates selectable dropdown values across dependent forms.' -ExpectedStatuses @(200,201) -Response $lovResp -InsertedKeys @{ lovId = $lovResp.body.id }

# Negative: LOV placeholder blocked
$lovBad = Invoke-Api -Method Post -Path '/api/list-of-values' -Body @{
  name = '__add_category__'
  itemtype = 'value'
  isactive = 'Y'
}
Add-Case -Module 'LOV' -CaseType 'Negative' -Scenario 'Create blocked placeholder LOV value' -Endpoint 'POST /api/list-of-values' -UiScreen 'Admin > Expense Category Management' -ImpactedModules 'LOV' -ImpactExplanation 'Protected placeholder key must be rejected.' -ExpectedStatuses @(400) -Response $lovBad -InsertedKeys @{}

# Save JSON
$resultsArray = @($results)
$resultsArray | ConvertTo-Json -Depth 12 | Set-Content -Path $OutputJson -Encoding UTF8

# Render HTML
$total = $resultsArray.Count
$passed = @($resultsArray | Where-Object { $_.passed }).Count
$failed = $total - $passed

$rows = foreach ($r in $resultsArray) {
  $statusClass = if ($r.passed) { 'pass' } else { 'fail' }
  $statusText = if ($r.passed) { 'PASS' } else { 'FAIL' }
  "<tr><td>$($r.module)</td><td>$($r.caseType)</td><td>$($r.scenario)</td><td><code>$($r.endpoint)</code></td><td>$($r.uiScreen)</td><td>$($r.impactedModules)</td><td>$($r.impactExplanation)</td><td>$($r.expectedStatuses)</td><td>$($r.actualStatus)</td><td class='$statusClass'>$statusText</td></tr>"
}

$html = @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>Positive/Negative API Impact Report</title>
  <style>
    body { font-family: Segoe UI, Arial, sans-serif; margin: 24px; color: #1d2630; background: #f7fafc; }
    h1 { margin-bottom: 4px; }
    .meta { color: #4b5563; margin-bottom: 20px; }
    .chips { display: flex; gap: 12px; margin-bottom: 20px; }
    .chip { background: #fff; border: 1px solid #d1d5db; border-radius: 10px; padding: 10px 12px; font-weight: 600; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #e5e7eb; }
    th, td { border: 1px solid #e5e7eb; padding: 10px; vertical-align: top; font-size: 13px; }
    th { background: #f3f4f6; text-align: left; }
    .pass { color: #166534; font-weight: 700; }
    .fail { color: #991b1b; font-weight: 700; }
    code { background: #f3f4f6; padding: 1px 5px; border-radius: 4px; }
  </style>
</head>
<body>
  <h1>Positive and Negative Test Data Impact Report</h1>
  <p class='meta'>RunTag: $runTag | Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
  <div class='chips'>
    <div class='chip'>Total Cases: $total</div>
    <div class='chip'>Passed: $passed</div>
    <div class='chip'>Failed: $failed</div>
  </div>
  <table>
    <thead>
      <tr>
        <th>Module</th>
        <th>Case</th>
        <th>Scenario</th>
        <th>API Endpoint</th>
        <th>UI Screen</th>
        <th>Modules Impacted</th>
        <th>Impact Explanation</th>
        <th>Expected Status</th>
        <th>Actual Status</th>
        <th>Result</th>
      </tr>
    </thead>
    <tbody>
      $($rows -join "`n")
    </tbody>
  </table>
</body>
</html>
"@

$html | Set-Content -Path $OutputHtml -Encoding UTF8
Write-Host "Saved JSON: $OutputJson"
Write-Host "Saved HTML: $OutputHtml"
