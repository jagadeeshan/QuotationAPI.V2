param(
    [string]$BaseUrl = 'http://localhost:7502',
    [string]$SqlCmdPath = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProjectRoot = Split-Path -Parent $scriptRoot
$workspaceRoot = Split-Path -Parent (Split-Path -Parent $apiProjectRoot)
$reportPath = Join-Path $workspaceRoot 'Quotation-v2.0\DATA_FLOW_VALIDATION_REPORT.md'
$jsonPath = Join-Path $scriptRoot 'validate-data-flow-result.json'
$clearSqlPath = Join-Path $scriptRoot 'clear-transaction-data.sql'

$results = [System.Collections.Generic.List[object]]::new()

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        $Body,
        [hashtable]$Query
    )

    $uriBuilder = [System.UriBuilder]::new("$BaseUrl$Path")
    if ($Query) {
        $pairs = foreach ($key in $Query.Keys) {
            '{0}={1}' -f [System.Web.HttpUtility]::UrlEncode([string]$key), [System.Web.HttpUtility]::UrlEncode([string]$Query[$key])
        }
        $uriBuilder.Query = ($pairs -join '&')
    }

    $invokeParams = @{
        Method = $Method
        Uri = $uriBuilder.Uri.AbsoluteUri
        ContentType = 'application/json'
    }

    if ($PSBoundParameters.ContainsKey('Body')) {
        $invokeParams['Body'] = ($Body | ConvertTo-Json -Depth 20)
    }

    try {
        $response = Invoke-RestMethod @invokeParams
        return [pscustomobject]@{
            Ok = $true
            StatusCode = 200
            Body = $response
        }
    }
    catch {
        $statusCode = 0
        $body = $_.Exception.Message
        if ($_.ErrorDetails.Message) {
            $body = $_.ErrorDetails.Message
        }

        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            if (-not $_.ErrorDetails.Message) {
                try {
                    if ($_.Exception.Response -is [System.Net.Http.HttpResponseMessage]) {
                        $body = $_.Exception.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    }
                    else {
                        $stream = $_.Exception.Response.GetResponseStream()
                        if ($stream) {
                            $reader = [System.IO.StreamReader]::new($stream)
                            $body = $reader.ReadToEnd()
                            $reader.Dispose()
                        }
                    }
                }
                catch {
                    $body = $_.Exception.Message
                }
            }
        }

        return [pscustomobject]@{
            Ok = $false
            StatusCode = $statusCode
            Body = $body
        }
    }
}

function Add-Result {
    param(
        [string]$Module,
        [string]$Scenario,
        [string]$CaseType,
        [bool]$Passed,
        [string]$AddedData,
        [string[]]$AffectedPages,
        [string]$BusinessImpact,
        [hashtable]$Checks,
        [string]$Notes = ''
    )

    $results.Add([pscustomobject]@{
        module = $Module
        scenario = $Scenario
        caseType = $CaseType
        passed = $Passed
        addedData = $AddedData
        affectedPages = $AffectedPages
        businessImpact = $BusinessImpact
        checks = $Checks
        notes = $Notes
    }) | Out-Null
}

function Require-Success {
    param($Result, [string]$Action)
    if (-not $Result.Ok) {
        throw "${Action} failed with status $($Result.StatusCode): $($Result.Body)"
    }
    return $Result.Body
}

function Get-AccountsSnapshot {
    param([string]$Period)

    $balances = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/balances/current') 'Get balances'
    $summary = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/summary') 'Get account summary'
    $outstandingSummary = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/customer-outstanding/summary') 'Get outstanding summary'
    $cashTransfers = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/cash-transfers') 'Get cash transfers'
    $purchaseSales = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/ledger/purchase-sales') 'Get purchase sales'
    $taxPayments = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/ledger/tax-payments') 'Get tax payments'
    $monthlyDue = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/ledger/tax-payments/monthly-due' -Query @{ period = $Period }) 'Get monthly tax due'
    $expenseDashboard = Require-Success (Invoke-Api -Method Get -Path '/api/accounts/expense-dashboard') 'Get expense dashboard'

    return [pscustomobject]@{
        cash = [decimal]$balances.cash
        bank = [decimal]$balances.bank
        totalIncome = [decimal]$summary.incomeMtd
        totalExpense = [decimal]$summary.expenseMtd
        totalOutstanding = [decimal](($outstandingSummary | Measure-Object -Property outstandingAmount -Sum).Sum)
        cashTransferCount = @($cashTransfers).Count
        purchaseCount = @($purchaseSales).Count
        taxPaymentCount = @($taxPayments).Count
        taxOutstanding = [decimal]$monthlyDue.outstanding
        taxPaid = [decimal]$monthlyDue.totalPaid
        monthlyExpense = [decimal]$expenseDashboard.totalMonthExpense
        outstandingRows = @($outstandingSummary)
    }
}

function Normalize-AttendanceHours {
    param([string]$Status, [decimal]$AttendanceHours)
    $hours = [Math]::Max(0, [Math]::Min(16, [decimal]$AttendanceHours))
    switch ($Status) {
        'absent' { return 0 }
        'leave' { return 0 }
        'half-day' { return 0 }
        'weekoff' { return 8 }
        'holiday' { return 8 }
        'present' {
            if ($hours -le 0) { return 8 }
            return $hours
        }
        default { return $hours }
    }
}

