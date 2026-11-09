using System.Diagnostics;
using System.Text.Json;
using Bridge.Core;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;
namespace Bridge.Transport.Mcp.Tests;

public sealed class ProtocolTests
{
    static string Gateway
    {
        get
        {
            var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"Bridge.sln")))dir=dir.Parent;
            if(dir==null)throw new Exception("Checkout not found.");
#if DEBUG
            const string configuration="Debug";
#else
            const string configuration="Release";
#endif
            return Path.Combine(dir.FullName,"samples","Bridge.Runtime.Mcp.Sample","bin",configuration,"net8.0","Bridge.Runtime.Mcp.Sample.dll");
        }
    }
    static Task<McpClient> Connect(string version="2026-07-28")=>McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
    {Command="dotnet",Arguments=new[]{Gateway,"--enable-stdio","--local-demo"},Name="Bridge protocol test",InheritEnvironmentVariables=false,EnvironmentVariables=StdioClientTransportOptions.GetDefaultEnvironmentVariables()}),new McpClientOptions {ProtocolVersion=version,InitializationTimeout=TimeSpan.FromSeconds(10)});
    static async Task<BridgeValue> Call(McpClient client,string tool,Dictionary<string,object?>? arguments=null,bool error=false)
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result=await client.CallToolAsync(tool,arguments,cancellationToken:deadline.Token);
        Assert.Equal(error,result.IsError is true);Assert.NotNull(result.StructuredContent);
        var text=Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Equal(BridgeValue.Parse(text).ToJson(),BridgeValue.Parse(result.StructuredContent.Value.GetRawText()).ToJson());
        return BridgeValue.Parse(text);
    }
    static Dictionary<string,object?> Execute(string command,object arguments)=>new(){{"command",command},{"arguments",arguments}};
    [Theory][InlineData("2026-07-28")][InlineData("2025-11-25")][InlineData("2025-06-18")]
    public async Task OfficialSdkNegotiatesAndExecutes(string version)
    {
        await using var client=await Connect(version);Assert.Equal("Bridge.Runtime",client.ServerInfo?.Name);Assert.NotNull(client.ServerCapabilities.Tools);
        var tools=await client.ListToolsAsync();Assert.Equal(new[]{"bridge_discover","bridge_execute","bridge_query"},tools.Select(t=>t.Name));
        Assert.All(tools,t=>Assert.False(t.JsonSchema.GetProperty("additionalProperties").GetBoolean()));
        var result=await Call(client,"bridge_execute",Execute("math.add",new{a=2,b=3}));Assert.Equal(5,result.GetProperty("data").As<int>());Assert.False(string.IsNullOrEmpty(result.Get<string>("operationId")));
    }
    [Fact]public async Task CatalogPublishesExactAuthorizedSchemas()
    {
        await using var client=await Connect();var r=await Call(client,"bridge_discover");var text=r.ToJson();
        Assert.DoesNotContain("private.reset",text);Assert.DoesNotContain("privateNote",text);Assert.DoesNotContain("private.config",text);
        var add=r.GetProperty("data").GetProperty("commands").Items.Single(c=>c.Get<string>("name")=="math.add");
        Assert.Equal("integer",add.GetProperty("inputSchema").GetProperty("properties").GetProperty("a").Get<string>("type"));Assert.False(add.GetProperty("inputSchema").Get<bool>("additionalProperties"));
    }
    [Fact]public async Task BinderErrorsAndPermissionsAreToolErrors()
    {
        await using var client=await Connect();
        foreach(var args in new object[]{new{a="bad",b=2},new{a=1,b=2,permission="Admin"},new{a=1}})
            Assert.Equal("INVALID_ARGUMENT",(await Call(client,"bridge_execute",Execute("math.add",args),true)).GetProperty("error").Get<string>("code"));
        var hidden=await Call(client,"bridge_execute",Execute("private.reset",new{}),true);var missing=await Call(client,"bridge_execute",Execute("unknown",new{}),true);
        Assert.Equal("NOT_FOUND",hidden.GetProperty("error").Get<string>("code"));Assert.Equal(hidden.GetProperty("error").ToJson(),missing.GetProperty("error").ToJson());
    }
    [Fact]public async Task QueryUsesCoreVersionsAndEnforcesFieldBlockPolicies()
    {
        await using var client=await Connect();
        foreach(var spec in new object[]{new{queryVersion=1},new{queryVersion=2,types=new[]{"settings"},fields=new[]{"speed"}}})
        {
            var r=await Call(client,"bridge_query",new(){{"source","config"},{"spec",spec}});Assert.DoesNotContain("privateNote",r.ToJson());Assert.Equal(1,BridgeDocument.Parse(r.GetProperty("data").ToJson()).Get<int>("settings","speed"));
        }
        foreach(var spec in new object[]{new{queryVersion=1,fields=new[]{"privateNote"}},new{queryVersion=1,id="hidden"}})
            Assert.Equal("NOT_FOUND",(await Call(client,"bridge_query",new(){{"source","config"},{"spec",spec}},true)).GetProperty("error").Get<string>("code"));
        Assert.Equal("INVALID_QUERY",(await Call(client,"bridge_query",new(){{"source","config"},{"spec",new{queryVersion=1,expression="arbitrary C#"}}},true)).GetProperty("error").Get<string>("code"));
    }
    [Fact]public async Task OuterSchemaAndUnknownToolAreProtocolErrors()
    {
        await using var client=await Connect();
        foreach(var pair in new[]{("unknown",new Dictionary<string,object?>()),("bridge_discover",new Dictionary<string,object?>{{"principal","admin"}}),("bridge_execute",new Dictionary<string,object?>{{"command","math.add"}})})
        {
            var ex=await Assert.ThrowsAsync<McpProtocolException>(async()=>await client.CallToolAsync(pair.Item1,pair.Item2));Assert.Equal(McpErrorCode.InvalidParams,ex.ErrorCode);
        }
    }
    [Fact]public async Task UnsupportedProtocolMethodReturnsMethodNotFound()
    {
        await using var client=await Connect("2025-11-25");
        var ex=await Assert.ThrowsAsync<McpProtocolException>(async()=>await client.SendRequestAsync(new JsonRpcRequest {Id=new RequestId(1234),Method="bridge.unsupported"}));Assert.Equal(McpErrorCode.MethodNotFound,ex.ErrorCode);
    }
    [Fact]public async Task ExceptionsAreSanitizedAndAsyncWorks()
    {
        await using var client=await Connect();var r=await Call(client,"bridge_execute",Execute("demo.throw",new{}),true);
        Assert.Equal("HANDLER_FAILED",r.GetProperty("error").Get<string>("code"));Assert.DoesNotContain("PRIVATE_EXCEPTION",r.ToJson());Assert.DoesNotContain("StackTrace",r.ToJson());
        Assert.Equal("hello",(await Call(client,"bridge_execute",Execute("text.echo",new{text="hello"}))).GetProperty("data").As<string>());
    }
    [Fact]public async Task ClientCancellationPropagatesAndServerRemainsUsable()
    {
        await using var client=await Connect();using var cancel=new CancellationTokenSource(150);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>await client.CallToolAsync("bridge_execute",Execute("demo.delay",new{}),cancellationToken:cancel.Token));
        Assert.Equal(3,(await Call(client,"bridge_execute",Execute("math.add",new{a=1,b=2}))).GetProperty("data").As<int>());
    }
    [Fact]public async Task OversizedArgumentsRejectedWithoutExecution()
    {
        await using var client=await Connect();var r=await Call(client,"bridge_execute",Execute("text.echo",new{text=new string('x',70000)}),true);Assert.Equal("REQUEST_LIMIT",r.GetProperty("error").Get<string>("code"));
    }
    [Fact]public async Task ExecutableDoesNotStartWithoutExplicitFlags()
    {
        var info=new ProcessStartInfo("dotnet"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};info.ArgumentList.Add(Gateway);
        using var run=Process.Start(info)!;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));await run.WaitForExitAsync(timeout.Token);
        Assert.Equal(2,run.ExitCode);Assert.Equal("",await run.StandardOutput.ReadToEndAsync());Assert.Contains("Explicit launch required",await run.StandardError.ReadToEndAsync());
    }
    [Fact]public async Task OversizedEndpointResultIsNotPartiallyReturned()
    {
        await using var client=await Connect();var r=await Call(client,"bridge_execute",Execute("demo.large",new{}),true);
        Assert.Equal("RESPONSE_LIMIT",r.GetProperty("error").Get<string>("code"));Assert.Equal(BridgeValueKind.Null,r.GetProperty("data").Kind);Assert.True(r.ToJson().Length<1024);
    }
    [Fact]public async Task OversizedRawStdioLineClosesTransport()
    {
        var info=new ProcessStartInfo("dotnet"){RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        foreach(string arg in new[]{Gateway,"--enable-stdio","--local-demo"})info.ArgumentList.Add(arg);
        using var run=Process.Start(info)!;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            try {await run.StandardInput.WriteAsync(new string('x',131073));await run.StandardInput.FlushAsync();}catch(IOException){}
            run.StandardInput.Close();await run.WaitForExitAsync(timeout.Token);
            Assert.InRange(run.ExitCode,0,1);Assert.DoesNotContain(new string('x',100),await run.StandardError.ReadToEndAsync());
        }
        finally {if(!run.HasExited)run.Kill(entireProcessTree:true);}
    }
}
