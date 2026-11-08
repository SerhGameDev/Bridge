using System;
using System.Linq;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;

public class QueryTests
{
    private static BridgeDocument Create()
    {
        var doc = BridgeDocument.Create("query");
        doc.AddBlock("b", "scene", BridgeValue.Parse("{\"objects\":[],\"secret\":42,\"a.b\":1}"));
        doc.AddBlock("a", "scene", BridgeValue.Parse("{\"objects\":[1],\"secret\":99,\"a.b\":2}"));
        doc.AddBlock("c", "settings", BridgeValue.From(1)); return doc;
    }
    [Fact] public void StandardLinqAndFluentAgreeInOrdinalOrder()
    {
        var doc = Create();
        var linq = doc.Blocks.Where(b => b.Type == "scene").OrderBy(b => b.Id, StringComparer.Ordinal).Select(b => b.Id).ToList();
        var query = doc.Query().WhereType("scene");
        Assert.Equal(linq, query.Execute().Blocks.Select(b => b.Id));
        Assert.Equal("a", Assert.Single(query.WhereId("a").Limit(1).Execute().Blocks).Id);
        Assert.Equal(2, query.Execute().Blocks.Count);
        Assert.Empty(query.WhereId("A").Execute().Blocks);
        Assert.Empty(query.Limit(0).Execute().Blocks);
        Assert.Single(query.Limit(1).Execute().Blocks);
    }
    [Fact] public void ProjectionIsDetachedAndDoesNotLeakFields()
    {
        var doc = Create();
        var result = doc.Query().WhereType("scene").SelectFields("objects", "a.b", "objects").Execute();
        doc.GetBlock("a").Set("a.b", 1000);
        Assert.Equal(2, result.Blocks[0].Data.Get<int>("a.b"));
        Assert.DoesNotContain("secret", result.ToJson());
        Assert.DoesNotContain("settings", result.ToJson());
        Assert.Equal(2, BridgeDocument.Parse(result.ToJson()).Blocks.Count);
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<BridgeProjection>)result.Blocks).Clear());
        Assert.Equal("{}", doc.Query().WhereId("a").SelectFields().Execute().Blocks[0].Data.ToJson());
    }
    [Fact] public void MissingFieldsAndScalarDataReturnExplicitDiagnostics()
    {
        var doc = Create();
        Assert.Equal("QUERY_FIELD_NOT_FOUND", doc.Query().WhereId("a").SelectFields("missing").TryExecute().Diagnostics[0].Code);
        Assert.Equal("QUERY_DATA_KIND", doc.Query().WhereId("c").SelectFields("x").TryExecute().Diagnostics[0].Code);
        Assert.Throws<ArgumentOutOfRangeException>(() => doc.Query().Limit(-1));
    }
    [Fact] public void SpecsAreSerializableImmutableAndRejectExecutableExpressions()
    {
        string[] fields = { "objects" }; var spec = new BridgeQuerySpec("scene", fields: fields, limit: 1); fields[0] = "secret";
        var copy = BridgeQuerySpec.FromJson(spec.ToJson());
        Assert.Equal(spec.ToJson(), copy.ToJson());
        Assert.DoesNotContain("secret", Create().Query(copy).Execute().ToJson());
        Assert.Throws<BridgeException>(() => BridgeQuerySpec.FromJson("{\"queryVersion\":1,\"expression\":\"File.Delete(x)\"}"));
        Assert.Throws<BridgeException>(() => BridgeQuerySpec.FromJson("{\"queryVersion\":1,\"limit\":1.0}"));
        Assert.Throws<BridgeException>(() => BridgeQuerySpec.FromJson("{\"queryVersion\":2}"));
    }
}
