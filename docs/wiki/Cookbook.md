<!-- Bridge.Core managed wiki source -->
# 8 практических рецептов

Каждый блок — самостоятельный Program.cs для проекта из .NET quickstart. Входной JSON задан в программе. Ожидаемый вывод каждого: RECIPE_PASS.

## Проверить наличие

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
if (!d.ContainsBlock("settings") || !d.HasField("settings", "speed")) throw new Exception("Missing");
Console.WriteLine("RECIPE_PASS");
```

## Прочитать необязательное поле

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
if (d.TryGet<double>("settings", "absent", out _)) throw new Exception("Unexpected");
Console.WriteLine("RECIPE_PASS");
```

## Прочитать null

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
if (!d.HasField("settings", "note") || d.Get<string?>("settings", "note") != null) throw new Exception("Null");
Console.WriteLine("RECIPE_PASS");
```

## Обработать неверный тип

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
var r = d.TryRead<int>("settings", "note"); if (r.Success || r.Diagnostics[0].Code != "VALUE_TYPE") throw new Exception("Type");
Console.WriteLine("RECIPE_PASS");
```

## Изменить скорость

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
d.Set("settings", "speed", d.Get<double>("settings", "speed") * 2); if (d.Get<double>("settings", "speed") != 2) throw new Exception("Update");
Console.WriteLine("RECIPE_PASS");
```

## Выбрать блоки LINQ

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
var ids = d.Blocks.Where(b => b.Type == "settings").Select(b => b.Id).ToArray(); if (ids.Length != 1) throw new Exception("LINQ");
Console.WriteLine("RECIPE_PASS");
```

## Вернуть только разрешённое поле

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
var r = d.Query().WhereTypes("settings", "layout").SelectFields("speed").Execute(); if (r.ToJson().Contains("note")) throw new Exception("Leak");
Console.WriteLine("RECIPE_PASS");
```

## Прочитать вложенный объект и массив явно

```csharp
using System;
using System.Linq;
using Bridge.Core;
var d = BridgeDocument.Create("recipe");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"note\":null,\"items\":[1,2,3],\"nested\":{\"x\":4}}"));
var data = d.GetBlock("settings").Data; if (data.GetProperty("nested").Get<int>("x") != 4 || data.GetProperty("items").Items[1].As<int>() != 2) throw new Exception("Nested");
Console.WriteLine("RECIPE_PASS");
```

[Начало](Home.md).
