$ErrorActionPreference = 'Stop'

$jsonPath = 'f:\Company Project\Quotation-v2.0\src\assets\mock-data\list-of-values-db.json'
$sqlPath = 'f:\Company Project\QuotationAPI\QuotationAPI.V2\scripts\tmp-seed-lov.sql'
$data = (Get-Content $jsonPath -Raw | ConvertFrom-Json).listOfValues

function Esc([string]$s) {
  if ($null -eq $s) { return 'NULL' }
  return "N'" + $s.Replace("'", "''") + "'"
}

function IntOrNull($v) {
  if ($null -eq $v) { return 'NULL' }
  return [string][int]$v
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('SET NOCOUNT ON;')
$lines.Add('BEGIN TRAN;')
$lines.Add('DELETE FROM dbo.LovItems;')
$lines.Add('SET IDENTITY_INSERT dbo.LovItems ON;')

foreach ($r in $data) {
  $insert = "INSERT INTO dbo.LovItems (Id,Parentname,Parentvalue,Name,Value,Description,Itemtype,Displayorder,Isactive,Createdby,Updatedby,Createddt,Updateddt) VALUES (" +
    ([int]$r.id) + ',' +
    (Esc([string]$r.parentname)) + ',' +
    (IntOrNull $r.parentvalue) + ',' +
    (Esc([string]$r.name)) + ',' +
    (IntOrNull $r.value) + ',' +
    (Esc([string]$r.description)) + ',' +
    (Esc([string]$r.itemtype)) + ',' +
    ([int]$r.displayorder) + ',' +
    (Esc([string]$r.isactive)) + ',' +
    (Esc([string]$r.createdby)) + ',' +
    (Esc([string]$r.updatedby)) + ',' +
    (Esc([string]$r.createddt)) + ',' +
    (Esc([string]$r.updateddt)) + ');'
  $lines.Add($insert)
}

$maxId = ($data | Measure-Object -Property id -Maximum).Maximum
$lines.Add('SET IDENTITY_INSERT dbo.LovItems OFF;')
$lines.Add("DBCC CHECKIDENT ('dbo.LovItems', RESEED, $maxId) WITH NO_INFOMSGS;")
$lines.Add('COMMIT;')
$lines.Add('SELECT COUNT(1) AS [RowCount], MIN(Id) AS [MinId], MAX(Id) AS [MaxId] FROM dbo.LovItems;')

Set-Content -Path $sqlPath -Value $lines -Encoding UTF8

$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
& $sqlcmd -S 'JAGAN-PC\SQLEXPRESS' -d 'QuotationV2' -U 'magizhpack' -P '9840960342' -b -i $sqlPath
