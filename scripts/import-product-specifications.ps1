#requires -Version 5.1
[CmdletBinding()]
param([switch]$Apply)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$savedRoot=$env:BOTZONE_ROOT
$savedDotnetHome=$env:DOTNET_CLI_HOME
Push-Location -LiteralPath $projectRoot
try {
    $env:BOTZONE_ROOT=$projectRoot
    $env:DOTNET_CLI_HOME=Join-Path $projectRoot '.dotnet-home'
    & dotnet build src/BotZone.Api/BotZone.Api.csproj --configuration Release --no-restore --nologo
    if($LASTEXITCODE -ne 0){throw 'C# specification importer build failed.'}
    $mode=if($Apply){'apply'}else{'plan'}
    & dotnet src/BotZone.Api/bin/Release/net10.0/BotZone.Api.dll --import-product-specifications $mode
    if($LASTEXITCODE -ne 0){throw 'Specification import failed. No reset or reseed was attempted.'}
} finally {
    $env:BOTZONE_ROOT=$savedRoot
    $env:DOTNET_CLI_HOME=$savedDotnetHome
    Pop-Location
}
