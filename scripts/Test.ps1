param([switch]$IncludeTray)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $root 'Air5-Control.exe'
if(!(Test-Path -LiteralPath $exe)){ throw 'Run Derle.ps1 first.' }
foreach($mode in @('--self-test','--preview')){
 $process=Start-Process -FilePath $exe -ArgumentList $mode -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0){throw "$mode failed: exit $($process.ExitCode)"}
}
$report=Join-Path $root 'self-test.txt'
if(!(Test-Path $report) -or !(Select-String -Path $report -Pattern '^PASS:' -Quiet)){throw 'Self-test PASS report missing.'}
Get-Content $report
if(!(Test-Path (Join-Path $root 'Onizleme.png'))){throw 'UI preview missing.'}
if($IncludeTray){
 $errorFile=Join-Path $root 'tray-test-error.txt'
 if(Test-Path $errorFile){Remove-Item -LiteralPath $errorFile}
 $process=Start-Process -FilePath $exe -ArgumentList '--tray-test' -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0 -or (Test-Path $errorFile)){throw 'Tray test failed.'}
 $trayReport=Join-Path $root 'tray-test.txt'
 if(!(Test-Path $trayReport) -or !(Select-String -Path $trayReport -Pattern '^PASS:' -Quiet)){throw 'Tray PASS report missing.'}
 Get-Content $trayReport
}
'Build and automated checks passed. No hardware validation implied.'
