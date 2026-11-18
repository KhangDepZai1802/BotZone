[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
try {
    docker compose stop
    if ($LASTEXITCODE -ne 0) { throw 'Could not stop BotZone containers.' }
    Write-Host 'Stopped. PostgreSQL, email and uploads have been preserved.'
} finally { Pop-Location }
