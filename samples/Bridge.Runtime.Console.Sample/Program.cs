using System;
using System.IO;
using Bridge.Core;
using Bridge.Runtime;

var path=Path.Combine(Path.GetTempPath(),"bridge-runtime-"+Guid.NewGuid().ToString("N")+".bridge.json");
var document=BridgeDocument.Create("private.document.id");
document.AddBlock("settings","settings",BridgeValue.Parse("{\"speed\":1,\"privateNote\":\"secret\"}"));
BridgeFile.Save(document,path);
var config=BridgeFile.Load(path); File.Delete(path);
using var host = BridgeHostBuilder.Create("console-demo")
    .AddDocument("config",config,access:BridgeDataPolicy.ReadOnly().AllowFields("settings","speed"))
    .AddCommands(new MathCommands()).Build();
if (host.CommandsFor(new BridgeRequestContext("reader", BridgePermission.Read)).Count != 1) throw new Exception("Catalog failed.");
Console.WriteLine("CATALOG_PASS: math.add");
var reader=new BridgeRequestContext("reader",BridgePermission.Read);
var catalog=await host.DiscoverAsync(reader);
var query=await host.QueryAsync("config",new BridgeQuerySpec(fields:new[]{"speed"}),reader);
if (!catalog.Success || !query.Success || query.ToJson().Contains("privateNote") || BridgeDocument.Parse(query.Data.ToJson()).Get<int>("settings","speed")!=1) throw new Exception("Data query failed.");
var denied=await host.QueryAsync("config",new BridgeQuerySpec(fields:new[]{"privateNote"}),reader);
if (denied.Error?.Code!="NOT_FOUND") throw new Exception("Policy failed.");
Console.WriteLine("DATA_PASS: speed=1; privateNote denied");
public sealed class MathCommands
{
    [BridgeCommand("math.add", Description="Add two integers", Permission=BridgePermission.Read)]
    public int Add(int a, int b) => a + b;
}
