using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Bridge.Core;

var document = BridgeDocument.Create("demo.factory");
var block = document.AddBlock("factory.settings", "simulation.settings", BridgeValue.Parse("{\"speed\":1.0,\"gravity\":9.81}"));
document.Serializers.Register<SimulationSettings>(
    v => new SimulationSettings { Speed = v.Get<double>("speed"), Gravity = v.Get<double>("gravity") },
    dto => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(dto.Speed)).WithField("gravity", BridgeValue.From(dto.Gravity)));
block.Set("speed", 2.0);
var dto = block.Deserialize<SimulationSettings>();
dto.Speed = 3;
document.SetBlockData("factory.settings", dto);
using var output = new MemoryStream();
document.Save(output);
output.Position = 0;
var restored = BridgeDocument.Load(output);
if (restored.GetBlock("factory.settings").Get<double>("speed") != 3) throw new Exception("Round-trip failed.");
Console.WriteLine(restored.ToJson(indented: true));
Console.WriteLine("Round-trip PASS");
document.AddBlock("factory.layout", "scene.layout", BridgeValue.Parse("{\"objects\":[{\"id\":\"cube_01\"}],\"privateNote\":\"excluded\"}"));
var summaries = document.Blocks.Where(b => b.Type == "scene.layout").OrderBy(b => b.Id).Select(b => new { b.Id, b.Type }).ToList();
var selection = document.Query().WhereType("scene.layout").WhereId("factory.layout").SelectFields("objects").Limit(10).Execute();
if (summaries.Count != 1 || selection.ToJson().Contains("privateNote")) throw new Exception("Query failed.");
Console.WriteLine(selection.ToJson());
Console.WriteLine("LINQ/fluent PASS");
if (args.Contains("--performance"))
{
    foreach (int count in new[] { 1000, 10000 })
    {
        var large = BridgeDocument.Create("performance");
        for (int i = 0; i < count; i++) large.AddBlock(i.ToString("D5"), i % 2 == 0 ? "even" : "odd", BridgeValue.Parse("{\"value\":1,\"excluded\":2}"));
        long memory = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        var result = large.Query().WhereType("even").SelectFields("value").Execute();
        timer.Stop();
        if (result.Blocks.Count != count / 2) throw new Exception("Performance result count failed.");
        Console.WriteLine($"Performance {count}: {timer.Elapsed.TotalMilliseconds:F3} ms; allocated {GC.GetAllocatedBytesForCurrentThread() - memory} bytes; selected {result.Blocks.Count}");
    }
}

public sealed class SimulationSettings
{
    public double Speed { get; set; }
    public double Gravity { get; set; }
}
