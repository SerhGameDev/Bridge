<!-- Bridge managed wiki source -->
# Ошибки и восстановление

| Симптом | Что проверить |
|---|---|
| command отсутствует в discovery | Экземпляр передан AddCommands, public instance метод отмечен, permission достаточен |
| Build/registration exception | Duplicate name, неподдержанный object/Type/Delegate/generic/ref/out/params, незарегистрированный enum/DTO codec |
| Source скрыт/NOT_FOUND | Read permission + explicit policy + AllowFields, точные ordinal имена; AllowTypes полей не открывает |
| INVALID_ARGUMENT | Required/optional/null, JSON types, range, extra fields, enum exact names из schema |
| INVALID_RESULT | Возвращённый DTO не соответствует output schema, включая лишние keys |
| FIELD_NOT_FOUND | Разрешённое явно выбранное поле отсутствует; implicit projection missing поля пропускает |
| BUSY | Очередь bounded, уменьшите частоту/длину handlers; не делайте автоматических mutation retries |
| REENTRANT_REQUEST | Не вызывайте тот же host из его handler; вызывайте свою C# функцию напрямую |
| REQUEST_TIMEOUT/CANCELLED | Cooperative cancellation, проверьте состояние перед новым mutation |
| Unity зависает | Нет Wait/Result на owner thread, Update работает, handler сохраняет context |
| WebGL async не завершается | Используйте Unity dispatcher; нет managed Task.Run/ThreadPool; не теряйте caller context |
| Unity UPM dependency not found | Установите все3Git packages в указанном порядке; они не опубликованы в registry |
| AOT method/DTO missing | Preserve/link.xml для своего reflection path, explicit generic codecs или typed registrar |
| MCP gateway exit2 | Нужны --enable-stdio и --player/--local-demo, Player credential через environment |
| MCP UNAUTHORIZED/connection refused | Совпадает credential, Player запущен с --bridge-listen, порт19731 свободен |
| Wiki Repository not found | Полный docs/wiki fallback рабочий; отдельное Wiki git storage не инициализировано |

[API](API-Reference.md) · [12 рецептов](Cookbook.md) · [безопасность](Security.md). Ошибки источника/handler санитизированы: подробную локальную диагностику добавляет приложение, не отправляя секреты клиенту. AuditCapacity opt-in сохраняет bounded metadata, не exception message/arguments.