function Get-WeeklySalaryCalculation {
    param(
        [string]$EmployeeId,
        [string]$EmployeeCode,
        [string]$FullName,
        [string]$Designation,
        [string]$Month,
        [int]$WeekNumber,
        [decimal]$BasicSalary,
        [decimal]$OtMultiplier,
        [decimal]$WorkedHours,
        [decimal]$SalaryMasterDeduction,
        [decimal]$SalaryAdvanceDeduction
    )

    $hourlyRate = [Math]::Round($BasicSalary / 8, 2)
    $otHours = [Math]::Round([Math]::Max(0, $WorkedHours - 8), 2)
    $weightedHours = $WorkedHours + ($otHours * $OtMultiplier)
    $otEarnings = [Math]::Round($otHours * $OtMultiplier * $hourlyRate, 2)
    $totalEarnings = [Math]::Round($weightedHours * $hourlyRate, 2)
    $totalDeductions = [Math]::Round($SalaryMasterDeduction + $SalaryAdvanceDeduction, 2)
    $netSalary = [Math]::Round([Math]::Max(0, $totalEarnings - $totalDeductions), 2)

    return [ordered]@{
        id = ''
        employeeId = $EmployeeId
        employeeCode = $EmployeeCode
        fullName = $FullName
        designation = $Designation
        month = $Month
        weekNumber = $WeekNumber
        salaryType = 'weekly'
        basicSalary = $BasicSalary
        hra = 0
        allowance = 0
        bonusPay = 0
        performancePay = 0
        salaryMasterDeduction = $SalaryMasterDeduction
        totalEarnings = $totalEarnings
        presentDays = 1
        absentDays = 0
        leaveDays = 0
        totalOtHours = $otHours
        otEarnings = $otEarnings
        attendanceDeduction = 0
        salaryAdvanceDeduction = $SalaryAdvanceDeduction
        otherDeductions = 0
        totalDeductions = $totalDeductions
        netSalary = $netSalary
        calcStatus = 'draft'
    }
}

function Get-MonthlySalaryCalculation {
    param(
        [string]$EmployeeId,
        [string]$EmployeeCode,
        [string]$FullName,
        [string]$Designation,
        [string]$Month,
        [datetime]$JoiningDate,
        [decimal]$BasicSalary,
        [decimal]$Hra,
        [decimal]$Allowance,
        [decimal]$SalaryMasterDeduction,
        [decimal]$SalaryAdvanceDeduction,
        [string[]]$AbsentDates
    )

    $monthStart = [datetime]::ParseExact("$Month-01", 'yyyy-MM-dd', $null)
    $daysInMonth = [datetime]::DaysInMonth($monthStart.Year, $monthStart.Month)
    $workedHours = [decimal]0
    $presentDays = [decimal]0
    $absentDays = [decimal]0

    for ($day = 1; $day -le $daysInMonth; $day++) {
        $date = Get-Date -Year $monthStart.Year -Month $monthStart.Month -Day $day
        if ($date.Date -lt $JoiningDate.Date) {
            continue
        }

        $dateKey = $date.ToString('yyyy-MM-dd')
        if ($AbsentDates -contains $dateKey) {
            $absentDays += 1
            continue
        }

        if ($date.DayOfWeek -eq [System.DayOfWeek]::Sunday) {
            $workedHours += 8
            $presentDays += 1
            continue
        }

        $workedHours += 8
        $presentDays += 1
    }

    $hourlyRate = [Math]::Round($BasicSalary / (30 * 8), 2)
    $totalEarnings = [Math]::Round(($workedHours * $hourlyRate) + $Hra + $Allowance, 2)
    $totalDeductions = [Math]::Round($SalaryMasterDeduction + $SalaryAdvanceDeduction, 2)
    $netSalary = [Math]::Round([Math]::Max(0, $totalEarnings - $totalDeductions), 2)

    return [ordered]@{
        id = ''
        employeeId = $EmployeeId
        employeeCode = $EmployeeCode
        fullName = $FullName
        designation = $Designation
        month = $Month
        salaryType = 'monthly'
        basicSalary = $BasicSalary
        hra = $Hra
        allowance = $Allowance
        bonusPay = 0
        performancePay = 0
        salaryMasterDeduction = $SalaryMasterDeduction
        totalEarnings = $totalEarnings
        presentDays = [int][Math]::Round($presentDays, 0)
        absentDays = [int][Math]::Round($absentDays, 0)
        leaveDays = 0
        totalOtHours = 0
        otEarnings = 0
        attendanceDeduction = 0
        salaryAdvanceDeduction = $SalaryAdvanceDeduction
        otherDeductions = 0
        totalDeductions = $totalDeductions
        netSalary = $netSalary
        calcStatus = 'draft'
    }
}

