# Bridge.Runtime0.1

Явно зарегистрированные C# команды, формальные schemas, permissions, discovery и ограниченное чтение Bridge.Core. netstandard2.1/C#9, единый source .NET/Unity UPM, MIT. Нет UnityEngine/MCP, assembly scanning, eval или automatic network listener.

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

Expected sum=5. ExecuteLocalAsync — trusted Admin, не публичный anonymous endpoint. AddDocument/AddDataSource без policy default deny; AllowFields открывает только названные keys. Core0.2 required. Command codecs и Core document serializers регистрируются отдельно. Async Task/Task<T>, cancellation cooperative, no rollback/retry/queued acknowledgement.

[Руководство](https://github.com/SerhGameDev/Bridge/blob/main/docs/wiki/runtime/Home.md) · [API](https://github.com/SerhGameDev/Bridge/blob/main/docs/wiki/runtime/API-Reference.md) · [Unity](https://github.com/SerhGameDev/Bridge/blob/main/docs/runtime/unity.md) · [MCP](https://github.com/SerhGameDev/Bridge/blob/main/docs/runtime/mcp.md). Пока пакеты не опубликованы в registry, Unity Package Manager требует сначала установить Core Git URL, затем Runtime Git URL; готовые instructions в Unity guide. NuGet можно собрать локально через tools/Verify-RuntimePackages.ps1.
