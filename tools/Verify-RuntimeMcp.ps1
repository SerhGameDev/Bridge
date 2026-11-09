param([string]$Player)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $Player){$Player=Join-Path $repo 'work/runtime-unity/windows/Bridge.Runtime.Demo.exe'}
dotnet build "$repo/tools/Bridge.Mcp.PlayerE2E" -c Release
if($LASTEXITCODE -ne 0){throw 'MCP client build failed.'}
$client=Join-Path $repo 'tools/Bridge.Mcp.PlayerE2E/bin/Release/net8.0/Bridge.Mcp.PlayerE2E.dll'
$gateway=Join-Path $repo 'samples/Bridge.Runtime.Mcp.Sample/bin/Release/net8.0/Bridge.Runtime.Mcp.Sample.dll'
$log=Join-Path $repo 'work/runtime-unity/player-MCP.log'
if(Test-Path -LiteralPath $log){Remove-Item -LiteralPath $log}
$oldEditor=$env:BRIDGE_PLAYER_TOKEN;$oldReader=$env:BRIDGE_READER_TOKEN;$run=$null
try {
 $env:BRIDGE_PLAYER_TOKEN=[Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N')
 $env:BRIDGE_READER_TOKEN=[Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N')
 $run=Start-Process -FilePath $Player -ArgumentList @('-batchmode','-nographics','--bridge-listen','-logFile',"`"$log`"") -WindowStyle Hidden -PassThru
 $ready=$false
 for($i=0;$i -lt 60;$i++) {
  if($run.HasExited){throw "Player exited; see $log"}
  $probe=[Net.Sockets.TcpClient]::new()
  try {$probe.Connect('127.0.0.1',19731);$ready=$true;break}catch{Start-Sleep -Milliseconds 250}finally{$probe.Dispose()}
 }
 if(-not $ready){throw 'Player startup timeout.'}
 $clientDir=Join-Path $repo 'work/mcp-e2e-client';New-Item -ItemType Directory -Path $clientDir -Force | Out-Null
 Push-Location $clientDir
 try {dotnet $client --gateway $gateway | Tee-Object -FilePath (Join-Path $repo 'work/runtime-unity/mcp-e2e-result.log');if($LASTEXITCODE -ne 0){throw 'MCP Player E2E failed.'}}finally{Pop-Location}
} finally {
 if($run -and -not $run.HasExited){Stop-Process -Id $run.Id}
 $env:BRIDGE_PLAYER_TOKEN=$oldEditor;$env:BRIDGE_READER_TOKEN=$oldReader
}
