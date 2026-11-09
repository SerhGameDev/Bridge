# Первый консольный пример

Из корня checkout v0.2: dotnet run --project samples/Bridge.Console.Beginner -c Release.

Ожидается BEGINNER_PASS: speed=2; projected=1. Программа создаёт временный JSON, сохраняет атомарно, проверяет Exists, загружает через TryLoad, проверяет блок/поле, меняет speed, выполняет LINQ и WhereTypes projection, сохраняет и повторно читает. Без аргумента временный файл удаляется. С аргументом пути программа создаёт или заменяет указанный пользовательский файл и оставляет его.
