<!-- Bridge.Core managed wiki source -->
# Первое приложение .NET

Используйте .NET 8 SDK и checkout feature-ветки v0.2. Из каталога репозитория можно сразу выполнить `dotnet run --project samples/Bridge.Console.Beginner -c Release`: ожидается `BEGINNER_PASS: speed=2; projected=1`.

Для своего приложения создайте `dotnet new console -n FirstBridge` рядом с checkout, затем `dotnet add FirstBridge reference Bridge/src/Bridge.Core/Bridge.Core.csproj`. Путь исправьте под свой каталог. Не устанавливайте одноимённый публичный NuGet-пакет: этот проект его не публиковал. Замените Program.cs полным примером:

```csharp
using System;
using Bridge.Core;

const string path = "factory.bridge.json";
var document = BridgeDocument.Create("factory");
document.AddBlock("settings", "simulation.settings", BridgeValue.Parse("{\"speed\":1}"));
BridgeFile.SaveAtomic(document, path);
if (!BridgeFile.Exists("factory.bridge.json")) throw new Exception("Файл не создан");
document = BridgeFile.Load(path);
if (document.ContainsBlock("settings") && document.HasField("settings", "speed"))
{
    double speed = document.Get<double>("settings", "speed");
    document.Set("settings", "speed", speed * 2);
}
BridgeFile.SaveAtomic(document, path);
if (BridgeFile.Load(path).Get<double>("settings", "speed") != 2) throw new Exception("Ошибка сохранения");
Console.WriteLine("speed=2");
```

Запуск: `dotnet run --project FirstBridge`. Ожидается `speed=2`. Программа намеренно создаёт или заменяет factory.bridge.json в текущем рабочем каталоге. Входные данные заданы в AddBlock: `settings` — точный ID блока, `simulation.settings` — его type, `speed` — прямое поле внутри data. Это не имя файла и не глобальная переменная. Регистр значим.

В примере первый блок создаётся через JSON-значение; при обычном изменении чисел/строк ручной BridgeValue не требуется. Для обработки пользовательского ввода вместо Load используйте [TryLoad](Working-with-Files.md). Далее: [блоки](Working-with-Blocks.md). [Начало](Home.md).
