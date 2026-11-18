#requires -Version 5.1
[CmdletBinding()]
param([switch]$Development,[switch]$Production,[switch]$Migrate,[switch]$Seed,[switch]$InstallDependencies,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$runtimeRoot=Join-Path $projectRoot 'artifacts/local-runtime'
$backendFile=Join-Path $projectRoot 'config/backend.local.json'
if($Development -and $Production){throw 'Choose Development or Production, not both. Default is Development.'}

# Toolhelp reads only parent/child process IDs. PID, creation time and executable
# are recorded independently; stop-local never kills by name or port alone.
if(-not ('BotZoneRuntimeTreeV2' -as [type])){
Add-Type @'
using System;using System.Collections.Generic;using System.Runtime.InteropServices;
public static class BotZoneRuntimeTreeV2{
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Entry{public uint size,usage,id;public IntPtr heap;public uint module,threads,parent;public int priority;public uint flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string name;}
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint process);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Process32FirstW(IntPtr h,ref Entry e);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Process32NextW(IntPtr h,ref Entry e);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
 public static int[] Children(int root){var parents=new Dictionary<int,int>();var h=CreateToolhelp32Snapshot(2,0);if(h==new IntPtr(-1))throw new System.ComponentModel.Win32Exception();try{var e=new Entry();e.size=(uint)Marshal.SizeOf(typeof(Entry));if(Process32FirstW(h,ref e))do{parents[(int)e.id]=(int)e.parent;}while(Process32NextW(h,ref e));}finally{CloseHandle(h);}var found=new List<int>{root};var starts=new Dictionary<int,DateTime>();try{using(var process=System.Diagnostics.Process.GetProcessById(root)){starts[root]=process.StartTime.ToUniversalTime();}}catch{return new int[0];}for(int n=0;n<found.Count;n++)foreach(var pair in parents)if(pair.Value==found[n]&&!found.Contains(pair.Key)){try{using(var child=System.Diagnostics.Process.GetProcessById(pair.Key)){var created=child.StartTime.ToUniversalTime();if(created>=starts[found[n]]){found.Add(pair.Key);starts[pair.Key]=created;}}}catch{}}return found.ToArray();}
}
'@
}
function Test-LocalPort([int]$Port){$socket=New-Object Net.Sockets.TcpClient;try{$task=$socket.ConnectAsync('127.0.0.1',$Port);if(-not $task.Wait(350)){return $false};return $socket.Connected}catch{return $false}finally{$socket.Dispose()}}
function Test-Api{try{$health=Invoke-RestMethod -Uri 'http://127.0.0.1:5080/health' -TimeoutSec 3;return $health.status -eq 'ready' -and $health.backend -eq 'ASP.NET Core'}catch{return $false}}
function Test-Web{try{$page=Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:3000/vi' -TimeoutSec 5;return $page.StatusCode -eq 200 -and $page.Content.Contains('BotZone')}catch{return $false}}
function Process-Record([int]$ProcessId){$p=Get-Process -Id $ProcessId -ErrorAction Stop;if([string]::IsNullOrWhiteSpace($p.Path)){throw "Cannot verify executable of PID $ProcessId."};return [pscustomobject]@{processId=$p.Id;startTimeUtc=$p.StartTime.ToUniversalTime().ToString('o');executable=$p.Path}}
function Save-Record($Record){$path=Join-Path $runtimeRoot ($Record.service+'.json');[IO.File]::WriteAllText($path,($Record|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))}
function Start-Tracked([string]$Service,[string]$Executable,[string[]]$Arguments,[int]$Port,[hashtable]$Variables){
    $stamp=Get-Date -Format 'yyyyMMdd-HHmmss-fff';$out=Join-Path $runtimeRoot ($Service+'-'+$stamp+'.stdout.log');$err=Join-Path $runtimeRoot ($Service+'-'+$stamp+'.stderr.log')
    $saved=@{}
    # Backend credentials remain in config/backend.local.json. Inherited legacy
    # credentials must not reach the storefront, or override the backend target.
    $names=@([Environment]::GetEnvironmentVariables('Process').Keys|Where-Object{$_ -match '^(DATABASE_URL|MIGRATION_DATABASE_URL|PGPASSWORD|PGPASSFILE|PGHOST|PGUSER|PGDATABASE|BETTER_AUTH_.*|SMTP_.*|UPLOAD_DIR|ConnectionStrings__.*|BOTZONE_.*)$'})
    foreach($name in ($names+@($Variables.Keys)|Select-Object -Unique)){$saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process');[Environment]::SetEnvironmentVariable($name,$null,'Process')}
    try{
        foreach($name in $Variables.Keys){[Environment]::SetEnvironmentVariable($name,[string]$Variables[$name],'Process')}
        $parameters=@{FilePath=$Executable;WorkingDirectory=$projectRoot;WindowStyle='Hidden';PassThru=$true;RedirectStandardOutput=$out;RedirectStandardError=$err}
        if($Arguments -and $Arguments.Count -gt 0){$parameters.ArgumentList=$Arguments}
        $process=Start-Process @parameters
    }finally{foreach($name in $saved.Keys){[Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')}}
    $record=[pscustomobject]@{service=$Service;workspace=$projectRoot;port=$Port;status='starting';rootProcessId=$process.Id;processes=@(Process-Record $process.Id);stdout=$out;stderr=$err;startedAtUtc=[DateTime]::UtcNow.ToString('o')};Save-Record $record
    $deadline=[DateTime]::UtcNow.AddSeconds(90);$ready=$false
    do{
        if(-not(Get-Process -Id $process.Id -ErrorAction SilentlyContinue)){throw "$Service exited. Inspect $err"}
        $ready=if($Service -eq 'api'){Test-Api}else{Test-Web};if(-not $ready){Start-Sleep -Milliseconds 500}
    }while(-not $ready -and [DateTime]::UtcNow -lt $deadline)
    $records=@();foreach($childId in [BotZoneRuntimeTreeV2]::Children($process.Id)){try{$candidate=Process-Record $childId;if([DateTime]::Parse($candidate.startTimeUtc).ToUniversalTime() -ge $process.StartTime.ToUniversalTime() -and $candidate.executable -in @($Executable,(Join-Path $env:WINDIR 'System32/conhost.exe'))){$records+=$candidate}}catch{}}
    $record.processes=$records;$record.status=if($ready){'running'}else{'unhealthy'};Save-Record $record
    if(-not $ready){throw "$Service did not become ready. It remains tracked; inspect $err and use stop-local.ps1 if needed."}
    Write-Host "$Service ready on 127.0.0.1:$Port; PID $($process.Id); logs $runtimeRoot"
}
function Assert-WebEnvironment{
    foreach($file in Get-ChildItem -LiteralPath $projectRoot -File -Filter '.env*'){
        foreach($line in [IO.File]::ReadAllLines($file.FullName)){
            if($line -match '^\s*(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*='){
                $name=$Matches[1];if($name -notin @('BOTZONE_API_URL','NEXT_TELEMETRY_DISABLED')){throw "Storefront env $($file.Name) contains '$name'. Preserve backend configuration in config/backend.local.json; do not pass it to Next.js."}
            }
        }
    }
}
function Invoke-StorefrontNpm([string[]]$NpmArguments){
    $saved=@{};$names=@([Environment]::GetEnvironmentVariables('Process').Keys|Where-Object{$_ -match '^(DATABASE_URL|MIGRATION_DATABASE_URL|PGPASSWORD|PGPASSFILE|PGHOST|PGUSER|PGDATABASE|BETTER_AUTH_.*|SMTP_.*|UPLOAD_DIR|ConnectionStrings__.*|BOTZONE_.*)$'})+@('BOTZONE_API_URL','NEXT_TELEMETRY_DISABLED')
    foreach($name in ($names|Select-Object -Unique)){$saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process');[Environment]::SetEnvironmentVariable($name,$null,'Process')}
    try{$env:BOTZONE_API_URL='http://127.0.0.1:5080';$env:NEXT_TELEMETRY_DISABLED='1';& npm.cmd @NpmArguments;if($LASTEXITCODE -ne 0){throw ('npm failed: '+($NpmArguments -join ' '))}}finally{foreach($name in $saved.Keys){[Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')}}
}
Push-Location -LiteralPath $projectRoot
try{
    foreach($command in @('docker','dotnet','node','npm.cmd')){Get-Command $command -ErrorAction Stop|Out-Null}
    Assert-WebEnvironment
    $apiBusy=Test-LocalPort 5080;$webBusy=Test-LocalPort 3000
    if(($Migrate -or $Seed) -and $apiBusy){throw 'Migration/seed requires the API worker to be stopped first. This script will not stop an existing process.'}
    if($InstallDependencies -and $webBusy){throw 'Stop the storefront before replacing node_modules.'}
    if($Migrate -and -not $env:BOTZONE_MIGRATION_CONNECTION){throw 'Set BOTZONE_MIGRATION_CONNECTION for the EF owner; it is never copied into Next.js env.'}
    & docker info --format '{{.ServerVersion}}';if($LASTEXITCODE -ne 0){throw 'Start Docker Desktop in Linux-container mode and wait for its engine.'}
    & docker compose config --quiet;if($LASTEXITCODE -ne 0){throw 'Invalid compose.yaml.'}
    if($CheckOnly){Write-Host "Read-only check: API port busy=$apiBusy; web port busy=$webBusy; backend config exists=$(Test-Path -LiteralPath $backendFile). No processes, containers or database data changed.";return}
    if(-not(Test-Path -LiteralPath $backendFile)){Copy-Item -LiteralPath 'config/backend.local.example.json' -Destination $backendFile}
    if(-not(Test-Path -LiteralPath '.env')){Copy-Item -LiteralPath '.env.example' -Destination '.env'}
    New-Item -ItemType Directory -Path $runtimeRoot -Force|Out-Null
    & docker compose up -d db mailpit bootstrap;if($LASTEXITCODE -ne 0){throw 'Infrastructure startup failed.'}
    & docker compose wait bootstrap;if($LASTEXITCODE -ne 0){throw 'Database role bootstrap failed.'}
    $env:DOTNET_CLI_HOME=Join-Path $projectRoot '.dotnet-home'
    if($Migrate){& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'migrate-ef.ps1');if($LASTEXITCODE -ne 0){throw 'EF migration failed; API was not started.'}}
    if(-not $apiBusy -or $Seed){& dotnet build 'src/BotZone.Api/BotZone.Api.csproj' --configuration Debug --nologo;if($LASTEXITCODE -ne 0){throw 'API build failed.'}}
    $apiExecutable=Join-Path $projectRoot 'src/BotZone.Api/bin/Debug/net10.0/BotZone.Api.exe'
    if($Seed){
        $seedSaved=@{};$seedNames=@([Environment]::GetEnvironmentVariables('Process').Keys|Where-Object{$_ -match '^(ConnectionStrings__.*|BOTZONE_.*)$'})+@('BOTZONE_ROOT')
        foreach($name in ($seedNames|Select-Object -Unique)){$seedSaved[$name]=[Environment]::GetEnvironmentVariable($name,'Process');[Environment]::SetEnvironmentVariable($name,$null,'Process')}
        try{$env:BOTZONE_ROOT=$projectRoot;& $apiExecutable --seed demo;if($LASTEXITCODE -ne 0){throw 'Explicit .NET demo seed failed.'}}finally{foreach($name in $seedSaved.Keys){[Environment]::SetEnvironmentVariable($name,$seedSaved[$name],'Process')}}
    }
    if($apiBusy){if(-not(Test-Api)){throw 'Port 5080 belongs to an unrecognized or unready service; it was left untouched.'};Write-Host 'Reusing existing API on 5080; no ownership claimed.'}
    else{Start-Tracked 'api' $apiExecutable @() 5080 @{BOTZONE_ROOT=$projectRoot;ASPNETCORE_URLS='http://127.0.0.1:5080'}}
    if($webBusy){if(-not(Test-Web)){throw 'Port 3000 is occupied by a service that did not pass the BotZone check; it was left untouched.'};Write-Host 'Reusing existing storefront on 3000; no ownership claimed.'}
    else{
        if($InstallDependencies -or -not(Test-Path -LiteralPath 'node_modules/next/dist/bin/next')){Invoke-StorefrontNpm @('ci','--no-audit','--no-fund')}
        if($Production){Invoke-StorefrontNpm @('run','build');$arguments=@('.next/standalone/server.js')}
        else{$arguments=@('node_modules/next/dist/bin/next','dev','--hostname','127.0.0.1','--port','3000')}
        $nodePath=(Get-Command node.exe -ErrorAction Stop).Source
        Start-Tracked 'web' $nodePath $arguments 3000 @{BOTZONE_API_URL='http://127.0.0.1:5080';NEXT_TELEMETRY_DISABLED='1';NODE_ENV=$(if($Production){'production'}else{'development'});HOSTNAME='127.0.0.1';PORT='3000'}
    }
    Write-Host 'Storefront http://localhost:3000 | API http://127.0.0.1:5080 | Mailpit http://localhost:8025'
    Write-Host 'Native admin: artifacts/admin/win-x64/BotZone.Admin.WinForms.exe (build-admin.ps1 -Publish). No Node database worker or migration runs.'
}finally{Pop-Location}
