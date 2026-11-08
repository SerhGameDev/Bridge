using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;

public class MutationTests
{
    [Fact] public void CrudUsesExactIdsAndDoesNotMutateSnapshots()
    {
        var doc = BridgeDocument.Parse(DocumentTests.Json);
        var before = doc.GetBlock("a").Data;
        doc.GetBlock("a").Set("speed", 2.0);
        Assert.Equal(1.25, before.Get<double>("speed"));
        Assert.Equal(2.0, doc.GetBlock("a").Get<double>("speed"));
        Assert.False(doc.TryGetBlock("A", out _));
        Assert.Equal("BLOCK_NOT_FOUND", Assert.Throws<BridgeException>(() => doc.GetBlock("A")).Diagnostics[0].Code);
        Assert.Equal("FIELD_NOT_FOUND", Assert.Throws<BridgeException>(() => doc.GetBlock("a").Get<int>("missing")).Diagnostics[0].Code);
        Assert.Equal("VALUE_TYPE", Assert.Throws<BridgeException>(() => doc.GetBlock("a").Get<string>("speed")).Diagnostics[0].Code);
        Assert.Throws<BridgeException>(() => doc.AddBlock("a", "duplicate", BridgeValue.Null));
        doc.AddBlock("A", "new", BridgeValue.Null);
        Assert.True(doc.RemoveBlock("A")); Assert.False(doc.RemoveBlock("A"));
        doc.SetBlockData("a", BridgeValue.Array(new[] { BridgeValue.Null, BridgeValue.From("✓") }));
        Assert.Equal("✓", doc.GetBlock("a").Data.Items[1].As<string>());
        Assert.Throws<BridgeException>(() => doc.GetBlock("a").Set("field", 1));
    }

    public sealed class Settings { public double Speed { get; set; } }
    [Fact] public void ExplicitDtoRoundTripNeverMutatesDocumentImplicitly()
    {
        var doc = BridgeDocument.Parse(DocumentTests.Json);
        Assert.Throws<BridgeException>(() => doc.GetBlock("a").Deserialize<Settings>());
        doc.Serializers.Register<Settings>(v => new Settings { Speed = v.Get<double>("speed") },
            s => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(s.Speed)));
        var dto = doc.GetBlock("a").Deserialize<Settings>(); dto.Speed = 9;
        Assert.Equal(1.25, doc.GetBlock("a").Get<double>("speed"));
        doc.SetBlockData("a", dto); dto.Speed = 100;
        Assert.Equal(9, doc.GetBlock("a").Get<double>("speed"));
        Assert.Equal(9, BridgeDocument.Parse(doc.ToJson()).GetBlock("a").Get<double>("speed"));
    }

    [Theory]
    [InlineData("null")][InlineData("true")][InlineData("\"Юникод \\uD83D\\uDE80\"")]
    [InlineData("[[1,2],[3,null],{\"x.y\":0,\"x\":{\"y\":1}}]")]
    [InlineData("123456789012345678901234567890.123456789e1000")]
    public void AllKindsRoundTripWithoutLoss(string data)
    {
        var doc = BridgeDocument.Create("doc"); doc.AddBlock("x", "value", BridgeValue.Parse(data));
        using var output = new MemoryStream(); doc.Save(output); Assert.True(output.CanWrite);
        output.Position = 0; var copy = BridgeDocument.Load(output);
        Assert.Equal(doc.ToJson(), copy.ToJson());
    }

    [Fact] public void ExtensionsExactKeysArraysAndUtf8ArePreservedWithInvariantOutput()
    {
        var doc = BridgeDocument.Parse("{\"bridgeVersion\":1,\"documentId\":\"ü\",\"extra\":{\"x\":1e1000},\"blocks\":{\"b\":{\"type\":\"t\",\"schemaVersion\":1,\"custom\":true,\"data\":{\"z\":[3,2,1],\"a.b\":2,\"a\":{\"b\":1}}}}}");
        Assert.Equal(2, doc.GetBlock("b").Get<int>("a.b"));
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            doc.GetBlock("b").Set("decimal", 1.5);
            using var output = new MemoryStream(); doc.Save(output);
            var bytes = output.ToArray(); Assert.False(bytes.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
            string json = Encoding.UTF8.GetString(bytes);
            Assert.Contains("1.5", json); Assert.Contains("1e1000", json); Assert.Contains("\"custom\":true", json);
            Assert.Contains("[3,2,1]", json); Assert.Equal(json, BridgeDocument.Parse(json).ToJson());
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact] public void DeterministicOrderAndNonFiniteValues()
    {
        var a = BridgeDocument.Create("a"); a.AddBlock("z", "t", BridgeValue.Parse("{\"z\":1,\"a\":2}")); a.AddBlock("a", "t", BridgeValue.Null);
        var b = BridgeDocument.Create("a"); b.AddBlock("a", "t", BridgeValue.Null); b.AddBlock("z", "t", BridgeValue.Parse("{\"a\":2,\"z\":1}"));
        Assert.Equal(a.ToJson(), b.ToJson());
        Assert.Throws<BridgeException>(() => BridgeValue.From(double.NaN));
        Assert.Throws<BridgeException>(() => BridgeValue.From(float.PositiveInfinity));
        Assert.Throws<BridgeException>(() => BridgeValue.Parse("1e1000").As<double>());
        Assert.Equal(3, BridgeValue.From((int?)3).As<int?>());
        Assert.Null(BridgeValue.Null.As<string>());
    }
}
