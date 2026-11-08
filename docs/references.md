# Именованные ссылки

BridgeReference содержит Document (relative путь либо пустая строка) и Block (точный ID). References — read-only snapshot; SetReference явно добавляет/заменяет метаданные. Parse/Get не загружают документы, Save не инлайнит цели.

```csharp
using System;
using Bridge.Core;

var source = BridgeDocument.Create("factory");
source.AddBlock("layout", "scene", BridgeValue.Null).SetReference("palette", new BridgeReference("assets/palette.bridge.json", "colors"));
var palette = BridgeDocument.Create("palette");
palette.AddBlock("colors", "palette", BridgeValue.From("red"));
var resolver = new InMemoryBridgeDocumentResolver();
resolver.Set("assets/palette.bridge.json", palette);
var result = source.ResolveReference("layout", "palette", resolver, sourcePath: "factory.bridge.json");
if (!result.Success) throw new BridgeException(result.Diagnostics);
if (result.Value.Block.Deserialize<string>() != "red") throw new Exception("Reference failed.");
```

ResolveReference возвращает Result<BridgeResolvedBlock> с Document/Block/DocumentPath. Проверяется весь достижимый от выбранной цели граф; неверная вложенная ссылка также делает операцию неуспешной. Локальная ссылка на другой блок допустима, self/cross-file cycle отвергается. sourcePath — явный root-relative путь текущего документа, в том числе in-memory; Source диагностики его не заменяет.

## Резолверы

IBridgeDocumentResolver.Load получает нормализованный root-relative путь и возвращает Result. PathComparer задаёт сравнение путей отдельно от ordinal block ID. InMemoryBridgeDocumentResolver.Set позволяет обновить документ, default comparer ordinal. FileBridgeDocumentResolver использует явно разрешённый root, сравнение путей платформы и BridgeReadOptions.

```csharp
using System;
using System.IO;
using Bridge.Core;

string root = Path.GetFullPath("bridge-reference-example");
Directory.CreateDirectory(root);
var palette = BridgeDocument.Create("palette");
palette.AddBlock("colors", "palette", BridgeValue.From("red"));
using (var output = File.Create(Path.Combine(root, "palette.bridge.json"))) palette.Save(output);
var source = BridgeDocument.Create("factory");
source.AddBlock("layout", "scene", BridgeValue.Null).SetReference("palette", new BridgeReference("palette.bridge.json", "colors"));
var result = source.ResolveReference("layout", "palette", new FileBridgeDocumentResolver(root), "factory.bridge.json");
if (!result.Success) throw new BridgeException(result.Diagnostics);
Console.WriteLine(result.Value.Block.Deserialize<string>());
```

Встроенная политика допускает только относительные .bridge.json пути. Slash/backslash нормализуются, '.' убирается, '..' разрешён только внутри корня. URL/UNC/absolute/drive paths, ADS/colon, control characters, Windows reserved device names, trailing dot/space segments отвергаются. Percent-encoding не декодируется и является буквальным именем.

File resolver отвергает symlinks/reparse points, включая root и его предков; проверяет путь до и после открытия файла. Он рассчитан на стабильный доверенный root. Portable managed API не гарантирует атомарную защиту от враждебной конкурентной замены каталогов; для этого нужен свой handle-based resolver и защищённый root. Собственный resolver приложения также отвечает за свою политику сети/доступа.

## Лимиты, кеш и ошибки

Кеш существует одну операцию ResolveReference, каждый нормализованный документ загружается максимум один раз. Новый вызов не использует старый файловый кеш. Defaults: MaxDepth 32 рёбра; MaxDocuments 64 включая источник; MaxTotalUtf8Bytes 64 MiB канонического JSON; MaxVisitedEdges 100000. Последний ограничивает стоимость многократного обхода DAG. Per-document read limits также действуют. Ошибка не оставляет частичного кеша между операциями.

Коды: REFERENCE_NOT_FOUND/DOCUMENT_NOT_FOUND/BLOCK_NOT_FOUND/PATH/IO/CYCLE/DEPTH/DOCUMENT_LIMIT/SIZE_LIMIT/EDGE_LIMIT (все с префиксом REFERENCE_). Ошибки чтения/JSON внешнего документа сохраняют исходные коды и source path.

Unity может заранее разобрать TextAsset/Addressables в in-memory resolver либо предоставить собственный resolver поверх подготовленного кеша. Асинхронная загрузка — задача приложения; интерфейс Core синхронный и не зависит от Unity.
