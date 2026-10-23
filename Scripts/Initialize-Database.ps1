[CmdletBinding()]
param(
	[string]$XamppPath = '',
	[string]$DatabaseUser = 'root',
	[string]$DatabasePassword = ''
)

$scriptDir = $PSScriptRoot
$sqlFile = Join-Path $scriptDir '..\Database\OopsGrocery.sql'

if (-not (Test-Path $sqlFile)) {
	$sqlFile = Join-Path $scriptDir '..\Database\schema.sql'
}

if (-not (Test-Path $sqlFile)) {
	throw "Database SQL file was not found in the Database directory."
}

# Locate mysql.exe across standard environments
$explicitPath = if ($XamppPath) { Join-Path $XamppPath 'mysql\bin\mysql.exe' } else { $null }

$mysqlCandidatePaths = @(
	$explicitPath,
	'C:\xampp\mysql\bin\mysql.exe',
	'D:\xampp\mysql\bin\mysql.exe',
	'E:\xampp\mysql\bin\mysql.exe',
	'C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe',
	'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe',
	'C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql.exe',
	'C:\wamp64\bin\mysql\mysql*\bin\mysql.exe',
	'C:\laragon\bin\mysql\mysql*\bin\mysql.exe'
)

$mysqlClient = $null

foreach ($candidate in $mysqlCandidatePaths) {
	if ($candidate) {
		$resolved = Resolve-Path $candidate -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Path
		if ($resolved -and (Test-Path $resolved)) {
			$mysqlClient = $resolved
			break
		}
	}
}

if (-not $mysqlClient) {
	$cmd = Get-Command 'mysql' -ErrorAction SilentlyContinue
	if ($cmd) {
		$mysqlClient = $cmd.Source
	}
}

if (-not $mysqlClient) {
	Write-Host "⚠️ Warning: mysql.exe was not automatically detected on this device." -ForegroundColor Yellow
	Write-Host "You can manually import '$(Resolve-Path $sqlFile)' into phpMyAdmin, MySQL Workbench, or run:" -ForegroundColor Cyan
	Write-Host "  mysql -u root -p < `"$sqlFile`"" -ForegroundColor White
	return
}

Write-Host "Using MySQL/MariaDB client at: $mysqlClient" -ForegroundColor Green

$arguments = @('--user', $DatabaseUser)
if ($DatabasePassword) {
	$arguments += "--password=$DatabasePassword"
}

Write-Host "Importing OopsGrocery database schema and initial data..." -ForegroundColor Cyan
Get-Content -Raw $sqlFile | & $mysqlClient @arguments

if ($LASTEXITCODE -ne 0) {
	Write-Host "Import returned exit code $LASTEXITCODE." -ForegroundColor Red
	Write-Host "If MySQL requires a password on this device, run:" -ForegroundColor Yellow
	Write-Host "  .\Initialize-Database.ps1 -DatabasePassword `"your_password`"" -ForegroundColor White
	throw "Failed to initialize OopsGrocery database."
}

Write-Host "Database setup and seeding completed successfully!" -ForegroundColor Green
