$ErrorActionPreference = 'Stop'

$baseUrl = 'http://localhost:7502'
$now = Get-Date
$suffix = $now.ToString('yyyyMMddHHmmss')
$today = $now.ToString('yyyy-MM-dd')
$thisMonth = $now.ToString('yyyy-MM')

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $false)]$Body,
        [Parameter(Mandatory = $false)][hashtable]$Query
    )

    $uriBuilder = [System.UriBuilder]::new("$baseUrl$Path")
    if ($Query) {
        $pairs = @()
        foreach ($k in $Query.Keys) {
            $pairs += ('{0}={1}' -f [System.Web.HttpUtility]::UrlEncode([string]$k), [System.Web.HttpUtility]::UrlEncode([string]$Query[$k]))
        }
        $uriBuilder.Query = ($pairs -join '&')
    }

    $uri = $uriBuilder.Uri.AbsoluteUri

    if ($PSBoundParameters.ContainsKey('Body')) {
        $json = $Body | ConvertTo-Json -Depth 20
        return Invoke-RestMethod -Method $Method -Uri $uri -ContentType 'application/json' -Body $json
    }

    return Invoke-RestMethod -Method $Method -Uri $uri
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$moduleResults = [System.Collections.Generic.List[object]]::new()

function Add-ModuleResult {
    param(
        [string]$Module,
        [string]$CoverageType,
        [bool]$Passed,
        [string]$Scenario,
        [string]$BusinessImpact,
        [string[]]$AffectedPages,
        [hashtable]$Checks,
        [string]$Notes = ''
    )

    $moduleResults.Add([pscustomobject]@{
        module = $Module
        coverageType = $CoverageType
        passed = $Passed
        scenario = $Scenario
        businessImpact = $BusinessImpact
        affectedPages = $AffectedPages
        checks = $Checks
        notes = $Notes
    }) | Out-Null
}

Write-Host "[1/8] Seeding auth + admin..."

$register = Invoke-Api -Method Post -Path '/api/auth/register' -Body @{
    username = "flowuser_$suffix"
    email = "flowuser_$suffix@example.com"
    firstName = 'Flow'
    lastName = 'User'
    password = 'Flow@123'
}

$group = Invoke-Api -Method Post -Path '/api/admin-module/user-groups' -Body @{
    name = "Flow Group $suffix"
    description = 'E2E validation group'
    parentGroup = $null
    permissions = @('inventory.read', 'inventory.write')
    members = @()
}

$feature = Invoke-Api -Method Post -Path '/api/admin-module/features' -Body @{
    name = "Flow Feature $suffix"
    description = 'E2E flow feature'
    key = "flow_feature_$suffix"
    isActive = $true
    enabledRoles = @('Admin', 'User')
}

$null = Invoke-Api -Method Post -Path '/api/admin-module/permissions' -Body @{
    groupId = $group.id
    featureId = $feature.id
    permissions = @('view', 'edit')
    grantedBy = 'e2e-script'
}

$null = Invoke-Api -Method Post -Path '/api/admin-module/users' -Body @{
    username = "adminflow_$suffix"
    email = "adminflow_$suffix@example.com"
    firstName = 'Admin'
    lastName = 'Flow'
    role = 'Admin'
    status = 'active'
    groups = @($group.id)
}

$null = Invoke-Api -Method Post -Path '/api/admin-module/company-profiles' -Body @{
    companyName = "Flow Company $suffix"
    address = 'E2E Test Address'
    gstNo = "33FLOW$suffix"
    isActive = $true
    updatedBy = 'e2e-script'
}

$login = Invoke-Api -Method Post -Path '/api/auth/login' -Body @{
    username = "flowuser_$suffix"
    password = 'Flow@123'
}
$groups = Invoke-Api -Method Get -Path '/api/admin-module/user-groups'
$features = Invoke-Api -Method Get -Path '/api/admin-module/features'
$permissions = Invoke-Api -Method Get -Path '/api/admin-module/permissions'
$adminUsers = Invoke-Api -Method Get -Path '/api/admin-module/users'
$companyProfiles = Invoke-Api -Method Get -Path '/api/admin-module/company-profiles/active'
$systemHealth = Invoke-Api -Method Get -Path '/api/admin-module/system-health'

