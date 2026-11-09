<!-- Bridge managed wiki source -->
# Добавляем Bridge-документ и читаем его извне

Документ хранит конфигурацию; публичное имя config — source, settings — block ID, simulation.settings — type, speed — field. В примере секретное поле есть в документе, но policy его не открывает.

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
var document = BridgeDocument.Create("private.config");
document.AddBlock("settings","simulation.settings",BridgeValue.Parse("{\"speed\":1,\"privateNote\":\"closed\"}"));
using var host = BridgeHostBuilder.Create("data")
    .AddDocument("config", document, BridgeDataPolicy.ReadOnly().AllowFields("simulation.settings","speed"))
    .Build();
var reader = new BridgeRequestContext("authenticated-reader",BridgePermission.Read);
var result = await host.QueryAsync("config",new BridgeQuerySpec(fields:new[]{"speed"}),reader);
if (!result.Success || result.ToJson().Contains("privateNote")) throw new Exception("Policy failed");
Console.WriteLine(BridgeDocument.Parse(result.Data.ToJson()).Get<int>("settings","speed"));
```

Вывод: `1`. Исходный documentId, source paths, refs, extensions, revision/time не раскрываются. Проекция имеет публичный documentId=config и schemaVersion=1. Без policy AddDocument закрыт; ReadOnly() само по себе полей не открывает. AllowFields открывает указанный тип и прямые поля. Для закрытого блока запросите ограничение AllowBlocks; явный запрос закрытого ID получает NOT_FOUND.

Для настоящего файла создайте `factory.bridge.json` с полным содержимым ниже и замените создание документа на `BridgeFile.Load("factory.bridge.json")`:

```json
{"bridgeVersion":1,"documentId":"factory","blocks":{"settings":{"type":"simulation.settings","schemaVersion":1,"data":{"speed":1,"privateNote":"closed"}}}}
```

Изменять document разрешайте через свой command handler на том же dispatcher. Повторный Query увидит изменение после завершения команды. Прямой Set извне dispatcher — ваша ответственность за синхронизацию; автоматической записи на диск нет. Полный runnable Load → command update → Query sample: `samples/Bridge.Runtime.Console.Sample`. [Динамические данные](Dynamic-Data.md).
