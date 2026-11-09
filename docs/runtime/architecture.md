# Bridge.Runtime v0.1: решения до реализации

Дата: 2026-11-09. Core v0.2 принят через PR1 в main5e06afd76b4e57251cba77c1ef8bce5f417f30c9. Public Core API проверен по исходникам; baseline109/109 PASS. Runtime разрабатывается от этого main в feature/bridge-runtime-v0.1, этапы R0–R8 — отдельные ветки и PR в неё, затем общий PR в main. Теги/Release/NuGet publish не входят в работу.

## Границы пакетов

Runtime: netstandard2.1/C#9, единственный source в Packages/com.serhgamedev.bridge.runtime/Runtime с linked Compile Include из src/Bridge.Runtime. Зависит только от Core и стандартной библиотеки. Unity adapter и demo находятся в отдельном UPM-пакете. MCP — отдельный .NET gateway, официальный SDK выбирается и проверяется в R6. Ни одного автоматического listener при Build.

## ADR1: доверенная идентичность и default deny

BridgeRequestContext создаётся доверенным приложением/transport после аутентификации, а не из JSON параметров. Principal, RequestId, Permission и CancellationToken обязательны на внешнем пути. Permission hierarchy: None < Read < Control < Edit < Admin; более высокий уровень включает нижние. Локальный ExecuteLocalAsync явно использует trusted Admin. Registry immutable после Build, не сканирует assemblies: AddCommands получает только конкретный объект и только public instance методы с BridgeCommand. Имя команды ordinal/case-sensitive, ограниченный ASCII contract. Неизвестный и недоступный ресурс внешне одинаково NOT_FOUND, чтобы не раскрывать скрытые имена.

## ADR2: данные

AddDocument/AddDataSource без policy закрыты внешнему клиенту. BridgeDataPolicy — immutable builder: точные allowlists типов/полей и необязательных ID блоков; ограничены bytes запроса/ответа и количество блоков. AllowFields явно открывает указанный тип и его прямые поля; AllowTypes не открывает все поля. Query фильтрует до сериализации, не обходит refs, формирует новый документ с публичным source name вместо исходного documentId; extensions/refs/source/revision/time не публикуются автоматически. Запросы к запрещённым явно указанным ID/type/field отвергаются. Query без явных фильтров возвращает только разрешённую проекцию, policy caps действуют и при отсутствии Limit. Dynamic source создаёт согласованный BridgeDataSnapshot на dispatcher; старые снимки не кэшируются хостом.

## ADR3: выполнение и lifecycle

BridgeHostBuilder.Create/AddDocument/AddDataSource/AddCommands/UseDispatcher/Build; никакого singleton. Default Serialized dispatcher — SemaphoreSlim без собственного потока, все query/discovery/execute проходят общий gate. Нет гарантии thread affinity в .NET. Unity adapter имеет очередь, обработку из Update и ожидание через Task; только одна операция до завершения async handler, кадры не ждут синхронно. Не использовать ConfigureAwait(false) вокруг Unity API в handlers.

ExecuteAsync ждёт фактического handler completion. Пока запрос стоит в очереди, Task не завершён; протокол не выдаёт ложный Completed. Enum Queued зарезервирован, acknowledgement/operation polling MVP не предоставляет. Статусы Completed/Rejected/Failed/Cancelled содержат request/operation ID. Cancellation/timeout кооперативны, без rollback и автоматических повторов: uncooperative handler может закончить side effect, gate удерживается до его фактического окончания. Dispose отменяет pending/running requests и закрывает собственный dispatcher, не блокирует owner thread. Очередь ограничена. Внешние ошибки санитизированы; audit ограничен метаданными без argument payload/exception message.

## ADR4: binding/schema/AOT

Атрибуты BridgeCommand/BridgeRange/BridgeDescription. Только JSON primitives, nullable, конечные enum names и явно зарегистрированные типизированные DTO codecs. JSON Schema subset: object/properties/required/additionalProperties=false, scalar type/enum/range/description, nullable; unsupported CLR signatures отвергаются при Build. Никакого CLR typename из JSON, dynamic/eval/Reflection.Emit/Expression.Compile. Sync, Task и Task<T>; ValueTask — вне минимального MVP, если не будет реальной необходимости.

Attribute registration отражает только переданный instance. Для AOT предусмотрен typed registrar с явными delegates/codecs; Unity demo также сохраняет отмеченные методы от stripping. Реальный IL2CPP High player проверяется отдельно. Core не меняется без подтверждённой необходимости.

## ADR5: готовый Player и MCP

Установлены Unity6000.6.7f1 и6000.7.0a2; Unity7 отсутствует. Есть Windows Mono и WebGL IL2CPP toolchains; Windows standalone IL2CPP не установлен. Поэтому машинный MCP E2E запускает настоящий собранный Windows Mono Player, а независимый High-stripping AOT smoke — настоящий WebGL IL2CPP Player в браузере.

MCP SDK не встраивается в Unity. Отдельный .NET stdio gateway обращается к явно запущенному loopback endpoint в Player, transport-independent Host выполняет команды через Unity dispatcher. Listener выключен без явного запуска; opaque token из environment, principal/permission назначаются server config. В R5 выбран length-prefixed TCP только на literal 127.0.0.1 вместо HTTP: не нужны Windows URL ACL, отсутствуют HTTP Origin/DNS rebinding endpoints. Проверяются magic, строгий UTF-8, ограничение 64 KiB/1 MiB, deadline и число connections; никакого bind-all. Это прикладной demo IPC, MCP wire format остаётся только в отдельном gateway. MCP test client не читает Unity sources: только catalog/query/execute. Документируются точные шаги Codex; если Codex лично не подключён, отчёт укажет compatible-client E2E вместо вымышленного Codex pass.

## Последовательность

R1 host/lifetime/source skeleton → R2 command registry/schema/binder → R3 discovery/data policies → R4 executor/permissions/concurrency → R5 Unity adapter/demo/players → R6 official MCP gateway/protocol/player E2E → R7 guides/snippets/packaging/CI → R8 regression/acceptance/final main merge. После каждого шага тесты, stage report, PR и зелёный CI до интеграции. Эти решения не заявляют реализацию будущих этапов.
