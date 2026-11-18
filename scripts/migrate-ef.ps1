param([string]$Connection = $env:BOTZONE_MIGRATION_CONNECTION)
$ErrorActionPreference='Stop'
if (-not $Connection) { throw 'Set BOTZONE_MIGRATION_CONNECTION to the owner connection. Never put it in the Next.js environment.' }
$env:BOTZONE_MIGRATION_CONNECTION=$Connection
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '../.dotnet-home'
Push-Location -LiteralPath (Join-Path $PSScriptRoot '..')
try {
dotnet tool restore --tool-manifest "$PSScriptRoot/../dotnet-tools.json"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build "$PSScriptRoot/../src/BotZone.Infrastructure" -p:DatabaseToolsOnly=true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet ef database update --project "$PSScriptRoot/../src/BotZone.Infrastructure" --no-build
exit $LASTEXITCODE
} finally { Pop-Location }
