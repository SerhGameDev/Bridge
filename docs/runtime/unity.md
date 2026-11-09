# Unity: реальная сцена и границы потоков

Пакет: [UPM README](../../Packages/com.serhgamedev.bridge.unity/README.md). Требуется Unity6000.6+, Core0.2 и Runtime0.1. Сначала установите все три зависимости через Git URL, затем импортируйте Scene Demo. Packages не опубликованы в Unity registry; version-only dependencies без предварительной установки Git dependencies не разрешатся.

1. Меню **Bridge → Create Runtime Demo Scene** создаёт сцену, Cube.prefab и Bridge JSON TextAsset.
2. Bootstrap создаёт dispatcher, документ config, динамический SceneWorld и явно регистрирует SceneCommands.
3. В Play mode команды вызываются только через Host. Источник scene собирает новый BridgeDocument из Transform на том же dispatcher.
4. Включите Self Test в Inspector для сценария трёх кубов. PASS означает реальные создание, перемещение и удаление, live snapshot, проверку owner thread, разрешений, enum/async DTO/typed registrar.
5. При остановке bootstrap закрывает explicit IPC, отменяет host и dispatcher. Он не блокирует главный поток ожиданием задач.

## Dispatcher

`UseDispatcher(dispatcher)` не передаёт ownership host, если явно не указано ownsDispatcher=true. В компоненте bootstrap dispatcher уничтожается вместе с GameObject. `UnityBridgeDispatcher` начинает максимум одну живую операцию за Update и держит gate через async await до окончания handler. Отменённые queued entries удаляются без выполнения. Result Task допускает continuation на фоне, но сам handler запускается на главном потоке.

Async handler должен сохранять UnitySynchronizationContext. Не обращайтесь к Transform после собственного `Task.Run`/`ConfigureAwait(false)`. Dispatcher не может исправить нарушение thread affinity внутри пользовательского кода. Долгий синхронный handler задержит кадр; runtime cancellation не прерывает CLR код и не откатывает изменения.

## IPC готового Windows Player

Сборка через `tools/Verify-RuntimeUnity.ps1 -Mode WindowsBuild` создаёт `work/runtime-unity/windows/Bridge.Runtime.Demo.exe`. В ней нет исходников Unity и нет MCP SDK. Listener отсутствует при обычном запуске. Для включения нужны `--bridge-listen` и секрет в environment `BRIDGE_PLAYER_TOKEN`; optional отдельный `BRIDGE_READER_TOKEN` назначает Read. Каждый должен иметь минимум32 символа, они должны отличаться. Не храните реальные токены в JSON конфигурации репозитория.

Wire contract demo IPC v1 (может меняться отдельно от Bridge JSON):

| Части запроса | Значение |
|---|---|
| magic | 9 ASCII bytes `BRIDGE/1\n` |
| length | 4 bytes unsigned big-endian, 1..65536 |
| payload | strict UTF-8 JSON object |
| response | 4-byte length + UTF-8 JSON, 1..1048576 |
| lifetime | одна операция на connection, максимум16, deadline15s |

Listener привязан только к literal `127.0.0.1:19731`, без hostname resolution, HTTP, произвольного remote bind. Все сообщения начинаются с токена; токен не попадает в Bridge command arguments/audit. Principal/permission создаёт сервер по своему credential mapping. Пример структуры запроса ниже содержит **метку**, не рабочий credential; клиент читает настоящий токен из environment и добавляет его в памяти.

```json
{"token":"<environment credential>","op":"execute","command":"scene.move","arguments":{"objectId":"stable-id-from-spawn","x":4,"y":2,"z":1}}
```

Операции: discover; query с source и spec (реальный Core BridgeQuerySpec v1/v2); execute с command и arguments. Остальные root keys, включая principal/permission, отвергаются. Неизвестный op — INVALID_REQUEST. Внешний клиент не задаёт CLR names и не выполняет выражения. Неверный токен — UNAUTHORIZED; неверный magic/length — закрытие соединения до выделения body. Ошибки parsing/dispatch не возвращают exception message/stack.

`tools/Verify-RuntimePlayer.ps1 -Mode Smoke` запускает настоящий Player, ждёт marker и exit0. `-Mode IPC` генерирует случайные credentials, запускает Player без self-test, ждёт listener и вызывает отдельный .NET клиент. Клиент проверяет реальные Transform, счёт объектов, отсутствие удалённого ID, обновление config, permission filtering и отрицательные protocol cases. Оба инструмента останавливают только свой Player и восстанавливают environment. Логи лежат в work, credentials не печатаются.

## AOT

Windows toolchain в этом окружении — Mono. Для отдельного AOT smoke используется WebGL IL2CPP High stripping. WebGL не имеет demo IPC listener/managed worker threads: встроенный smoke отправляет запросы через Update и выводит DOM marker. `[Preserve]` на reflected sample methods и explicit generic codecs/typed registrar нужны независимо от наличия атрибутов Runtime. Установленный6000.7.0a2 — alpha Unity6, не Unity7.

Runtime сохраняет SynchronizationContext через await; Unity dispatcher в WebGL завершает Task inline на Update, поскольку managed ThreadPool отсутствует. На desktop completion использует RunContinuationsAsynchronously. Не добавляйте тяжёлую синхронную continuation к WebGL Task. Demo явно включает runInBackground, чтобы внешний клиент/фоновая вкладка не останавливал Update. Проверка cancellation в WebGL охватывает явную отмену токена; timer-based deadline отдельно не квалифицируется этим smoke.