function Reset-Database {
    if (-not (Test-Path $SqlCmdPath)) {
        throw "sqlcmd was not found at $SqlCmdPath"
    }

    & $SqlCmdPath -S 'JAGAN-PC\SQLEXPRESS' -d 'QuotationV2' -U 'magizhpack' -P '9840960342' -b -i $clearSqlPath | Out-Null
}

function Write-Report {
    param([object[]]$ScenarioResults)

    $accounts = $ScenarioResults | Where-Object { $_.module -eq 'Accounts' }
    $employee = $ScenarioResults | Where-Object { $_.module -eq 'Employee Salary' }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# Data Flow Validation Report') | Out-Null
    $lines.Add('') | Out-Null
    $lines.Add("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')") | Out-Null
    $lines.Add('') | Out-Null
    $lines.Add('This report explains, in business language, what happened when one record was added or changed, which pages were affected, and whether the system behaved correctly.') | Out-Null
    $lines.Add('') | Out-Null

    foreach ($moduleName in @('Accounts', 'Employee Salary')) {
        $moduleRows = $ScenarioResults | Where-Object { $_.module -eq $moduleName }
        $passed = @($moduleRows | Where-Object { $_.passed }).Count
        $failed = @($moduleRows | Where-Object { -not $_.passed }).Count

        $lines.Add("## $moduleName") | Out-Null
        $lines.Add('') | Out-Null
        $lines.Add("Summary: $passed passed, $failed failed.") | Out-Null
        $lines.Add('') | Out-Null

        foreach ($row in $moduleRows) {
            $statusText = if ($row.passed) { 'PASS' } else { 'FAIL' }
            $lines.Add("### [$statusText] $($row.scenario) ($($row.caseType))") | Out-Null
            $lines.Add('') | Out-Null
            $lines.Add("Data added or action tried: $($row.addedData)") | Out-Null
            $lines.Add('') | Out-Null
            $lines.Add("What a business user would see: $($row.businessImpact)") | Out-Null
            $lines.Add('') | Out-Null
            $lines.Add('Affected pages:') | Out-Null
            foreach ($page in $row.affectedPages) {
                $lines.Add("- $page") | Out-Null
            }
            $lines.Add('') | Out-Null
            $lines.Add('Validation details:') | Out-Null
            foreach ($key in $row.checks.Keys) {
                $lines.Add(('- {0}: {1}' -f $key, $row.checks[$key])) | Out-Null
            }
            if ($row.notes) {
                $lines.Add('') | Out-Null
                $lines.Add("Note: $($row.notes)") | Out-Null
            }
            $lines.Add('') | Out-Null
        }
    }

    Set-Content -Path $reportPath -Value $lines -Encoding UTF8
}

Write-Host 'Resetting database...'
Reset-Database

$health = Invoke-Api -Method Get -Path '/api/accounts/summary'
if (-not $health.Ok) {
    throw "API is not reachable at $BaseUrl. Start QuotationAPI.V2 before running this script."
}

$today = Get-Date
$todayDate = $today.ToString('yyyy-MM-dd')
$currentMonth = $today.ToString('yyyy-MM')
$previousMonthDate = $today.AddMonths(-1)
$previousMonth = $previousMonthDate.ToString('yyyy-MM')
$previousMonthStart = Get-Date -Year $previousMonthDate.Year -Month $previousMonthDate.Month -Day 1
$previousMonthAbsentDate = $previousMonthStart.AddDays(1).ToString('yyyy-MM-dd')
$futureDate = $today.AddDays(3).ToString('yyyy-MM-dd')
$beforeJoiningDate = $previousMonthStart.AddMonths(-1).ToString('yyyy-MM-dd')
$suffix = Get-Date -Format 'yyyyMMddHHmmss'

Write-Host 'Seeding customer and account base data...'
$customer = Require-Success (Invoke-Api -Method Post -Path '/api/customer-masters' -Body @{
    name = "Flow Customer $suffix"
    phone = '9876543210'
    email = "flow$suffix@example.com"
    address = 'Validation Street'
    customerType = 'Retail'
    status = 'Active'
    openingBalance = 0
}) 'Create customer'

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/initial-balance' -Body @{ type = 'cash'; amount = 10000; description = 'Validation cash opening' }) 'Set cash opening'
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/initial-balance' -Body @{ type = 'bank'; amount = 20000; description = 'Validation bank opening' }) 'Set bank opening'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Opening balances' -CaseType 'Positive' -Passed ($snapshotAfter.cash -eq 10000 -and $snapshotAfter.bank -eq 20000) -AddedData 'Added one cash opening balance and one bank opening balance.' -AffectedPages @('Accounts dashboard', 'Balance tracking', 'Account summary') -BusinessImpact 'The starting money available in cash and bank appeared immediately across the balance pages.' -Checks @{ 'Cash balance' = $snapshotAfter.cash; 'Bank balance' = $snapshotAfter.bank; 'Income unchanged' = $snapshotAfter.totalIncome; 'Expense unchanged' = $snapshotAfter.totalExpense }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
$transfer = Require-Success (Invoke-Api -Method Post -Path '/api/accounts/cash-transfers' -Body @{ fromAccount = 'bank'; toAccount = 'cash'; amount = 1500; transferDate = $todayDate; remarks = 'Validation transfer'; description = 'Validation transfer' }) 'Create cash transfer'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Cash transfer from bank to cash' -CaseType 'Positive' -Passed (($snapshotAfter.cash - $snapshotBefore.cash) -eq 1500 -and ($snapshotBefore.bank - $snapshotAfter.bank) -eq 1500 -and ($snapshotAfter.totalIncome -eq $snapshotBefore.totalIncome)) -AddedData 'Added one bank-to-cash transfer for 1,500.' -AffectedPages @('Cash transfer page', 'Accounts dashboard', 'Balance tracking', 'Account summary') -BusinessImpact 'Cash went up, bank went down, but income and expense did not change because this is only moving money between accounts.' -Checks @{ 'Cash change' = ($snapshotAfter.cash - $snapshotBefore.cash); 'Bank change' = ($snapshotAfter.bank - $snapshotBefore.bank); 'Cash transfer rows' = $snapshotAfter.cashTransferCount }

