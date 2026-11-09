<!-- Bridge.Core managed wiki source -->
# DTO, общие конвертеры и AOT

DTO — ваш C# класс для данных. Для чтения произвольного класса нужна явная пара функций. Bridge не исследует поля класса через reflection. Это делает зависимости понятными IL2CPP/AOT.

```csharp
using System;
using Bridge.Core;

var codecs = new BridgeSerializer();
codecs.Register<Settings>(v => new Settings { Speed = v.Get<double>("speed") },
    s => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(s.Speed)));
var first = BridgeDocument.Create("one").UseSerializers(codecs);
first.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":2}"));
var second = BridgeDocument.Parse(first.ToJson()).UseSerializers(codecs);
first.Serializers.Register<Settings>(_ => new Settings { Speed = 99 }, _ => BridgeValue.Null);
if (second.GetBlock("settings").Deserialize<Settings>().Speed != 2) throw new Exception("Registry isolation");
var dto = second.GetBlock("settings").Deserialize<Settings>(); dto.Speed = 3;
if (second.Get<double>("settings", "speed") != 2) throw new Exception("Implicit mutation");
second.SetBlockData("settings", dto);
Console.WriteLine("DTO_PASS: speed=" + second.Get<double>("settings", "speed"));
public sealed class Settings { public double Speed { get; set; } }
```

Ожидается DTO_PASS: speed=3. Для двух файлов можно вызвать BridgeFile.Load(path1, serializers: codecs) и BridgeFile.Load(path2, serializers: codecs). UseSerializers заменяет локальную таблицу её копией, поэтому поздняя регистрация в одном документе/исходном реестре не меняет другие документы. Старый document.Serializers.Register работает. Registry не сериализуется: после Parse/Load явно привяжите его снова.

Копируется таблица, а не замыкания делегатов: используйте stateless converters или собственную синхронизацию mutable state. BridgeDocument и изменяемый BridgeSerializer не thread-safe. FormatException/OverflowException/InvalidCastException конвертера становятся DTO_CONVERSION, BridgeException сохраняется; непредвиденные пользовательские исключения не скрываются. Writer должен вернуть BridgeValue.Null, а не C# null.

SetBlockData заменяет весь data. Конвертер должен явно сохранить дополнительные поля, если они нужны. Для обычных scalar Get/Set DTO не нужен. В Unity используйте явно вызываемые generic-регистрации; пример SmokeSettings имеет [Preserve]. Проверьте собственные DTO на IL2CPP с выбранным stripping, особенно если их использует внешний код. Source generator — возможное будущее развитие, сейчас его нет. [Начало](Home.md).
