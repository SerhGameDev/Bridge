<!-- Bridge managed wiki source -->
# FAQ и12 рецептов

Каждый csharp блок ниже — отдельный полный Program.cs с ProjectReference на Runtime, как в [установке](Installation-NET.md). Блоки автоматически компилируются и выполняются Verify-Snippets.ps1. Unity-specific программы лежат в импортируемом Scene Demo и компилируются настоящим Editor.

## 1. Узнать доступные команды

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var host=BridgeHostBuilder.Create("catalog").AddCommands(new Commands()).Build();
var r=await host.DiscoverAsync(new BridgeRequestContext("reader",BridgePermission.Read));
if(!r.Success || !r.ToJson().Contains("math.one"))throw new Exception("Discovery failed");
Console.WriteLine("catalog=math.one");
public sealed class Commands { [BridgeCommand("math.one",Permission=BridgePermission.Read)] public int One()=>1; }
```

## 2. Async с CancellationToken

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using var host=BridgeHostBuilder.Create("async").AddCommands(new Commands()).Build();
var r=await host.ExecuteLocalAsync("text.echo",BridgeValue.Parse("{\"text\":\"hello\"}"));
if(!r.Success)throw new Exception(r.Error!.Code);
Console.WriteLine(r.Data.As<string>());
public sealed class Commands
{
 [BridgeCommand("text.echo",Permission=BridgePermission.Read)]
 public async Task<string> Echo(string text,CancellationToken token){await Task.Delay(1,token);return text;}
}
```

## 3. Optional и nullable reference input

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var host=BridgeHostBuilder.Create("optional").AddCommands(new Commands()).Build();
var r=await host.ExecuteLocalAsync("text.optional",BridgeValue.Parse("{}"));
if(!r.Success)throw new Exception(r.Error!.Code);
Console.WriteLine(r.Data.As<string>());
public sealed class Commands
{
 [BridgeCommand("text.optional")]
 public string Echo([BridgeAllowNull] string? text=null)=>text??"empty";
}
```

Вывод empty. CancellationToken не становится клиентским required argument; null не равен missing для required parameter.

## 4. Enum с явной регистрацией

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
var codecs=new BridgeCodecRegistry().RegisterEnum<Mode>();
using var host=BridgeHostBuilder.Create("enum").UseCodecs(codecs).AddCommands(new Commands()).Build();
var r=await host.ExecuteLocalAsync("mode.echo",BridgeValue.Parse("{\"mode\":\"Ready\"}"));
if(!r.Success)throw new Exception(r.Error!.Code);
Console.WriteLine(r.Data.As<string>());
public enum Mode { Ready, Running }
public sealed class Commands { [BridgeCommand("mode.echo")] public Mode Echo(Mode mode)=>mode; }
```

Wire names case-sensitive, numeric enum input запрещён. Unity reflected method дополнительно Preserve.

## 5. DTO и typed registrar без reflection-команды

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
var schema=BridgeValue.Parse("{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"number\"}},\"required\":[\"x\"],\"additionalProperties\":false}");
var codecs=new BridgeCodecRegistry().Register<Point>(schema,v=>new Point(v.Get<double>("x")),
 p=>BridgeValue.Object(new Dictionary<string,BridgeValue>{{"x",BridgeValue.From(p.X)}}));
using var host=BridgeHostBuilder.Create("dto").UseCodecs(codecs)
 .AddCommand<Point,Point>("point.echo",(p,ct)=>Task.FromResult(p),BridgePermission.Read).Build();
var r=await host.ExecuteLocalAsync("point.echo",BridgeValue.Parse("{\"x\":2.5}"));
if(!r.Success)throw new Exception(r.Error!.Code);
Console.WriteLine(r.Data.Get<double>("x"));
public sealed class Point { public double X {get;} public Point(double x){X=x;} }
```

Вывод2.5 (2,5 при русской CultureInfo). Для `[BridgeCommand]` с DTO-parameter используйте тот же UseCodecs; это другой путь регистрации, codec schema та же. Core BridgeSerializer для document DTO регистрируется отдельно.

## 6. Проверить диапазон до handler

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var host=BridgeHostBuilder.Create("range").AddCommands(new Commands()).Build();
var r=await host.ExecuteLocalAsync("speed.set",BridgeValue.Parse("{\"speed\":20}"));
if(r.Success || r.Error!.Code!="INVALID_ARGUMENT")throw new Exception("Range failed");
Console.WriteLine(r.Error.Code);
public sealed class Commands { [BridgeCommand("speed.set")] public double Speed([BridgeRange(0.1,10)]double speed)=>speed; }
```

## 7. Ограничить блоки и поля

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
var d=BridgeDocument.Create("config");d.AddBlock("visible","settings",BridgeValue.Parse("{\"speed\":1}"));d.AddBlock("hidden","settings",BridgeValue.Parse("{\"speed\":2}"));
using var host=BridgeHostBuilder.Create("policy").AddDocument("config",d,
 BridgeDataPolicy.ReadOnly().AllowFields("settings","speed").AllowBlocks("visible")).Build();
