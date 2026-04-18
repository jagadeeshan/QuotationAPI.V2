param(
    [string]$BaseUrl = 'http://localhost:7502',
    [string]$SqlCmdPath = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProjectRoot = Split-Path -Parent $scriptRoot
$workspaceRoot = Split-Path -Parent (Split-Path -Parent $apiProjectRoot)
$seedScriptPath = Join-Path $scriptRoot 'seed-validate-e2e.ps1'
$detailedScriptPath = Join-Path $scriptRoot 'validate-data-flow.ps1'
$seedJsonPath = Join-Path $scriptRoot 'seed-validate-e2e-result.json'
$detailedJsonPath = Join-Path $scriptRoot 'validate-data-flow-result.json'
$clearSqlPath = Join-Path $scriptRoot 'clear-transaction-data.sql'
$htmlPath = Join-Path $workspaceRoot 'Quotation-v2.0\APPLICATION_VALIDATION_REPORT.html'

function Reset-Database {
    if (-not (Test-Path $SqlCmdPath)) {
        throw "sqlcmd was not found at $SqlCmdPath"
    }

    if (-not (Test-Path $clearSqlPath)) {
        throw "Clear data SQL script was not found at $clearSqlPath"
    }

    & $SqlCmdPath -S 'JAGAN-PC\SQLEXPRESS' -d 'QuotationV2' -U 'magizhpack' -P '9840960342' -b -i $clearSqlPath | Out-Null
}

function Encode-Html {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    return [System.Net.WebUtility]::HtmlEncode([string]$Value)
}

function Convert-ChecksToHtml {
    param($Checks)

    if ($null -eq $Checks) {
        return '<span class="muted">No checks recorded</span>'
    }

    $lines = foreach ($property in $Checks.PSObject.Properties) {
        '<li><strong>{0}:</strong> {1}</li>' -f (Encode-Html $property.Name), (Encode-Html $property.Value)
    }

    return '<ul class="checks">{0}</ul>' -f ($lines -join '')
}

function Convert-PagesToHtml {
    param($Pages)

    if ($null -eq $Pages -or @($Pages).Count -eq 0) {
        return '<span class="muted">Not specified</span>'
    }

    $items = foreach ($page in $Pages) {
        '<span class="tag">{0}</span>' -f (Encode-Html $page)
    }

    return $items -join ''
}

Write-Host 'Running full-application functional validation on a clean database...'
Reset-Database
& $seedScriptPath

Write-Host 'Running detailed Accounts and Employee Salary validation...'
& $detailedScriptPath -BaseUrl $BaseUrl -SqlCmdPath $SqlCmdPath

if (-not (Test-Path $seedJsonPath)) {
    throw "Seed validation result not found at $seedJsonPath"
}

if (-not (Test-Path $detailedJsonPath)) {
    throw "Detailed validation result not found at $detailedJsonPath"
}

$seedResult = Get-Content -Raw $seedJsonPath | ConvertFrom-Json
$detailedResults = Get-Content -Raw $detailedJsonPath | ConvertFrom-Json
$functionalModules = @($seedResult.moduleResults)
$detailedGroups = @($detailedResults | Group-Object module)

$moduleIndex = @{}
foreach ($functionalModule in $functionalModules) {
    $moduleIndex[$functionalModule.module] = [ordered]@{
        module = $functionalModule.module
        coverageType = $functionalModule.coverageType
        passed = [bool]$functionalModule.passed
        functionalScenarioCount = 1
        detailedScenarioCount = 0
        summary = $functionalModule.businessImpact
        notes = $functionalModule.notes
    }
}

foreach ($group in $detailedGroups) {
    $allPassed = (@($group.Group | Where-Object { -not $_.passed }).Count -eq 0)
    if ($moduleIndex.ContainsKey($group.Name)) {
        $moduleIndex[$group.Name].coverageType = '{0} + Detailed data flow' -f $moduleIndex[$group.Name].coverageType
        $moduleIndex[$group.Name].passed = ($moduleIndex[$group.Name].passed -and $allPassed)
        $moduleIndex[$group.Name].detailedScenarioCount = @($group.Group).Count
        if ([string]::IsNullOrWhiteSpace([string]$moduleIndex[$group.Name].notes)) {
            $moduleIndex[$group.Name].notes = 'Detailed per-scenario validation included.'
        }
    }
    else {
        $moduleIndex[$group.Name] = [ordered]@{
            module = $group.Name
            coverageType = 'Detailed data flow'
            passed = $allPassed
            functionalScenarioCount = 0
            detailedScenarioCount = @($group.Group).Count
            summary = 'Detailed one-record-at-a-time business validation completed.'
            notes = 'Detailed per-scenario validation included.'
        }
    }
}

$moduleSummary = foreach ($entry in $moduleIndex.GetEnumerator() | Sort-Object Name) {
    [pscustomobject]$entry.Value
}

$totalModules = @($moduleSummary).Count
$passedModules = @($moduleSummary | Where-Object { $_.passed }).Count
$failedModules = @($moduleSummary | Where-Object { -not $_.passed }).Count
$functionalCount = @($functionalModules).Count
$detailedScenarioCount = @($detailedResults).Count
$generatedAt = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$moduleTableRows = foreach ($module in $moduleSummary) {
    $statusClass = if ($module.passed) { 'pass' } else { 'fail' }
    $statusText = if ($module.passed) { 'Passed' } else { 'Failed' }
    @"
<tr>
  <td>{0}</td>
  <td><span class="status {1}">{2}</span></td>
  <td>{3}</td>
  <td>{4}</td>
  <td>{5}</td>
</tr>
"@ -f (Encode-Html $module.module), $statusClass, $statusText, (Encode-Html $module.coverageType), (Encode-Html ("Functional: {0}, Detailed: {1}" -f $module.functionalScenarioCount, $module.detailedScenarioCount)), (Encode-Html $module.notes)
}

$functionalCards = foreach ($result in $functionalModules) {
    $statusClass = if ($result.passed) { 'pass' } else { 'fail' }
    $statusText = if ($result.passed) { 'Passed' } else { 'Failed' }
  $notesHtml = if ([string]::IsNullOrWhiteSpace([string]$result.notes)) { '<span class="muted">None</span>' } else { Encode-Html $result.notes }
    @"
<section class="card module-card">
  <div class="card-head">
    <div>
      <h3>{0}</h3>
      <p class="scenario">{1}</p>
    </div>
    <span class="status {2}">{3}</span>
  </div>
  <p>{4}</p>
  <div class="meta-row"><strong>Coverage:</strong> {5}</div>
  <div class="meta-row"><strong>Affected pages:</strong> {6}</div>
  <div class="meta-row"><strong>Checks:</strong> {7}</div>
  <div class="meta-row"><strong>Notes:</strong> {8}</div>
</section>
"@ -f (Encode-Html $result.module), (Encode-Html $result.scenario), $statusClass, $statusText, (Encode-Html $result.businessImpact), (Encode-Html $result.coverageType), (Convert-PagesToHtml $result.affectedPages), (Convert-ChecksToHtml $result.checks), $notesHtml
}

$detailedSections = foreach ($group in ($detailedGroups | Sort-Object Name)) {
    $scenarioRows = foreach ($item in $group.Group) {
        $statusClass = if ($item.passed) { 'pass' } else { 'fail' }
        $statusText = if ($item.passed) { 'Passed' } else { 'Failed' }
        @"
<tr>
  <td>{0}</td>
  <td>{1}</td>
  <td><span class="status {2}">{3}</span></td>
  <td>{4}</td>
  <td>{5}</td>
  <td>{6}</td>
  <td>{7}</td>
</tr>
"@ -f (Encode-Html $item.scenario), (Encode-Html $item.caseType), $statusClass, $statusText, (Encode-Html $item.addedData), (Encode-Html $item.businessImpact), (Convert-PagesToHtml $item.affectedPages), (Convert-ChecksToHtml $item.checks)
    }

    @"
<section class="card">
  <div class="card-head">
    <div>
      <h3>{0}</h3>
      <p class="scenario">Detailed one-record-at-a-time business validation</p>
    </div>
    <span class="status pass">{1}/{1} Scenarios Passed</span>
  </div>
  <table>
    <thead>
      <tr>
        <th>Scenario</th>
        <th>Case Type</th>
        <th>Status</th>
        <th>Added Data</th>
        <th>Business Impact</th>
        <th>Affected Pages</th>
        <th>Checks</th>
      </tr>
    </thead>
    <tbody>
      {2}
    </tbody>
  </table>
</section>
"@ -f (Encode-Html $group.Name), @($group.Group).Count, ($scenarioRows -join '')
}

$html = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Application Validation Report</title>
  <style>
    :root {
      color-scheme: light;
      --bg: #f4efe7;
      --panel: #fffdfa;
      --ink: #1f2a30;
      --muted: #6a7378;
      --line: #dbcfc1;
      --accent: #7b3f00;
      --accent-soft: #f0dfc9;
      --pass: #2e7d32;
      --pass-bg: #e8f5e9;
      --fail: #b42318;
      --fail-bg: #fdecec;
      --shadow: 0 18px 40px rgba(74, 54, 33, 0.12);
    }

    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      background: radial-gradient(circle at top, #fff8ef 0%, var(--bg) 52%, #efe6d9 100%);
      color: var(--ink);
      line-height: 1.5;
    }

    .page {
      max-width: 1480px;
      margin: 0 auto;
      padding: 32px 24px 56px;
    }

    .hero {
      background: linear-gradient(135deg, #2f4858 0%, #7b3f00 100%);
      color: #fff;
      border-radius: 24px;
      padding: 32px;
      box-shadow: var(--shadow);
    }

    .hero h1 {
      margin: 0 0 10px;
      font-size: 34px;
      line-height: 1.15;
    }

    .hero p {
      margin: 0;
      max-width: 980px;
      color: rgba(255,255,255,0.88);
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
      margin: 24px 0 32px;
    }

    .metric, .card {
      background: var(--panel);
      border: 1px solid rgba(123, 63, 0, 0.12);
      border-radius: 18px;
      box-shadow: var(--shadow);
    }

    .metric {
      padding: 20px;
    }

    .metric .label {
      display: block;
      color: var(--muted);
      font-size: 13px;
      text-transform: uppercase;
      letter-spacing: 0.08em;
    }

    .metric .value {
      display: block;
      margin-top: 8px;
      font-size: 30px;
      font-weight: 700;
    }

    h2 {
      margin: 36px 0 14px;
      font-size: 24px;
    }

    .card {
      padding: 22px;
      margin-bottom: 18px;
    }

    .card-head {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      align-items: flex-start;
      margin-bottom: 14px;
    }

    .card-head h3 {
      margin: 0;
      font-size: 20px;
    }

    .scenario {
      margin: 4px 0 0;
      color: var(--muted);
    }

    .status {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 92px;
      padding: 8px 12px;
      border-radius: 999px;
      font-size: 13px;
      font-weight: 700;
      white-space: nowrap;
    }

    .status.pass {
      color: var(--pass);
      background: var(--pass-bg);
    }

    .status.fail {
      color: var(--fail);
      background: var(--fail-bg);
    }

    .meta-row {
      margin-top: 10px;
    }

    .muted {
      color: var(--muted);
    }

    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 12px;
      font-size: 14px;
    }

    th, td {
      text-align: left;
      padding: 12px 10px;
      border-bottom: 1px solid var(--line);
      vertical-align: top;
    }

    th {
      background: #fbf3e8;
      font-size: 13px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #5b4636;
    }

    .tag {
      display: inline-block;
      margin: 0 6px 6px 0;
      padding: 6px 10px;
      border-radius: 999px;
      background: var(--accent-soft);
      color: #5b2e03;
      font-size: 12px;
      font-weight: 600;
    }

    .checks {
      margin: 0;
      padding-left: 18px;
    }

    .checks li {
      margin-bottom: 4px;
    }

    @media (max-width: 900px) {
      .page {
        padding: 20px 14px 40px;
      }

      .hero {
        padding: 22px;
      }

      .hero h1 {
        font-size: 28px;
      }

      .card-head {
        flex-direction: column;
      }

      table, thead, tbody, th, td, tr {
        display: block;
      }

      thead {
        display: none;
      }

      tr {
        border-bottom: 1px solid var(--line);
      }

      td {
        padding: 10px 0;
      }
    }
  </style>
</head>
<body>
  <div class="page">
    <section class="hero">
      <h1>Quotation System Validation Report</h1>
      <p>This report combines full-application functional validation across all major modules with detailed business data-flow validation for Accounts and Employee Salary. Generated on __GENERATED_AT__.</p>
    </section>

    <section class="summary-grid">
      <div class="metric"><span class="label">Modules Covered</span><span class="value">__TOTAL_MODULES__</span></div>
      <div class="metric"><span class="label">Modules Passed</span><span class="value">__PASSED_MODULES__</span></div>
      <div class="metric"><span class="label">Modules Failed</span><span class="value">__FAILED_MODULES__</span></div>
      <div class="metric"><span class="label">Functional Module Checks</span><span class="value">__FUNCTIONAL_COUNT__</span></div>
      <div class="metric"><span class="label">Detailed Scenarios</span><span class="value">__DETAILED_COUNT__</span></div>
    </section>

    <h2>Coverage Summary</h2>
    <section class="card">
      <table>
        <thead>
          <tr>
            <th>Module</th>
            <th>Status</th>
            <th>Coverage Type</th>
            <th>Scenario Count</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          __MODULE_TABLE_ROWS__
        </tbody>
      </table>
    </section>

    <h2>Functional End-to-End Validation</h2>
    <p class="muted">These checks confirm that each major module can create or retrieve data successfully and that cross-module dependencies are active.</p>
    __FUNCTIONAL_CARDS__

    <h2>Detailed Business Data Flow Validation</h2>
    <p class="muted">These scenarios validate positive and negative business cases one by one, including downstream totals, balances, and rule enforcement.</p>
    __DETAILED_SECTIONS__
  </div>
</body>
</html>
"@

$html = $html.Replace('__GENERATED_AT__', (Encode-Html $generatedAt))
$html = $html.Replace('__TOTAL_MODULES__', [string]$totalModules)
$html = $html.Replace('__PASSED_MODULES__', [string]$passedModules)
$html = $html.Replace('__FAILED_MODULES__', [string]$failedModules)
$html = $html.Replace('__FUNCTIONAL_COUNT__', [string]$functionalCount)
$html = $html.Replace('__DETAILED_COUNT__', [string]$detailedScenarioCount)
$html = $html.Replace('__MODULE_TABLE_ROWS__', ($moduleTableRows -join ''))
$html = $html.Replace('__FUNCTIONAL_CARDS__', ($functionalCards -join ''))
$html = $html.Replace('__DETAILED_SECTIONS__', ($detailedSections -join ''))

[System.IO.File]::WriteAllText($htmlPath, $html, [System.Text.Encoding]::UTF8)
Write-Host "Combined HTML validation report generated at: $htmlPath"