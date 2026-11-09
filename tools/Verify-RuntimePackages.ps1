param([string]$PackageDirectory)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $PackageDirectory){$PackageDirectory=Join-Path $repo 'work/packages'}
$PackageDirectory=[IO.Path]::GetFullPath($PackageDirectory)
New-Item -ItemType Directory -Path $PackageDirectory -Force | Out-Null
foreach($name in @('Bridge.Core','Bridge.Runtime','Bridge.Transport.Mcp')) {
 dotnet pack "$repo/src/$name/$name.csproj" -c Release -o $PackageDirectory
 if($LASTEXITCODE -ne 0){throw "Pack failed: $name"}
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach($item in @(@{Name='Bridge.Runtime';Version='0.1.0';Tfm='netstandard2.1'},@{Name='Bridge.Transport.Mcp';Version='0.1.0';Tfm='net8.0'})) {
 $zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $PackageDirectory "$($item.Name).$($item.Version).nupkg"))
 try {
  $names=@($zip.Entries.FullName)
  foreach($required in @("lib/$($item.Tfm)/$($item.Name).dll",'README.md','LICENSE',"$($item.Name).nuspec")){if($names -notcontains $required){throw "Missing $required"}}
  if($item.Name -eq 'Bridge.Transport.Mcp' -and $names -notcontains 'THIRD-PARTY-NOTICES.md'){throw 'Missing dependency notices.'}
  if($names | Where-Object {$_ -match '(Newtonsoft|ModelContextProtocol|UnityEngine).*\.dll|(^|/)(bin|obj|Library)/'}){throw 'Vendored dependency/build artifacts in package.'}
  $reader=[IO.StreamReader]::new($zip.GetEntry("$($item.Name).nuspec").Open())
  try{[xml]$spec=$reader.ReadToEnd()}finally{$reader.Dispose()}
  if($spec.package.metadata.license.'#text' -ne 'MIT' -or $spec.package.metadata.license.type -ne 'expression'){throw 'Incorrect MIT metadata.'}
  $dependency=@($spec.package.metadata.dependencies.group.dependency | Where-Object id -eq 'Bridge.Core')
  if($item.Name -eq 'Bridge.Runtime' -and -not $dependency){throw 'Runtime Core dependency missing.'}
 } finally {$zip.Dispose()}
}
$folder=Join-Path $repo 'work/runtime-package-consumer'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$feed=[Security.SecurityElement]::Escape($PackageDirectory)
@"
<configuration><packageSources><clear/><add key="local" value="$feed"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="local"><package pattern="Bridge.*"/></packageSource><packageSource key="nuget"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
"@ | Set-Content -LiteralPath "$folder/NuGet.Config" -Encoding utf8
@'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><PackageReference Include="Bridge.Runtime" Version="[0.1.0]"/><PackageReference Include="Bridge.Transport.Mcp" Version="[0.1.0]"/></ItemGroup></Project>
'@ | Set-Content -LiteralPath "$folder/Consumer.csproj" -Encoding utf8
@'
using Bridge.Core;
using Bridge.Runtime;
using Bridge.Transport.Mcp;
var d=BridgeDocument.Create("private");d.AddBlock("settings","settings",BridgeValue.Parse("{\"speed\":1,\"privateNote\":\"closed\"}"));
using var host=BridgeHostBuilder.Create("consumer").AddCommands(new Commands()).AddDocument("config",d,BridgeDataPolicy.ReadOnly().AllowFields("settings","speed")).Build();
var sum=await host.ExecuteLocalAsync("math.add",BridgeValue.Parse("{\"a\":2,\"b\":3}"));
if(!sum.Success || sum.Data.As<int>()!=5)throw new Exception("Runtime package execution failed.");
var endpoint=new RuntimeMcpEndpoint(host,"consumer-reader",BridgePermission.Read);
var query=await endpoint.QueryAsync("config",new BridgeQuerySpec(),default);
if(query.Get<string>("status")!="Completed" || query.ToJson().Contains("privateNote"))throw new Exception("Packaged endpoint policy failed.");
await using var server=BridgeMcpServer.Create(new MemoryStream(),new MemoryStream(),endpoint);
Console.WriteLine("RUNTIME_NUGET_CONSUMER_PASS: Runtime+Core+MCP PackageReference; add=5; policy enforced; SDK loaded");
public sealed class Commands {[BridgeCommand("math.add",Permission=BridgePermission.Read)]public int Add(int a,int b)=>a+b;}
'@ | Set-Content -LiteralPath "$folder/Program.cs" -Encoding utf8
$oldPackages=$env:NUGET_PACKAGES
$env:NUGET_PACKAGES=Join-Path $folder ('packages-'+[Guid]::NewGuid().ToString('N'))
Push-Location $folder
try {
 dotnet restore Consumer.csproj --configfile NuGet.Config
 if($LASTEXITCODE -ne 0){throw 'Local package consumer restore failed.'}
 dotnet run --project Consumer.csproj -c Release --no-restore
 if($LASTEXITCODE -ne 0){throw 'Local package consumer failed.'}
}finally {Pop-Location;$env:NUGET_PACKAGES=$oldPackages}
