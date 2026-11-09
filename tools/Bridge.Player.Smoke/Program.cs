using Bridge.Core;
using Bridge.Player;

var token=Environment.GetEnvironmentVariable("BRIDGE_PLAYER_TOKEN")??throw new Exception("BRIDGE_PLAYER_TOKEN is required.");
var client=new PlayerClient(token);
void Require(bool ok,string step){if(!ok)throw new Exception("IPC smoke: "+step);}
var catalog=await client.DiscoverAsync();Require(catalog.Get<string>("status")=="Completed","catalog");
Require(!catalog.ToJson().Contains("scene.admin_reset"),"hidden admin");
var ids=new List<string>();
for(int i=0;i<3;i++) {var r=await client.ExecuteAsync("scene.spawn_cube",BridgeValue.Parse("{\"x\":0,\"y\":1,\"z\":0}"));Require(r.Get<string>("status")=="Completed","spawn");ids.Add(r.GetProperty("data").As<string>());}
var move=BridgeValue.Parse("{\"x\":7,\"y\":2,\"z\":3}").WithField("objectId",BridgeValue.From(ids[0]));
Require((await client.ExecuteAsync("scene.move",move)).GetProperty("data").As<bool>(),"move");
Require((await client.ExecuteAsync("scene.delete",BridgeValue.Parse("{}").WithField("objectId",BridgeValue.From(ids[1])))).GetProperty("data").As<bool>(),"delete");
var query=await client.QueryAsync("scene",new BridgeQuerySpec());var doc=BridgeDocument.Parse(query.GetProperty("data").ToJson());
Require(doc.Blocks.Count==2&&doc.Get<float>(ids[0],"x")==7&&!doc.ContainsBlock(ids[1]),"live transform snapshot");
Require(!query.ToJson().Contains("privateNote"),"private data");
Require((await client.ExecuteAsync("scene.admin_reset",BridgeValue.Parse("{}"))).GetProperty("error").Get<string>("code")=="NOT_FOUND","denied admin");
Require((await client.QueryAsync("scene",new BridgeQuerySpec(fields:new[]{"privateNote"}))).GetProperty("error").Get<string>("code")=="NOT_FOUND","denied field");
for(int i=0;i<5;i++)Require((await client.ExecuteAsync("scene.thread_check",BridgeValue.Parse("{}"))).GetProperty("data").As<bool>(),"owner thread");
Require((await new PlayerClient(Guid.NewGuid().ToString("N")).DiscoverAsync()).GetProperty("error").Get<string>("code")=="UNAUTHORIZED","wrong token");
Require((await client.SendAsync(BridgeValue.Parse("{\"op\":\"discover\",\"permission\":\"Admin\"}"))).GetProperty("error").Get<string>("code")=="INVALID_REQUEST","permission spoof");
Require((await client.QueryAsync("config",new BridgeQuerySpec(id:"hidden"))).GetProperty("error").Get<string>("code")=="NOT_FOUND","hidden block");
var speed=await client.ExecuteAsync("simulation.set_speed",BridgeValue.Parse("{\"speed\":3}"));Require(speed.GetProperty("data").As<float>()==3,"set speed");
Require(BridgeDocument.Parse((await client.QueryAsync("config",new BridgeQuerySpec())).GetProperty("data").ToJson()).Get<float>("settings","speed")==3,"updated config");
if(Environment.GetEnvironmentVariable("BRIDGE_READER_TOKEN") is string readerToken)
{
 var reader=new PlayerClient(readerToken);
 Require((await reader.DiscoverAsync()).ToJson().Contains("scene.spawn_cube")==false,"reader catalog");
 Require((await reader.ExecuteAsync("scene.spawn_cube",BridgeValue.Parse("{\"x\":0,\"y\":0,\"z\":0}"))).GetProperty("error").Get<string>("code")=="NOT_FOUND","reader mutation denied");
 Require((await reader.QueryAsync("scene",new BridgeQuerySpec())).Get<string>("status")=="Completed","reader snapshot");
}
foreach(bool badMagic in new[]{true,false})
{
 using var socket=new System.Net.Sockets.TcpClient();await socket.ConnectAsync(System.Net.IPAddress.Loopback,19731);
 using var stream=socket.GetStream();await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(badMagic?"INVALID!!":"BRIDGE/1\n"));
 if(!badMagic)await stream.WriteAsync(new byte[]{0x7f,0xff,0xff,0xff});
 using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));var b=new byte[1];
 Require(await stream.ReadAsync(b,timeout.Token)==0,badMagic?"bad magic closed":"oversize closed before body");
}
Console.WriteLine("PLAYER_IPC_PASS: actual cubes=2; x=7; deleted absent; owner=true; private/admin/auth denied");

