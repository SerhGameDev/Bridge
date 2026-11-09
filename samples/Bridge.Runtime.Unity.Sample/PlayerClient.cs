using System.Net;
using System.Net.Sockets;
using System.Text;
using Bridge.Core;
namespace Bridge.Player;

/// <summary>Bounded demo IPC client. Only literal loopback is supported; token never becomes a command argument.</summary>
public sealed class PlayerClient
{
    readonly string token;
    readonly int port;
    public PlayerClient(string token,int port=19731)
    {
        if(string.IsNullOrWhiteSpace(token)||token.Length<32)throw new ArgumentException("Player token must contain at least 32 characters.");
        if(port<1||port>65535)throw new ArgumentOutOfRangeException(nameof(port));this.token=token;this.port=port;
    }
    public async Task<BridgeValue> SendAsync(BridgeValue request,CancellationToken cancellationToken=default)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var client=new TcpClient();await client.ConnectAsync(IPAddress.Loopback,port,deadline.Token);
        using var stream=client.GetStream();var data=Encoding.UTF8.GetBytes(request.WithField("token",BridgeValue.From(token)).ToJson());
        if(data.Length>65536)throw new ArgumentException("Request exceeds IPC byte limit.");
        await stream.WriteAsync(Encoding.ASCII.GetBytes("BRIDGE/1\n"),deadline.Token);
        int n=data.Length;await stream.WriteAsync(new[]{(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n},deadline.Token);await stream.WriteAsync(data,deadline.Token);
        var prefix=new byte[4];await stream.ReadExactlyAsync(prefix,deadline.Token);n=(prefix[0]<<24)|(prefix[1]<<16)|(prefix[2]<<8)|prefix[3];
        if(n<1||n>1048576)throw new InvalidDataException("Invalid IPC response size.");
        var response=new byte[n];await stream.ReadExactlyAsync(response,deadline.Token);return BridgeValue.Parse(new UTF8Encoding(false,true).GetString(response));
    }
    public Task<BridgeValue> DiscoverAsync(CancellationToken ct=default)=>SendAsync(BridgeValue.Parse("{\"op\":\"discover\"}"),ct);
    public Task<BridgeValue> ExecuteAsync(string command,BridgeValue arguments,CancellationToken ct=default)=>SendAsync(BridgeValue.Parse("{\"op\":\"execute\"}").WithField("command",BridgeValue.From(command)).WithField("arguments",arguments),ct);
    public Task<BridgeValue> QueryAsync(string source,BridgeQuerySpec spec,CancellationToken ct=default)=>SendAsync(BridgeValue.Parse("{\"op\":\"query\"}").WithField("source",BridgeValue.From(source)).WithField("spec",BridgeValue.Parse(spec.ToJson())),ct);
}
