[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$BackupPath,
    [ValidateSet('botzone_restore','botzone_baseline_test')][string]$DatabasePrefix = 'botzone_restore'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$backupRoot = Join-Path $projectRoot 'backups'
$resolvedBackup = [IO.Path]::GetFullPath($BackupPath)
if (-not $resolvedBackup.StartsWith($backupRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Restore accepts a BotZone backup directory inside this workspace/backups only.'
}
$manifest = Get-Content -LiteralPath (Join-Path $resolvedBackup 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$dump = Join-Path $resolvedBackup 'database.dump'
if ((Get-FileHash -LiteralPath $dump -Algorithm SHA256).Hash -ne $manifest.databaseSha256) { throw 'Database backup checksum mismatch.' }
$suffix = (Get-Date -Format 'yyyyMMddHHmmss') + '_' + [guid]::NewGuid().ToString('N').Substring(0,8)
$restoreDatabase = $DatabasePrefix + '_' + $suffix
$restoreDirectory = Join-Path $backupRoot ('restore-' + $suffix)
Push-Location -LiteralPath $projectRoot
try {
    $container = docker compose ps -q db
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($container)) { throw 'PostgreSQL container must be running.' }
    New-Item -ItemType Directory -Path $restoreDirectory | Out-Null
    $containerDump = '/tmp/' + $restoreDatabase + '.dump'
    docker cp $dump ($container.Trim() + ':' + $containerDump)
    if ($LASTEXITCODE -ne 0) { throw 'Could not copy backup into container.' }
    docker compose exec -T db createdb -U postgres -O botzone $restoreDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Could not create a new restore database; no existing database has been modified.' }
    docker compose exec -T db pg_restore -U postgres -d $restoreDatabase --exit-on-error $containerDump
    if ($LASTEXITCODE -ne 0) { throw ('Restore failed. Inspect the isolated database: ' + $restoreDatabase) }
    $restoredUploads = Join-Path $restoreDirectory 'uploads'
    $archive = Join-Path $resolvedBackup 'uploads.zip'
    if (Test-Path -LiteralPath $archive) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [IO.Compression.ZipFile]::OpenRead($archive)
        try {
            foreach ($entry in $zip.Entries) {
                $entryTarget = [IO.Path]::GetFullPath((Join-Path $restoredUploads $entry.FullName))
                if (-not $entryTarget.StartsWith($restoredUploads + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe archive path.' }
            }
        } finally { $zip.Dispose() }
        [IO.Compression.ZipFile]::ExtractToDirectory($archive, $restoredUploads)
        foreach ($media in $manifest.media) {
            $mediaPath = Join-Path $restoredUploads $media.path
            if ((Get-FileHash -LiteralPath $mediaPath -Algorithm SHA256).Hash -ne $media.sha256) { throw ('Media checksum mismatch: ' + $media.path) }
        }
    }
    [pscustomobject]@{
        database = $restoreDatabase
        databaseUrl = ('postgres://botzone_app:botzone_app_local@127.0.0.1:54329/' + $restoreDatabase)
        uploadsDirectory = $restoredUploads
        originalDatabaseUnchanged = $true
    } | ConvertTo-Json
} finally { Pop-Location }
