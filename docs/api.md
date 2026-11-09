# API Bridge.Core 0.2

Типы находятся в namespace Bridge.Core. Newtonsoft DOM скрыт из публичных сигнатур. Core работает с данными и не выполняет команды/выражения из JSON.

## Загрузка и диагностика

Parse(string, options?, source?) и Load(Stream, options?, source?) возвращают готовый документ либо бросают BridgeException. TryParse/TryLoad возвращают BridgeResult<BridgeDocument>; проверяйте Success до Value. Ошибочный Value бросает BridgeException. Нарушения C# контракта (null аргументы, отрицательные лимиты) дают ArgumentException. Поток остаётся открытым; Load принимает UTF-8 с/без BOM. Парсинг строки не требует файловой системы.

BridgeDiagnostic содержит Code/Severity/Message/DocumentId/Source/Path/Line/Column. Координаты указываются только если известны parser/lexer; схема может не иметь line/column. Path диагностический, не API JsonPath. Проверка сообщает первую ошибку и не возвращает частичный документ.

```csharp
using System;
using Bridge.Core;

var result = BridgeDocument.TryParse("{", source: "broken.bridge.json");
if (result.Success) throw new Exception("Expected invalid input.");
foreach (var diagnostic in result.Diagnostics)
    Console.WriteLine($"{diagnostic.Code} {diagnostic.Source}:{diagnostic.Line}:{diagnostic.Column}");
```

Основные коды: JSON_SYNTAX/JSON_DUPLICATE_KEY/JSON_ENCODING, FORMAT_SCHEMA/FORMAT_VERSION, INPUT_LIMIT/DOCUMENT_IO, BLOCK_NOT_FOUND/BLOCK_DUPLICATE/FIELD_NOT_FOUND/VALUE_TYPE/DTO_CONVERTER_MISSING. Query/Reference коды описаны отдельно.

## Блоки и JSON-значения

Create(documentId) создаёт пустой документ. Blocks — read-only snapshot списка, отсортированного по ID ordinal; блоки внутри допускают явные изменения. GetBlock бросает BLOCK_NOT_FOUND, TryGetBlock имеет bool/out форму. AddBlock отвергает дубли; RemoveBlock возвращает false при отсутствии. ID/type/schemaVersion фиксированы при создании.

```csharp
using System;
using Bridge.Core;

var document = BridgeDocument.Create("crud");
var block = document.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1}"));
var snapshot = block.Data;
block.Set("speed", 2.0);
if (snapshot.Get<double>("speed") != 1) throw new Exception("Snapshot mutated.");
document.SetBlockData("settings", BridgeValue.Parse("[1,null,[2,3]]"));
Console.WriteLine(document.GetBlock("settings").Data.Items[2].ToJson());
if (!document.RemoveBlock("settings")) throw new Exception("Remove failed.");
```

BridgeValue неизменяем: Kind, GetProperty/TryGetProperty, Properties, Items, As<T>/Get<T>, WithField, Parse, From<T>, Object, Array, Null. WithField возвращает новое значение; factories копируют входные данные. Имена точные: `a.b` — буквальное свойство. Отсутствие поля отличается от null. Документ и registry не потокобезопасны.

## DTO и typed значения

Поддержаны string/bool, целые CLR-типы, float/double/decimal/BigInteger, nullable варианты и BridgeValue. Typed числовое чтение ограничено точностью CLR-типа; исходная лексема JSON не округляется. Для целочисленных CLR-типов требуется целая лексема без экспоненты/дроби. NaN/Infinity запрещены. DTO требуют явно зарегистрированной пары функций; reflection serializer отсутствует.

```csharp
using System;
using Bridge.Core;

var document = BridgeDocument.Create("dto");
document.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1}"));
document.Serializers.Register<Settings>(
    value => new Settings { Speed = value.Get<double>("speed") },
    dto => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(dto.Speed)));
var settings = document.GetBlock("settings").Deserialize<Settings>();
settings.Speed = 3;
if (document.GetBlock("settings").Get<double>("speed") != 1) throw new Exception("Implicit mutation.");
document.SetBlockData("settings", settings);
if (document.GetBlock("settings").Get<double>("speed") != 3) throw new Exception("Update failed.");

public sealed class Settings { public double Speed { get; set; } }
```

Registry принадлежит документу и не сериализуется. Используйте .UseSerializers(sharedRegistry) после Create/Parse/Load(Stream), либо BridgeFile.Load(path, serializers: sharedRegistry). Таблица регистраций копируется; изменения одного документа не меняют другой. Делегаты и состояние их замыканий не клонируются. Конвертер должен создавать независимый DTO. SetBlockData полностью заменяет data; сохранение дополнительных полей при DTO update — задача конвертера. См. [AOT](aot.md).

## Запись

ToJson(indented=false) и Save(Stream,indented=false) сортируют object keys ordinal, сохраняют порядок массивов/неизвестные поля/числовые лексемы. Save пишет UTF-8 без BOM, оставляет поток открытым. Путь файла выбирает приложение.

```csharp
using System.IO;
using Bridge.Core;

var document = BridgeDocument.Create("file-example");
document.AddBlock("message", "text", BridgeValue.From("Привет"));
using (var output = File.Create("example.bridge.json")) document.Save(output, indented: true);
using var input = File.OpenRead("example.bridge.json");
var loaded = BridgeDocument.Load(input, source: "example.bridge.json");
if (loaded.GetBlock("message").Deserialize<string>() != "Привет") throw new System.Exception("Round-trip failed.");
```

Ошибки Get/Set/Deserialize/SetBlockData включают известные DocumentId и Source; путь к данным блока записан как JSON Pointer. Parser diagnostics сохраняют собственный Newtonsoft path. Некорректный Unicode (непарные surrogates) отвергается, числовые typed read соблюдают точность целевого типа. Повторный Register<T> заменяет конвертер этого типа.

Удобный API v0.2 и полные примеры: [блоки/поля](wiki/Working-with-Blocks.md), [файлы](wiki/Working-with-Files.md), [общие DTO](wiki/DTO-and-AOT.md). Removed handles сообщают BLOCK_REMOVED; Id/SchemaVersion остаются историческими, ранее выданные BridgeValue snapshots валидны.
