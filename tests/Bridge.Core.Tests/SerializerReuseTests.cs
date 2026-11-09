using System;
using System.IO;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;
public class SerializerReuseTests
{
    public sealed class Settings { public int Speed; }
    static BridgeSerializer Registry()
    {
        var registry = new BridgeSerializer();
        registry.Register<Settings>(v => new Settings { Speed = v.Get<int>("speed") }, s => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(s.Speed)));
        return registry;
    }
    [Fact] public void BindingsAreSnapshotsAndExistingBlockHandlesUseNewRegistry()
    {
        var registry = Registry();
        var first = BridgeDocument.Create("one"); var block = first.AddBlock("b", "t", BridgeValue.Parse("{\"speed\":2}"));
        first.UseSerializers(registry);
        using var stream = new MemoryStream(); first.Save(stream); stream.Position = 0;
        var second = BridgeDocument.Load(stream).UseSerializers(registry);
        first.Serializers.Register<Settings>(_ => new Settings { Speed = 99 }, _ => BridgeValue.Null);
        registry.Register<Settings>(_ => new Settings { Speed = 100 }, _ => BridgeValue.Null);
        Assert.Equal(99, block.Deserialize<Settings>().Speed);
        Assert.Equal(2, second.GetBlock("b").Deserialize<Settings>().Speed);
        Assert.Equal(100, registry.Clone().Deserialize<Settings>(BridgeValue.Null).Speed);
        Assert.Same(second, second.UseSerializers(second.Serializers));
        Assert.Equal(2, second.GetBlock("b").Deserialize<Settings>().Speed);
    }
    [Fact] public void FileRegistryIsReusableAndExpectedConverterFailuresAreDiagnostic()
    {
        string path = Path.Combine(Path.GetTempPath(), "bridge-registry-" + Guid.NewGuid().ToString("N") + ".bridge.json");
        try
        {
            var registry = Registry(); var doc = BridgeDocument.Create("d"); doc.AddBlock("b", "t", BridgeValue.Parse("{\"speed\":3}"));
            BridgeFile.Save(doc, path);
            Assert.Equal(3, BridgeFile.Load(path, serializers: registry).GetBlock("b").Deserialize<Settings>().Speed);
            Assert.True(BridgeFile.TryLoad(path, serializers: registry).Success);
            doc.UseSerializers(registry); doc.Serializers.Register<Settings>(_ => throw new FormatException("invalid DTO"), _ => throw new OverflowException("overflow"));
            var error = Assert.Throws<BridgeException>(() => doc.GetBlock("b").Deserialize<Settings>()).Diagnostics[0];
            Assert.Equal("DTO_CONVERSION", error.Code); Assert.Equal("d", error.DocumentId); Assert.Equal("/blocks/b/data", error.Path);
            Assert.Throws<BridgeException>(() => doc.SetBlockData("b", new Settings()));
            doc.Serializers.Register<Settings>(_ => new Settings(), _ => null!);
            Assert.Equal("DTO_CONVERSION", Assert.Throws<BridgeException>(() => doc.SetBlockData("b", new Settings())).Diagnostics[0].Code);
        }
        finally { File.Delete(path); }
    }
}
