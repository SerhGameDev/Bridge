#if !UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;

namespace Bridge.Unity.Demo
{
    // Application-specific authenticated IPC. This is not an MCP server.
    public sealed class PlayerServer : IDisposable
    {
        readonly BridgeHost host;
        readonly string editorToken,readerToken;
        readonly TcpListener listener;
        readonly CancellationTokenSource stop=new CancellationTokenSource();
        readonly SemaphoreSlim slots=new SemaphoreSlim(16);
        readonly HashSet<TcpClient> clients=new HashSet<TcpClient>();
        static readonly byte[] Magic=Encoding.ASCII.GetBytes("BRIDGE/1\n");
        static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        public PlayerServer(BridgeHost host,int port,string editorToken,string readerToken=null)
        {
            if(string.IsNullOrWhiteSpace(editorToken)||editorToken.Length<32) throw new ArgumentException("BRIDGE_PLAYER_TOKEN must contain at least 32 characters.");
            if(readerToken!=null && (readerToken.Length<32 || readerToken==editorToken)) throw new ArgumentException("Reader token must be distinct and at least 32 characters.");
            this.host=host;this.editorToken=editorToken;this.readerToken=readerToken;
            listener=new TcpListener(IPAddress.Loopback,port);listener.Start(16); _=Accept();
        }
        async Task Accept()
        {
            try { while(!stop.IsCancellationRequested) { var client=await listener.AcceptTcpClientAsync().ConfigureAwait(false); if(!slots.Wait(0)){client.Close();continue;} lock(clients)clients.Add(client); _=Serve(client); } }
            catch(ObjectDisposedException) { } catch(SocketException) { if(!stop.IsCancellationRequested) Dispose(); }
        }
        async Task Serve(TcpClient client)
        {
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
            using(deadline.Token.Register(()=>client.Close()))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    var stream=client.GetStream();var magic=await Read(stream,Magic.Length,deadline.Token);
                    for(int i=0;i<Magic.Length;i++)if(magic[i]!=Magic[i])return;
                    var prefix=await Read(stream,4,deadline.Token);int count=(prefix[0]<<24)|(prefix[1]<<16)|(prefix[2]<<8)|prefix[3];
                    if(count<1||count>65536)return;
                    var request=BridgeValue.Parse(Utf8.GetString(await Read(stream,count,deadline.Token)));
                    BridgeCommandResult result;
                    var permission=Equal(request.GetProperty("token").As<string>(),editorToken)?BridgePermission.Edit:Equal(request.GetProperty("token").As<string>(),readerToken)?BridgePermission.Read:BridgePermission.None;
                    if(permission==BridgePermission.None) { await Reply(stream,"{\"status\":\"Rejected\",\"error\":{\"code\":\"UNAUTHORIZED\",\"message\":\"Authentication required.\"}}",deadline.Token);return; }
                    foreach(var key in request.Properties.Keys) if(key!="token"&&key!="op"&&key!="command"&&key!="arguments"&&key!="source"&&key!="spec")throw new InvalidDataException();
                    var ctx=new BridgeRequestContext(permission==BridgePermission.Read?"ipc-reader":"ipc-editor",permission,cancellationToken:deadline.Token);
                    switch(request.GetProperty("op").As<string>())
                    {
                        case "discover":result=await host.DiscoverAsync(ctx);break;
                        case "query":result=await host.QueryAsync(request.GetProperty("source").As<string>(),BridgeQuerySpec.FromJson(request.GetProperty("spec").ToJson()),ctx);break;
                        case "execute":result=await host.ExecuteAsync(request.GetProperty("command").As<string>(),request.GetProperty("arguments"),ctx);break;
                        default:throw new InvalidDataException();
                    }
                    await Reply(stream,result.ToJson(),deadline.Token);
                }
                catch(Exception ex) when(ex is IOException || ex is SocketException || ex is OperationCanceledException || ex is ObjectDisposedException) { }
                catch(Exception) { try { await Reply(client.GetStream(),"{\"status\":\"Rejected\",\"error\":{\"code\":\"INVALID_REQUEST\",\"message\":\"Invalid request.\"}}",deadline.Token); } catch(Exception) { } }
                finally { client.Close();lock(clients)clients.Remove(client);slots.Release(); }
            }
        }
        static bool Equal(string a,string b) { if(a==null||b==null||a.Length!=b.Length)return false;int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];return diff==0; }
        static async Task<byte[]> Read(Stream stream,int count,CancellationToken token)
        { var data=new byte[count];int offset=0;while(offset<count){int n=await stream.ReadAsync(data,offset,count-offset,token);if(n==0)throw new EndOfStreamException();offset+=n;}return data; }
        static async Task Reply(Stream stream,string json,CancellationToken token)
        { var data=Utf8.GetBytes(json);if(data.Length>1048576)throw new InvalidDataException();int n=data.Length;var prefix=new[]{(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n};await stream.WriteAsync(prefix,0,4,token);await stream.WriteAsync(data,0,data.Length,token); }
        public void Dispose() { if(stop.IsCancellationRequested)return;stop.Cancel();listener.Stop();lock(clients)foreach(var client in clients)client.Close(); }
    }
}
#endif
