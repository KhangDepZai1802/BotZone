#requires -Version 5.1
[CmdletBinding()]
param([switch]$Containers,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$runtimeRoot=Join-Path $projectRoot 'artifacts/local-runtime'
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
function Match-Process($Saved){
    $process=Get-Process -Id ([int]$Saved.processId) -ErrorAction SilentlyContinue
    if(-not $process -or [string]::IsNullOrWhiteSpace($process.Path) -or [string]::IsNullOrWhiteSpace($Saved.executable)){return $null}
    if($process.StartTime.ToUniversalTime().ToString('o') -ne $Saved.startTimeUtc -or $process.Path -ne $Saved.executable){return $null}
    return $process
}
Push-Location -LiteralPath $projectRoot
try{
    foreach($name in @('web','api')){
        $path=Join-Path $runtimeRoot ($name+'.json');if(-not(Test-Path -LiteralPath $path)){Write-Host "No tracked $name process; externally started processes are untouched.";continue}
        $record=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
        if($record.workspace -ne $projectRoot -or $record.service -ne $name){throw "State file $path does not belong to this workspace/service."}
        $savedProcesses=@($record.processes);$rootSaved=@($savedProcesses|Where-Object{$_.processId -eq $record.rootProcessId})
        if($rootSaved.Count -ne 1){throw "Missing or ambiguous tracked root for $name; no process was stopped."}
        $rootCreated=[DateTime]::Parse($rootSaved[0].startTimeUtc).ToUniversalTime()
        # Reject stale parent-PID records from older tracking versions before any stop.
        # Only the captured application executable and Windows console host are expected.
        $allowedExecutables=@($rootSaved[0].executable,(Join-Path $env:WINDIR 'System32/conhost.exe'))
        $savedProcesses=@($savedProcesses|Where-Object{[DateTime]::Parse($_.startTimeUtc).ToUniversalTime() -ge $rootCreated -and $_.executable -in $allowedExecutables})
        if($rootSaved.Count -eq 1 -and (Match-Process $rootSaved[0])){
            foreach($childId in [BotZoneRuntimeTreeV2]::Children([int]$record.rootProcessId)){
                if($savedProcesses.processId -contains $childId){continue}
                $child=Get-Process -Id $childId -ErrorAction SilentlyContinue
                if($child -and $child.Path -in $allowedExecutables -and $child.StartTime.ToUniversalTime() -ge $rootCreated){$savedProcesses+=[pscustomobject]@{processId=$child.Id;startTimeUtc=$child.StartTime.ToUniversalTime().ToString('o');executable=$child.Path}}
            }
        }
        # Stop the verified root first so development watchers cannot respawn a
        # child; then stop only captured descendants with matching creation times.
        foreach($saved in $savedProcesses){
            $process=Match-Process $saved;if(-not $process){Write-Host "Skipped exited/reused PID $($saved.processId).";continue}
            if($CheckOnly){Write-Host "Would stop verified $name PID $($process.Id), started $($saved.startTimeUtc)."}
            else{Stop-Process -Id $process.Id -Force -ErrorAction Stop;Write-Host "Stopped verified $name PID $($process.Id)."}
        }
        if(-not $CheckOnly){$record.status='stopped';[IO.File]::WriteAllText($path,($record|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))}
    }
    if($Containers){if($CheckOnly){Write-Host 'Would stop only compose services db and mailpit; volumes stay intact.'}else{& docker compose stop db mailpit;if($LASTEXITCODE -ne 0){throw 'Could not stop db/mailpit.'}}}
    Write-Host 'No volumes, database rows, uploads, or untracked processes were deleted.'
}finally{Pop-Location}
