param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$Publish,
    [switch]$SelfContained,
    [switch]$Smoke,
    [string]$ApiUrl = 'http://127.0.0.1:5080'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $taskRoot 'src/BotZone.Admin.WinForms/BotZone.Admin.WinForms.csproj'
$env:DOTNET_CLI_HOME = Join-Path $taskRoot '.dotnet-home'
Push-Location $taskRoot
try {
    & dotnet restore $project --locked-mode --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed. No runtime test was run.' }
    & dotnet build $project --configuration $Configuration --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'WinForms build failed.' }
    if ($Publish) {
        $destination = Join-Path $taskRoot 'artifacts/admin/win-x64'
        $self = if ($SelfContained) { 'true' } else { 'false' }
        & dotnet publish $project --configuration $Configuration --self-contained $self --output $destination --nologo
        if ($LASTEXITCODE -ne 0) { throw 'WinForms publish failed.' }
        Write-Host ('Published local application: ' + $destination)
    }
    if ($Smoke) {
        $executable = Join-Path $taskRoot ('src/BotZone.Admin.WinForms/bin/' + $Configuration + '/net10.0-windows/BotZone.Admin.WinForms.dll')
        $output = Join-Path $taskRoot ('artifacts/winforms/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        & dotnet $executable --smoke --api $ApiUrl --output $output
        if ($LASTEXITCODE -ne 0) { throw ('Native runtime smoke failed; inspect ' + (Join-Path $output 'smoke.json')) }
        Write-Host ('Native runtime smoke results: ' + $output)
    }
} finally { Pop-Location }
