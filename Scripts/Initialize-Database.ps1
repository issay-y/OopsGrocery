[CmdletBinding()]
param(
	[string]$XamppPath = 'C:\xampp',
	[string]$DatabaseUser = 'root',
	[string]$DatabasePassword = ''
)

$mysqlClient = Join-Path $XamppPath 'mysql\bin\mysql.exe'
$schemaPath = Join-Path $PSScriptRoot '..\Database\schema.sql'

if (-not (Test-Path $mysqlClient)) {
	throw "MariaDB client was not found at '$mysqlClient'. Pass the XAMPP location with -XamppPath."
}

if (-not (Test-Path $schemaPath)) {
	throw "Database schema was not found at '$schemaPath'."
}

$arguments = @('--user', $DatabaseUser)
if ($DatabasePassword) {
	$arguments += "--password=$DatabasePassword"
}

$seedPath = Join-Path $PSScriptRoot '..\Database\seed.sql'

Write-Host 'Creating the OopsGrocery database and Users table...'
Get-Content -Raw $schemaPath | & $mysqlClient @arguments

if ($LASTEXITCODE -ne 0) {
	throw "MariaDB returned exit code $LASTEXITCODE. Ensure XAMPP MariaDB is running and the credentials are correct."
}

if (Test-Path $seedPath) {
	Write-Host 'Seeding initial data...'
	Get-Content -Raw $seedPath | & $mysqlClient @arguments
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to apply seed data with exit code $LASTEXITCODE."
	}
}

Write-Host 'Database setup and seeding completed successfully.'
