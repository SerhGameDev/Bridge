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
document.GetBlock("factory.layout").SetReference("palette", new BridgeReference("assets/palette.bridge.json", "common.palette"));
var resolver = new FileBridgeDocumentResolver(AppContext.BaseDirectory);
var palette = document.ResolveReference("factory.layout", "palette", resolver, "factory.bridge.json").Value;
if (palette.Block.Get<string>("color") != "red") throw new Exception("Reference failed.");
document.GetBlock("factory.layout").SetReference("escape", new BridgeReference("../outside.bridge.json", "x"));
var rejected = document.ResolveReference("factory.layout", "escape", resolver, "factory.bridge.json");
if (rejected.Success || rejected.Diagnostics[0].Code != "REFERENCE_PATH") throw new Exception("Sandbox check failed.");
Console.WriteLine("External palette and sandbox PASS");
if (args.Contains("--performance")) PerformanceSmoke.Run();

public sealed class SimulationSettings { public double Speed { get; set; } public double Gravity { get; set; } }