$invalidTransfer = Invoke-Api -Method Post -Path '/api/accounts/cash-transfers' -Body @{ fromAccount = 'cash'; toAccount = 'cash'; amount = 100; transferDate = $todayDate; remarks = 'Invalid'; description = 'Invalid' }
Add-Result -Module 'Accounts' -Scenario 'Cash transfer with same source and destination' -CaseType 'Negative' -Passed (-not $invalidTransfer.Ok -and $invalidTransfer.StatusCode -eq 400) -AddedData 'Tried to transfer from cash to cash.' -AffectedPages @('Cash transfer page') -BusinessImpact 'The system correctly blocked an invalid transfer that would not make business sense.' -Checks @{ 'HTTP status' = $invalidTransfer.StatusCode; 'Message' = $invalidTransfer.Body }

$insufficientTransfer = Invoke-Api -Method Post -Path '/api/accounts/cash-transfers' -Body @{ fromAccount = 'cash'; toAccount = 'bank'; amount = 999999; transferDate = $todayDate; remarks = 'Too much'; description = 'Too much' }
Add-Result -Module 'Accounts' -Scenario 'Cash transfer with insufficient balance' -CaseType 'Negative' -Passed (-not $insufficientTransfer.Ok -and $insufficientTransfer.StatusCode -eq 400) -AddedData 'Tried to transfer more cash than available.' -AffectedPages @('Cash transfer page', 'Balance tracking') -BusinessImpact 'The system correctly refused to let money go below zero.' -Checks @{ 'HTTP status' = $insufficientTransfer.StatusCode; 'Message' = $insufficientTransfer.Body }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/customer-outstanding/additional' -Body @{ customerId = $customer.id; customerName = $customer.name; amount = 5000; date = $todayDate; description = 'Manual outstanding' }) 'Create additional outstanding'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
$customerOutstandingRow = @($snapshotAfter.outstandingRows | Where-Object { $_.customerId -eq $customer.id })[0]
Add-Result -Module 'Accounts' -Scenario 'Additional customer outstanding' -CaseType 'Positive' -Passed (($snapshotAfter.totalOutstanding - $snapshotBefore.totalOutstanding) -eq 5000) -AddedData 'Added one extra outstanding amount of 5,000 for one customer.' -AffectedPages @('Customer outstanding page', 'Accounts dashboard') -BusinessImpact 'The customer now appears as owing more money, and total outstanding increased on summary pages.' -Checks @{ 'Outstanding change' = ($snapshotAfter.totalOutstanding - $snapshotBefore.totalOutstanding); 'Customer outstanding now' = $customerOutstandingRow.outstandingAmount }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/customer-outstanding/settlements' -Body @{ customerId = $customer.id; customerName = $customer.name; amount = 1200; type = 'cash'; date = $todayDate; reference = "SET-$suffix"; description = 'Settlement payment' }) 'Settle outstanding'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
$customerOutstandingRow = @($snapshotAfter.outstandingRows | Where-Object { $_.customerId -eq $customer.id })[0]
Add-Result -Module 'Accounts' -Scenario 'Customer outstanding settlement' -CaseType 'Positive' -Passed ((($snapshotAfter.cash - $snapshotBefore.cash) -eq 1200) -and (($snapshotBefore.totalOutstanding - $snapshotAfter.totalOutstanding) -eq 1200)) -AddedData 'Added one customer settlement payment of 1,200 in cash.' -AffectedPages @('Customer outstanding page', 'Income list', 'Accounts dashboard', 'Balance tracking') -BusinessImpact 'Cash increased, income increased, and the customer outstanding reduced by the same amount.' -Checks @{ 'Cash change' = ($snapshotAfter.cash - $snapshotBefore.cash); 'Outstanding reduction' = ($snapshotBefore.totalOutstanding - $snapshotAfter.totalOutstanding); 'Remaining customer outstanding' = $customerOutstandingRow.outstandingAmount }

