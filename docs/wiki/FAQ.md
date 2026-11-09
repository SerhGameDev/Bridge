<!-- Bridge.Core managed wiki source -->
# Частые вопросы

1. **Чем Bridge отличается от обычного JSON?** Договором о документе, блоках, версиях и ссылках, типизированными операциями и диагностикой. Сам текст остаётся стандартным JSON.
2. **Нужен ли Unity?** Нет. Core netstandard2.1 работает в .NET; Unity нужен только для Unity sample.
3. **Как проверить speed?** document.HasField("settings", "speed"). Это прямое поле data блока settings, регистр значим.
4. **Почему TryGet вернул false?** Нет блока или поля. Неверный тип не превращается в false; используйте TryRead, чтобы получить диагностику без BridgeException.
5. **Как отличить null?** HasField true и Get<string?>/TryGet<string?> возвращает null; non-nullable число даёт VALUE_TYPE.
6. **Как прочитать вложенное поле?** Явно GetBlock(...).Data.GetProperty("object").Get<T>("field"); HasField не интерпретирует точки/слеши. JSON Pointer API пока нет.
7. **Можно ли Python?** Python может читать стандартный .bridge.json своим JSON parser. C# API и типизированные конвертеры Bridge.Core предназначены для .NET; Python binding не предоставляется.
8. **Почему Bridge не исполняет команды?** Это библиотека данных. Исполнение, безопасность команд и предметный runtime — ответственность отдельного приложения.
9. **Как загрузить несколько файлов?** Несколько BridgeFile.Load(path, serializers: registry). Для явных refs используйте resolver.
10. **Exists=true гарантирует Load?** Нет: файл может исчезнуть/быть недоступным или содержать invalid JSON. Проверяйте TryLoad.Success.
11. **Почему DTO изменён, а JSON нет?** DTO независим; вызовите SetBlockData. Это явное применение изменений.
12. **Можно ли передавать один документ между потоками?** Только с собственной синхронизацией. Полученные BridgeValue snapshots неизменяемы, но block handles принадлежат изменяемому документу.
13. **Где NuGet и Release?** Пока не опубликованы. Используйте checkout/project reference, локальный pack или Git UPM.
14. **Что означает BLOCK_REMOVED?** Handle относится к удалённому блоку; получите новый через GetBlock после Add. Старый handle не оживает.

[Рецепты](Cookbook.md). [Начало](Home.md).