Add-ModuleResult -Module 'Authentication' -CoverageType 'Functional E2E' -Passed ($null -ne $login.accessToken -and $login.user.username -eq "flowuser_$suffix") -Scenario 'User registration and login' -BusinessImpact 'A newly created user can sign in immediately and receive application access.' -AffectedPages @('Login', 'Register', 'Protected modules') -Checks @{ 'Logged in username' = $login.user.username; 'Token issued' = [string](-not [string]::IsNullOrWhiteSpace($login.accessToken)) }

Add-ModuleResult -Module 'Admin' -CoverageType 'Functional E2E' -Passed ((@($groups).Count -ge 1) -and (@($features).Count -ge 1) -and (@($permissions).Count -ge 1) -and (@($adminUsers).Count -ge 1) -and (@($companyProfiles).Count -ge 1)) -Scenario 'Admin setup objects and active company profile' -BusinessImpact 'Groups, features, permissions, admin users, and company profile data are available to the admin screens and system health summary.' -AffectedPages @('Admin users', 'User groups', 'Features', 'Permissions', 'Company profile', 'System health') -Checks @{ 'Groups count' = @($groups).Count; 'Features count' = @($features).Count; 'Permissions count' = @($permissions).Count; 'Users count' = @($adminUsers).Count; 'Active company profiles' = @($companyProfiles).Count; 'System health response present' = [string]($null -ne $systemHealth) }

Write-Host "[2/8] Seeding accounts + customer..."

$null = Invoke-Api -Method Post -Path '/api/accounts/initial-balance' -Body @{
    type = 'cash'
    amount = 100000
    description = 'E2E initial cash'
}
$null = Invoke-Api -Method Post -Path '/api/accounts/initial-balance' -Body @{
    type = 'bank'
    amount = 200000
    description = 'E2E initial bank'
}

$customer = Invoke-Api -Method Post -Path '/api/customer-masters' -Body @{
    name = "Flow Customer $suffix"
    phone = '9876543210'
    email = "customer$suffix@example.com"
    address = 'Flow Street'
    customerType = 'Retail'
    status = 'Active'
    openingBalance = 0
}

$customerList = Invoke-Api -Method Get -Path '/api/customer-masters/active'

$purchase = Invoke-Api -Method Post -Path '/api/accounts/ledger/purchase-sales' -Body @{
    customerId = $customer.id
    customerName = $customer.name
    paymentType = 'bank'
    totalAmountPaid = 12500
    taxPercent = 18
    voucherDate = $today
}

$null = Invoke-Api -Method Post -Path '/api/accounts/cash-transfers' -Body @{
    fromAccount = 'bank'
    toAccount = 'cash'
    amount = 1500
    transferDate = $today
    remarks = 'E2E transfer'
    description = 'E2E transfer'
}

$outstanding = Invoke-Api -Method Post -Path '/api/accounts/customer-outstanding/additional' -Body @{
    customerId = $customer.id
    customerName = $customer.name
    amount = 5000
    date = $today
    description = 'E2E outstanding'
}

$null = Invoke-Api -Method Post -Path '/api/accounts/customer-outstanding/settlements' -Body @{
    customerId = $customer.id
    customerName = $customer.name
    amount = 1200
    type = 'cash'
    date = $today
    reference = "SET-$suffix"
    description = 'E2E settlement'
}

$null = Invoke-Api -Method Post -Path '/api/accounts/income-entries' -Body @{
    customerId = $customer.id
    customerName = $customer.name
    description = 'Independent income'
    amount = 900
    type = 'bank'
    incomeType = 'independent'
    date = $today
    category = 'Misc'
    reference = "INC-$suffix"
    status = 'active'
}

$null = Invoke-Api -Method Post -Path '/api/accounts/expense-entries' -Body @{
    description = 'Office expense'
    amount = 450
    type = 'cash'
    date = $today
    category = 'Office'
    reference = "EXP-$suffix"
    status = 'active'
}

$null = Invoke-Api -Method Post -Path '/api/accounts/ledger/tax-payments' -Body @{
    taxType = 'GST'
    mode = 'bank'
    amount = 500
    paymentDate = $today
    period = $thisMonth
}

$accountsSummary = Invoke-Api -Method Get -Path '/api/accounts/summary'
$outstandingSummary = Invoke-Api -Method Get -Path '/api/accounts/customer-outstanding/summary'

