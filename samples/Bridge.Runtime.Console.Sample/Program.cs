using System;
using System.IO;
using Bridge.Core;
using Bridge.Runtime;
using System.Threading;
using System.Threading.Tasks;

// Scenario 1: registered commands, no document, network or Unity.
using (var simple=BridgeHostBuilder.Create("commands-only").AddCommands(new MathCommands()).Build())
{
    var sum=await simple.ExecuteLocalAsync("math.add",BridgeValue.Parse("{\"a\":2,\"b\":3}"));
    var echo=await simple.ExecuteLocalAsync("text.echo",BridgeValue.Parse("{\"text\":\"hello\"}"));
    if (!sum.Success || sum.Data.As<int>()!=5 || !echo.Success || echo.Data.As<string>()!="hello") throw new Exception("Commands failed.");
    if ((await simple.ExecuteLocalAsync("missing",BridgeValue.Parse("{}"))).Error?.Code!="NOT_FOUND" ||
        (await simple.ExecuteLocalAsync("math.add",BridgeValue.Parse("{}"))).Error?.Code!="INVALID_ARGUMENT") throw new Exception("Diagnostics failed.");
    Console.WriteLine("COMMAND_PASS: add=5; echo=hello");
}

var path=Path.Combine(Path.GetTempPath(),"bridge-runtime-"+Guid.NewGuid().ToString("N")+".bridge.json");
var document=BridgeDocument.Create("private.document.id");
document.AddBlock("settings","settings",BridgeValue.Parse("{\"speed\":1,\"privateNote\":\"secret\"}"));
BridgeFile.Save(document,path);
var config=BridgeFile.Load(path); File.Delete(path);
using var host = BridgeHostBuilder.Create("console-demo")
    .AddDocument("config",config,access:BridgeDataPolicy.ReadOnly().AllowFields("settings","speed"))
    .AddCommands(new MathCommands()).AddCommands(new ConfigCommands(config)).Build();
if (host.CommandsFor(new BridgeRequestContext("reader", BridgePermission.Read)).Count != 2) throw new Exception("Catalog failed.");
Console.WriteLine("CATALOG_PASS: math.add,text.echo");
var reader=new BridgeRequestContext("reader",BridgePermission.Read);
var catalog=await host.DiscoverAsync(reader);
var query=await host.QueryAsync("config",new BridgeQuerySpec(fields:new[]{"speed"}),reader);
if (!catalog.Success || !query.Success || query.ToJson().Contains("privateNote") || BridgeDocument.Parse(query.Data.ToJson()).Get<int>("settings","speed")!=1) throw new Exception("Data query failed.");
var denied=await host.QueryAsync("config",new BridgeQuerySpec(fields:new[]{"privateNote"}),reader);
if (denied.Error?.Code!="NOT_FOUND") throw new Exception("Policy failed.");
Console.WriteLine("DATA_PASS: speed=1; privateNote denied");
var applied=await host.ExecuteAsync("simulation.set_speed",BridgeValue.Parse("{\"speed\":3}"),new BridgeRequestContext("controller",BridgePermission.Control));
var updated=await host.QueryAsync("config",new BridgeQuerySpec(),reader);
if (!applied.Success || BridgeDocument.Parse(updated.Data.ToJson()).Get<double>("settings","speed")!=3) throw new Exception("Update failed.");
Console.WriteLine("UPDATE_PASS: speed=3; persistence remains explicit");
public sealed class MathCommands
{
    [BridgeCommand("math.add", Description="Add two integers", Permission=BridgePermission.Read)]
    public int Add(int a, int b) => a + b;
    [BridgeCommand("text.echo",Description="Echo text asynchronously",Permission=BridgePermission.Read)]
    public async Task<string> Echo(string text,CancellationToken cancellationToken) { await Task.Delay(1,cancellationToken); return text; }
}
public sealed class ConfigCommands
{
    readonly BridgeDocument document;
    public ConfigCommands(BridgeDocument document) { this.document=document; }
    [BridgeCommand("simulation.set_speed",Permission=BridgePermission.Control)]
    public double SetSpeed([BridgeRange(0.1,10)] double speed) { document.Set("settings","speed",speed); return speed; }
}
