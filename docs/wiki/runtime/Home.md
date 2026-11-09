<!-- Bridge managed wiki source -->
# Bridge.Runtime: от первой команды к готовому Player

Runtime связывает явно зарегистрированные C# обработчики с разрешёнными данными Bridge.Core. Обычное приложение работает без Unity, MCP, сети и Bridge-файла. Unity adapter добавляет главный поток; отдельный MCP gateway подключает внешнего клиента к готовому Player.

| Термин | Простое значение |
|---|---|
| Документ | Bridge JSON с именованными блоками |
| Блок | ID, type, schemaVersion и data, например settings |
| Поле | Прямой ключ data, например speed |
| source | Публичное имя документа или динамического снимка, например config/scene |
| command | Уникальное публичное имя зарегистрированного обработчика |
| principal | Идентичность, назначенная доверенным приложением после авторизации |

Маршрут новичка:

1. [Установка .NET](Installation-NET.md) → [первая команда за5минут](First-Command.md).
2. [Документ и чтение](Documents.md) → [динамические данные](Dynamic-Data.md).
3. [Dispatcher](Dispatchers.md) → [permissions/default deny](Security.md) → [результаты и ошибки](Results.md).
4. [Unity](Installation-Unity.md) → [готовый Player и Codex/MCP](Codex-MCP.md).
5. [12 рецептов и FAQ](Cookbook.md), [API reference](API-Reference.md), [свои расширения](Advanced.md), [troubleshooting](Troubleshooting.md).

Runtime не создаёт физику, PLC, Python SDK, редактор, Bridge Language, произвольный eval или automatic save. QuerySpec — данные, а C# LINQ — локальный код: лямбды из сети не выполняются. Команды запускают только зарегистрированный разработчиком C# код.

Исходники MIT; SDK dependencies сохраняют [свои лицензии](../../../THIRD-PARTY-NOTICES.md). Пакеты не опубликованы в NuGet/registry, теги/Release не создавались. Этот полный docs/wiki маршрут — опубликованный в репозитории fallback; отдельная GitHub Wiki пока не инициализирована. [Безопасная синхронизация](../../wiki-sync.md). [Архитектурные решения](../../runtime/architecture.md).
