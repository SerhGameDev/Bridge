<!-- Bridge.Core managed wiki source -->
# Ссылки и рабочая область

Ссылки именованные и разрешаются явно. Локальная ссылка использует document=""; внешняя — относительный .bridge.json. Никакого автоматического исполнения или сетевой загрузки нет. Отдельного BridgeWorkspace в v0.2 нет.

```csharp
using System;
using Bridge.Core;

var first = BridgeDocument.Create("factory");
first.AddBlock("settings", "settings", BridgeValue.Parse("{}"));
var palette = BridgeDocument.Create("palette");
palette.AddBlock("colors", "palette", BridgeValue.From("red"));
first.GetBlock("settings").SetReference("palette", new BridgeReference("palette.bridge.json", "colors"));
var resolver = new InMemoryBridgeDocumentResolver(); resolver.Set("palette.bridge.json", palette);
var resolved = first.ResolveReference("settings", "palette", resolver, "factory.bridge.json");
if (!resolved.Success || resolved.Value.Block.Deserialize<string>() != "red") throw new Exception("Reference");
Console.WriteLine("REFERENCE_PASS");
```

Ожидается REFERENCE_PASS. InMemoryBridgeDocumentResolver подходит для заранее загруженных TextAsset. FileBridgeDocumentResolver(root) ограничивает пути корнем; запрещает абсолютные пути, URL, UNC/ADS/devices, выход через .. и symlinks/reparse points. Циклы, глубина, число документов, суммарные bytes и visited edges ограничены. Исходный путь нужен для относительных ссылок; ID документа и имя файла различаются.

Root должен оставаться доверенным и стабильным. Portable managed resolver не обеспечивает атомарную защиту от враждебной конкурентной подмены каталогов: для этого нужен собственный resolver на открытых handles или защита root. Загрузку Addressables/сети/async выполняет приложение до in-memory resolution. Для нескольких файлов без ссылок используйте несколько BridgeFile.Load с одним набором конвертеров. [Технические ограничения](../references.md). [Начало](Home.md).