Add-ModuleResult -Module 'Customer' -CoverageType 'Functional E2E' -Passed (@($customerList | Where-Object { $_.id -eq $customer.id }).Count -eq 1) -Scenario 'Customer creation and availability in active list' -BusinessImpact 'A newly added customer appears in the customer master and becomes selectable in sales, quotation, invoice, and accounts flows.' -AffectedPages @('Customer list', 'Quotation form', 'Invoice form', 'Sales forms', 'Accounts customer outstanding') -Checks @{ 'Customer ID' = $customer.id; 'Active list contains customer' = [string](@($customerList | Where-Object { $_.id -eq $customer.id }).Count -eq 1) }

Add-ModuleResult -Module 'Accounts' -CoverageType 'Functional E2E' -Passed ($accountsSummary.cashInHand -ge 0 -and $accountsSummary.bankBalance -ge 0 -and @($outstandingSummary).Count -ge 1) -Scenario 'Core accounts transactions update balances and outstanding summary' -BusinessImpact 'Opening balances, purchase, transfer, settlement, income, expense, and tax payment all contribute to finance pages without producing negative balances.' -AffectedPages @('Accounts dashboard', 'Balance tracking', 'Customer outstanding', 'Expense dashboard', 'Tax payment') -Checks @{ 'Cash in hand' = $accountsSummary.cashInHand; 'Bank balance' = $accountsSummary.bankBalance; 'Outstanding customers' = @($outstandingSummary).Count } -Notes 'Detailed one-record-at-a-time accounts coverage is available in the dedicated data flow report.'

Write-Host "[3/8] Seeding inventory + price master..."

$priceBefore = Invoke-Api -Method Get -Path '/api/inventory/material-prices'

$stock = Invoke-Api -Method Post -Path '/api/inventory/reel-stocks' -Body @{
    reelNumber = "RL-$suffix"
    stockType = 'reel'
    material = 'Kraft'
    rollSize = '40-inch'
    gsm = 120
    bf = 18
    quantity = 10
    unitCost = 62
    weight = 100
    amount = 6200
    taxPercent = 18
    taxAmount = 1116
    finalAmount = 7316
    purchaseVoucherNumber = $purchase.voucherNumber
    purchaseInvoiceNumber = "PI-$suffix"
    receivedDate = $today
    remarks = 'E2E stock'
    currentStock = 100
    reorderLevel = 20
    unit = 'kg'
    status = 'active'
}

$priceAfterStock = Invoke-Api -Method Get -Path '/api/inventory/material-prices'
$matchedPriceAfterStock = @($priceAfterStock | Where-Object { $_.material -eq 'Kraft' -and [int]$_.gsm -eq 120 -and [decimal]$(if ($null -eq $_.bf) { 0 } else { $_.bf }) -eq 18 })

$inventorySyncGapDetected = ($matchedPriceAfterStock.Count -eq 0)

if ($inventorySyncGapDetected) {
    Write-Host 'Gap detected: Stock save did not upsert material price master via API path.'
}

if ($matchedPriceAfterStock.Count -eq 0) {
    $null = Invoke-Api -Method Post -Path '/api/inventory/material-prices' -Body @{
        material = 'Kraft'
        gsm = 120
        bf = 18
        price = 62
        unit = 'kg'
        effectiveDate = $today
        supplier = 'E2E Supplier'
        status = 'active'
    }
}

$lookup = Invoke-Api -Method Get -Path '/api/inventory/material-prices/lookup' -Query @{ material = 'Kraft'; gsm = 120; bf = 18 }
Assert-True -Condition ($null -ne $lookup -and [decimal]$lookup.price -gt 0) -Message 'Material price lookup failed after seed.'

$reelStocks = Invoke-Api -Method Get -Path '/api/inventory/reel-stocks'
$materialPrices = Invoke-Api -Method Get -Path '/api/inventory/material-prices'

