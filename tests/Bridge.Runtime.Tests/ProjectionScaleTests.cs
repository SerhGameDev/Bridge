using System;
using System.Text;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using Xunit;
namespace Bridge.Runtime.Tests;
public sealed class ProjectionScaleTests
{
    static BridgeRequestContext Reader()=>new("reader",BridgePermission.Read);
    [Fact]public async Task ExactUtf8BudgetIncludesEscapedIdsUnicodeAndCommas()
    {
        var doc=BridgeDocument.Create("private");
        foreach(string id in new[]{"quote\"\\Куб","second"})doc.AddBlock(id,"настройки",BridgeValue.Parse("{}").WithField("поле\"",BridgeValue.From("✈\n")).WithField("privateNote",BridgeValue.From("closed")));
        var policy=BridgeDataPolicy.ReadOnly().AllowFields("настройки","поле\"");
        using var baseline=BridgeHostBuilder.Create("base").AddDocument("public.source",doc,policy).Build();
        var full=await baseline.QueryAsync("public.source",new BridgeQuerySpec(),Reader());Assert.True(full.Success);Assert.DoesNotContain("privateNote",full.ToJson());
        int bytes=Encoding.UTF8.GetByteCount(full.Data.ToJson());
        using var exact=BridgeHostBuilder.Create("exact").AddDocument("public.source",doc,policy.WithLimits(maxResponseBytes:bytes)).Build();
        var ok=await exact.QueryAsync("public.source",new BridgeQuerySpec(),Reader());Assert.True(ok.Success);Assert.Equal(full.Data.ToJson(),ok.Data.ToJson());Assert.Equal(2,BridgeDocument.Parse(ok.Data.ToJson()).Blocks.Count);
        using var small=BridgeHostBuilder.Create("small").AddDocument("public.source",doc,policy.WithLimits(maxResponseBytes:bytes-1)).Build();
        var denied=await small.QueryAsync("public.source",new BridgeQuerySpec(),Reader());Assert.Equal("RESPONSE_LIMIT",denied.Error?.Code);Assert.Equal(BridgeValueKind.Null,denied.Data.Kind);
    }
    static BridgeHost Host(int count)
    {
        // Parse one valid document, avoiding unrelated Core mutation costs in this allocation test.
        var text=new StringBuilder("{\"bridgeVersion\":1,\"documentId\":\"private\",\"blocks\":{");
        for(int i=0;i<count;i++){if(i>0)text.Append(',');text.Append("\"b").Append(i).Append("\":{\"type\":\"row\",\"schemaVersion\":1,\"data\":{\"value\":1,\"privateNote\":\"closed\"}}");}text.Append("}}");
        return BridgeHostBuilder.Create("scale").AddDocument("rows",BridgeDocument.Parse(text.ToString()),BridgeDataPolicy.ReadOnly().AllowFields("row","value").WithLimits(maxBlocks:count,maxResponseBytes:524288)).Build();
    }
    [Fact]public async Task ProjectionAllocationsGrowLinearlyRatherThanQuadratically()
    {
        async Task<long> Allocated(int count)
        {
            using var host=Host(count);Assert.True((await host.QueryAsync("rows",new BridgeQuerySpec(),Reader())).Success);
            int thread=Environment.CurrentManagedThreadId;long before=GC.GetAllocatedBytesForCurrentThread();
            var r=await host.QueryAsync("rows",new BridgeQuerySpec(),Reader());long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.Equal(thread,Environment.CurrentManagedThreadId);Assert.True(r.Success);return bytes;
        }
        long small=await Allocated(200),large=await Allocated(400);
        Assert.True(large<small*3,$"Doubling projection allocated {small} -> {large}; quadratic construction regressed.");
    }
    [Fact]public async Task ThousandBlocksRemainValidCoreDocumentsAndPolicyBounded()
    {
        using var host=Host(1000);var result=await host.QueryAsync("rows",new BridgeQuerySpec(),Reader());Assert.True(result.Success);
        var doc=BridgeDocument.Parse(result.Data.ToJson());Assert.Equal(1000,doc.Blocks.Count);Assert.Equal("rows",doc.DocumentId);Assert.DoesNotContain("privateNote",result.ToJson());
        var limited=await host.QueryAsync("rows",new BridgeQuerySpec(limit:3),Reader());Assert.Equal(3,BridgeDocument.Parse(limited.Data.ToJson()).Blocks.Count);
    }
}