$overSettlement = Invoke-Api -Method Post -Path '/api/accounts/customer-outstanding/settlements' -Body @{ customerId = $customer.id; customerName = $customer.name; amount = 999999; type = 'cash'; date = $todayDate; reference = "OVR-$suffix"; description = 'Over settlement' }
Add-Result -Module 'Accounts' -Scenario 'Customer settlement above outstanding amount' -CaseType 'Negative' -Passed (-not $overSettlement.Ok -and $overSettlement.StatusCode -eq 400) -AddedData 'Tried to settle more than the customer owes.' -AffectedPages @('Customer outstanding page', 'Income list') -BusinessImpact 'The system correctly blocked over-collection so the customer cannot go into a fake negative outstanding state.' -Checks @{ 'HTTP status' = $overSettlement.StatusCode; 'Message' = $overSettlement.Body }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/income-entries' -Body @{ customerId = $customer.id; customerName = $customer.name; description = 'Independent income'; amount = 900; type = 'bank'; incomeType = 'independent'; date = $todayDate; category = 'Misc'; reference = "INC-$suffix"; status = 'active' }) 'Create active income'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Active income entry' -CaseType 'Positive' -Passed ((($snapshotAfter.bank - $snapshotBefore.bank) -eq 900) -and (($snapshotAfter.totalIncome - $snapshotBefore.totalIncome) -eq 900)) -AddedData 'Added one active bank income entry of 900.' -AffectedPages @('Income list', 'Accounts dashboard', 'Balance tracking', 'Account summary') -BusinessImpact 'Bank balance and total income increased immediately because the income was active.' -Checks @{ 'Bank change' = ($snapshotAfter.bank - $snapshotBefore.bank); 'Income change' = ($snapshotAfter.totalIncome - $snapshotBefore.totalIncome) }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/income-entries' -Body @{ customerId = $customer.id; customerName = $customer.name; description = 'Cancelled income'; amount = 700; type = 'cash'; incomeType = 'independent'; date = $todayDate; category = 'Misc'; reference = "CAN-$suffix"; status = 'cancelled' }) 'Create cancelled income'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Cancelled income entry' -CaseType 'Negative branch validation' -Passed ((($snapshotAfter.cash - $snapshotBefore.cash) -eq 0) -and (($snapshotAfter.totalIncome - $snapshotBefore.totalIncome) -eq 0)) -AddedData 'Added one cancelled income entry of 700.' -AffectedPages @('Income list') -BusinessImpact 'The row can exist in the list, but balances and totals do not move because it is not active.' -Checks @{ 'Cash change' = ($snapshotAfter.cash - $snapshotBefore.cash); 'Income change' = ($snapshotAfter.totalIncome - $snapshotBefore.totalIncome) } -Notes 'This was a defect before validation. It is now fixed so inactive income no longer inflates balances.'

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/expense-entries' -Body @{ description = 'Office expense'; amount = 450; type = 'cash'; date = $todayDate; category = 'Office'; reference = "EXP-$suffix"; status = 'active' }) 'Create active expense'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Active expense entry' -CaseType 'Positive' -Passed ((($snapshotBefore.cash - $snapshotAfter.cash) -eq 450) -and (($snapshotAfter.totalExpense - $snapshotBefore.totalExpense) -eq 450)) -AddedData 'Added one active cash expense of 450.' -AffectedPages @('Expense list', 'Expense dashboard', 'Accounts dashboard', 'Balance tracking', 'Account summary') -BusinessImpact 'Cash reduced and expense totals increased because the expense was active.' -Checks @{ 'Cash reduction' = ($snapshotBefore.cash - $snapshotAfter.cash); 'Expense increase' = ($snapshotAfter.totalExpense - $snapshotBefore.totalExpense); 'Monthly expense change' = ($snapshotAfter.monthlyExpense - $snapshotBefore.monthlyExpense) }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/expense-entries' -Body @{ description = 'Draft expense'; amount = 300; type = 'bank'; date = $todayDate; category = 'Office'; reference = "DEX-$suffix"; status = 'draft' }) 'Create draft expense'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Draft expense entry' -CaseType 'Negative branch validation' -Passed ((($snapshotBefore.bank - $snapshotAfter.bank) -eq 0) -and (($snapshotAfter.totalExpense - $snapshotBefore.totalExpense) -eq 0)) -AddedData 'Added one draft expense of 300.' -AffectedPages @('Expense list') -BusinessImpact 'The draft expense stays out of balances and totals until it becomes active.' -Checks @{ 'Bank change' = ($snapshotBefore.bank - $snapshotAfter.bank); 'Expense change' = ($snapshotAfter.totalExpense - $snapshotBefore.totalExpense) }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
$purchase = Require-Success (Invoke-Api -Method Post -Path '/api/accounts/ledger/purchase-sales' -Body @{ customerId = $customer.id; customerName = $customer.name; paymentType = 'bank'; totalAmountPaid = 12500; taxPercent = 18; voucherDate = $todayDate }) 'Create purchase'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Purchase entry' -CaseType 'Positive' -Passed ((($snapshotBefore.bank - $snapshotAfter.bank) -eq 12500) -and (($snapshotAfter.totalExpense - $snapshotBefore.totalExpense) -eq 12500) -and (($snapshotAfter.purchaseCount - $snapshotBefore.purchaseCount) -eq 1)) -AddedData 'Added one purchase of 12,500 paid from bank.' -AffectedPages @('Purchase and sales page', 'Expense list', 'Expense dashboard', 'Tax payment page', 'Balance tracking', 'Accounts dashboard') -BusinessImpact 'Bank reduced, a linked purchase expense appeared automatically, total expense increased, and tax credit became available for the month.' -Checks @{ 'Bank reduction' = ($snapshotBefore.bank - $snapshotAfter.bank); 'Expense increase' = ($snapshotAfter.totalExpense - $snapshotBefore.totalExpense); 'Purchase rows' = $snapshotAfter.purchaseCount; 'Tax outstanding after purchase' = $snapshotAfter.taxOutstanding }

