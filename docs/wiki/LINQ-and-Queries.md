<!-- Bridge.Core managed wiki source -->
# LINQ и сериализуемые запросы

System.Linq — обычный локальный C# над Blocks: им удобно искать все блоки нескольких типов. Этот код не сериализуется в безопасную спецификацию. Fluent Query сохраняет только ограниченный словарь операций и создаёт независимый результат.

```csharp
using System;
using System.Linq;
using Bridge.Core;

var d = BridgeDocument.Create("query");
d.AddBlock("b", "simulation.settings", BridgeValue.Parse("{\"speed\":2,\"private\":9}"));
d.AddBlock("a", "scene.layout", BridgeValue.Parse("{\"speed\":1,\"private\":8}"));
var ids = d.Blocks.Where(b => b.Type == "scene.layout" || b.Type == "simulation.settings").Select(b => b.Id).ToArray();
var q = d.Query().WhereTypes("scene.layout", "simulation.settings").SelectFields("speed").Limit(10);
var spec = BridgeQuerySpec.FromJson(q.Spec.ToJson());
var selected = d.Query(spec).Execute();
if (ids.Length != 2 || selected.Blocks[0].Id != "a" || selected.ToJson().Contains("private")) throw new Exception("Projection");
Console.WriteLine("QUERY_PASS");
```

Ожидается QUERY_PASS. OR внутри WhereTypes, AND с WhereId. Повторные WhereType/WhereTypes заменяют type-фильтр, исходный builder не меняется. Пустой список типов — пустой результат, дубли удаляются ordinal; регистр значим. Порядок ID ordinal, limit после фильтрации; Limit(0) пуст, отрицательный limit — ошибка. SelectFields выбирает прямые поля object-data, недостающее поле — QUERY_FIELD_NOT_FOUND, другой kind — QUERY_DATA_KIND. Смешанные типы должны иметь совместимые поля проекции. Проекция не включает refs/расширения исходного блока.

```json
{"queryVersion":2,"types":["scene.layout","simulation.settings"],"fields":["speed"],"limit":10}
```

Старые single-type спецификации сохраняют queryVersion 1 и type. В v2 требуется types; любые неизвестные операции отвергаются. Не передавайте произвольный C# expression/JsonPath. Для глубоко вложенных объектов используйте явный код приложения; [RFC будущей выборки](../query-v2-proposal.md) не является реализованным API. [Начало](Home.md).
