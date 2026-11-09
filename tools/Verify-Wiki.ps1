$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& "$PSScriptRoot/Sync-Wiki.ps1"
$preview=Join-Path $repo 'work/wiki-preview'
$pages=@(Get-ChildItem -LiteralPath $preview -Filter '*.md')
$count=@(Get-ChildItem -LiteralPath (Join-Path $repo 'docs/wiki') -Filter '*.md' -Recurse).Count
if($pages.Count -ne $count){throw 'Preview/source page count mismatch; inspect extra files.'}
if(-not (Test-Path -LiteralPath "$preview/Runtime-Home.md")){throw 'Runtime pages missing.'}
foreach($page in $pages) {
 foreach($match in [regex]::Matches((Get-Content -LiteralPath $page.FullName -Raw),'\[[^\]]+\]\(([^)]+)\)')) {
  $target=$match.Groups[1].Value.Trim('<','>');$uri=$null
  if($target.StartsWith('#') -or [Uri]::TryCreate($target,[UriKind]::Absolute,[ref]$uri)){continue}
  $target=($target -split '#',2)[0]
  if(-not (Test-Path -LiteralPath (Join-Path $preview ($target+'.md')))){throw "Broken generated Wiki link: $target"}
 }
}
$collision=Join-Path $preview 'Runtime-Home.md'
$original=Get-Content -LiteralPath $collision -Raw
try {
 Set-Content -LiteralPath $collision -Value 'Unmanaged verification page; do not overwrite.' -Encoding utf8
 $rejected=$false
 try {& "$PSScriptRoot/Sync-Wiki.ps1"}catch {if($_.Exception.Message -notmatch 'Unmanaged preview collision'){throw};$rejected=$true}
 if(-not $rejected -or (Get-Content -LiteralPath $collision -Raw) -notmatch '^Unmanaged verification page'){throw 'Unmanaged collision was not protected.'}
}finally {Set-Content -LiteralPath $collision -Value $original -Encoding utf8}
& "$PSScriptRoot/Sync-Wiki.ps1"
Write-Output "WIKI_SAFETY_PASS: $count pages, rewritten links, unmanaged collision rejected; no publish"
