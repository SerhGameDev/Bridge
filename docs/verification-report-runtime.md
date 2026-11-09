# Bridge.Runtime 0.1 — финальная приёмка

Дата: 2026-11-09. Windows, .NET SDK8.0.408. Функциональный source: `f0e968d45266aa59b2a52aa45ad39e442fd4d84f`; последующий R8 commit добавляет отчёт и delivery scan. Core0.2 принят PR1, baseline `5e06afd76b4e57251cba77c1ef8bce5f417f30c9`. Core C# и csproj относительно baseline не менялись. Реальные merge SHA и checks доступны в [PR history](https://github.com/SerhGameDev/Bridge/pulls?q=is%3Apr); финальное слияние выполняется после зелёного Windows/Ubuntu CI.

## Матрица фактически выполненных проверок

| Проверка | Результат |
| --- | --- |
| Windows .NET Debug и Release build | PASS, 0 warnings/errors |
| Core regression Debug / Release | 109/109 в каждой конфигурации |
| Runtime Debug / Release | 41/41 в каждой конфигурации |
| MCP protocol Debug / Release | 14/14 в каждой конфигурации, настоящий SDK client/server stdio |
| Console samples | PASS, commands/config/policy/update/diagnostics |
| Markdown examples | 40/40 отдельных программ compile + run PASS |
| Documentation links / JSON | PASS; checker повторяется после добавления отчёта |
| Local NuGet | Core0.2, Runtime0.1, Transport.Mcp0.1; isolated PackageReference consumer PASS |
| Wiki | 29 pages, links и отказ unmanaged collision PASS, без публикации |
| Unity6000.6.7f1 file UPM | EditMode2/2, PlayMode3/3 PASS |
| Unity6000.6.7f1 Git UPM | все3 packages закреплены на f0e968d; lock source=git/hash совпадают; EditMode2/2, PlayMode3/3 PASS |
| Unity6000.7.0a2 alpha file UPM | EditMode2/2, PlayMode3/3 PASS; это не Unity7 |
| Windows Mono Player | build exit0 и реальный PLAYER_RUNTIME_PASS |
| Windows Player IPC | реальный внешний.NET client, PLAYER_IPC_PASS |
| Windows Player → MCP gateway → SDK client | MCP_PLAYER_E2E_PASS, три отдельных процесса |
| WebGL IL2CPP High stripping | build exit0 и браузер BRIDGE_RUNTIME_PLAYER_PASS |
| Windows IL2CPP | SKIPPED: соответствующий installed build module отсутствует |
| Unity7 | SKIPPED: Editor не установлен |
| Личная подключённая сессия Codex | не запускалась; предусмотренная brief альтернатива compatible SDK client выполнена |
| GitHub-hosted Windows/Ubuntu CI | обязательный green gate для R8 и final PR; точные run links в PR checks |

Unity7/Windows IL2CPP нужно повторить после установки Editor/module: тот же Unity sample, tests, Player smoke и MCP E2E. Совместимость с этими отсутствующими окружениями не заявляется. Linux проверяется в CI для.NET; Linux Unity Player не собирался.

## Команды воспроизведения

Из repository root, PowerShell7:

```powershell
dotnet build Bridge.sln -c Debug
dotnet test Bridge.sln -c Debug --no-build
dotnet build Bridge.sln -c Release
dotnet test Bridge.sln -c Release --no-build
./tools/Verify-Snippets.ps1
./tools/Verify-RuntimePackages.ps1
./tools/Verify-Wiki.ps1
./tools/Verify-RuntimeDelivery.ps1 -CoreBaseline 5e06afd76b4e57251cba77c1ef8bce5f417f30c9
./tools/Verify-RuntimeUnity.ps1 -Mode EditMode
./tools/Verify-RuntimeUnity.ps1 -Mode PlayMode
./tools/Verify-RuntimeUnity.ps1 -Mode WindowsBuild
./tools/Verify-RuntimePlayer.ps1 -Mode Smoke
./tools/Verify-RuntimePlayer.ps1 -Mode IPC
./tools/Verify-RuntimeMcp.ps1
./tools/Verify-RuntimeUnity.ps1 -Mode WebGLBuild
./tools/Verify-RuntimeUnity.ps1 -Mode EditMode -GitRevision f0e968d45266aa59b2a52aa45ad39e442fd4d84f
./tools/Verify-RuntimeUnity.ps1 -Mode PlayMode -GitRevision f0e968d45266aa59b2a52aa45ad39e442fd4d84f
```

Default Editor: `C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe`. Для alpha в двух test commands передан `-UnityEditor 'C:/Program Files/Unity/Hub/Editor/6000.7.0a2/Editor/Unity.exe'`. Каждый helper проверяет настоящий process exit/XML; build отдельно от runtime.

WebGL output `work/runtime-unity/webgl-runtime` обслуживался локальным HTTP server на127.0.0.1:8768. В браузере2026-11-09T12:38:30.346Z получено `BRIDGE_RUNTIME_PLAYER_PASS: spawn=3 move=1 delete=1 live=2 owner=true enum/DTO/typed=true`, DOM marker и видимые два куба. Предыдущий HTTP server давал ERR_EMPTY_RESPONSE; после перезапуска server все assets вернули200, Player source не менялся. Screenshot/console сохранены локально; server остановлен. Это настоящий AOT runtime, не compile-only.

