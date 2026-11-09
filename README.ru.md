# Bridge: Core и Runtime

Core0.2 — версионированные JSON blocks, ссылки и запросы. Runtime0.1 — явно зарегистрированные C# команды и ограниченное чтение этих данных. Оба netstandard2.1, без UnityEngine/MCP; MIT. Unity adapter и .NET MCP gateway — отдельные пакеты.

**[Начать Runtime](docs/wiki/runtime/Home.md)** · [Core tutorial](docs/wiki/Home.md) · [English](README.md) · [API Runtime](docs/wiki/runtime/API-Reference.md)

## Первая команда без Unity, MCP и файла

Создайте .NET8 console project и ProjectReference на `src/Bridge.Runtime/Bridge.Runtime.csproj` ([точные шаги](docs/wiki/runtime/Installation-NET.md)). Весь Program.cs:

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

Ожидается sum=5. ExecuteLocalAsync — доверенный Admin путь внутри приложения; для внешнего клиента необходим trusted context и явная policy. Build не открывает порт. [Permissions/default deny](docs/wiki/runtime/Security.md).

## Пакеты и примеры

| Пакет | Назначение |
|---|---|
| Bridge.Core0.2 | JSON document/block/field, serializers, refs, QuerySpec |
| Bridge.Runtime0.1 | Host, commands, schemas, discovery/data policy, executor |
| com.serhgamedev.bridge.unity0.1 | Unity owner-thread dispatcher, Scene Demo |
| Bridge.Transport.Mcp0.1 | .NET8 stdio gateway, official SDK2.4.0 |

NuGet/UPM registry, tags и Release **не публиковались**. Используйте checkout ProjectReference, [локальные nupkgs](docs/wiki/runtime/Installation-NET.md) или [Git UPM](docs/wiki/runtime/Installation-Unity.md). Core0.2 принят в main через PR1; Core API и bridgeVersion=1 сохранены.

- `dotnet run --project samples/Bridge.Runtime.Console.Sample -c Release` — commands/discovery/BridgeFile/policy/update.
- [Unity Scene Demo](docs/runtime/unity.md) — настоящие Cube prefab/scene, spawn/move/delete/fresh snapshots.
- [Готовый Player → MCP → Codex](docs/runtime/mcp.md) — сборка, token/environment, TOML и воспроизводимый SDK client E2E. Личный Codex connected E2E отдельно не заявляется.
- [12 рецептов и FAQ](docs/wiki/runtime/Cookbook.md), [результаты/cancel](docs/wiki/runtime/Results.md), [расширения](docs/wiki/runtime/Advanced.md), [troubleshooting](docs/wiki/runtime/Troubleshooting.md).

## Проверки и Wiki

`dotnet test Bridge.sln -c Release`, `tools/Verify-Snippets.ps1`, `tools/Verify-RuntimePackages.ps1`, `tools/Verify-RuntimeUnity.ps1`, `tools/Verify-RuntimeMcp.ps1`. CI Windows/Ubuntu, .NET Debug/Release и local package consumers. Фактические платформы/ограничения — [R5](docs/stages/runtime-R5.md), [R6](docs/stages/runtime-R6.md), последующая R8 приёмка; успешная сборка не заменяет Player runtime test.

Отдельная GitHub Wiki пока не инициализирована: полный [docs/wiki fallback](docs/wiki/runtime/Home.md) доступен в репозитории. [Безопасная dry-run синхронизация](docs/wiki-sync.md). [Changelog](CHANGELOG.md) · [roadmap](docs/runtime/roadmap.md) · [MIT/dependency notices](THIRD-PARTY-NOTICES.md).
