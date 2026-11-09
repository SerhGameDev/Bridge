<!-- Bridge.Core managed wiki source -->
# Bridge.Core: начало

Bridge.Core — библиотека C# для данных в JSON. Документ содержит именованные блоки: например, блок `settings` хранит скорость, а блок `layout` — описание расположения. Библиотека проверяет формат, читает и меняет данные, выбирает блоки и разрешает явные ссылки между документами.

Нужны основы C# и .NET 8 SDK для запуска примеров. Ядро рассчитано на netstandard2.1 и пригодно для .NET/Unity. Команды из JSON не выполняются; runtime, физики, PLC, AI и MCP в библиотеке нет. Обычный JSON по-прежнему можно читать другими языками.

Начните с [первого приложения .NET](Getting-Started-NET.md) или [Unity](Getting-Started-Unity.md). Затем прочтите [формат](JSON-Format.md), [блоки и поля](Working-with-Blocks.md), [файлы](Working-with-Files.md), [запросы](LINQ-and-Queries.md), [DTO/AOT](DTO-and-AOT.md), [ссылки](References-and-Workspace.md) и [ошибки](Errors-and-Troubleshooting.md).

Для копирования: [8 рецептов](Cookbook.md). Для поиска: [API](API-Reference.md) и [FAQ](FAQ.md). [English quickstart](Getting-Started-English.md).

Это документация v0.2 в feature-ветке. NuGet-пакет и GitHub Release не опубликованы. Публичная Wiki ещё не инициализирована; основной источник — эти файлы `docs/wiki`. При публикации скрипт Sync-Wiki.ps1 копирует их и переписывает внутренние ссылки под Wiki; правки вносите сюда.
