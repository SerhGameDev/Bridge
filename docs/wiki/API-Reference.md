<!-- Bridge.Core managed wiki source -->
# Публичный API: указатель

Namespace: Bridge.Core. Ниже сигнатуры для поиска в IntelliSense, не самостоятельная программа. Начните с рабочего [quickstart](Getting-Started-NET.md).

| Сущность | Основные операции | Руководство |
| --- | --- | --- |
| BridgeFile | Exists(path); Load/TryLoad(path, options?, serializers?); Save/SaveAtomic(document,path,indented=true); TrySaveAtomic | [Файлы](Working-with-Files.md) |
| BridgeDocument | Create(id); Parse/TryParse(json,options?,source?); Load/TryLoad(Stream,...); UseSerializers(registry); Save(Stream,...); ToJson | [Старт](Getting-Started-NET.md) |
| BridgeDocument | ContainsBlock(id); GetBlock/TryGetBlock; AddBlock(id,type,data,schemaVersion=1); RemoveBlock; SetBlockData | [Блоки](Working-with-Blocks.md) |
| BridgeDocument | HasField(blockId,fieldName); Get<T>; Set<T>; TryGet<T>(...,out T); TryRead<T> | [Блоки](Working-with-Blocks.md) |
| BridgeBlock | Id/Type/SchemaVersion/Data; HasField; Get/Set/Deserialize; References/SetReference | [Блоки](Working-with-Blocks.md) |
| BridgeValue | Parse/From/Object/Array/Null; Kind; Properties/Items; GetProperty/TryGetProperty; As/Get; WithField | [Технический API](../api.md) |
| BridgeSerializer | Register<T>(read,write); Serialize/Deserialize; Clone | [DTO](DTO-and-AOT.md) |
| BridgeQuery / BridgeQuerySpec | Query; WhereType/WhereTypes/WhereId; SelectFields; Limit; Execute/TryExecute; Spec.ToJson/FromJson; ForTypes | [Запросы](LINQ-and-Queries.md) |
| BridgeSelection / Projection | DocumentId/Blocks, Id/Type/SchemaVersion/Data; selection.ToJson | [Запросы](LINQ-and-Queries.md) |
| BridgeReference / resolvers | Document/Block; InMemory.Set; File resolver(root); ResolveReference | [Ссылки](References-and-Workspace.md) |
| BridgeResult / Diagnostic / Exception | Success/Value/Diagnostics; Code/Message/контекст; BridgeException.Diagnostics | [Ошибки](Errors-and-Troubleshooting.md) |
| BridgeReadOptions / ResolutionOptions | Ограничения чтения и графа ссылок | [Формат](JSON-Format.md) |

BridgeReadOptions defaults: 16 MiB UTF-8, depth 64, 100000 blocks, 4096 characters per number. ResolutionOptions: depth32, documents64, total64MiB, edges100000. Изменяйте лимиты явно по потребности приложения. Thread safety: документ и изменяемые registry требуют внешней синхронизации. [Начало](Home.md).
