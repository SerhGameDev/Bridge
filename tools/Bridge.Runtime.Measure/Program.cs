using System.Diagnostics;
using System.Globalization;
using Bridge.Core;
using Bridge.Runtime;
var d=BridgeDocument.Create("measurement");
for(int i=0;i<1000;i++)d.AddBlock("b"+i.ToString("D4"),"row",BridgeValue.Parse("{}").WithField("value",BridgeValue.From(i)).WithField("privateNote",BridgeValue.From("closed")));
using var host=BridgeHostBuilder.Create("measure").AddDocument("rows",d,BridgeDataPolicy.ReadOnly().AllowFields("row","value").WithLimits(maxBlocks:1000,maxResponseBytes:524288)).AddCommands(new Commands()).Build();
var ctx=new BridgeRequestContext("measurement-reader",BridgePermission.Read);
async Task Measure(string name,int count,Func<Task<BridgeCommandResult>> action)
{
 for(int i=0;i<3;i++)if(!(await action()).Success)throw new Exception("Warmup failed.");
 long bytes=GC.GetTotalAllocatedBytes(true);var times=new List<double>();
 for(int i=0;i<count;i++){var watch=Stopwatch.StartNew();var r=await action();watch.Stop();if(!r.Success)throw new Exception(r.Error!.Code);times.Add(watch.Elapsed.TotalMilliseconds);}
 bytes=GC.GetTotalAllocatedBytes(true)-bytes;times.Sort();
 Console.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0}: n={1}; median_ms={2:F3}; max_ms={3:F3}; managed_bytes_per_call={4:F0}",name,count,times[count/2],times[^1],(double)bytes/count));
}
await Measure("discover",100,()=>host.DiscoverAsync(ctx));
await Measure("query1000blocks",20,()=>host.QueryAsync("rows",new BridgeQuerySpec(),ctx));
await Measure("execute",1000,()=>host.ExecuteLocalAsync("math.add",BridgeValue.Parse("{\"a\":2,\"b\":3}")));
Console.WriteLine("MEASUREMENT_ONLY: one Windows process, warm JIT, default dispatcher; managed process allocations, not peak memory or a performance SLA.");
public sealed class Commands {[BridgeCommand("math.add",Permission=BridgePermission.Read)]public int Add(int a,int b)=>a+b;}
