# Runtime R5 — Unity adapter и настоящий Player

Base integration `f21f7344fec404a4bf9a3dc8d7032c0d96af93f1` (R4 PR6, CI42606835845 Windows/Ubuntu success). Branch `feature/runtime-r5-unity`. План этапа: тонкий owner-thread adapter, Scene Demo и explicit IPC, Editor tests, настоящий Windows Player, отдельный IL2CPP High runtime smoke, затем PR/CI в integration.

## Реализация

* Новый MIT UPM `com.serhgamedev.bridge.unity`0.1: `UnityBridgeDispatcher`, bounded queue, one live operation per Update, gate через async await, cancellation/disposal. Нет собственного worker thread, сеть не включается при создании host.
* Scene Demo: меню создаёт настоящую сцену, Cube prefab, Bridge TextAsset. Stable GUID объектов, spawn/move/delete реальных GameObject/Transform. `config` читает ограниченный BridgeDocument; `scene` строит свежий snapshot реальных координат. privateNote закрыт, Admin-команда скрыта от Edit, Read не меняет сцену. speed меняет документ явно, без автоматического save/предметной физики.
* Reflected sample methods с Preserve, explicit enum/DTO codec, async DTO и typed registrar. WebGL smoke вызывает каждый из этих путей; не только compile/build.
* Demo-only TCP IPC включается только `--bridge-listen` + environment token. Literal127.0.0.1, fixed magic, bounded frames, strict UTF-8,16connections/15s, один request на connection. Сервер назначает trusted Read/Edit principal; клиентские permission/principal не принимаются. SDK/wire MCP отсутствует в Unity, Core и Runtime. Выбор TCP вместо HTTP и причины уточнены в ADR5.

```csharp
var dispatcher = gameObject.AddComponent<Bridge.Unity.UnityBridgeDispatcher>();
var host = Bridge.Runtime.BridgeHostBuilder.Create("scene")
    .AddCommands(commands).UseDispatcher(dispatcher).Build();
// В OnDestroy: host.Dispose(); dispatcher.Dispose();
```

Коммиты реализации: `6bfac94` и `0996cbf` (context regression); точные SHA/CI и итог PR доступны в истории этапной ветки. Изменены Unity UPM, внешний PlayerClient/smoke tool, Verify-RuntimeUnity/Player, solution, Runtime await chains, ContextTests, ADR и Unity guide. Core исходники/API не менялись.

## Фактические проверки 2026-11-09

Unity `6000.6.7f1`; .NET SDK8.0.408, netstandard2.1 Runtime. Команды из корня checkout:

| Команда | Результат |
|---|---|
| `dotnet test Bridge.sln -c Release --no-restore` | Core109/109 и Runtime37/37 PASS перед context regression |
| `dotnet test tests/Bridge.Runtime.Tests -c Release --no-restore` | Финальные Runtime38/38 PASS,0warnings/errors |
| `tools/Verify-RuntimeUnity.ps1 -Mode EditMode` | Повторный запуск:2/2 PASS |
| `tools/Verify-RuntimeUnity.ps1 -Mode PlayMode` | Повторный запуск:3/3 PASS |
| `tools/Verify-RuntimeUnity.ps1 -Mode WindowsBuild` | Реальная Mono Windows64 сборка, exit0 |
| `tools/Verify-RuntimePlayer.ps1 -Mode Smoke` | Настоящий Player, exit0, PLAYER_RUNTIME_PASS |
| `tools/Verify-RuntimePlayer.ps1 -Mode IPC` | Настоящий отдельный Player и .NET client, PLAYER_IPC_PASS |
| `tools/Verify-RuntimeUnity.ps1 -Mode WebGLBuild` | Реальная IL2CPP, Managed Stripping High, build exit0 |
| HTTP server + запуск WebGL в IAB | **BRIDGE_RUNTIME_PLAYER_PASS**,2026-11-09T11:40:37Z |

Windows проверяет создание3/перемещение/удаление/live2, x=7, отсутствие удалённого ID, config update,5owner-thread calls, default closed field/block/Admin, Read mutation denial, wrong token, permission spoof, bad magic и oversized frame до body allocation. PlayMode также проверяет отмену queued mutation и gate через async yield. WebGL проверяет spawn3/move/delete/live2/owner/enum/async DTO/typed registrar/manual cancellation. Логи/XML/builds — ignored `work/runtime-unity/`; screenshot — пользовательский `outputs/runtime-webgl-pass.jpg`. WebGL test server и browser tab закрыты после проверки.

## Найденная проблема и исправление

Первые WebGL сборки компилировались, но async runtime smoke зависал. Managed ThreadPool отсутствует: ConfigureAwait(false) и asynchronous TCS completion могли отправлять продолжения в недоступный scheduler. Runtime теперь сохраняет SynchronizationContext; WebGL dispatcher завершает TCS inline на Update, desktop сохраняет RunContinuationsAsynchronously. Demo async DTO использует Task.Yield, runInBackground=true. После исправления получен реальный browser PASS. Новый ContextTests проверяет contended gate на caller context; Core не затронут. Синхронное ожидание Task на owner thread запрещено.

В повторном shell harness устаревший LASTEXITCODE после PowerShell script ошибочно остановил цепочку после успешного EditMode2/2. По XML тесты Passed; исправлен способ вызова harness, затем PlayMode3/3 и WindowsBuild запущены отдельно. Это не скрытое падение Unity test.

## Ограничения и интеграция

Windows standalone IL2CPP **SKIPPED**: module отсутствует; AOT проверен настоящим WebGL High. Unity6000.7alpha и Unity7 в этом этапе не заявляются проверенными. WebGL не поддерживает demo TCP IPC/managed workers; timer-based timeout отдельно этим smoke не квалифицируется. Отмена кооперативная без rollback. Долгие handlers/inline WebGL continuations могут задерживать кадр. Пользовательские reflected handlers требуют собственных Preserve/link.xml либо typed registrar. IPC — локальный application sample, без remote/TLS и без MCP SDK; настоящий MCP E2E выполняется следующим R6.

Этап отправляется PR в integration после этих проверок; обязательный Windows/Ubuntu CI должен быть success перед merge. Main ожидает окончательной R8 приёмки; NuGet/tag/Release не публикуются.