var r=await host.QueryAsync("config",new BridgeQuerySpec(id:"hidden"),new BridgeRequestContext("reader",BridgePermission.Read));
if(r.Error?.Code!="NOT_FOUND")throw new Exception("Policy failed");
Console.WriteLine(r.Error.Code);
```

## 8. Читать свежее состояние без cache

Скопируйте полный [CounterSource пример](Dynamic-Data.md), выполните два Query. Expected2 доказывает повторное чтение. Добавьте `.WithLimits(maxBlocks:10)` к policy, если снимок может содержать много блоков; no selectors всё равно ограничены policy cap. Unity SceneWorld использует тот же IBridgeDataSource contract для реальных Transform.

## 9. Кооперативный timeout

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using var host=BridgeHostBuilder.Create("timeout").UseOptions(new BridgeHostOptions {RequestTimeout=TimeSpan.FromMilliseconds(30)}).AddCommands(new Commands()).Build();
var r=await host.ExecuteLocalAsync("wait",BridgeValue.Parse("{}"));
if(r.Status!=BridgeCommandStatus.Cancelled || r.Error?.Code!="REQUEST_TIMEOUT")throw new Exception("Timeout failed");
Console.WriteLine(r.Error.Code);
public sealed class Commands
{
 [BridgeCommand("wait")] public async Task<int> Wait(CancellationToken token){await Task.Delay(10000,token);return 1;}
}
```

Проверяется .NET cooperative Task.Delay; не обещает preemption синхронного кода или timer deadline WebGL.

## 10. Явно сохранить изменённый документ

```csharp
using System;
using System.IO;
using Bridge.Core;
using Bridge.Runtime;
var d=BridgeDocument.Create("config");d.AddBlock("settings","settings",BridgeValue.Parse("{\"speed\":1}"));
var path=Path.Combine(Path.GetTempPath(),"bridge-doc-"+Guid.NewGuid().ToString("N")+".json");
using var host=BridgeHostBuilder.Create("save").AddCommands(new Commands(d)).Build();
try
{
 var r=await host.ExecuteLocalAsync("speed.set",BridgeValue.Parse("{\"speed\":2}"));if(!r.Success)throw new Exception(r.Error!.Code);
 BridgeFile.SaveAtomic(d,path);Console.WriteLine("persisted="+BridgeFile.Load(path).Get<int>("settings","speed"));
}
finally {if(File.Exists(path))File.Delete(path);}
public sealed class Commands
{
 readonly BridgeDocument d;public Commands(BridgeDocument d){this.d=d;}
 [BridgeCommand("speed.set")] public int Set(int speed){d.Set("settings","speed",speed);return speed;}
}
```

## 11. Два независимых host

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var left=BridgeHostBuilder.Create("left").AddCommands(new Commands(1)).Build();
using var right=BridgeHostBuilder.Create("right").AddCommands(new Commands(2)).Build();
var a=await left.ExecuteLocalAsync("value",BridgeValue.Parse("{}"));var b=await right.ExecuteLocalAsync("value",BridgeValue.Parse("{}"));
if(!a.Success || !b.Success || a.Data.As<int>()==b.Data.As<int>())throw new Exception("Isolation failed");
Console.WriteLine("1,2");
public sealed class Commands {readonly int value;public Commands(int value){this.value=value;}[BridgeCommand("value")]public int Value()=>value;}
```

## 12. Локальный audit без утечки exception message

```csharp
using System;
using Bridge.Core;
using Bridge.Runtime;
using var host=BridgeHostBuilder.Create("audit").UseOptions(new BridgeHostOptions{AuditCapacity=2}).AddCommands(new Commands()).Build();
var r=await host.ExecuteLocalAsync("fail",BridgeValue.Parse("{}"));
if(r.Success || r.ToJson().Contains("private-detail") || host.GetAuditSnapshot().Count!=1)throw new Exception("Audit failed");
Console.WriteLine(host.GetAuditSnapshot()[0].ErrorCode);
public sealed class Commands { [BridgeCommand("fail")]public int Fail()=>throw new Exception("private-detail"); }
```

## FAQ

Нужен MCP для локального C#? Нет. Нужен Bridge-файл для command-only host? Нет. ReadOnly() открывает весь документ? Нет, поля открываются явно. UnityEngine входит в Runtime? Нет, отдельный adapter. Почему мой DTO отвергнут? Нужен explicit codec с формальной схемой; arbitrary CLR reflection serialization отсутствует. Почему повтор requestId повторил изменение? Deduplication MVP не реализует. Почему не записался файл? Save явный, после согласованного изменения. Можно ValueTask? MVP поддерживает Task/Task<T>, ValueTask вне scope. Почему Queued не приходит? Task ждёт фактическое выполнение. Можно удалённый HTTP? Этот gateway stdio; remote security/hosting — отдельная работа.
