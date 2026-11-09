<!-- Bridge.Core managed wiki source -->
# English quickstart

Bridge.Core is a netstandard2.1 data library for versioned JSON block documents. v0.2 adds file helpers, exact presence checks, typed document access, reusable explicit converters and multi-type queries. No NuGet package or GitHub Release has been published. Use a project reference to src/Bridge.Core/Bridge.Core.csproj from the v0.2 feature checkout.

Run `dotnet run --project samples/Bridge.Console.Beginner -c Release` for the complete create/save/exists/load/read/update/query/save/reload scenario. Expected: BEGINNER_PASS: speed=2; projected=1. For your own console app, use this complete Program.cs:

```csharp
using System;
using Bridge.Core;
var d = BridgeDocument.Create("factory");
d.AddBlock("settings", "simulation.settings", BridgeValue.Parse("{\"speed\":1}"));
d.Set("settings", "speed", 2d);
if (!d.ContainsBlock("settings") || !d.HasField("settings", "speed")) throw new Exception("Missing");
Console.WriteLine(d.Get<double>("settings", "speed"));
```

Expected output: 2. Block IDs and field names are exact, ordinal and case sensitive. HasField checks only top-level keys; null counts as present. TryGet returns false for missing only; conversion failures throw BridgeException. TryRead returns a diagnostic BridgeResult instead. BridgeFile.Exists does not validate JSON or promise that a later Load succeeds. Save/SaveAtomic serialize before IO and use same-directory replacement; universal filesystem atomicity/power-loss durability is not promised.

Use .UseSerializers(registry) after Create/Parse/Load(Stream), or BridgeFile.Load(path, serializers: registry). Each document copies registrations; delegate closure state remains the caller's responsibility. Documents and mutable registries are not thread safe. WhereTypes is OR across types, AND with ID; repeated type filters replace prior filters. Multi-type specs use queryVersion2; v1 stays supported. Removed handles throw BLOCK_REMOVED, while prior BridgeValue snapshots remain valid.

Unity: install Git UPM from the v0.2 feature branch as documented in [Unity](Getting-Started-Unity.md), import Beginner TextAsset, attach BridgeBeginner to a GameObject, assign its TextAsset and run. [API](API-Reference.md), [examples](Cookbook.md), [Home](Home.md).
