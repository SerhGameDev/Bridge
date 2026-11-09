# Runtime R6 — официальный MCP SDK и готовый Unity Player

Base `dc9b2aa0114bef1fed935ebf4b0ce5e109046d91`, R5 PR7 merged после CI42611341805/42611294372/42611294033 success на Windows/Ubuntu. Branch `feature/runtime-r6-mcp`. Мини-план: проверить актуальную спецификацию/SDK/license; отдельный stdio adapter; protocol/security tests; реальный Player E2E; точная конфигурация Codex; PR/CI в integration.

## Реализация и API

`Bridge.Transport.Mcp` net8.0 → Runtime/Core + NuGet **ModelContextProtocol.Core2.4.0**, официальный stable release2026-11-07. MCP2026-07-28, SDK down-level negotiation. Ни одного MCP/ASP.NET reference в Core, Runtime или Unity. SDK не копируется DLL в Git, dependency сохраняет Apache-2.0; Bridge остаётся MIT. [Third party notices](../../THIRD-PARTY-NOTICES.md).

```csharp
var endpoint = new Bridge.Transport.Mcp.RuntimeMcpEndpoint(
    host, "trusted-reader", Bridge.Runtime.BridgePermission.Read);
await using var server = Bridge.Transport.Mcp.BridgeMcpServer.Create(
    Console.OpenStandardInput(), Console.OpenStandardOutput(), endpoint);
await server.RunAsync(cancellationToken);
```

Create не запускает server и не открывает сеть. Строго три MCP tools: bridge_discover публикует разрешённые command name/description/exact schemas и data catalog; bridge_query принимает Core QuerySpec v1/v2; bridge_execute вызывает только зарегистрированный command. Principal/permission берутся из trusted endpoint config либо Player auth mapping, не из tool arguments. Fixed MCP tools не раскрывают private command names. SDK handles actual negotiation/JSON-RPC; вручную протокол не имитируется.

Business errors → isError=true, одинаковый sanitized Bridge envelope в structuredContent/text. Wrong outer arguments/unknown tool → InvalidParams; unsupported protocol method → MethodNotFound. Request64KiB, raw stdio line128KiB до parser accumulation, Bridge result256KiB без partial output,16active calls,15s cooperative deadline. No retry/rollback/fake queued completion. Executable sample требует explicit flags; stdout только SDK protocol, stderr diagnostics. Authentication для loopback Player остаётся обязательной; HTTP endpoints отсутствуют.

Файлы: src/Bridge.Transport.Mcp (server/endpoints/bounded stream/csproj/README), samples/Bridge.Runtime.Mcp.Sample (explicit executable + Codex TOML), tests/Bridge.Transport.Mcp.Tests, tools/Bridge.Mcp.PlayerE2E, Verify-RuntimeMcp.ps1, solution/CI, notices, [MCP guide](../runtime/mcp.md). Core/Runtime/Unity implementation в R6 не меняется.

## Фактические проверки

.NET SDK8.0.408 на Windows; gateway/client SDK2.4.0; настоящий Windows64 Mono Player Unity6000.6.7f1 из завершённого R5.

| Команда/сценарий | Результат |
|---|---|
| `dotnet build samples/Bridge.Runtime.Mcp.Sample -c Release` | PASS,0warnings/errors |
| `dotnet test tests/Bridge.Transport.Mcp.Tests -c Release` | **14/14 PASS**, настоящие отдельные stdio server processes |
| `dotnet test Bridge.sln -c Release --no-restore` | Core109/109, Runtime38/38, MCP14/14 PASS |
| `dotnet pack src/Bridge.Transport.Mcp -c Release --no-build -o work/packages` | Локальный nupkg PASS, не опубликован |
| `tools/Verify-RuntimeMcp.ps1` | Повторный настоящий **MCP_PLAYER_E2E_PASS** |
| Python tomllib prepared Codex config | CODEX_CONFIG_TOML_PASS |

Protocol tests фактически подключают официальный SDK client к отдельному executable:2026-07-28,2025-11-25,2025-06-18; SDK capabilities/tools/schema; command catalog и schema parity; binder/permissions; Core query v1/v2 и forbidden selectors; sanitized exceptions; async; client cancellation с последующим successful call; wrong tool/outer args(-32602), missing method(-32601); oversized args/result/raw stdio line; executable disabled without explicit flags.

Real Player E2E: Player launched without self-test/source project, gateway — отдельный процесс, SDK client — ещё один процесс в `work/mcp-e2e-client`. Client использует только MCP tools, не открывает Unity source files. Получены catalog/spawn schema/public config; созданы3 GUID cubes (0/2/4,1,0), первый перемещён в(7,2,3), второй удалён. Core snapshot подтверждает live2, все координаты первого, третий x=4 и отсутствие удалённого ID. Отказы privateNote/hidden block/Admin, Read catalog/mutation denial, invalid credential UNAUTHORIZED;5owner-thread calls и async DTO owner=true. Лог `work/runtime-unity/mcp-e2e-result.log`; Player log `player-MCP.log`. Инструмент восстанавливает environment и закрывает только свои Player/gateway/client processes; реальные credentials не печатаются и не сохраняются.

## Codex и ограничения

Проверены local CLI0.163.0 `codex mcp --help`/`add --help` и актуальная [официальная инструкция](https://learn.chatgpt.com/docs/extend/mcp?surface=cli). Подготовлен TOML с command/args/env_vars, startup/tool timeouts и prompt approvals; настоящий token не хранится в конфигурации. [Guide](../runtime/mcp.md) содержит publish/start/config/restart, готовое задание трёх кубов, expected output и cleanup.

**Codex personally connected E2E — не запускался**: текущая сессия не подменялась, пользовательский global config не менялся. Вместо этого выполнен предусмотренный brief compatible-client E2E, не mock. Установка конфигурации в пользовательский клиент и его новый сеанс остаются действиями владельца. Это не блокирует уже подтверждённый MCP protocol/Player путь.

MCP HTTP/OAuth/remote hosting не реализуются, stdio использует OS process boundary. Для своего HTTP transport нужны отдельные Host/Origin/auth меры. Player IPC локальный application demo, не универсальный промышленный transport. Windows IL2CPP отсутствует; отдельный WebGL AOT PASS — R5. Gateway response cap меньше host cap и может отказаться от большого разрешённого снимка целиком.

Primary sources проверены2026-11-09: [SDK2.4.0 release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.4.0), [SDK API/tools](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/tools/tools.html), [stdio spec](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/stdio), [tools spec](https://modelcontextprotocol.io/specification/2026-07-28/server/tools). Этапной PR/CI проходит до интеграции и R7; main ожидает R8, NuGet/tag/Release не публикуются.
