using Bridge.Core;
using Bridge.Runtime;
using Bridge.Player;
namespace Bridge.Transport.Mcp;

/// <summary>Trusted application endpoint. JSON tool arguments never select a principal or permission.</summary>
public interface IBridgeMcpEndpoint
{
    Task<BridgeValue> DiscoverAsync(CancellationToken cancellationToken);
    Task<BridgeValue> QueryAsync(string source,BridgeQuerySpec spec,CancellationToken cancellationToken);
    Task<BridgeValue> ExecuteAsync(string command,BridgeValue arguments,CancellationToken cancellationToken);
}
public sealed class RuntimeMcpEndpoint : IBridgeMcpEndpoint
{
    readonly BridgeHost host;
    readonly string principal;
    readonly BridgePermission permission;
    public RuntimeMcpEndpoint(BridgeHost host,string principal,BridgePermission permission)
    {this.host=host??throw new ArgumentNullException(nameof(host));var validated=new BridgeRequestContext(principal,permission);this.principal=validated.Principal;this.permission=validated.Permission;}
    BridgeRequestContext Context(CancellationToken ct)=>new(principal,permission,cancellationToken:ct);
    public async Task<BridgeValue> DiscoverAsync(CancellationToken ct)=>(await host.DiscoverAsync(Context(ct))).ToValue();
    public async Task<BridgeValue> QueryAsync(string source,BridgeQuerySpec spec,CancellationToken ct)=>(await host.QueryAsync(source,spec,Context(ct))).ToValue();
    public async Task<BridgeValue> ExecuteAsync(string command,BridgeValue arguments,CancellationToken ct)=>(await host.ExecuteAsync(command,arguments,Context(ct))).ToValue();
}
/// <summary>The built Player authenticates this token and assigns its own principal. Only literal loopback IPC.</summary>
public sealed class PlayerMcpEndpoint : IBridgeMcpEndpoint
{
    readonly PlayerClient client;
    public PlayerMcpEndpoint(string token,int port=19731)=>client=new PlayerClient(token,port);
    public Task<BridgeValue> DiscoverAsync(CancellationToken ct)=>client.DiscoverAsync(ct);
    public Task<BridgeValue> QueryAsync(string source,BridgeQuerySpec spec,CancellationToken ct)=>client.QueryAsync(source,spec,ct);
    public Task<BridgeValue> ExecuteAsync(string command,BridgeValue arguments,CancellationToken ct)=>client.ExecuteAsync(command,arguments,ct);
}
