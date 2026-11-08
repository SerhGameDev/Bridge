$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$files = @((Join-Path $repo 'README.md'), (Join-Path $repo 'README.ru.md')) + @(Get-ChildItem (Join-Path $repo 'docs') -Filter '*.md' | Select-Object -ExpandProperty FullName)
$results = @()
$index = 0
foreach ($file in $files) {
    foreach ($snippet in [regex]::Matches((Get-Content -LiteralPath $file -Raw), '(?ms)^```csharp\s*\r?\n(.*?)^```\s*$')) {
        $index++
        $folder = Join-Path $repo "work\snippets\$index"
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        $reference = [Security.SecurityElement]::Escape((Join-Path $repo 'src\Bridge.Core\Bridge.Core.csproj'))
        "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include=`"$reference`" /></ItemGroup></Project>" | Set-Content -LiteralPath "$folder\Snippet.csproj" -Encoding utf8
        $snippet.Groups[1].Value | Set-Content -LiteralPath "$folder\Program.cs" -Encoding utf8
        Push-Location $folder
        try { & dotnet run --project Snippet.csproj -c Release; $code = $LASTEXITCODE }
        finally { Pop-Location }
        $results += [pscustomobject]@{ File = $file; Snippet = $index; ExitCode = $code }
        if ($code -ne 0) { throw "Documentation snippet $index failed: $file" }
    }
}
if ($index -eq 0) { throw 'No C# snippets were found.' }
$results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $repo 'work\snippets\results.json') -Encoding utf8
Write-Output "Documentation snippets PASS: $index"
