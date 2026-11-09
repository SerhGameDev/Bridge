using System.Text;
using System.Text.Json;
using Bridge.Core;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
namespace Bridge.Transport.Mcp;

/// <summary>Creates an SDK transport on explicitly supplied streams (SDK reading starts immediately).
/// RunAsync starts server processing. Does not open a socket or own the endpoint.</summary>
public static class BridgeMcpServer
{
    static readonly string[] Names={"bridge_discover","bridge_execute","bridge_query"};
    static JsonElement Json(string text)=>JsonSerializer.Deserialize<JsonElement>(text);
    static Tool[] Tools()=>new[]
    {
        new Tool {Name=Names[0],Description="List authorized Bridge commands with exact input/output schemas and permitted data sources/types/fields. No filesystem access.",InputSchema=Json("{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}"),Annotations=new(){ReadOnlyHint=true,DestructiveHint=false,IdempotentHint=true,OpenWorldHint=false}},
        new Tool {Name=Names[1],Description="Execute an explicitly registered Bridge command. Discover its argument schema first. Awaits actual completion; never retries. Cancellation does not roll back changes.",InputSchema=Json("{\"type\":\"object\",\"properties\":{\"command\":{\"type\":\"string\",\"maxLength\":128},\"arguments\":{\"type\":\"object\"}},\"required\":[\"command\",\"arguments\"],\"additionalProperties\":false}"),Annotations=new(){ReadOnlyHint=false,DestructiveHint=true,IdempotentHint=false,OpenWorldHint=false}},
        new Tool {Name=Names[2],Description="Read an authorized fresh Bridge snapshot using Core QuerySpec v1 or v2. No LINQ, expressions, references traversal or filesystem paths.",InputSchema=Json("{\"type\":\"object\",\"properties\":{\"source\":{\"type\":\"string\",\"maxLength\":128},\"spec\":{\"type\":\"object\",\"description\":\"Core QuerySpec: queryVersion 1 with optional type/id/fields/limit, or version 2 with required types and optional id/fields/limit. Unknown operations rejected.\"}},\"required\":[\"source\",\"spec\"],\"additionalProperties\":false}"),Annotations=new(){ReadOnlyHint=true,DestructiveHint=false,IdempotentHint=false,OpenWorldHint=false}}
    };
    public static McpServer Create(Stream input,Stream output,IBridgeMcpEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(input);ArgumentNullException.ThrowIfNull(output);ArgumentNullException.ThrowIfNull(endpoint);
        var slots=new SemaphoreSlim(16);
        var options=new McpServerOptions
        {
            ServerInfo=new(){Name="Bridge.Runtime",Version="0.1.0"},
            Capabilities=new(){Tools=new(){ListChanged=false}},
            Handlers=new()
            {
                ListToolsHandler=(request,ct)=>
                {
                    if(request.Params.Cursor!=null)throw Invalid();
                    return ValueTask.FromResult(new ListToolsResult {Tools=Tools()});
                },
                CallToolHandler=async(request,ct)=>
                {
                    var p=request.Params;
                    if(!Names.Contains(p.Name,StringComparer.Ordinal))throw Invalid();
                    var args=p.Arguments??new Dictionary<string,JsonElement>();
                    string[] keys=p.Name==Names[0]?Array.Empty<string>():p.Name==Names[1]?new[]{"command","arguments"}:new[]{"source","spec"};
                    if(args.Count!=keys.Length||keys.Any(k=>!args.ContainsKey(k)))throw Invalid();
                    var json=JsonSerializer.Serialize(args);
                    if(Encoding.UTF8.GetByteCount(json)>65536)return Result(Error("REQUEST_LIMIT"));
                    BridgeValue a;
                    try {a=BridgeValue.Parse(json);}catch(BridgeException){throw Invalid();}
                    if(keys.Length>0 && (a.GetProperty(keys[0]).Kind!=BridgeValueKind.String || a.Get<string>(keys[0]).Length is <1 or >128 || a.GetProperty(keys[1]).Kind!=BridgeValueKind.Object))throw Invalid();
                    if(!slots.Wait(0))return Result(Error("BUSY"));
                    try
                    {
                        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(15));
                        BridgeValue response;
                        try
                        {
                            if(p.Name==Names[0])response=await endpoint.DiscoverAsync(deadline.Token);
                            else if(p.Name==Names[1])response=await endpoint.ExecuteAsync(a.Get<string>("command"),a.GetProperty("arguments"),deadline.Token);
                            else
                            {
                                BridgeQuerySpec spec;
                                try {spec=BridgeQuerySpec.FromJson(a.GetProperty("spec").ToJson());}
                                catch(BridgeException){return Result(Error("INVALID_QUERY"));}
                                response=await endpoint.QueryAsync(a.Get<string>("source"),spec,deadline.Token);
                            }
                        }
                        catch(OperationCanceledException) {return Result(Error(ct.IsCancellationRequested?"REQUEST_CANCELLED":"REQUEST_TIMEOUT","Cancelled"));}
                        catch(Exception ex) when(ex is not OutOfMemoryException and not AccessViolationException) {return Result(Error("ENDPOINT_UNAVAILABLE","Failed"));}
                        return Result(Encoding.UTF8.GetByteCount(response.ToJson())>262144?Error("RESPONSE_LIMIT","Failed"):response);
                    }
                    finally {slots.Release();}
                }
            }
        };
        return McpServer.Create(new StreamServerTransport(new BoundedInputStream(input,131072),output,"Bridge.Runtime"),options);
    }
    static McpProtocolException Invalid()=>new("Invalid tool name or arguments.",McpErrorCode.InvalidParams);
    static BridgeValue Error(string code,string status="Rejected")=>BridgeValue.Parse("{\"data\":null,\"error\":{\"message\":\"Bridge request could not be completed.\"}}").WithField("status",BridgeValue.From(status)).WithField("requestId",BridgeValue.From(Guid.NewGuid().ToString("N"))).WithField("operationId",BridgeValue.From(Guid.NewGuid().ToString("N"))).WithField("error",BridgeValue.Parse("{\"message\":\"Bridge request could not be completed.\"}").WithField("code",BridgeValue.From(code)));
    static CallToolResult Result(BridgeValue response)
    {
        string text=response.ToJson();
        return new CallToolResult {IsError=response.Get<string>("status")!="Completed",StructuredContent=Json(text),Content=new List<ContentBlock>{new TextContentBlock {Text=text}}};
    }
}