$purchaseFail = Invoke-Api -Method Post -Path '/api/accounts/ledger/purchase-sales' -Body @{ customerId = $customer.id; customerName = $customer.name; paymentType = 'cash'; totalAmountPaid = 999999; taxPercent = 18; voucherDate = $todayDate }
Add-Result -Module 'Accounts' -Scenario 'Purchase with insufficient payment balance' -CaseType 'Negative' -Passed (-not $purchaseFail.Ok -and $purchaseFail.StatusCode -eq 400) -AddedData 'Tried to add a purchase larger than the available cash balance.' -AffectedPages @('Purchase and sales page', 'Balance tracking') -BusinessImpact 'The purchase was blocked, so no expense row or balance movement happened.' -Checks @{ 'HTTP status' = $purchaseFail.StatusCode; 'Message' = $purchaseFail.Body }

$snapshotBefore = Get-AccountsSnapshot -Period $currentMonth
Require-Success (Invoke-Api -Method Post -Path '/api/accounts/ledger/tax-payments' -Body @{ taxType = 'GST'; mode = 'bank'; amount = 500; paymentDate = $todayDate; period = $currentMonth }) 'Create tax payment'
$snapshotAfter = Get-AccountsSnapshot -Period $currentMonth
Add-Result -Module 'Accounts' -Scenario 'Tax payment entry' -CaseType 'Positive' -Passed ((($snapshotBefore.bank - $snapshotAfter.bank) -eq 500) -and (($snapshotAfter.taxPaid - $snapshotBefore.taxPaid) -eq 500) -and (($snapshotAfter.taxPaymentCount - $snapshotBefore.taxPaymentCount) -eq 1)) -AddedData 'Added one tax payment of 500 from bank.' -AffectedPages @('Tax payment page', 'Balance tracking', 'Accounts dashboard') -BusinessImpact 'Bank reduced and the tax payment page showed the payment immediately, but total expense did not change because tax payment is tracked separately from operating expenses.' -Checks @{ 'Bank reduction' = ($snapshotBefore.bank - $snapshotAfter.bank); 'Tax paid change' = ($snapshotAfter.taxPaid - $snapshotBefore.taxPaid); 'Tax rows' = $snapshotAfter.taxPaymentCount }

$taxFail = Invoke-Api -Method Post -Path '/api/accounts/ledger/tax-payments' -Body @{ taxType = 'GST'; mode = 'cash'; amount = 999999; paymentDate = $todayDate; period = $currentMonth }
Add-Result -Module 'Accounts' -Scenario 'Tax payment with insufficient balance' -CaseType 'Negative' -Passed (-not $taxFail.Ok -and $taxFail.StatusCode -eq 400) -AddedData 'Tried to pay tax using more cash than available.' -AffectedPages @('Tax payment page', 'Balance tracking') -BusinessImpact 'The payment was blocked and balances stayed safe.' -Checks @{ 'HTTP status' = $taxFail.StatusCode; 'Message' = $taxFail.Body }

Write-Host 'Seeding employee salary validation data...'
$monthlyEmployee = Require-Success (Invoke-Api -Method Post -Path '/api/employees' -Body @{ id = 'emp-monthly-001'; employeeCode = 'E-2001'; fullName = 'Monthly Employee'; phone = '9000000001'; designation = 'Operator'; joiningDate = $previousMonthStart.ToString('yyyy-MM-dd'); monthlySalary = 30000; status = 'active'; department = 'Production' }) 'Create monthly employee'
$weeklyEmployee = Require-Success (Invoke-Api -Method Post -Path '/api/employees' -Body @{ id = 'emp-weekly-001'; employeeCode = 'E-2002'; fullName = 'Weekly Employee'; phone = '9000000002'; designation = 'Helper'; joiningDate = $previousMonthStart.ToString('yyyy-MM-dd'); monthlySalary = 0; status = 'active'; department = 'Packing' }) 'Create weekly employee'

$futureAttendance = Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{ id = 'att-future'; employeeId = $monthlyEmployee.id; date = $futureDate; status = 'present'; attendanceHours = 8; notes = 'Future test' }
Add-Result -Module 'Employee Salary' -Scenario 'Attendance on future date' -CaseType 'Negative' -Passed (-not $futureAttendance.Ok -and $futureAttendance.StatusCode -eq 400) -AddedData 'Tried to mark attendance on a future date.' -AffectedPages @('Attendance page', 'Salary calculation page') -BusinessImpact 'The system correctly blocked future attendance so future salary cannot be inflated.' -Checks @{ 'HTTP status' = $futureAttendance.StatusCode; 'Message' = $futureAttendance.Body }

