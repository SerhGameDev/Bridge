# Runtime R1 — host skeleton

Base integration a2260789629f93814578794ea7a4a8098b04a781 (R0 PR2 merged, Windows/Ubuntu CI42602210266 success). Branch feature/runtime-r1-host.

Мини-план: создать независимый netstandard2.1 Runtime, один source .NET/UPM, immutable-after-Build host, явное владение и serial dispatcher; затем lifecycle/duplicate tests и console bootstrap. Реализованы BridgeHostBuilder.Create/AddDocument/AddDataSource/AddCommands/UseDispatcher/UseOptions/Build, Host.Name/Dispose, trusted context/permission hierarchy, immutable data policy configuration, snapshot/source contracts и SerializedBridgeDispatcher. Build не открывает сеть/поток. Несколько hosts независимы. Изменяемый builder после Build запрещён; injected dispatcher по умолчанию не owned, source/command instances всегда принадлежат приложению.

Файлы: новый Packages/com.serhgamedev.bridge.runtime с asmdef/MIT, linked src/Bridge.Runtime, tests/Bridge.Runtime.Tests, samples/Bridge.Runtime.Console.Sample, Bridge.sln и CI. Схемы/исполнение команд ещё не реализованы: это R2/R4. Policy только настроена, её фактическое enforcement появится в R3; внешнего пути чтения пока нет.

Проверки: dotnet build -c Release — PASS0warnings/errors; dotnet test -c Release --no-build — Core109/109 + Runtime4/4 PASS. Console sample — HOST_PASS: console-demo. Тесты проверяют freeze/duplicate/exact names, independent hosts, explicit ownership, serial async dispatch, disposal queued work, assembly boundary. CI этого этапа и merge фиксируются в PR перед интеграцией.

Минимальный runnable bootstrap (Program.cs, ProjectReference на src/Bridge.Runtime):

```csharp
using System;
using Bridge.Runtime;
using var host = BridgeHostBuilder.Create("demo").Build();
Console.WriteLine(host.Name);
```

Ожидается demo. Следующий этап — R2 attributes/registry/binder/schema, только после зелёного CI и принятия PR этого этапа.
