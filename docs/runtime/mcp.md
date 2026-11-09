# Готовый Unity Player → MCP → Codex

Bridge.Transport.Mcp — отдельный .NET8 gateway. В Unity/Core/Runtime нет MCP SDK. Официальный [C# SDK2.4.0](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.4.0) установлен через NuGet, версия фиксирована. Используется [MCP2026-07-28 stdio](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/stdio); SDK также обслуживает старые initialize-based clients. Отдельно проверены2025-11-25 и2025-06-18. [Лицензии зависимостей](../../THIRD-PARTY-NOTICES.md).

## Что где работает

| Часть | Роль |
|---|---|
| Unity Player | Только GameObject/Transform, BridgeHost, Unity dispatcher и явно включаемый authenticated loopback IPC |
| .NET gateway | MCP SDK stdio server, перевод трёх tool calls в Runtime/Player endpoint |
| Codex или SDK client | Читает каталог/схемы, вызывает команды, получает fresh snapshots; исходники Unity ему не нужны |

`bridge_discover` возвращает доступные command name/description/inputSchema/outputSchema и разрешённые sources/types/fields. `bridge_query` принимает source и **Core QuerySpec**, `bridge_execute` — command и arguments. Это три общих MCP tools; схемы аргументов каждой команды остаются в каталоге. Отдельные handwritten tools для каждого метода не нужны.

## 1. Собрать и проверить

В PowerShell из checkout `C:/GitHub/Bridge`, с .NET8 SDK и Unity6000.6.7f1 + Windows build support:

```powershell
./tools/Verify-RuntimeUnity.ps1 -Mode WindowsBuild
dotnet publish samples/Bridge.Runtime.Mcp.Sample -c Release -o work/mcp-gateway
./tools/Verify-RuntimeMcp.ps1
```

Ожидается `MCP_PLAYER_E2E_PASS`. Инструмент сам генерирует credentials в памяти, запускает **реальный собранный Player** и отдельный gateway, вызывает официальный SDK client и закрывает свои процессы. Лог Player: `work/runtime-unity/player-MCP.log`. Клиент читает только MCP/IPC, не Unity source files. Это воспроизводимая проверка совместимым MCP-клиентом, **не** заявление о личном подключении Codex.

Для обычного .NET host без Unity/Bridge-файла запускайте gateway с `--enable-stdio --local-demo`: in-process Read principal, math.add/text.echo и config snapshot. Diagnostic commands demo.throw/delay/large демонстрируют sanitized errors, cancellation и response cap. Без явных flags executable завершится exit2; никакого listener по умолчанию.

## 2. Запустить Player для своей сессии

В отдельной PowerShell задайте случайный секрет и сохраните эту shell до завершения сеанса. Значение не печатается и не записывается в Git/config:

```powershell
$env:BRIDGE_PLAYER_TOKEN = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$player = Start-Process 'C:/GitHub/Bridge/work/runtime-unity/windows/Bridge.Runtime.Demo.exe' -ArgumentList @('--bridge-listen','-logFile','C:/GitHub/Bridge/work/runtime-unity/player-codex.log') -WindowStyle Hidden -PassThru
```

Player назначает этому credential Edit (включает Read/Control), но не Admin. При желании отдельный BRIDGE_READER_TOKEN задаётся **до запуска Player**; он должен отличаться и иметь минимум32 символа. Передав его gateway как BRIDGE_PLAYER_TOKEN, получите Read-сессию. Не используйте Self Test одновременно с MCP: smoke меняет сцену.

## 3. Настроить Codex

Проверьте [официальную документацию Codex MCP](https://learn.chatgpt.com/docs/extend/mcp?surface=cli). На момент проверки CLI0.163.0 поддерживает stdio command/args и environment forwarding. Добавьте [подготовленную TOML секцию](../../samples/Bridge.Runtime.Mcp.Sample/codex-config.example.toml) в `C:/Users/user/.codex/config.toml` либо в `.codex/config.toml` доверенного проекта. Исправьте абсолютный путь к опубликованному DLL, если checkout находится иначе. `env_vars` передаёт имя переменной; значение credential в TOML не хранится. Approval policy клиента сохраняет пользовательский контроль tool calls.

Запустите **новый Codex из той же PowerShell**, где создан token, и проверьте конфигурацию:

```powershell
codex mcp list
codex
```

Для Desktop app процесс тоже должен унаследовать BRIDGE_PLAYER_TOKEN; уже работающий экземпляр может иметь старое environment. После изменения конфигурации откройте новую сессию. `codex mcp list` подтверждает регистрацию, но само по себе не доказывает успешное выполнение команды. Пользовательский global config автоматически этой разработкой не меняется.

Задание для новой сессии Codex:

> Используй bridge_runtime MCP. Вызови bridge_discover и изучи схему scene.spawn_cube. Прочитай config. Создай три куба в (0,1,0), (2,1,0), (4,1,0), сохрани ID. Перемести первый в (7,2,3), удали второй. Через bridge_query прочитай scene и проверь, что осталось два объекта, первый имеет новые координаты, а ID второго отсутствует. Проверь scene.thread_check. Запрос privateNote и scene.admin_reset должен дать NOT_FOUND. Не читай Unity source files.

Примеры **аргументов MCP tools**, а не shell commands:

```json
{"source":"scene","spec":{"queryVersion":2,"types":["scene.cube"],"fields":["x","y","z"],"limit":100}}
```

```json
{"command":"scene.spawn_cube","arguments":{"x":0,"y":1,"z":0}}
```

Ожидаемый результат successful tool — `isError=false`, Bridge envelope со status Completed, requestId/operationId и data; создание возвращает стабильный GUID. Ошибка разрешений — `isError=true`, error.code NOT_FOUND, без подтверждения существования закрытого ресурса. Завершите Player из исходной shell: `Stop-Process -Id $player.Id`; удалите environment variable: `Remove-Item Env:BRIDGE_PLAYER_TOKEN`.

## Безопасность, ошибки, лимиты

MCP использует stdin/stdout локального дочернего процесса, без HTTP endpoint. Только SDK пишет protocol bytes в stdout, diagnostics идут stderr. Отдельный Player IPC привязан к literal127.0.0.1:19731 и аутентифицирует opaque token; loopback сам по себе не считается авторизацией. Нет удалённого bind, HTTP Origin/CORS/DNS endpoints, произвольных файловых операций, CLR type names или eval. Для своего HTTP transport потребуется отдельная реализация Origin/Host/auth согласно MCP security; этот gateway HTTP не включает.

SDK обрабатывает negotiation, JSON-RPC и unknown methods. Неизвестный tool/неверные внешние аргументы → InvalidParams(-32602). Правильный tool с denied command/invalid command arguments/handler error → isError=true и sanitized Bridge result. Код ошибки исходного Runtime сохраняется. SDK cancellation прокидывается к Runtime/Player; rollback/retries отсутствуют. Queued MVP не выдаёт: ответ приходит после фактического completion либо cancellation/failure.

Лимиты gateway:128KiB на входящую stdio JSON line **до** накопления parser,64KiB tool arguments,256KiB Bridge result (иначе целиком RESPONSE_LIMIT),16active calls,15s cooperative deadline. StructuredContent и text содержат одинаковый envelope. BoundedInputStream закрывает транспорт при oversized raw line; это намеренный отказ соединения. Ограничения Player IPC дополнительно действуют независимо.

Troubleshooting: connection refused → Player/flag/порт; UNAUTHORIZED → разный token/environment; gateway exit2 → flags/credential отсутствуют; timeout → убедитесь, что Unity Update работает и handler не блокирует main thread; INVALID_QUERY → queryVersion и только Core QuerySpec keys; HOST_STOPPED → сессия Player уже остановлена. Не повторяйте автоматически mutation после timeout: side effect мог завершиться позже.
