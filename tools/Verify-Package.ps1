param([string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$PackageDirectory) { $PackageDirectory = Join-Path $repo 'work\packages' }
$PackageDirectory = [IO.Path]::GetFullPath($PackageDirectory)
[xml]$coreProject = Get-Content -LiteralPath (Join-Path $repo 'src/Bridge.Core/Bridge.Core.csproj')
$packageVersion = $coreProject.Project.PropertyGroup.Version
$package = Join-Path $PackageDirectory "Bridge.Core.$packageVersion.nupkg"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $names = @($zip.Entries | Select-Object -ExpandProperty FullName)
    foreach ($required in @('lib/netstandard2.1/Bridge.Core.dll','README.md','LICENSE','Bridge.Core.nuspec')) {
        if ($names -notcontains $required) { throw "Missing package file: $required" }
    }
    if ($names | Where-Object { $_ -match 'Newtonsoft.*\.dll|(^|/)(bin|obj|Library)/' }) { throw 'Unexpected package artifacts.' }
} finally { $zip.Dispose() }
$folder = Join-Path $repo 'work\package-smoke'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$localSource = [Security.SecurityElement]::Escape($PackageDirectory)
@"
<configuration>
  <packageSources><clear/><add key="local" value="$localSource"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources>
  <packageSourceMapping><packageSource key="local"><package pattern="Bridge.Core"/></packageSource><packageSource key="nuget"><package pattern="Newtonsoft.Json"/></packageSource></packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath "$folder\NuGet.Config" -Encoding utf8
@'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Bridge.Core" Version="[0.1.0]" /></ItemGroup></Project>
'@.Replace('0.1.0', $packageVersion) | Set-Content -LiteralPath "$folder\PackageSmoke.csproj" -Encoding utf8
@'
using System;
using Bridge.Core;
var document = BridgeDocument.Create("package");
document.AddBlock("a", "t", BridgeValue.Parse("{\"value\":1,\"secret\":2}"));
document.GetBlock("a").Set("value", 3);
if (!document.ContainsBlock("a") || !document.HasField("a", "value") || document.Get<int>("a", "value") != 3) throw new Exception("Convenience API missing.");
var registry = new BridgeSerializer(); document.UseSerializers(registry);
if (document.Query().WhereTypes("t", "other").Execute().Blocks.Count != 1) throw new Exception("Multi-type query missing.");
var restored = BridgeDocument.Parse(document.ToJson());
if (restored.GetBlock("a").Get<int>("value") != 3 || document.Query().SelectFields("value").Execute().ToJson().Contains("secret")) throw new Exception("Package smoke failed.");
Console.WriteLine("LOCAL_NUGET_PACKAGE_PASS");
'@ | Set-Content -LiteralPath "$folder\Program.cs" -Encoding utf8
$previousPackages = $env:NUGET_PACKAGES
$env:NUGET_PACKAGES = Join-Path $folder ('packages-' + [Guid]::NewGuid().ToString('N'))
Push-Location $folder
try {
    & dotnet restore PackageSmoke.csproj --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }
    & dotnet run --project PackageSmoke.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Package consumer failed.' }
} finally { Pop-Location; $env:NUGET_PACKAGES = $previousPackages }
