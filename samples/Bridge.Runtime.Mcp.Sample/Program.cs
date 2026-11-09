using Bridge.Core;
using Bridge.Runtime;
using Bridge.Transport.Mcp;

if(args.Length!=2 || args[0]!="--enable-stdio" || args[1] is not ("--player" or "--local-demo"))
{
    Console.Error.WriteLine("Explicit launch required: --enable-stdio --player | --enable-stdio --local-demo");return 2;
}
BridgeHost? host=null;
try
{
    IBridgeMcpEndpoint endpoint;
    if(args[1]=="--player")
    {
        var token=Environment.GetEnvironmentVariable("BRIDGE_PLAYER_TOKEN");
        if(string.IsNullOrWhiteSpace(token)||token.Length<32){Console.Error.WriteLine("BRIDGE_PLAYER_TOKEN is required (at least 32 characters).");return 2;}
        endpoint=new PlayerMcpEndpoint(token);
    }
    else
    {
        var doc=BridgeDocument.Create("private.config");doc.AddBlock("settings","settings",BridgeValue.Parse("{\"speed\":1,\"privateNote\":\"private\"}"));
        host=BridgeHostBuilder.Create("mcp-local-demo").AddCommands(new MathCommands())
            .AddDocument("config",doc,BridgeDataPolicy.ReadOnly().AllowFields("settings","speed")).Build();
        endpoint=new RuntimeMcpEndpoint(host,"stdio-demo-reader",BridgePermission.Read);
    }
    using var stop=new CancellationTokenSource();
    Console.CancelKeyPress+=(s,e)=>{e.Cancel=true;stop.Cancel();};
    await using var server=BridgeMcpServer.Create(Console.OpenStandardInput(),Console.OpenStandardOutput(),endpoint);
    Console.Error.WriteLine("BRIDGE_MCP_STDIO_READY");await server.RunAsync(stop.Token);return 0;
}
catch(OperationCanceledException){return 0;}
catch(Exception ex) when(ex is not OutOfMemoryException and not AccessViolationException){Console.Error.WriteLine("Bridge MCP gateway stopped: protocol or endpoint failure.");return 1;}
finally {host?.Dispose();}

public sealed class MathCommands
{
    [BridgeCommand("math.add",Permission=BridgePermission.Read,Description="Add two integers")]public int Add(int a,int b)=>a+b;
    [BridgeCommand("text.echo",Permission=BridgePermission.Read)]public async Task<string> Echo(string text,CancellationToken token){await Task.Delay(1,token);return text;}
    [BridgeCommand("private.reset",Permission=BridgePermission.Admin)]public bool Reset()=>true;
    [BridgeCommand("demo.throw",Permission=BridgePermission.Read)]public int Throw()=>throw new Exception("PRIVATE_EXCEPTION_DETAIL");
    [BridgeCommand("demo.delay",Permission=BridgePermission.Read)]public async Task<int> Delay(CancellationToken token){await Task.Delay(10000,token);return 1;}
    [BridgeCommand("demo.large",Permission=BridgePermission.Read)]public string Large()=>new string('x',300000);
}
