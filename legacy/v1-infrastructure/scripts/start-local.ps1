[CmdletBinding()]
param([switch]$Development, [switch]$Seed)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    if (-not (Test-Path -LiteralPath '.env')) {
        Copy-Item -LiteralPath '.env.example' -Destination '.env'
    }
    docker info --format '{{.ServerVersion}}'
    if ($LASTEXITCODE -ne 0) { throw 'Docker engine is unavailable. Start Docker Desktop (Linux containers) first.' }
    docker compose config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Docker Compose configuration is invalid.' }
    if ($Development) {
        docker compose up -d db mailpit bootstrap
        if ($LASTEXITCODE -ne 0) { throw 'Database/email startup failed.' }
        docker compose wait bootstrap
        if ($LASTEXITCODE -ne 0) { throw 'Database role bootstrap failed.' }
        npm.cmd ci --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw 'Dependency installation failed.' }
        npm.cmd run db:migrate
        if ($LASTEXITCODE -ne 0) { throw 'Database migration failed.' }
        if ($Seed) {
            npm.cmd run db:seed
            if ($LASTEXITCODE -ne 0) { throw 'Demo seed failed.' }
        }
        Write-Host 'Database and Mailpit are ready. Run npm.cmd run dev and npm.cmd run worker in separate terminals.'
    } else {
        docker compose up -d --build
        if ($LASTEXITCODE -ne 0) { throw 'Container startup failed. Inspect docker compose logs.' }
        if ($Seed) {
            docker compose exec -T app npm run db:seed
            if ($LASTEXITCODE -ne 0) { throw 'Demo seed failed.' }
        }
        Write-Host 'BotZone: http://localhost:3000 | Mailpit: http://localhost:8025'
    }
} finally { Pop-Location }
