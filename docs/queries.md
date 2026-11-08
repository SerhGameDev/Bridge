# LINQ и сериализуемые запросы

Blocks реализует IEnumerable<BridgeBlock>. System.Linq выполняется локальным C# кодом. Fluent API использует неизменяемый BridgeQuerySpec (type/id/fields/limit). JSON не принимает произвольные выражения, IQueryable или JsonPath.

```csharp
using System;
using System.Linq;
using Bridge.Core;

var document = BridgeDocument.Create("query");
document.AddBlock("layout", "scene", BridgeValue.Parse("{\"objects\":[1],\"private\":42}"));
var ids = document.Blocks.Where(b => b.Type == "scene").Select(b => b.Id).ToArray();
var query = document.Query().WhereType("scene").WhereId("layout").SelectFields("objects").Limit(10);
var spec = BridgeQuerySpec.FromJson(query.Spec.ToJson());
var selection = document.Query(spec).Execute();
if (ids.Length != 1 || selection.ToJson().Contains("private")) throw new Exception("Query failed.");
Console.WriteLine(selection.ToJson());
```

Фильтры type/id точные, case-sensitive, сочетаются AND. Повторная установка той же операции заменяет её значение; builder возвращает новый запрос. Порядок — ID ordinal, limit применяется после фильтров. Limit(0) даёт пустой результат, отрицательный limit — ошибка аргумента. Без SelectFields возвращается весь data; пустой список полей даёт пустой объект. Повторяющиеся поля устраняются.

SelectFields выбирает верхнеуровневые поля объекта data. Отсутствующее поле даёт QUERY_FIELD_NOT_FOUND, non-object data — QUERY_DATA_KIND. Execute бросает BridgeException; TryExecute возвращает Result. Имя `a.b` буквальное, без вложенного перехода.

BridgeSelection/BridgeProjection — независимые неизменяемые snapshots. Их JSON содержит bridgeVersion/documentId и только выбранные блоки с type/schemaVersion/data. refs и неизвестные расширения источника не включаются. Результат можно разобрать как самостоятельный BridgeDocument.

JSON queryVersion 1 разрешает только queryVersion/type/id/fields/limit; иные операции/версии/типы дают QUERY_SPEC. Авторизация принадлежит приложению. Performance smoke 1k/10k запускается Console Sample с `--performance`; [отчёт этапа 3](stages/03-query.md) содержит реальные измерения без аппаратного SLA.
