using System;
using System.IO;
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

public sealed class SimulationSettings
{
    public double Speed { get; set; }
    public double Gravity { get; set; }
}
