# Bridge.Core

Bridge.Core хранит блоки и именованные ссылки в стандартном JSON, предоставляет LINQ и ограниченные сериализуемые запросы. Ядро рассчитано на **netstandard2.1**, не зависит от UnityEngine и не исполняет данные документов. MIT, 2026 SershDev.

[Формат](docs/format-v1.md) · [API](docs/api.md) · [Запросы](docs/queries.md) · [Ссылки](docs/references.md) · [Unity/AOT](docs/unity-compatibility.md) · [English](README.md)

## Локальная установка

0.1.0 — локальный кандидат. GitHub-репозиторий, NuGet-пакет и релиз не опубликованы. Добавьте project reference на `src/Bridge.Core/Bridge.Core.csproj` из этого checkout. Для локального NuGet source соберите пакет через `dotnet pack -c Release -o work/packages`. Одноимённый публичный пакет нельзя считать пакетом этого проекта.

Для Unity выберите Package Manager → Add package from disk → `Packages/com.serhgamedev.bridge.core/package.json`. .NET/Unity используют одни исходники, Newtonsoft подключается официальным Unity-пакетом. Точные результаты Editor/IL2CPP — в [отчёте совместимости](docs/unity-compatibility.md).

## Быстрый старт

Полный консольный пример: чтение, изменение, LINQ, выборка полей, сохранение и повторная загрузка.

```csharp
using System;
using System.IO;
using System.Linq;
using Bridge.Core;

var document = BridgeDocument.Parse("{\"bridgeVersion\":1,\"documentId\":\"demo\",\"blocks\":{\"settings\":{\"type\":\"settings\",\"schemaVersion\":1,\"data\":{\"speed\":1,\"note\":\"private\"}}}}");
var block = document.GetBlock("settings");
block.Set("speed", block.Get<double>("speed") + 1);
var ids = document.Blocks.Where(b => b.Type == "settings").Select(b => b.Id).ToArray();
var selection = document.Query().WhereType("settings").SelectFields("speed").Limit(10).Execute();
Console.WriteLine(selection.ToJson());
using var stream = new MemoryStream();
document.Save(stream);
stream.Position = 0;
var restored = BridgeDocument.Load(stream);
if (ids.Length != 1 || restored.GetBlock("settings").Get<double>("speed") != 2)
    throw new Exception("Quickstart failed.");
```

Parse/Load бросают BridgeException; TryParse/TryLoad возвращают Result с диагностикой. BridgeValue и query projections — независимые неизменяемые snapshots. Изменения DTO применяются явно через SetBlockData после регистрации конвертера; полный пример в [API](docs/api.md).

## Проверки

```text
dotnet restore Bridge.sln
dotnet build Bridge.sln -c Release
dotnet test Bridge.sln -c Release
dotnet run --project samples/Bridge.Console.Sample -c Release -- --performance
dotnet pack src/Bridge.Core/Bridge.Core.csproj -c Release -o work/packages
```

Настроен CI .NET для Windows/Linux, удалённый прогон ещё не выполнялся. Локальные проверки записаны в [отчётах этапов](docs/stages/). Unity/IL2CPP требуют отдельного редактора и не объявляются частью hosted CI.