Локальные доказательства (ignored): `work/test-results/runtime-final-debug`, `runtime-final-release`; Unity version folders с XML/Editor/build logs; `work/runtime-unity/mcp-e2e-result.log`, `player-MCP.log`, `webgl-final-console.json`. Бинарники, логи и credentials не коммитятся.

## Три сценария

Console sample обнаруживает команды, вызывает math.add/echo, проверяет invalid args/unknown command. BridgeFile config доступен с явной policy; privateNote и закрытый блок не выдаются; обработчик явно обновляет документ и последующий query читает изменение. Автоматической записи произвольных объектов нет.

MCP E2E использует official ModelContextProtocol.Core2.4.0, negotiated protocol2026-07-28. Protocol suite также проверяет2025-11-25 и2025-06-18. Внешний клиент не открывает Unity sources: tools/discovery/schema/config, spawn3 GUID cubes, move первого в(7,2,3), delete второго, fresh snapshot с2 cubes/координатами/отсутствующим удалённым ID. Проверены Read mutation denial, private field/block/Admin denial, invalid credential UNAUTHORIZED,5 owner-thread calls и async DTO. Лог: `MCP_PLAYER_E2E_PASS: SDK2.4.0 protocol2026-07-28; created3 moved(7,2,3) deleted1 live2; owner5+async; config/schema/policies/auth PASS`.

[Player и Codex пошагово](runtime/mcp.md), [готовый TOML](../samples/Bridge.Runtime.Mcp.Sample/codex-config.example.toml), [Unity setup](runtime/unity.md). Пользовательский Codex config не изменён; secrets создаются в памяти окружения на время теста и не печатаются.

## Производительность: найденная проблема и исправление

R8 выявил повторное транзакционное клонирование Core document и сериализацию всей projection на каждом block. Runtime теперь строит одну bounded projection и считает точный UTF8 budget инкрементально, Unity SceneWorld создаёт один bulk snapshot. Core API не менялся. Новые tests: escaped/Unicode exact-byte boundary, scaling allocations200→400blocks, валидная1000-block projection и privacy.

`dotnet run --project tools/Bridge.Runtime.Measure -c Release`, warm JIT, один Windows process, default serial dispatcher:

| Операция | N | До median ms / alloc bytes per call | После median ms / alloc bytes per call |
| --- | --- | --- | --- |
| discover | 100 | 0.216 /123901 | 0.206 /123901 |
| query1000blocks | 20 | 1543.647 /1615832839 | 56.889 /30255853 |
| execute | 1000 | 0.035 /34961 | 0.051 /34961 |

Query max после исправления181.125ms. Это наблюдения этой машины, не SLA. Alloc — суммарные managed allocations на вызов, не peak memory; peak memory и отдельная стоимость Unity frame не профилировались. Default100blocks и bounded queries рекомендуются для main thread; большой snapshot может занять кадр. Сеть не ждёт синхронно внутри Update; один async handler сохраняет serial ownership до фактического завершения. Отмена cooperative, rollback/retry/dedup не добавляются.

## Приёмка контрактов и границ

Host builder/lifecycle/multiple hosts/dispose, explicit tagged commands и formal schema/binder/DTO/enum/null/optional, async statuses/cancellation/serial queue покрыты Runtime suite. Default deny, trusted principal mapping, hidden commands/fields/metadata/refs и size limits enforce в коде. Build не запускает listener. Runtime/Core независимы от Unity/MCP, что дополнительно проверяет delivery scan в Windows/Ubuntu CI. MCP использует SDK, не самодельную имитацию JSON-RPC. Unity adapter и demo IPC отделены от portable host.

Все16 критериев brief имеют реализацию/проверку либо явно разрешённый fallback: Core PR1; host/API/schema; registration-only invocation; query/config/dynamic source; enforced policies; Completed/cancel; Unity owner dispatcher; отдельный MCP/E2E; samples; CI; docs/wiki fallback; последовательная PR history; final main merge после green checks. PLC, Python SDK и JSON arbitrary execution отсутствуют.

Остаются документированные границы MVP: reflection требует сохранения registered methods/codecs в AOT (реальный WebGLHigh прошёл); Queued зарезервирован, не выдаётся; ValueTask/durable polling/hot registry/remote HTTP-OAuth вне0.1. Demo loopback IPC не промышленный transport. NuGet, registry UPM, tag и GitHub Release не публиковались. Wiki storage не инициализирован, полный fallback готов. Необязательные Bridge.Testing/PLC/Python перечислены в [roadmap](runtime/roadmap.md) и не выполнялись.

## Установка и документация

[.NET ProjectReference/local NuGet](wiki/runtime/Installation-NET.md), [Unity Git UPM](wiki/runtime/Installation-Unity.md), [первая команда](wiki/runtime/First-Command.md), [API](wiki/runtime/API-Reference.md), [12 recipes](wiki/runtime/Cookbook.md), [весь Runtime guide](wiki/runtime/Home.md). MIT metadata, LICENSE/README и MCP dependency notices проверены local package consumer. Этапы R0–R8 сохранены в docs/stages; финальный origin/main SHA устанавливается только после фактического merge, не предсказывается в этом commit.
