using System;
using Bridge.Core;
using Bridge.Runtime;

using var host = BridgeHostBuilder.Create("console-demo").AddDocument("config", BridgeDocument.Create("config")).AddCommands(new MathCommands()).Build();
if (host.CommandsFor(new BridgeRequestContext("reader", BridgePermission.Read)).Count != 1) throw new Exception("Catalog failed.");
Console.WriteLine("CATALOG_PASS: math.add");
public sealed class MathCommands
{
    [BridgeCommand("math.add", Description="Add two integers", Permission=BridgePermission.Read)]
    public int Add(int a, int b) => a + b;
}
