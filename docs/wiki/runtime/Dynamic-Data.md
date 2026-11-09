<!-- Bridge managed wiki source -->
# Динамические данные сцены или приложения

IBridgeDataSource строит актуальный snapshot, когда вызывается Query. Config и текущее состояние называйте разными sources. Framework не сохраняет скрытый snapshot cache и не обходит refs.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
var source = new CounterSource();
using var host = BridgeHostBuilder.Create("dynamic")
    .AddDataSource("live",source,BridgeDataPolicy.ReadOnly().AllowFields("counter","value")).Build();
var reader = new BridgeRequestContext("reader",BridgePermission.Read);
var first = await host.QueryAsync("live",new BridgeQuerySpec(),reader);
var second = await host.QueryAsync("live",new BridgeQuerySpec(),reader);
if (!first.Success || !second.Success) throw new Exception("Query failed");
Console.WriteLine(BridgeDocument.Parse(second.Data.ToJson()).Get<int>("counter","value"));
public sealed class CounterSource : IBridgeDataSource
{
    private int reads;
    public Task<BridgeDataSnapshot> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var d=BridgeDocument.Create("private.live");
        d.AddBlock("counter","counter",BridgeValue.Parse("{}").WithField("value",BridgeValue.From(++reads)));
        return Task.FromResult(new BridgeDataSnapshot(d));
    }
}
```

Вывод: `2`. Source читается на выбранном dispatcher. Snapshot metadata не публикуются автоматически; если tick нужен клиенту, откройте его отдельным полем policy. Запросы не должны обходить ownership/synchronization приложения.

В Unity SceneWorld создаёт документы из реальных Transform, а UnityBridgeDispatcher гарантирует owner thread. Stable ID — GUID приложения, не Unity GetInstanceID. [Полная сцена и тесты](../../runtime/unity.md).
