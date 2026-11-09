using System;
using System.Linq;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;
public class QueryLifecycleTests
{
    [Fact] public void MultiTypesAreOrdinalImmutableOrAndReplacePreviousFilter()
    {
        var doc = BridgeDocument.Create("d");
        foreach (var id in new[] { "c", "a", "b" }) doc.AddBlock(id, id == "b" ? "T" : "t", BridgeValue.Parse("{\"x\":1,\"secret\":2}"));
        var source = new[] { "t", "T", "t" }; var query = doc.Query().WhereTypes(source); source[0] = "other";
        Assert.Equal(new[] { "a", "b", "c" }, query.Execute().Blocks.Select(b => b.Id));
        Assert.Equal(2, query.Spec.Types!.Count);
        Assert.Equal("b", Assert.Single(query.WhereId("b").SelectFields("x").Limit(1).Execute().Blocks).Id);
        Assert.Equal(2, query.WhereType("t").Execute().Blocks.Count);
        Assert.Equal(3, query.Execute().Blocks.Count);
        Assert.Empty(query.WhereTypes().Execute().Blocks);
        Assert.Empty(query.Limit(0).Execute().Blocks);
        Assert.Throws<ArgumentNullException>(() => query.WhereTypes(null!));
        Assert.Throws<ArgumentException>(() => query.WhereTypes(" "));
        Assert.Equal("b", Assert.Single(doc.Query().WhereType("t").WhereTypes("T").Execute().Blocks).Id);
    }
    [Fact] public void V1RoundTripIsStableAndV2CannotSneakIntoV1()
    {
        var old = new BridgeQuerySpec("t", "b", new[] { "x" }, 2);
        Assert.Equal(old.ToJson(), BridgeQuerySpec.FromJson(old.ToJson()).ToJson());
        Assert.Contains("\"queryVersion\":1", old.ToJson());
        var multi = BridgeQuerySpec.ForTypes(new[] { "t", "u" }, fields: new[] { "x" });
        Assert.Contains("\"queryVersion\":2", multi.ToJson());
        Assert.Equal(multi.ToJson(), BridgeQuerySpec.FromJson(multi.ToJson()).ToJson());
        Assert.Equal(2, BridgeQuerySpec.FromJson(multi.ToJson()).Types!.Count);
        Assert.Empty(BridgeQuerySpec.FromJson(BridgeQuerySpec.ForTypes(Array.Empty<string>()).ToJson()).Types!);
        foreach (var json in new[] { "{\"queryVersion\":1,\"types\":[]}", "{\"queryVersion\":2,\"type\":\"t\",\"types\":[]}", "{\"queryVersion\":2}", "{\"queryVersion\":2,\"types\":[null]}", "{\"queryVersion\":3}" })
            Assert.Throws<BridgeException>(() => BridgeQuerySpec.FromJson(json));
    }
    [Fact] public void RemovedHandlesCannotWriteAndCachedListsRemainStructuralSnapshots()
    {
        var doc = BridgeDocument.Create("d"); var old = doc.AddBlock("b", "t", BridgeValue.Parse("{\"x\":1}"));
        var data = old.Data; var list = doc.Blocks; Assert.Same(list, doc.Blocks);
        old.Set("x", 2); Assert.Same(list, doc.Blocks); Assert.Equal(2, list[0].Get<int>("x"));
        Assert.True(doc.RemoveBlock("b")); Assert.Empty(doc.Blocks); Assert.Single(list);
        var fresh = doc.AddBlock("b", "t", BridgeValue.Parse("{\"x\":3}"));
        Assert.NotSame(old, fresh); Assert.NotSame(list, doc.Blocks);
        Assert.Equal("BLOCK_REMOVED", Assert.Throws<BridgeException>(() => old.Set("x", 99)).Diagnostics[0].Code);
        Assert.Throws<BridgeException>(() => old.Get<int>("x")); Assert.Throws<BridgeException>(() => old.HasField("x"));
        Assert.Throws<BridgeException>(() => old.SetReference("r", new BridgeReference("", "b")));
        Assert.Equal(3, doc.Get<int>("b", "x")); Assert.Equal(1, data.Get<int>("x"));
    }
}