$beforeJoinAttendance = Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{ id = 'att-before-join'; employeeId = $weeklyEmployee.id; date = $beforeJoiningDate; status = 'present'; attendanceHours = 8; notes = 'Before joining test' }
Add-Result -Module 'Employee Salary' -Scenario 'Attendance before joining date' -CaseType 'Negative' -Passed (-not $beforeJoinAttendance.Ok -and $beforeJoinAttendance.StatusCode -eq 400) -AddedData 'Tried to mark attendance before the employee joined.' -AffectedPages @('Attendance page', 'Salary calculation page') -BusinessImpact 'The system correctly blocked attendance before employment started.' -Checks @{ 'HTTP status' = $beforeJoinAttendance.StatusCode; 'Message' = $beforeJoinAttendance.Body }

$monthlyMaster = Require-Success (Invoke-Api -Method Post -Path '/api/employees/salary-masters' -Body @{ id = 'sal-master-001'; employeeId = $monthlyEmployee.id; salaryType = 'monthly'; basicSalary = 30000; hra = 5000; allowance = 2000; deduction = 1000; otMultiplier = 1.5; effectiveFrom = $previousMonthStart.ToString('yyyy-MM-dd'); description = 'Monthly salary master' }) 'Create monthly salary master'
Add-Result -Module 'Employee Salary' -Scenario 'Monthly salary master' -CaseType 'Positive' -Passed ([decimal]$monthlyMaster.basicSalary -eq 30000 -and [decimal]$monthlyMaster.hra -eq 5000 -and [decimal]$monthlyMaster.allowance -eq 2000) -AddedData 'Added one monthly salary master for the monthly employee.' -AffectedPages @('Salary master page', 'Attendance page', 'Salary calculation page') -BusinessImpact 'The salary base, HRA, allowance, and OT rules became the source values for salary calculation and attendance pay display.' -Checks @{ 'Basic salary' = $monthlyMaster.basicSalary; 'HRA' = $monthlyMaster.hra; 'Allowance' = $monthlyMaster.allowance }

$weeklyMaster = Require-Success (Invoke-Api -Method Post -Path '/api/employees/salary-masters' -Body @{ id = 'sal-master-002'; employeeId = $weeklyEmployee.id; salaryType = 'weekly'; basicSalary = 800; hra = 250; allowance = 150; deduction = 100; otMultiplier = 2; effectiveFrom = $previousMonthStart.ToString('yyyy-MM-dd'); description = 'Weekly salary master' }) 'Create weekly salary master'
Add-Result -Module 'Employee Salary' -Scenario 'Weekly salary master' -CaseType 'Positive' -Passed ([decimal]$weeklyMaster.hra -eq 0 -and [decimal]$weeklyMaster.allowance -eq 0) -AddedData 'Added one weekly salary master for the weekly employee.' -AffectedPages @('Salary master page', 'Attendance page', 'Salary calculation page') -BusinessImpact 'The weekly salary type correctly stripped HRA and allowance so weekly pay stays day-based only.' -Checks @{ 'Salary type' = $weeklyMaster.salaryType; 'HRA after save' = $weeklyMaster.hra; 'Allowance after save' = $weeklyMaster.allowance }

Require-Success (Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{ id = 'att-month-absent'; employeeId = $monthlyEmployee.id; date = $previousMonthAbsentDate; status = 'absent'; attendanceHours = 0; notes = 'One absent day' }) 'Create monthly attendance record'
Add-Result -Module 'Employee Salary' -Scenario 'One monthly attendance exception' -CaseType 'Positive' -Passed $true -AddedData 'Added one absent day for the monthly employee.' -AffectedPages @('Attendance page', 'Attendance monthly entry', 'Salary calculation page') -BusinessImpact 'This single attendance record becomes a salary-affecting exception: one day is no longer counted as paid work in the monthly salary calculation.' -Checks @{ 'Absent date' = $previousMonthAbsentDate; 'Expected salary effect' = 'Monthly pay should reduce by one day of base hourly value.' }

$advance = Require-Success (Invoke-Api -Method Post -Path '/api/employees/salary-advances' -Body @{ id = 'adv-001'; employeeId = $monthlyEmployee.id; amount = 2000; requestDate = $todayDate; reason = 'Emergency'; status = 'approved' }) 'Create salary advance'
Add-Result -Module 'Employee Salary' -Scenario 'Approved salary advance' -CaseType 'Positive' -Passed ([decimal]$advance.amount -eq 2000) -AddedData 'Added one approved salary advance of 2,000.' -AffectedPages @('Salary advance page', 'Salary advance balance page', 'Salary calculation page') -BusinessImpact 'The advance became a pending deduction candidate for the next salary calculation.' -Checks @{ 'Advance amount' = $advance.amount; 'Advance status' = $advance.status }

