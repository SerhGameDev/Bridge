# Runtime R4 — выполнение, права, очередь, отмена

Base integration0ede2b15f9d68d551ddd107cd4e28e7ea096995b (R3 PR5, CI42605357281 success). Branch feature/runtime-r4-execution. Мини-план: public execute/local path, sanitization/audit и verification concurrency/lifecycle без ложного acknowledgement.

ExecuteAsync(command,arguments,context) проверяет permission/registry и argument schema до handler. ExecuteLocalAsync явно создаёт trusted local Admin. Read<Control<Edit<Admin, None закрыт; unknown и запрещённая команда одинаково NOT_FOUND. Context назначает сервер, не JSON body. Все discovery/query/execute используют один dispatcher; serial gate удерживается через await обработчика. Одновременные query не видят промежуточный count/checksum update. Reentrant запрос в тот же host отвергается REENTRANT_REQUEST вместо ожидания собственного gate.

ExecuteAsync возвращает Completed только после handler completion; ожидающая очередь представлена незавершённым Task, Queued в MVP не выдаётся. Нет polling/авторетрай/dedup. Повтор с тем же RequestId — новый OperationId и новое исполнение. После timeout/cancellation/disposal результат Cancelled, но side effects не откатываются: некооперативный handler всё ещё может завершить изменение, gate остаётся занят до фактического окончания. Host.Dispose не блокирует owner thread и отменяет очередь.

AuditCapacity0 по умолчанию выключает audit; при включении ring buffer ограничен0..10000, metadata principal/request/operation/command/status/code/exception type, без аргументов, exception message, stack и секретных имён unknown commands. GetAuditSnapshot — локальный C# API, не transport tool. Handler exceptions дают HANDLER_FAILED без раскрытия текста. Ошибка output schema — Failed/INVALID_RESULT, не успешное действие. Fatal OutOfMemory/AccessViolation не подавляются.

Файлы: Runtime CommandExecution.cs, общий Respond/Contracts/Result, ExecutionTests.cs и Console sample. Проверки: Release0warnings/errors, Core109/109 + Runtime36/36 PASS. Тесты role negative cases, no unauthorized effects, coherent concurrent reads/writes, bounded pending queue, cancellation/disposal/timeout, no false Completed/rollback, sanitized exceptions/bounded audit/no automatic retries, reentrancy. Console реально выполняет команды без документов/MCP, затем BridgeFile/Core query/controlled update с сохранением privateNote закрытым.

Следующий этап после CI/PR — R5 Unity owner-thread adapter, demo и настоящие players. Runtime не имеет Unity/MCP references.

Финальный regression после исправления self-cancellation и output DTO: Runtime **37/37**, Core109/109; CI42606835845 PASS на Windows/Ubuntu. PR6 merged, integration f21f7344fec404a4bf9a3dc8d7032c0d96af93f1.
