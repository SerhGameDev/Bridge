# Bridge.Core

Bridge.Core хранит версионированные блоки данных и ссылки в стандартном JSON. Ядро netstandard2.1 подходит для .NET и Unity, использует MIT и не зависит от UnityEngine.

**[Начать с руководства](docs/wiki/Home.md)** · [English](README.md) · [Технический API](docs/api.md) · [Формат](docs/format-v1.md)

## Первый пример

v0.2 разрабатывается в feature/core-v0.2-developer-experience. Добавьте ProjectReference на src/Bridge.Core/Bridge.Core.csproj из checkout этой ветки либо запустите готовое приложение: `dotnet run --project samples/Bridge.Console.Beginner -c Release`.

```csharp
using System;
using Bridge.Core;
var d = BridgeDocument.Create("factory");
d.AddBlock("settings", "simulation.settings", BridgeValue.Parse("{\"speed\":1}"));
if (d.ContainsBlock("settings") && d.HasField("settings", "speed"))
    d.Set("settings", "speed", d.Get<double>("settings", "speed") * 2);
if (d.Get<double>("settings", "speed") != 2) throw new Exception("Ошибка примера");
Console.WriteLine("speed=2");
```

Полный маршрут с SaveAtomic → Exists → Load → Get/Set → повторным открытием: [первый файл .NET](docs/wiki/Getting-Started-NET.md). Установка UPM по Git URL, TextAsset и runnable MonoBehaviour: [Unity](docs/wiki/Getting-Started-Unity.md). Документация содержит 8 рецептов, FAQ, DTO/AOT и диагностику missing/null/type mismatch.

Репозиторий опубликован: https://github.com/SerhGameDev/Bridge. NuGet, GitHub Release и теги версии не публиковались. Wiki включена, но её хранилище ещё не инициализировано; до публикации ссылки ведут на полный набор docs/wiki. Эти файлы — единый источник; [синхронизация](docs/wiki-sync.md).

## Проверки

```text
dotnet restore Bridge.sln
dotnet build Bridge.sln -c Release --no-restore
dotnet test Bridge.sln -c Release --no-build
pwsh -File tools/Verify-Snippets.ps1
dotnet pack src/Bridge.Core/Bridge.Core.csproj -c Release -o work/packages
```

CI настроен на Windows/Linux. Актуальные результаты, Unity/IL2CPP ограничения и команды — в [отчёте v0.2](docs/verification-report-v0.2.md). [Исторический отчёт v0.1](docs/verification-report.md) описывает состояние до публикации репозитория.
