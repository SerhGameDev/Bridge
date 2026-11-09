using Bridge.Core;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

if(args.Length!=2||args[0]!="--gateway")throw new ArgumentException("--gateway <absolute built gateway dll path> required.");
string gateway=Path.GetFullPath(args[1]);
var token=Environment.GetEnvironmentVariable("BRIDGE_PLAYER_TOKEN")??throw new Exception("BRIDGE_PLAYER_TOKEN required.");
var readerToken=Environment.GetEnvironmentVariable("BRIDGE_READER_TOKEN")??throw new Exception("BRIDGE_READER_TOKEN required.");
void Require(bool value,string step){if(!value)throw new Exception("MCP Player E2E: "+step);}
async Task<McpClient> Connect(string credential)
{
    var env=StdioClientTransportOptions.GetDefaultEnvironmentVariables();env["BRIDGE_PLAYER_TOKEN"]=credential;
    return await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions {Command="dotnet",Arguments=new[]{gateway,"--enable-stdio","--player"},Name="Bridge real Player E2E",InheritEnvironmentVariables=false,EnvironmentVariables=env}),new McpClientOptions {ProtocolVersion="2026-07-28",InitializationTimeout=TimeSpan.FromSeconds(10)});
}
async Task<BridgeValue> Call(McpClient client,string tool,Dictionary<string,object?>? arguments=null,bool error=false)
{
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
    var r=await client.CallToolAsync(tool,arguments,cancellationToken:timeout.Token);
    Require((r.IsError is true)==error,tool+" error mapping");Require(r.StructuredContent!=null,tool+" structured content");
    return BridgeValue.Parse(r.StructuredContent!.Value.GetRawText());
}
Dictionary<string,object?> Exec(string command,object arguments)=>new(){{"command",command},{"arguments",arguments}};
Dictionary<string,object?> Query(string source,object spec)=>new(){{"source",source},{"spec",spec}};
await using var client=await Connect(token);
var tools=await client.ListToolsAsync();Require(tools.Count==3,"three SDK tools");
var catalog=await Call(client,"bridge_discover");Require(!catalog.ToJson().Contains("scene.admin_reset")&&!catalog.ToJson().Contains("privateNote"),"filtered catalog");
var spawnSchema=catalog.GetProperty("data").GetProperty("commands").Items.Single(c=>c.Get<string>("name")=="scene.spawn_cube").GetProperty("inputSchema");
Require(!spawnSchema.Get<bool>("additionalProperties")&&spawnSchema.GetProperty("required").Items.Count==3,"formal spawn schema");
var config=await Call(client,"bridge_query",Query("config",new{queryVersion=1}));Require(BridgeDocument.Parse(config.GetProperty("data").ToJson()).Get<float>("settings","speed")==1&&!config.ToJson().Contains("privateNote"),"public config");
var ids=new List<string>();
for(int i=0;i<3;i++)
{
    var r=await Call(client,"bridge_execute",Exec("scene.spawn_cube",new{x=i*2,y=1,z=0}));string id=r.GetProperty("data").As<string>();Require(Guid.TryParseExact(id,"N",out _),"stable GUID");ids.Add(id);
}
Require(ids.Distinct().Count()==3,"three distinct cubes");
Require((await Call(client,"bridge_execute",Exec("scene.move",new{objectId=ids[0],x=7,y=2,z=3}))).GetProperty("data").As<bool>(),"move");
Require((await Call(client,"bridge_execute",Exec("scene.delete",new{objectId=ids[1]}))).GetProperty("data").As<bool>(),"delete");
var scene=await Call(client,"bridge_query",Query("scene",new{queryVersion=2,types=new[]{"scene.cube"},fields=new[]{"x","y","z"}}));var snapshot=BridgeDocument.Parse(scene.GetProperty("data").ToJson());
Require(snapshot.Blocks.Count==2&&!snapshot.ContainsBlock(ids[1]),"live two / deleted absent");Require(snapshot.Get<float>(ids[0],"x")==7&&snapshot.Get<float>(ids[0],"y")==2&&snapshot.Get<float>(ids[0],"z")==3,"real moved Transform");Require(snapshot.Get<float>(ids[2],"x")==4,"third cube position");
Require(!scene.ToJson().Contains("privateNote"),"private data omitted");
Require((await Call(client,"bridge_execute",Exec("scene.admin_reset",new{}),true)).GetProperty("error").Get<string>("code")=="NOT_FOUND","Admin denied");
Require((await Call(client,"bridge_query",Query("config",new{queryVersion=1,id="hidden"}),true)).GetProperty("error").Get<string>("code")=="NOT_FOUND","hidden block denied");
Require((await Call(client,"bridge_query",Query("scene",new{queryVersion=1,fields=new[]{"privateNote"}}),true)).GetProperty("error").Get<string>("code")=="NOT_FOUND","private field denied");
for(int i=0;i<5;i++)Require((await Call(client,"bridge_execute",Exec("scene.thread_check",new{}))).GetProperty("data").As<bool>(),"owner thread");
Require((await Call(client,"bridge_execute",Exec("demo.async_dto",new{}))).GetProperty("data").Get<bool>("owner"),"async DTO owner");
await using(var reader=await Connect(readerToken))
{
    Require(!(await Call(reader,"bridge_discover")).ToJson().Contains("scene.spawn_cube"),"Read catalog");
    Require((await Call(reader,"bridge_execute",Exec("scene.spawn_cube",new{x=0,y=0,z=0}),true)).GetProperty("error").Get<string>("code")=="NOT_FOUND","Read mutation denied");
    Require(BridgeDocument.Parse((await Call(reader,"bridge_query",Query("scene",new{queryVersion=1}))).GetProperty("data").ToJson()).Blocks.Count==2,"Read live data");
}
await using(var unauthorized=await Connect(Guid.NewGuid().ToString("N")))
    Require((await Call(unauthorized,"bridge_discover",error:true)).GetProperty("error").Get<string>("code")=="UNAUTHORIZED","bad credential denied");
Console.WriteLine("MCP_PLAYER_E2E_PASS: SDK2.4.0 protocol2026-07-28; created3 moved(7,2,3) deleted1 live2; owner5+async; config/schema/policies/auth PASS");
Console.WriteLine("Client used only SDK stdio tools and authenticated IPC; no Unity source files were opened.");
