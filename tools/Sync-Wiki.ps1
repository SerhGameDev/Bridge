param([switch]$Publish)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceRoot=Join-Path $repo 'docs/wiki'
$preview=Join-Path $repo 'work/wiki-preview'
$marker='<!-- Bridge managed wiki source -->'
$managedPattern='<!-- Bridge(?:\.Core)? managed wiki source -->'
$map=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
$names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$sources=@(Get-ChildItem -LiteralPath $sourceRoot -Filter '*.md' -Recurse | Sort-Object FullName)
foreach($source in $sources) {
 $relative=[IO.Path]::GetRelativePath($sourceRoot,$source.FullName).Replace('\','/')
 $page=$relative.Substring(0,$relative.Length-3).Replace('runtime/','Runtime-').Replace('/','-')
 if(-not $names.Add($page+'.md')){throw "Wiki page name collision: $page"}
 $map[$source.FullName]=$page
}
New-Item -ItemType Directory -Path $preview -Force | Out-Null
foreach($source in $sources) {
 $destination=Join-Path $preview ($map[$source.FullName]+'.md')
 if((Test-Path -LiteralPath $destination) -and (Get-Content -LiteralPath $destination -Raw) -notmatch $managedPattern){throw "Unmanaged preview collision: $destination"}
 $text=Get-Content -LiteralPath $source.FullName -Raw
 if($text -notmatch $managedPattern){$text=$marker+"`n"+$text}
 $sourceDirectory=$source.DirectoryName
 $text=[regex]::Replace($text,'\]\(([^)]+)\)',[System.Text.RegularExpressions.MatchEvaluator]{param($match)
  $target=$match.Groups[1].Value.Trim('<','>')
  $uri=$null
  if($target.StartsWith('#') -or [Uri]::TryCreate($target,[UriKind]::Absolute,[ref]$uri)){return $match.Value}
  $parts=$target -split '#',2
  $resolved=[IO.Path]::GetFullPath((Join-Path $sourceDirectory ([Uri]::UnescapeDataString($parts[0]))))
  $anchor=if($parts.Count -gt 1){'#'+$parts[1]}else{''}
  if($map.ContainsKey($resolved)){return ']('+$map[$resolved]+$anchor+')'}
  if(-not (Test-Path -LiteralPath $resolved)){throw "Broken Wiki source link: $($source.FullName) -> $target"}
  $relative=[IO.Path]::GetRelativePath($repo,$resolved).Replace('\','/')
  if($relative.StartsWith('../')){throw 'Wiki link leaves repository.'}
  $encoded=($relative.Split('/') | ForEach-Object {[Uri]::EscapeDataString($_)}) -join '/'
  $kind=if(Test-Path -LiteralPath $resolved -PathType Container){'tree'}else{'blob'}
  return '](https://github.com/SerhGameDev/Bridge/'+$kind+'/main/'+$encoded+$anchor+')'
 })
 Set-Content -LiteralPath $destination -Value $text -Encoding utf8
}
Write-Output "WIKI_PREVIEW_PASS: $($sources.Count) managed pages; $preview"
if(-not $Publish){Write-Output 'Dry run only. No Wiki pages were published.';return}
$remote='https://github.com/SerhGameDev/Bridge.wiki.git'
& git ls-remote $remote | Out-Null
if($LASTEXITCODE -ne 0){throw 'Wiki Git storage is not initialized. docs/wiki fallback and local preview are complete; create a managed first Home page, then rerun. Nothing pushed.'}
$folder=Join-Path $repo 'work/wiki-sync'
if(Test-Path -LiteralPath $folder){throw "Inspect existing sync checkout before reuse: $folder. Nothing pushed."}
& git clone $remote $folder
if($LASTEXITCODE -ne 0){throw 'Wiki clone failed.'}
foreach($name in $names){
 $target=Join-Path $folder $name
 if((Test-Path -LiteralPath $target) -and (Get-Content -LiteralPath $target -Raw) -notmatch $managedPattern){throw "Unmanaged Wiki collision: $name. Nothing pushed."}
}
foreach($name in $names){Copy-Item -LiteralPath (Join-Path $preview $name) -Destination (Join-Path $folder $name)}
Push-Location $folder
try {
 & git add -- @($names)
 if($LASTEXITCODE -ne 0){throw 'Wiki staging failed.'}
 & git diff --cached --quiet
 if($LASTEXITCODE -eq 0){Write-Output 'Wiki already synchronized.';return}
 & git commit -m 'docs(wiki): synchronize Core and Runtime guides'
 if($LASTEXITCODE -ne 0){throw 'Wiki commit failed.'}
 & git push origin HEAD
 if($LASTEXITCODE -ne 0){throw 'Wiki push failed; inspect checkout. No force push attempted.'}
}finally {Pop-Location}