Add-ModuleResult -Module 'Inventory' -CoverageType 'Functional E2E' -Passed (@($reelStocks).Count -ge 1 -and @($materialPrices).Count -ge 1 -and [decimal]$lookup.price -eq 62) -Scenario 'Reel stock creation and material price lookup' -BusinessImpact 'The stock item becomes visible in inventory and the same material setup can be used by pricing and calculation screens.' -AffectedPages @('Inventory stock list', 'Material price master', 'Quotation calculation', 'Invoice calculation') -Checks @{ 'Reel stock rows' = @($reelStocks).Count; 'Material price rows' = @($materialPrices).Count; 'Lookup price' = $lookup.price } -Notes $(if ($inventorySyncGapDetected) { 'API did not auto-create the price row from stock save, so the validation script inserted the missing price master entry to complete the downstream flow.' } else { '' })

Write-Host "[4/8] Seeding sales + expense workflow..."

$null = Invoke-Api -Method Post -Path '/api/sales/waste' -Body @{
    customerId = $customer.id
    customerName = $customer.name
    weightKg = 25
    unitPrice = 28
    description = 'E2E waste sale'
    saleDate = $today
}

$null = Invoke-Api -Method Post -Path '/api/sales/roll' -Body @{
    customerId = $customer.id
    customerName = $customer.name
    weightKg = 120
    unitPrice = 35
    paperPricePerKg = 20
    rollSize = 'Small'
    description = 'E2E roll sale'
    saleDate = $today
}

$expenseRecord = Invoke-Api -Method Post -Path '/api/expense-records' -Body @{
    category = 'Transport'
    amount = 700
    expenseDate = $today
    paidBy = 'Flow User'
    paymentMethod = 'cash'
    remarks = 'E2E expense record'
    status = 'approved'
}

$null = Invoke-Api -Method Post -Path '/api/expense-records/sync-accounting'

$wasteSales = Invoke-Api -Method Get -Path '/api/sales/waste'
$rollSales = Invoke-Api -Method Get -Path '/api/sales/roll'
$expenseRecords = Invoke-Api -Method Get -Path '/api/expense-records'
$expenseRecordTotal = Invoke-Api -Method Get -Path '/api/expense-records/total' -Query @{ startDate = $today; endDate = $today }
$updatedOutstandingSummary = Invoke-Api -Method Get -Path '/api/accounts/customer-outstanding/summary'

Add-ModuleResult -Module 'Sales' -CoverageType 'Functional E2E' -Passed (@($wasteSales).Count -ge 1 -and @($rollSales).Count -ge 1 -and @($updatedOutstandingSummary).Count -ge 1) -Scenario 'Waste sale and roll sale create customer-linked commercial records' -BusinessImpact 'Sales rows are created and customer outstanding is updated so collection tracking can happen later in accounts.' -AffectedPages @('Waste sales', 'Roll sales', 'Customer outstanding', 'Accounts dashboard') -Checks @{ 'Waste sale count' = @($wasteSales).Count; 'Roll sale count' = @($rollSales).Count; 'Outstanding rows after sales' = @($updatedOutstandingSummary).Count }

Add-ModuleResult -Module 'Expense Workflow' -CoverageType 'Functional E2E' -Passed (@($expenseRecords).Count -ge 1 -and [decimal]$expenseRecordTotal -ge 700) -Scenario 'Expense record approval and accounting synchronization' -BusinessImpact 'An approved operational expense can be stored in the workflow module and then synchronized into accounting.' -AffectedPages @('Expense workflow list', 'Expense dashboard', 'Accounts expenses') -Checks @{ 'Expense workflow rows' = @($expenseRecords).Count; 'Expense workflow total for the day' = $expenseRecordTotal }

Write-Host "[5/8] Seeding employee module..."

$employee = Invoke-Api -Method Post -Path '/api/employees' -Body @{
    id = "emp-$suffix"
    employeeCode = "EMP$suffix"
    fullName = 'Flow Employee'
    phone = '9000000000'
    designation = 'Operator'
    joiningDate = $today
    monthlySalary = 18000
    status = 'active'
    department = 'Production'
}

$null = Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{
    id = "att-$suffix"
    employeeId = $employee.id
    date = $today
    status = 'present'
    attendanceHours = 9
    notes = 'E2E attendance'
}

$null = Invoke-Api -Method Post -Path '/api/employees/holidays' -Body @{
    id = "hol-$suffix"
    date = $today
    name = 'E2E Holiday'
    description = 'E2E'
}

