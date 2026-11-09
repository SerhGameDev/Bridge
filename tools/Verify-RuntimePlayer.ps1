param([ValidateSet('Smoke','IPC')][string]$Mode='Smoke',[string]$Player)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $Player){$Player=Join-Path $repo 'work/runtime-unity/windows/Bridge.Runtime.Demo.exe'}
$log=Join-Path $repo "work/runtime-unity/player-$Mode.log"
if(Test-Path -LiteralPath $log){Remove-Item -LiteralPath $log}
$oldEditor=$env:BRIDGE_PLAYER_TOKEN;$oldReader=$env:BRIDGE_READER_TOKEN
$run=$null
try {
 $env:BRIDGE_PLAYER_TOKEN=[Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N')
 $env:BRIDGE_READER_TOKEN=[Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N')
 $playerArgs=@('-batchmode','-nographics','-logFile',"`"$log`"")
 if($Mode -eq 'Smoke'){$playerArgs+=@('--bridge-self-test','--bridge-exit-after-smoke')}else{$playerArgs+='--bridge-listen'}
 $run=Start-Process -FilePath $Player -ArgumentList $playerArgs -WindowStyle Hidden -PassThru
 if($Mode -eq 'Smoke') {
  if(-not $run.WaitForExit(45000)){throw 'Player smoke timeout.'}
  if($run.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -Pattern 'BRIDGE_RUNTIME_PLAYER_PASS' -Quiet)){throw "Player failed; see $log"}
  Write-Output 'PLAYER_RUNTIME_PASS: real Windows Mono Player, owner/enum/async DTO/typed'
 } else {
  $ready=$false
  for($i=0;$i -lt 60;$i++) {
   if($run.HasExited){throw "Player exited; see $log"}
   $probe=[Net.Sockets.TcpClient]::new()
   try {$probe.Connect('127.0.0.1',19731);$ready=$true;break}catch{Start-Sleep -Milliseconds 250}finally{$probe.Dispose()}
  }
  if(-not $ready){throw 'Player IPC startup timeout.'}
  dotnet run --project "$repo/tools/Bridge.Player.Smoke" -c Release
  if($LASTEXITCODE -ne 0){throw 'Player IPC verification failed.'}
 }
} finally {
 if($run -and -not $run.HasExited){Stop-Process -Id $run.Id}
 $env:BRIDGE_PLAYER_TOKEN=$oldEditor;$env:BRIDGE_READER_TOKEN=$oldReader
}
