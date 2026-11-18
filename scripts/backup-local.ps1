[CmdletBinding()]
param([switch]$WritersStopped, [ValidatePattern('^botzone(?:_(?:baseline_test|integration_test)_[a-zA-Z0-9_]+)?$')][string]$Database = 'botzone')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $WritersStopped) {
    throw 'Stop the app and worker (including host npm processes), then run again with -WritersStopped. This keeps DB and media consistent.'
}
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$backupRoot = Join-Path $projectRoot 'backups'
$backupName = 'botzone-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$backupPath = Join-Path $backupRoot $backupName
$uploadsPath = Join-Path $projectRoot 'uploads'
Push-Location -LiteralPath $projectRoot
try {
    $container = docker compose ps -q db
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($container)) { throw 'PostgreSQL container must be running.' }
    New-Item -ItemType Directory -Path $backupPath | Out-Null
    $dumpInContainer = '/tmp/' + $backupName + '.dump'
    docker compose exec -T db pg_dump -U postgres -d $Database -Fc -f $dumpInContainer
    if ($LASTEXITCODE -ne 0) { throw 'pg_dump failed.' }
    docker cp ($container.Trim() + ':' + $dumpInContainer) (Join-Path $backupPath 'database.dump')
    if ($LASTEXITCODE -ne 0) { throw 'Could not copy database dump.' }
    $mediaHashes = @()
    if (Test-Path -LiteralPath $uploadsPath) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::CreateFromDirectory($uploadsPath, (Join-Path $backupPath 'uploads.zip'))
        $mediaHashes = @(Get-ChildItem -LiteralPath $uploadsPath -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
            [pscustomobject]@{ path = $_.FullName.Substring($uploadsPath.Length + 1).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
        })
    }
    $manifest = [ordered]@{
        version = 1
        createdAt = [DateTime]::UtcNow.ToString('o')
        database = $Database
        databaseSha256 = (Get-FileHash -LiteralPath (Join-Path $backupPath 'database.dump') -Algorithm SHA256).Hash
        media = $mediaHashes
        writersStopped = $true
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backupPath 'manifest.json') -Encoding UTF8
    Write-Output $backupPath
} finally { Pop-Location }
