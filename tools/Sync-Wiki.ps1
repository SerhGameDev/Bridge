param([switch]$Publish)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$remote = 'https://github.com/SerhGameDev/Bridge.wiki.git'
if (!$Publish) { Write-Output 'Dry run: docs/wiki is the source. Run with -Publish after the first Wiki page exists.'; exit 0 }
& git ls-remote $remote | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Wiki Git storage is not initialized. Create a first page in GitHub Wiki UI, then rerun. No pages were published.' }
$folder = Join-Path $repo 'work/wiki-sync'
if (Test-Path -LiteralPath $folder) { throw "Use a fresh sync checkout: $folder already exists; inspect it before renaming/removing it." }
& git clone $remote $folder
if ($LASTEXITCODE -ne 0) { throw 'Wiki clone failed.' }
$names = @()
foreach ($source in Get-ChildItem (Join-Path $repo 'docs/wiki') -Filter '*.md') {
    $destination = Join-Path $folder $source.Name
    if ((Test-Path -LiteralPath $destination) -and (Get-Content -LiteralPath $destination -Raw) -notmatch '<!-- Bridge.Core managed wiki source -->') {
        throw "Unmanaged Wiki page collision: $($source.Name). Reconcile manually; nothing has been pushed."
    }
    $text = Get-Content -LiteralPath $source.FullName -Raw
    $text = [regex]::Replace($text, '\]\(\.\./([^)#]+\.md)\)', '](' + 'https://github.com/SerhGameDev/Bridge/blob/feature/core-v0.2-developer-experience/docs/$1)')
    $text = [regex]::Replace($text, '\]\(([A-Za-z0-9_-]+)\.md\)', ']($1)')
    Set-Content -LiteralPath $destination -Value $text -Encoding utf8
    $names += $source.Name
}
Push-Location $folder
try {
    & git add -- $names
    if ($LASTEXITCODE -ne 0) { throw 'Wiki staging failed.' }
    & git diff --cached --quiet
    if ($LASTEXITCODE -eq 0) { Write-Output 'Wiki already synchronized.'; exit 0 }
    & git commit -m 'docs(wiki): synchronize Bridge.Core guides'
    if ($LASTEXITCODE -ne 0) { throw 'Wiki commit failed.' }
    & git push origin HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Wiki push failed; inspect checkout. No force push was attempted.' }
} finally { Pop-Location }
