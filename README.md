# Bridge.Core

Bridge.Core is a netstandard2.1 library for versioned JSON block documents, explicit references, LINQ and bounded serializable queries. MIT. The core has no UnityEngine dependency.

**[Start here](docs/wiki/Getting-Started-English.md)** · [Русский](README.ru.md) · [API](docs/api.md) · [Format](docs/format-v1.md)

v0.2 is developed on feature/core-v0.2-developer-experience. Reference src/Bridge.Core/Bridge.Core.csproj from that checkout, or run `dotnet run --project samples/Bridge.Console.Beginner -c Release` for the complete file round-trip. Expected: BEGINNER_PASS: speed=2; projected=1.

```csharp
using System;
using Bridge.Core;
var d = BridgeDocument.Create("factory");
d.AddBlock("settings", "simulation.settings", BridgeValue.Parse("{\"speed\":1}"));
if (d.ContainsBlock("settings") && d.HasField("settings", "speed"))
    d.Set("settings", "speed", d.Get<double>("settings", "speed") * 2);
if (d.Get<double>("settings", "speed") != 2) throw new Exception("Example failed");
Console.WriteLine("speed=2");
```

IDs and direct field names are ordinal and case sensitive. HasField treats null as present. TryGet returns false for missing only; type failures throw BridgeException. Use TryRead for diagnostic results. BridgeFile provides Exists/Load/TryLoad/Save/SaveAtomic; Exists does not validate JSON or guarantee a later read. DTO converters are explicit and reusable with isolated document registration tables. WhereTypes supports OR filters and keeps QuerySpec v1 compatible; multi-type specs use v2.

For Unity Git UPM installation and a runnable TextAsset MonoBehaviour see [Unity](docs/wiki/Getting-Started-Unity.md). [Cookbook](docs/wiki/Cookbook.md) and [FAQ](docs/wiki/FAQ.md) give practical examples. No NuGet package, GitHub Release or version tag has been published. The repository is public. Wiki is enabled but its separate Git storage is not initialized; docs/wiki is the complete documentation source until publication. [Wiki sync](docs/wiki-sync.md).

Build: dotnet restore, dotnet build -c Release, dotnet test -c Release. Validate documentation with tools/Verify-Snippets.ps1; pack locally with dotnet pack -c Release -o work/packages. See the [v0.2 verification report](docs/verification-report-v0.2.md) for actual CI/Unity results and limitations.
