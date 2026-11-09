<!-- Bridge managed wiki source -->
# Permissions и default deny

Runtime принимает уже авторизованный BridgeRequestContext. Приложение/transport назначает principal и permission после собственной аутентификации. Клиентские JSON поля role/permission/principal не могут менять эту конфигурацию. Пример trusted reader — [чтение документа](Documents.md); network mapping — [MCP](Codex-MCP.md).

| Permission | Доступ |
|---|---|
| None (default) | Ничего |
| Read | Discovery/read и команды с минимумом Read |
| Control | Read + Control |
| Edit | Control + Edit |
| Admin | Все зарегистрированные уровни |

BridgeCommand без Permission требует Admin. ExecuteLocalAsync явно trusted Admin; не публикуйте его как анонимный network endpoint. Неавторизованный и неизвестный command/source/block/type/field дают одинаковый NOT_FOUND без подтверждения существования. Каталог отфильтрован тем же permission. Private/unmarked/unregistered methods недоступны.

AddDocument/AddDataSource без policy закрыты. AllowTypes **не** открывает поля. AllowFields(type,fields) открывает только названные прямые keys; nested object открывается целиком, поэтому не кладите секрет внутрь разрешённого subtree. Повторный AllowFields заменяет список типа; AllowTypes объединяет типы; AllowBlocks заменяет optional IDs. Empty allowlist ничего не открывает. Immutable policies копируются при регистрации; registry immutable после Build.

Caps: host64KiB request/1MiB response/64pending/30s, data policy100blocks/64KiB projection/16KiB query; изменяются явно через options/WithLimits. Нет partial projection при превышении bytes. Результаты commands тоже могут раскрыть данные: разработчик обязан возвращать безопасный DTO; policy применяется к Query, а не к произвольному handler return.

Нет CLR type names из сети, assembly scanning, eval, выражений LINQ или автоматического refs traversal. Только известные primitives/enums/явные DTO codecs, строгая schema и дополнительные параметры запрещены. Schema subset не является полным JSON Schema engine.

Ошибки не содержат exception message/stack/path; audit opt-in хранит только ограниченные metadata, не arguments. Cancellation не откатывает side effects; повтор mutation после timeout требует прикладного решения. Loopback сам по себе не заменяет аутентификацию. [Результаты](Results.md).
