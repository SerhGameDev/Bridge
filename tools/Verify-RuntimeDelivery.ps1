param([string]$CoreBaseline)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $repo
try {
 $files=@(& git ls-files)
 if($LASTEXITCODE -ne 0){throw 'Cannot inspect tracked files.'}
 $bad=@($files | Where-Object {$_ -match '(?i)(^|/)(bin|obj|Library|Logs|Temp|work)/|\.(dll|exe|pdb|nupkg|zip|trx)$'})
 if($bad.Count){throw "Unexpected tracked binary/build artifacts: $($bad -join ', ')"}
 $secret='(?:gh[pousr]_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{50,}|sk-[A-Za-z0-9]{32,}|-----BEGIN [A-Z ]*PRIVATE KEY-----)'
 $literalToken='BRIDGE_(?:PLAYER|READER)_TOKEN\s*=\s*["''][A-Za-z0-9+/_-]{32,}["'']'
 foreach($file in $files) {
  $text=[IO.File]::ReadAllText((Join-Path $repo $file))
  if($text -match $secret -or $text -match $literalToken){throw "Possible credential in tracked file: $file (value not printed)."}
 }
 foreach($name in @('core','runtime')) {
  $folder=Join-Path $repo "Packages/com.serhgamedev.bridge.$name/Runtime"
  $assembly=Get-Content -LiteralPath "$folder/Bridge.$((Get-Culture).TextInfo.ToTitleCase($name)).asmdef" -Raw | ConvertFrom-Json
  if(-not $assembly.noEngineReferences){throw "Engine reference boundary changed: $name"}
  foreach($source in Get-ChildItem -LiteralPath $folder -Filter '*.cs') {
   if((Get-Content -LiteralPath $source.FullName -Raw) -match '(?m)^\s*using\s+(UnityEngine|UnityEditor|ModelContextProtocol|Microsoft\.AspNetCore)(\.|;)'){throw "Forbidden dependency: $($source.Name)"}
  }
 }
 if($CoreBaseline) {
  & git cat-file -e "$CoreBaseline`^{commit}"
  if($LASTEXITCODE -ne 0){throw 'Core baseline commit unavailable.'}
  $changed=@(& git diff --name-only $CoreBaseline HEAD -- 'Packages/com.serhgamedev.bridge.core/Runtime/*.cs' 'src/Bridge.Core/Bridge.Core.csproj')
  if($LASTEXITCODE -ne 0 -or $changed.Count){throw "Core implementation changed from accepted baseline: $($changed -join ', ')"}
 }
 Write-Output "DELIVERY_SCAN_PASS: $($files.Count) tracked files; no binary/build artifacts or credential patterns; Core/Runtime boundaries intact"
}finally {Pop-Location}