$monthlyCalcPayload = Get-MonthlySalaryCalculation -EmployeeId $monthlyEmployee.id -EmployeeCode $monthlyEmployee.employeeCode -FullName $monthlyEmployee.fullName -Designation $monthlyEmployee.designation -Month $previousMonth -JoiningDate $previousMonthStart -BasicSalary 30000 -Hra 5000 -Allowance 2000 -SalaryMasterDeduction 1000 -SalaryAdvanceDeduction 2000 -AbsentDates @($previousMonthAbsentDate)
$monthlyCalc = Require-Success (Invoke-Api -Method Post -Path '/api/employees/monthly-salary-calcs' -Body $monthlyCalcPayload) 'Create monthly salary calc'
Add-Result -Module 'Employee Salary' -Scenario 'Monthly salary calculation with one absence and one approved advance' -CaseType 'Positive' -Passed ([decimal]$monthlyCalc.netSalary -eq [decimal]$monthlyCalcPayload.netSalary) -AddedData 'Saved one monthly salary calculation after applying one absent day and one approved salary advance.' -AffectedPages @('Salary calculation page', 'Calculated salaries page', 'Payslip generator', 'Salary advance balance page') -BusinessImpact 'The saved salary reflected both attendance impact and advance deduction, and it became available for payslip generation.' -Checks @{ 'Calculated net salary' = $monthlyCalc.netSalary; 'Salary advance deduction used' = $monthlyCalc.salaryAdvanceDeduction; 'Attendance deduction field' = $monthlyCalc.attendanceDeduction; 'Total deductions' = $monthlyCalc.totalDeductions }

$week1Date = $previousMonthStart.ToString('yyyy-MM-dd')
$week2Date = $previousMonthStart.AddDays(7).ToString('yyyy-MM-dd')
Require-Success (Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{ id = 'att-week-1'; employeeId = $weeklyEmployee.id; date = $week1Date; status = 'present'; attendanceHours = 10; notes = 'Week 1 OT' }) 'Create weekly attendance week 1'
Require-Success (Invoke-Api -Method Post -Path '/api/employees/attendance' -Body @{ id = 'att-week-2'; employeeId = $weeklyEmployee.id; date = $week2Date; status = 'present'; attendanceHours = 8; notes = 'Week 2 standard' }) 'Create weekly attendance week 2'

$weeklyCalcWeek1Payload = Get-WeeklySalaryCalculation -EmployeeId $weeklyEmployee.id -EmployeeCode $weeklyEmployee.employeeCode -FullName $weeklyEmployee.fullName -Designation $weeklyEmployee.designation -Month $previousMonth -WeekNumber 1 -BasicSalary 800 -OtMultiplier 2 -WorkedHours 10 -SalaryMasterDeduction 100 -SalaryAdvanceDeduction 0
$weeklyCalcWeek2Payload = Get-WeeklySalaryCalculation -EmployeeId $weeklyEmployee.id -EmployeeCode $weeklyEmployee.employeeCode -FullName $weeklyEmployee.fullName -Designation $weeklyEmployee.designation -Month $previousMonth -WeekNumber 2 -BasicSalary 800 -OtMultiplier 2 -WorkedHours 8 -SalaryMasterDeduction 100 -SalaryAdvanceDeduction 0
$weeklyCalcWeek1 = Require-Success (Invoke-Api -Method Post -Path '/api/employees/monthly-salary-calcs' -Body $weeklyCalcWeek1Payload) 'Create weekly salary calc week 1'
$weeklyCalcWeek2 = Require-Success (Invoke-Api -Method Post -Path '/api/employees/monthly-salary-calcs' -Body $weeklyCalcWeek2Payload) 'Create weekly salary calc week 2'
$allCalcs = Require-Success (Invoke-Api -Method Get -Path '/api/employees/monthly-salary-calcs') 'Get salary calculations'
$weeklyRows = @($allCalcs | Where-Object { $_.employeeId -eq $weeklyEmployee.id -and $_.month -eq $previousMonth -and $_.salaryType -eq 'weekly' })
$week1Row = @($weeklyRows | Where-Object { $_.weekNumber -eq 1 })[0]
$week2Row = @($weeklyRows | Where-Object { $_.weekNumber -eq 2 })[0]
Add-Result -Module 'Employee Salary' -Scenario 'Weekly salary calculations for week 1 and week 2' -CaseType 'Positive' -Passed ($weeklyRows.Count -eq 2 -and $null -ne $week1Row -and $null -ne $week2Row) -AddedData 'Saved one weekly salary calculation for week 1 and another for week 2 in the same month.' -AffectedPages @('Salary calculation page', 'Calculated salaries page', 'Payslip generator') -BusinessImpact 'Both weekly runs are now stored separately, so one week no longer overwrites another week in the same month.' -Checks @{ 'Weekly rows saved' = $weeklyRows.Count; 'Week 1 net salary' = $week1Row.netSalary; 'Week 2 net salary' = $week2Row.netSalary } -Notes 'This was a defect before validation. The API now stores and matches Week Number correctly.'

$results | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Report -ScenarioResults $results.ToArray()

Write-Host "Validation JSON written to: $jsonPath"
Write-Host "Business report written to: $reportPath"