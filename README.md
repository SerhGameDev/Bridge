# Bridge.Core

Bridge.Core stores blocks and named references in standard JSON. It targets **netstandard2.1**, provides ordinary LINQ and bounded serializable queries, and contains no UnityEngine dependency. MIT, copyright 2026 SershDev.

[Русская документация](README.ru.md) · [Format](docs/format-v1.md) · [API](docs/api.md) · [Queries](docs/queries.md) · [References](docs/references.md) · [Unity/AOT](docs/unity-compatibility.md)

## Local installation

Version 0.1.0 is a local release candidate. No GitHub repository, NuGet package or release has been published. Add a project reference to `src/Bridge.Core/Bridge.Core.csproj` from this checkout, or build a local package with `dotnet pack -c Release -o work/packages` and add that directory as a NuGet source. Do not assume a similarly named public package belongs to this project.

For Unity use Package Manager **Add package from disk** and `Packages/com.serhgamedev.bridge.core/package.json`. Core uses shared source with .NET and the official Unity Newtonsoft dependency. Exact tested Editor and IL2CPP versions are listed in [Unity compatibility](docs/unity-compatibility.md).

## Quickstart

Complete console example: parse, modify, query, save and reload UTF-8 data.

```csharp
using System;
using System.IO;
using System.Linq;
using Bridge.Core;

var document = BridgeDocument.Parse("{\"bridgeVersion\":1,\"documentId\":\"demo\",\"blocks\":{\"settings\":{\"type\":\"settings\",\"schemaVersion\":1,\"data\":{\"speed\":1,\"note\":\"private\"}}}}");
var block = document.GetBlock("settings");
block.Set("speed", block.Get<double>("speed") + 1);
var ids = document.Blocks.Where(b => b.Type == "settings").Select(b => b.Id).ToArray();
var selection = document.Query().WhereType("settings").SelectFields("speed").Limit(10).Execute();
Console.WriteLine(selection.ToJson());
using var stream = new MemoryStream();
document.Save(stream);
stream.Position = 0;
var restored = BridgeDocument.Load(stream);
if (ids.Length != 1 || restored.GetBlock("settings").Get<double>("speed") != 2)
    throw new Exception("Quickstart failed.");
```

Parse/Load throw BridgeException for input failures; TryParse/TryLoad return BridgeResult<T> and diagnostics. JSON values and projections are detached immutable snapshots. DTOs need an explicitly registered converter. Mutations are explicit; see [API](docs/api.md).

## Build and checks

```text
dotnet restore Bridge.sln
dotnet build Bridge.sln -c Release
dotnet test Bridge.sln -c Release
dotnet run --project samples/Bridge.Console.Sample -c Release -- --performance
dotnet pack src/Bridge.Core/Bridge.Core.csproj -c Release -o work/packages
```

CI is configured for .NET on Windows/Linux; no GitHub run has occurred. Local evidence is in [stage reports](docs/stages/). Unity checks require a licensed installed Editor and are not part of the hosted .NET workflow.
