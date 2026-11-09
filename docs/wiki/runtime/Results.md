<!-- Bridge managed wiki source -->
# Возвраты, ожидание и ошибки

ExecuteAsync/QueryAsync/DiscoverAsync возвращают Task<BridgeCommandResult>. Пока запрос стоит в очереди, Task остаётся pending. MVP не отправляет acknowledgement: Queued зарезервирован, но никогда не выдаётся. Completed означает успешное завершение реального handler/чтения, не принятие в очередь.

| Status | Значение |
|---|---|
| Completed | Success=true, data содержит результат |
| Rejected | NOT_FOUND, INVALID_ARGUMENT, BUSY, REQUEST_LIMIT, HOST_STOPPED |
| Failed | HANDLER_FAILED, OPERATION_FAILED, INVALID_RESULT, RESPONSE_LIMIT |
| Cancelled | REQUEST_CANCELLED/REQUEST_TIMEOUT/HOST_STOPPED; side effects могли произойти |

Bridge envelope: status, requestId, operationId, data, error(code/message). Идентификаторы запросов не реализуют deduplication: повтор одного requestId снова вызывает handler и получает новый operationId. void/Task без результата → JSON null; Task<T> и sync T используют известный codec. Nullable value types поддержаны; nullable reference input явно отмечается BridgeAllowNull. Optional argument использует объявленный default.

Всегда проверяйте Success/Status перед Data.As<T>(). INVALID_ARGUMENT исправляется по discovery schema; NOT_FOUND не различает закрытый ресурс и опечатку. REENTRANT_REQUEST означает вложенный request того же host из handler: вместо него вызовите свою C# логику напрямую, чтобы не ждать собственный serial gate.

Timeout/cancellation кооперативны. Некооперативный handler может закончить side effect позже; gate остаётся занят до его фактического окончания. Dispose не блокирует owner thread и отменяет queued/running requests, но не откатывает изменения. Автоматических retries нет. WebGL smoke подтверждает manual cancellation, не даёт отдельной квалификации timer-based deadline.

MCP mapping сохраняет Bridge error.code и ставит isError=true. Outer protocol errors остаются JSON-RPC errors; Queued не подделывается. [MCP guide](Codex-MCP.md), [troubleshooting](Troubleshooting.md).