$null = Invoke-Api -Method Post -Path '/api/employees/salary-masters' -Body @{
    id = "sm-$suffix"
    employeeId = $employee.id
    salaryType = 'monthly'
    basicSalary = 15000
    hra = 2000
    allowance = 1000
    deduction = 500
    otMultiplier = 1.5
    effectiveFrom = $today
}

$null = Invoke-Api -Method Post -Path '/api/employees/salary-advances' -Body @{
    id = "sa-$suffix"
    employeeId = $employee.id
    amount = 2000
    requestDate = $today
    reason = 'Emergency'
    status = 'approved'
}

$null = Invoke-Api -Method Post -Path '/api/employees/monthly-salary-calcs' -Body @{
    id = "msc-$suffix"
    employeeId = $employee.id
    employeeCode = $employee.employeeCode
    fullName = $employee.fullName
    designation = $employee.designation
    month = $thisMonth
    salaryType = 'monthly'
    basicSalary = 15000
    hra = 2000
    allowance = 1000
    bonusPay = 500
    performancePay = 300
    salaryMasterDeduction = 500
    totalEarnings = 18800
    presentDays = 24
    absentDays = 1
    leaveDays = 1
    totalOtHours = 4
    otEarnings = 450
    attendanceDeduction = 0
    otherDeductions = 100
    totalDeductions = 600
    netSalary = 18200
    calcStatus = 'approved'
}

$employees = Invoke-Api -Method Get -Path '/api/employees'
$attendance = Invoke-Api -Method Get -Path '/api/employees/attendance'
$holidays = Invoke-Api -Method Get -Path '/api/employees/holidays'
$salaryMasters = Invoke-Api -Method Get -Path '/api/employees/salary-masters'
$salaryAdvances = Invoke-Api -Method Get -Path '/api/employees/salary-advances'
$salaryCalcs = Invoke-Api -Method Get -Path '/api/employees/monthly-salary-calcs'

Add-ModuleResult -Module 'Employee' -CoverageType 'Functional E2E' -Passed ((@($employees).Count -ge 1) -and (@($attendance).Count -ge 1) -and (@($salaryMasters).Count -ge 1) -and (@($salaryAdvances).Count -ge 1) -and (@($salaryCalcs).Count -ge 1)) -Scenario 'Employee master, attendance, holiday, advance, and calculation records' -BusinessImpact 'The employee module has connected data across attendance, payroll setup, advance tracking, and saved salary calculations.' -AffectedPages @('Employee list', 'Attendance', 'Holiday management', 'Salary master', 'Salary advance', 'Calculated salaries') -Checks @{ 'Employees' = @($employees).Count; 'Attendance rows' = @($attendance).Count; 'Holiday rows' = @($holidays).Count; 'Salary masters' = @($salaryMasters).Count; 'Salary advances' = @($salaryAdvances).Count; 'Salary calculations' = @($salaryCalcs).Count } -Notes 'Detailed salary calculation flow coverage is available in the dedicated data flow report.'

Write-Host "[6/8] Seeding quotation/invoice calculation records..."

$quotation = Invoke-Api -Method Post -Path '/api/quotations' -Body @{
    customerName = $customer.name
    email = "customer$suffix@example.com"
    amount = 22500
    description = 'E2E quotation'
    validityDays = 30
    lineItems = @(
        @{ itemDescription = 'Box A'; quantity = 100; unitPrice = 125 },
        @{ itemDescription = 'Box B'; quantity = 50; unitPrice = 200 }
    )
}

$null = Invoke-Api -Method Post -Path "/api/quotations/$($quotation.id)/approve"

$qCalc = Invoke-Api -Method Post -Path '/api/quotation-calc-records' -Body @{
    companyName = 'Flow Co'
    description = 'E2E quotation calc'
    amount = 1000
    dataJson = '{"price":{"actualAmount":1500,"profit":250},"expense":{"includeRent":true,"contractLabour":20}}'
}

$iCalc = Invoke-Api -Method Post -Path '/api/invoice-calc-records' -Body @{
    companyName = 'Flow Co'
    description = 'E2E invoice calc'
    amount = 2000
    dataJson = '{"price":{"actualAmount":2500,"profit":300},"expense":{"includeRent":false,"contractLabour":10}}'
}

$null = Invoke-Api -Method Post -Path "/api/invoice-calc-records/$($iCalc.id)/duplicate"

