# Решения по дизайну

## Границы и данные

Bridge.Core — независимое netstandard2.1 ядро для JSON документов. Core не зависит от UnityEngine, не исполняет data/команды/методы и не реализует Runtime/MCP/AI/авторизацию. Формат использует bridgeVersion 1 и явный положительный schemaVersion блока. Неизвестные версии документа отклоняются, extensions сохраняются. refs — метаданные, $ref/$type в data не интерпретируются.

Newtonsoft.Json NuGet 13.0.4 и официальный Unity package 3.2.2/DLL 13.0.2 проверены фактически. Newtonsoft разбирает структуру JSON. Лексический адаптер необходим из-за permissive JavaScript extensions и потери точности стандартного float parsing: он проверяет lexemes, заменяет числа безопасными placeholders для структурного parser и восстанавливает исходные JRaw значения после parsing. Публичный API скрывает JToken; strictness/точность покрыты тестами.

## API и сценарии

| Сущности | Практический сценарий |
| --- | --- |
| BridgeDocument/BridgeBlock | Владение JSON, доступ к блокам и явное изменение. |
| BridgeValue/BridgeValueKind | Неизменяемые detached JSON значения без backend DOM в публичных сигнатурах. |
| BridgeDiagnostic/Severity/Result/Exception | Ожидаемые ошибки и реальные source/path/line/column. Parse/Load throws; Try формы дают Result. |
| BridgeReadOptions | Ограниченное чтение строки/потока. |
| BridgeSerializer | Типизированные функции DTO без runtime генерации/AOT сюрпризов. |
| BridgeQuerySpec/BridgeQuery | Serializable ограниченные операции и удобный fluent builder. |
| BridgeSelection/BridgeProjection | Read-only snapshots с сериализацией только выбранных данных. |
| BridgeReference/IBridgeDocumentResolver/встроенные резолверы | Метаданные ссылок и внедряемый источник, включая Unity preloaded assets. |
| BridgeResolvedBlock/BridgeResolutionOptions | Целевой блок вместе с документом/путём и ограничениями обхода. |

Изменение DTO не меняет JSON автоматически. Registry принадлежит документу, требует повторной регистрации после Load и не является сериализуемым состоянием. Reflection serializer отсутствует. Get/Set выбирают буквальные поля верхнего уровня data, без JsonPath. Blocks — обычный IEnumerable, без IQueryable provider. Query JSON не исполняет выражения.

## Ссылки и ограничения

Разрешение только явное; операция проверяет достижимый граф и ограничена depth/docs/bytes/edges. Кеш живёт одну операцию, новый вызов читает внешние файлы заново. Встроенный file resolver работает под заданным стабильным root и отвергает symlinks/reparse points. Ограничение атомарной защиты от конкурентной замены дерева документировано в references.md; для такого приложения нужен свой resolver.

## Источник кода и совместимость

Runtime исходники живут в Packages/com.serhgamedev.bridge.core/Runtime и linked Compile items .NET проекта. Unity .asmdef имеет noEngineReferences. Samples/Tests отдельно. Лицензия MIT, 2026 SershDev. Ветки этапов после проверок включаются fast-forward в локальный main по команде владельца. Пакеты/релизы/удалённый GitHub не публикуются.

Точные форматы/лимиты: format-v1.md, queries.md, references.md. Реальные Unity/IL2CPP результаты: unity-compatibility.md. Ожидаемая будущая совместимость: future-compatibility.md. История последовательной проверки: stages/ и task-checklist.md.
