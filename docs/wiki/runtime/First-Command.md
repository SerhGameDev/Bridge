<!-- Bridge managed wiki source -->
# Первая команда за пять минут

После [создания console проекта](Installation-NET.md) замените Program.cs **целиком**. Этот пример не использует Bridge-файл или MCP.

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var host = BridgeHostBuilder.Create("first").AddCommands(new MathCommands()).Build();
var result = await host.ExecuteLocalAsync("math.add", BridgeValue.Parse("{\"a\":2,\"b\":3}"));
if (!result.Success) throw new Exception(result.Error!.Code);
Console.WriteLine("sum=" + result.Data.As<int>());
public sealed class MathCommands
{
    [BridgeCommand("math.add", Description="Add integers", Permission=BridgePermission.Read)]
    public int Add(int a, int b) => a + b;
}
```

Ожидается `sum=5`. AddCommands получает конкретный экземпляр; только его public instance методы с BridgeCommand попадают в immutable каталог. Неотмеченные методы и private методы не публикуются. Build не включает listener и не сканирует assemblies.

ExecuteLocalAsync — **явно доверенный Admin путь** внутри приложения. Для внешнего клиента создавайте BridgeRequestContext на доверенной стороне после аутентификации; не преобразуйте клиентское поле permission в этот context. [Подробнее о безопасности](Security.md).

Измените a на строку или уберите b: result.Success станет false, error.code — INVALID_ARGUMENT. Не вызывайте Data.As<int>() после неуспешного результата. Исправьте аргументы и отправьте новый запрос; Runtime автоматически ничего не повторяет. [Следующий шаг: данные](Documents.md).
