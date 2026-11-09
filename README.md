# Bridge: Core and Runtime

Core0.2 provides versioned JSON block documents, references and bounded queries. Runtime0.1 adds explicitly registered C# commands, discovery and policy-bound data access. Both target netstandard2.1 with no UnityEngine/MCP dependency. MIT. Unity adapter and the .NET MCP gateway are separate packages.

**[Runtime guide](docs/wiki/runtime/Home.md)** · [Core English tutorial](docs/wiki/Getting-Started-English.md) · [Русский](README.ru.md) · [Runtime API](docs/wiki/runtime/API-Reference.md)

## First command: no Unity, MCP or file

Create a .NET8 console project and reference `src/Bridge.Runtime/Bridge.Runtime.csproj` ([steps](docs/wiki/runtime/Installation-NET.md)). Complete Program.cs:

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var host = BridgeHostBuilder.Create("demo").AddCommands(new MathCommands()).Build();
var result = await host.ExecuteLocalAsync("math.add", BridgeValue.Parse("{\"a\":2,\"b\":3}"));
if (!result.Success) throw new Exception(result.Error!.Code);
Console.WriteLine("sum=" + result.Data.As<int>());
public sealed class MathCommands
{
    [BridgeCommand("math.add", Permission=BridgePermission.Read)]
    public int Add(int a, int b) => a + b;
}
```

Expected sum=5. ExecuteLocalAsync is an explicitly trusted Admin path. External clients require a trusted authenticated context and explicit data policies. Build opens no listener. [Security/default deny](docs/wiki/runtime/Security.md).

## Packages and examples

- Bridge.Core0.2: JSON document/block/field, serializers, references and QuerySpec. Public Core API and bridgeVersion=1 remain compatible.
- Bridge.Runtime0.1: host, command attributes/schemas, policy-filtered discovery/query and async executor.
- com.serhgamedev.bridge.unity0.1: owner-thread dispatcher and importable Cube Scene Demo.
- Bridge.Transport.Mcp0.1: separate .NET8 stdio gateway using official SDK2.4.0.

Packages, tags and GitHub Releases have **not** been published. Use checkout ProjectReference, [verified local NuGet packages](docs/wiki/runtime/Installation-NET.md), or [Git UPM installation](docs/wiki/runtime/Installation-Unity.md).

Run `dotnet run --project samples/Bridge.Runtime.Console.Sample -c Release`. [Unity guide](docs/runtime/unity.md) covers actual prefab/scene creation, Windows Mono and WebGL IL2CPP High. [Built Player → MCP → Codex](docs/runtime/mcp.md) includes publish/start/environment/TOML instructions and a real official-SDK compatible-client E2E. A personal Codex connected session is not claimed. [Recipes](docs/wiki/runtime/Cookbook.md) · [Troubleshooting](docs/wiki/runtime/Troubleshooting.md).

CI runs Windows/Ubuntu, .NET Debug/Release and local package consumers. [R5 evidence](docs/stages/runtime-R5.md), [R6 evidence](docs/stages/runtime-R6.md), [final R8 acceptance record](docs/verification-report-runtime.md) distinguish build, Editor and real Player execution. Core0.2 was accepted into main through PR1.

The separate GitHub Wiki storage is not initialized; the complete [docs/wiki fallback](docs/wiki/runtime/Home.md) is the source of truth. [Safe Wiki synchronization](docs/wiki-sync.md) · [Changelog](CHANGELOG.md) · [Roadmap](docs/runtime/roadmap.md) · [Dependency notices](THIRD-PARTY-NOTICES.md).
