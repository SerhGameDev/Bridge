# Ожидаемый API Bridge.Core (контракт Stage 0)

Следующие примеры задают желаемую эргономику. Они являются набросками, не гарантируют существование реализации и будут уточняться так, чтобы примеры компилировались после соответствующих этапов.

## Чтение и изменение

```csharp
var document = BridgeDocument.Parse(jsonText);
var block = document.GetBlock("factory.settings");
var speed = block.Get<double>("speed");
block.Set("speed", 2.0);
var settings = block.Deserialize<SimulationSettings>();
document.SetBlockData("factory.settings", settings);
var json = document.ToJson();
```

Изменения DTO не должны менять документ автоматически; только явная операция `SetBlockData`/`Update` применяет DTO.

## LINQ

```csharp
var summaries = document.Blocks
    .Where(b => b.Type == "scene.layout")
    .OrderBy(b => b.Id)
    .Select(b => new { b.Id, b.Type })
    .ToList();
```

`Blocks` будет обычным `IEnumerable<BridgeBlock>`; собственный `IQueryable` provider не планируется.

## Безопасная fluent выборка

```csharp
var selection = document.Query()
    .WhereType("scene.layout")
    .WhereId("factory.layout")
    .SelectFields("objects")
    .Limit(10)
    .Execute();

var compactJson = selection.ToJson();
```

Условия — ограниченная модель `BridgeQuerySpec`, которую можно сериализовать. `SelectFields` выбирает только поля верхнего уровня объекта `data`; для `data` другого JSON-типа будет определён явный документированный результат/диагностика. Выборка не мутирует исходный документ и сериализуется только с выбранными полями.

## Ссылки

Резолвер внедряется приложением, чтобы Core не требовал файловую систему и мог использоваться с Unity `TextAsset`/Addressables:

```csharp
var resolved = await resolver.ResolveAsync(document, "factory.layout", "palette");
```

Финальная сигнатура, Result-тип и политика исключений будут закреплены при реализации. Показанный вызов не является готовым компилируемым API.