$quotations = Invoke-Api -Method Get -Path '/api/quotations'
$quotationCalcRows = Invoke-Api -Method Get -Path '/api/quotation-calc-records'
$invoiceCalcRows = Invoke-Api -Method Get -Path '/api/invoice-calc-records'

Add-ModuleResult -Module 'Quotation' -CoverageType 'Functional E2E' -Passed (@($quotations).Count -ge 1 -and @($quotationCalcRows).Count -ge 1) -Scenario 'Quotation creation, approval, and quotation calculation save' -BusinessImpact 'The quotation master and calculation record are both saved, enabling list, edit, and commercial follow-up flows.' -AffectedPages @('Quotation list', 'Quotation form', 'Quotation approval flow') -Checks @{ 'Quotation rows' = @($quotations).Count; 'Quotation calculation rows' = @($quotationCalcRows).Count; 'Created quotation ID' = $quotation.id }

Add-ModuleResult -Module 'Invoice / Order' -CoverageType 'Functional E2E' -Passed (@($invoiceCalcRows).Count -ge 2) -Scenario 'Invoice calculation record creation and duplicate action' -BusinessImpact 'Invoice/order calculation data can be saved and duplicated, which supports reusing a previous order configuration.' -AffectedPages @('Order details', 'Invoice calculation list', 'Order preview') -Checks @{ 'Invoice calculation rows' = @($invoiceCalcRows).Count; 'Original invoice calc ID' = $iCalc.id }

Write-Host "[7/8] Seeding configuration history + snapshots..."

$null = Invoke-Api -Method Post -Path '/api/configuration-history/record-change' -Query @{
    settingKey = 'ebRateDefault'
    oldValue = '3200'
    newValue = '3300'
    description = 'E2E update'
    notes = 'seed validation'
    changedBy = 'e2e-script'
}

$null = Invoke-Api -Method Post -Path '/api/configuration-history/create-quotation-snapshot/1'
$null = Invoke-Api -Method Post -Path '/api/configuration-history/create-invoice-snapshot/1'

$history = Invoke-Api -Method Get -Path '/api/configuration-history/history'
$quotationSnapshot = Invoke-Api -Method Get -Path '/api/configuration-history/quotation-snapshot/1'
$invoiceSnapshot = Invoke-Api -Method Get -Path '/api/configuration-history/invoice-snapshot/1'

Add-ModuleResult -Module 'Configuration History' -CoverageType 'Functional E2E' -Passed (@($history).Count -ge 1 -and $null -ne $quotationSnapshot -and $null -ne $invoiceSnapshot) -Scenario 'Configuration change recording and quotation/invoice snapshot capture' -BusinessImpact 'Configuration changes are audited and frozen snapshots are available so past quotations and invoices can be explained using the correct settings.' -AffectedPages @('Configuration history', 'Quotation snapshot', 'Invoice snapshot') -Checks @{ 'History rows' = @($history).Count; 'Quotation snapshot available' = [string]($null -ne $quotationSnapshot); 'Invoice snapshot available' = [string]($null -ne $invoiceSnapshot) }

Write-Host "[8/8] Validation checks..."

$summary = Invoke-Api -Method Get -Path '/api/accounts/summary'
Assert-True -Condition ($summary.cashInHand -ge 0 -and $summary.bankBalance -ge 0) -Message 'Invalid accounts summary balances.'

$profit = Invoke-Api -Method Get -Path '/api/accounts/orders/profit'
Assert-True -Condition ($profit.orderCount -ge 1) -Message 'Orders profit endpoint did not detect invoice calc records.'

$result = [PSCustomObject]@{
    seededAt = (Get-Date).ToString('o')
    username = "flowuser_$suffix"
    customerId = $customer.id
    purchaseVoucher = $purchase.voucherNumber
    reelStockId = $stock.id
    quotationId = $quotation.id
    quotationCalcId = $qCalc.id
    invoiceCalcId = $iCalc.id
    inventorySyncGapDetected = $inventorySyncGapDetected
    moduleResults = $moduleResults
}

$result | ConvertTo-Json -Depth 10 | Set-Content -Path "f:/Company Project/QuotationAPI/QuotationAPI.V2/scripts/seed-validate-e2e-result.json" -Encoding UTF8

Write-Host 'E2E seed and validation completed.'
Write-Output ($result | ConvertTo-Json -Depth 10)
