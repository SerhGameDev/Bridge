using System;
using Bridge.Core;
using Bridge.Runtime;

using var host = BridgeHostBuilder.Create("console-demo").AddDocument("config", BridgeDocument.Create("config")).AddCommands(new MathCommands()).Build();
Console.WriteLine("HOST_PASS: " + host.Name);
public sealed class MathCommands { public int Add(int a, int b) => a + b; }
