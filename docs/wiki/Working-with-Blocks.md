<!-- Bridge.Core managed wiki source -->
# Блоки, поля и значения

ContainsBlock проверяет ID, HasField — только прямой ключ data. Оба учитывают регистр. HasField возвращает false для отсутствующего блока, non-object data или отсутствующего ключа. Null, false, 0 и пустая строка — существующие значения. Точки и слеши в ключе буквальны; вложенных путей в HasField нет.

```csharp
using System;
using Bridge.Core;

var d = BridgeDocument.Create("demo");
d.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":0,\"note\":null,\"enabled\":false,\"label\":\"\",\"a.b\":3}"));
if (!d.ContainsBlock("settings") || d.ContainsBlock("Settings")) throw new Exception("ID");
if (!d.HasField("settings", "note") || d.HasField("absent", "speed")) throw new Exception("Presence");
if (d.TryGet<double>("settings", "missing", out _)) throw new Exception("Missing");
if (!d.TryGet<string?>("settings", "note", out var note) || note != null) throw new Exception("Null");
var wrong = d.TryRead<int>("settings", "label");
if (wrong.Success || wrong.Diagnostics[0].Code != "VALUE_TYPE") throw new Exception("Type");
try { d.TryGet<int>("settings", "label", out _); throw new Exception("Type hidden"); }
catch (BridgeException error) { Console.WriteLine(error.Diagnostics[0].Code); }
d.Set("settings", "speed", 2d);
if (d.Get<double>("settings", "speed") != 2 || d.Get<int>("settings", "a.b") != 3) throw new Exception("Value");
Console.WriteLine("BLOCKS_PASS");
```

Запустите как Program.cs в проекте из .NET quickstart. Ожидается VALUE_TYPE и BLOCKS_PASS. TryGet даёт false только для missing блока/поля, но бросает BridgeException для неверного типа/non-object data. TryRead возвращает BridgeResult для всех ожидаемых ошибок данных. Проверяйте Success перед Value; Value ошибочного результата бросает BridgeException. Get/Set также бросают диагностируемые ошибки. Set может добавить прямое поле существующего object-блока; произвольный блок не создаётся. Null/пустые/whitespace аргументы convenience API дают ArgumentException.

Удаление инвалидирует block handle: после RemoveBlock вызовы Get/Set/Data/Type/References сообщают BLOCK_REMOVED; повторный Add с тем же ID создаёт новый handle. Id и SchemaVersion старого handle остаются историческими. Уже полученный BridgeValue остаётся неизменяемым снимком. Blocks — кэшированный снимок состава с живыми handles, не глубокая копия документа. Документ не thread-safe. [Начало](Home.md).
