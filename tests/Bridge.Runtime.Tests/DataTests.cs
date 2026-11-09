using System;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using Xunit;

namespace Bridge.Runtime.Tests;
public class DataTests
{
    static BridgeRequestContext Reader => new BridgeRequestContext("reader",BridgePermission.Read);
    static BridgeDocument Document()
    {
        var doc=BridgeDocument.Parse("{\"bridgeVersion\":1,\"documentId\":\"SECRET_ID\",\"extension\":\"SECRET_EXTENSION\",\"blocks\":{\"settings\":{\"type\":\"settings\",\"schemaVersion\":67,\"extension\":\"SECRET_META\",\"data\":{\"speed\":2,\"privateNote\":\"SECRET_NOTE\"},\"refs\":{\"private\":{\"document\":\"SECRET_PATH.bridge.json\",\"block\":\"secret\"}}},\"secret\":{\"type\":\"private\",\"schemaVersion\":1,\"data\":{\"password\":\"SECRET_PASSWORD\"}}}}");
        return doc;
    }
    sealed class Source : IBridgeDataSource
    {
        public int Reads; public bool Throw; public BridgeDocument Document=DataTests.Document();
        public Task<BridgeDataSnapshot> ReadAsync(CancellationToken token) { Reads++; if(Throw) throw new InvalidOperationException("SECRET_EXCEPTION"); return Task.FromResult(new BridgeDataSnapshot(Document,"SECRET_REVISION")); }
    }
    [Fact] public async Task DefaultDenyHidesNamesAndDoesNotReadSources()
    {
        var source=new Source(); using var host=BridgeHostBuilder.Create("host").AddDataSource("SECRET_SOURCE",source).Build();
        var catalog=await host.DiscoverAsync(Reader); Assert.True(catalog.Success); Assert.DoesNotContain("SECRET",catalog.ToJson());
        var closed=await host.QueryAsync("SECRET_SOURCE",new BridgeQuerySpec(),Reader);
        var absent=await host.QueryAsync("missing",new BridgeQuerySpec(),Reader);
        Assert.Equal("NOT_FOUND",closed.Error!.Code); Assert.Equal(absent.Error!.Message,closed.Error.Message); Assert.Equal(0,source.Reads);
    }
    [Fact] public async Task ProjectionStripsHiddenDataRefsMetadataAndOriginalIdentity()
    {
        var source=new Source(); var policy=BridgeDataPolicy.ReadOnly().AllowFields("settings","speed","optional");
        using var host=BridgeHostBuilder.Create("host").AddDataSource("config",source,policy).Build();
        var result=await host.QueryAsync("config",new BridgeQuerySpec(),Reader); Assert.True(result.Success);
        Assert.DoesNotContain("SECRET",result.ToJson()); Assert.DoesNotContain("privateNote",result.ToJson()); Assert.DoesNotContain("refs",result.ToJson());
        var data=BridgeDocument.Parse(result.Data.ToJson()); Assert.Equal("config",data.DocumentId); Assert.Single(data.Blocks);
        Assert.Equal(1,data.GetBlock("settings").SchemaVersion); Assert.Equal(2,data.Get<int>("settings","speed"));
        var catalog=await host.DiscoverAsync(Reader); Assert.Contains("config",catalog.ToJson()); Assert.DoesNotContain("SECRET",catalog.ToJson());
        var noRights=await host.DiscoverAsync(new BridgeRequestContext("anonymous")); Assert.Empty(noRights.Data.GetProperty("sources").Items);
        Assert.Equal("NOT_FOUND",(await host.QueryAsync("config",new BridgeQuerySpec(),new BridgeRequestContext("none"))).Error!.Code);
        source.Document.Set("settings","speed",3); Assert.Equal(3,BridgeDocument.Parse((await host.QueryAsync("config",new BridgeQuerySpec(),Reader)).Data.ToJson()).Get<int>("settings","speed"));
        Assert.Equal(2,data.Get<int>("settings","speed"));
    }
    [Theory]
    [InlineData("{\"queryVersion\":1,\"id\":\"secret\"}")]
    [InlineData("{\"queryVersion\":1,\"type\":\"private\"}")]
    [InlineData("{\"queryVersion\":1,\"fields\":[\"privateNote\"]}")]
    [InlineData("{\"queryVersion\":2,\"types\":[\"settings\",\"private\"]}")]
    public async Task ExplicitForbiddenSelectorsCannotBypassPolicy(string json)
    {
        using var host=BridgeHostBuilder.Create("host").AddDocument("config",Document(),BridgeDataPolicy.ReadOnly().AllowFields("settings","speed")).Build();
        var result=await host.QueryAsync("config",BridgeQuerySpec.FromJson(json),Reader); Assert.Equal(BridgeCommandStatus.Rejected,result.Status); Assert.Equal("NOT_FOUND",result.Error!.Code); Assert.Equal(BridgeValueKind.Null,result.Data.Kind);
    }
    [Fact] public async Task BlockByteAndRequestCapsApplyWithoutClientLimit()
    {
        var source=new Source(); source.Document=BridgeDocument.Create("secret");
        for(int i=0;i<5;i++) source.Document.AddBlock("b"+i,"t",BridgeValue.Parse("{\"x\":1}"));
        var policy=BridgeDataPolicy.ReadOnly().AllowFields("t","x").AllowBlocks("b0","b1","b2").WithLimits(maxBlocks:2);
        using var host=BridgeHostBuilder.Create("host").AddDataSource("s",source,policy).Build();
        var result=await host.QueryAsync("s",new BridgeQuerySpec(limit:100),Reader); Assert.Equal(2,BridgeDocument.Parse(result.Data.ToJson()).Blocks.Count);
        Assert.Equal("NOT_FOUND",(await host.QueryAsync("s",new BridgeQuerySpec(id:"b3"),Reader)).Error!.Code);
        using var tiny=BridgeHostBuilder.Create("tiny").AddDataSource("s",source,policy.WithLimits(maxQueryBytes:1)).Build();
        int before=source.Reads; Assert.Equal("REQUEST_LIMIT",(await tiny.QueryAsync("s",new BridgeQuerySpec(),Reader)).Error!.Code); Assert.Equal(before,source.Reads);
        source.Document.Set("b0","x",new string('a',500));
        using var small=BridgeHostBuilder.Create("small").AddDataSource("s",source,policy.WithLimits(maxResponseBytes:100)).Build();
        Assert.Equal("RESPONSE_LIMIT",(await small.QueryAsync("s",new BridgeQuerySpec(),Reader)).Error!.Code);
    }
    [Fact] public async Task MissingAllowedFieldAndSourceExceptionsAreSanitized()
    {
        var source=new Source(); using var host=BridgeHostBuilder.Create("host").AddDataSource("s",source,BridgeDataPolicy.ReadOnly().AllowFields("settings","speed","gravity")).Build();
        Assert.Equal("FIELD_NOT_FOUND",(await host.QueryAsync("s",new BridgeQuerySpec(fields:new[]{"gravity"}),Reader)).Error!.Code);
        source.Throw=true; var failure=await host.QueryAsync("s",new BridgeQuerySpec(),Reader); Assert.Equal(BridgeCommandStatus.Failed,failure.Status); Assert.DoesNotContain("SECRET",failure.ToJson());
    }
}
