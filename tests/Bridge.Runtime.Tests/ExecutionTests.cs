using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using Xunit;

namespace Bridge.Runtime.Tests;
public class ExecutionTests
{
    public sealed class Commands
    {
        readonly BridgeDocument document;
        public int Called,Running,Maximum;
        public readonly TaskCompletionSource<int> Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<int> Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public BridgeHost? Host;
        public Commands(BridgeDocument document) { this.document=document; }
        [BridgeCommand("state.increment",Permission=BridgePermission.Control)]
        public async Task<int> Increment(CancellationToken token)
        {
            Called++; Maximum=Math.Max(Maximum,Interlocked.Increment(ref Running));
            int value=document.Get<int>("state","count")+1;
            document.Set("state","count",value); await Task.Delay(1,token); document.Set("state","checksum",value*2);
            Interlocked.Decrement(ref Running); return value;
        }
        [BridgeCommand("test.wait",Permission=BridgePermission.Read)]
        public async Task<int> Wait() { Entered.TrySetResult(1); await Release.Task; Called++; return 1; }
        [BridgeCommand("test.delay",Permission=BridgePermission.Read)] public async Task<int> Delay(CancellationToken token) { await Task.Delay(10000,token); return 1; }
        [BridgeCommand("test.throw",Permission=BridgePermission.Read)] public string Throw(string text) => throw new InvalidOperationException("SECRET_EXCEPTION_"+text);
        [BridgeCommand("test.large",Permission=BridgePermission.Read)] public string Large() { Called++; return new string('a',5000); }
        [BridgeCommand("test.admin")] public int Admin() { Called++; return 99; }
        [BridgeCommand("test.nested",Permission=BridgePermission.Read)] public async Task<string> Nested() => (await Host!.ExecuteLocalAsync("test.admin",BridgeValue.Parse("{}"))).Error!.Code;
    }
    static BridgeDocument Document()
    { var d=BridgeDocument.Create("state"); d.AddBlock("state","state",BridgeValue.Parse("{\"count\":0,\"checksum\":0}")); return d; }
    static BridgeHost Host(Commands commands,BridgeDocument document,BridgeHostOptions? options=null)
    {
        var b=BridgeHostBuilder.Create("host").AddCommands(commands).AddDocument("state",document,BridgeDataPolicy.ReadOnly().AllowFields("state","count","checksum"));
        if(options!=null)b.UseOptions(options); var host=b.Build(); commands.Host=host; return host;
    }
    [Fact] public async Task PermissionHierarchyUnknownCommandsAndArgumentsAreEnforced()
    {
        foreach(BridgePermission permission in Enum.GetValues<BridgePermission>())
        foreach(BridgePermission minimum in Enum.GetValues<BridgePermission>())
            Assert.Equal(permission!=BridgePermission.None&&permission>=minimum,new BridgeRequestContext("p",permission).Allows(minimum));
        var d=Document(); var c=new Commands(d); using var host=Host(c,d);
        var reader=new BridgeRequestContext("reader",BridgePermission.Read);
        var denied=await host.ExecuteAsync("state.increment",BridgeValue.Parse("{}"),reader);
        Assert.Equal("NOT_FOUND",denied.Error!.Code); Assert.Equal(0,c.Called);
        var unknown=await host.ExecuteAsync("missing",BridgeValue.Parse("{}"),reader); Assert.Equal(denied.Error.Message,unknown.Error!.Message);
        var invalid=await host.ExecuteLocalAsync("state.increment",BridgeValue.Parse("{\"extra\":1}")); Assert.Equal("INVALID_ARGUMENT",invalid.Error!.Code); Assert.Equal(0,c.Called);
        var completed=await host.ExecuteAsync("state.increment",BridgeValue.Parse("{}"),new BridgeRequestContext("controller",BridgePermission.Control,"request1"));
        Assert.Equal(BridgeCommandStatus.Completed,completed.Status); Assert.Equal("request1",completed.RequestId); Assert.Equal(1,completed.Data.As<int>());
        Assert.NotEmpty(completed.OperationId); Assert.Equal(1,c.Called);
    }
    [Fact] public async Task ConcurrentReadsAndWritesAreCoherentAcrossAwait()
    {
        var d=Document(); var c=new Commands(d); using var host=Host(c,d);
        var reads=Enumerable.Range(0,20).Select(async _=>
        {
            var write=host.ExecuteLocalAsync("state.increment",BridgeValue.Parse("{}"));
            var read=host.QueryAsync("state",new BridgeQuerySpec(),BridgeRequestContext.Local());
            Assert.True((await write).Success); var result=await read; Assert.True(result.Success);
            var data=BridgeDocument.Parse(result.Data.ToJson()); Assert.Equal(data.Get<int>("state","count")*2,data.Get<int>("state","checksum"));
        });
        await Task.WhenAll(reads); Assert.Equal(20,d.Get<int>("state","count")); Assert.Equal(1,c.Maximum);
    }
    [Fact] public async Task QueuedTaskIsPendingCancellationAndCapacityAreHonest()
    {
        var d=Document(); var c=new Commands(d); using var host=Host(c,d,new BridgeHostOptions{MaxPendingRequests=2});
        var first=host.ExecuteLocalAsync("test.wait",BridgeValue.Parse("{}")); await c.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var stop=new CancellationTokenSource();
        var queued=host.ExecuteLocalAsync("test.admin",BridgeValue.Parse("{}"),stop.Token);
        Assert.False(queued.IsCompleted); Assert.False(first.IsCompleted); Assert.Equal(0,c.Called);
        var busy=await host.DiscoverAsync(BridgeRequestContext.Local()); Assert.Equal("BUSY",busy.Error!.Code);
        stop.Cancel(); Assert.Equal(BridgeCommandStatus.Cancelled,(await queued).Status);
        c.Release.SetResult(1); Assert.Equal(BridgeCommandStatus.Completed,(await first).Status); Assert.Equal(1,c.Called);
    }
    [Fact] public async Task DisposeCancelsPendingAndNeverPretendsRollback()
    {
        var d=Document(); var c=new Commands(d); var host=Host(c,d);
        var first=host.ExecuteLocalAsync("test.wait",BridgeValue.Parse("{}")); await c.Entered.Task;
        var pending=host.ExecuteLocalAsync("test.admin",BridgeValue.Parse("{}")); host.Dispose();
        Assert.Equal(BridgeCommandStatus.Cancelled,(await pending.WaitAsync(TimeSpan.FromSeconds(5))).Status);
        Assert.Equal("HOST_STOPPED",(await host.DiscoverAsync(BridgeRequestContext.Local())).Error!.Code);
        c.Release.SetResult(1); var actual=await first;
        Assert.Equal(BridgeCommandStatus.Cancelled,actual.Status); Assert.Equal(1,c.Called); Assert.Equal(BridgeValueKind.Null,actual.Data.Kind);
    }
    [Fact] public async Task TimeoutAndPreCancellationAreStructured()
    {
        var d=Document(); var c=new Commands(d); using var host=Host(c,d,new BridgeHostOptions{RequestTimeout=TimeSpan.FromMilliseconds(100)});
        var timed=await host.ExecuteLocalAsync("test.delay",BridgeValue.Parse("{}")); Assert.Equal("REQUEST_TIMEOUT",timed.Error!.Code); Assert.Equal(BridgeCommandStatus.Cancelled,timed.Status);
        using var stop=new CancellationTokenSource(); stop.Cancel();
        var cancelled=await host.ExecuteLocalAsync("test.admin",BridgeValue.Parse("{}"),stop.Token); Assert.Equal("REQUEST_CANCELLED",cancelled.Error!.Code); Assert.Equal(0,c.Called);
    }
    [Fact] public async Task ExceptionsAndAuditNeverExposePayloadsAndRetriesAreNotAutomatic()
    {
        var d=Document(); var c=new Commands(d); using var host=Host(c,d,new BridgeHostOptions{AuditCapacity=3,MaxResponseBytes=1024});
        var failed=await host.ExecuteLocalAsync("test.throw",BridgeValue.Parse("{\"text\":\"SECRET_PAYLOAD\"}"));
        Assert.Equal("HANDLER_FAILED",failed.Error!.Code); Assert.DoesNotContain("SECRET",failed.ToJson());
        var audit=Assert.Single(host.GetAuditSnapshot()); Assert.Equal("System.InvalidOperationException",audit.ExceptionType); Assert.Equal("test.throw",audit.Operation);
        var large=await host.ExecuteLocalAsync("test.large",BridgeValue.Parse("{}")); Assert.Equal("RESPONSE_LIMIT",large.Error!.Code); Assert.Equal(1,c.Called);
        var context=BridgeRequestContext.Local(); var a=await host.ExecuteAsync("test.admin",BridgeValue.Parse("{}"),context); var b=await host.ExecuteAsync("test.admin",BridgeValue.Parse("{}"),context);
        Assert.Equal(a.RequestId,b.RequestId); Assert.NotEqual(a.OperationId,b.OperationId); Assert.Equal(3,c.Called); Assert.Equal(3,host.GetAuditSnapshot().Count);
        Assert.DoesNotContain(host.GetAuditSnapshot(),entry=>entry.Operation.Contains("SECRET") || (entry.ExceptionType?.Contains("SECRET")??false));
    }
    [Fact] public async Task ReentrantRequestRejectsInsteadOfDeadlocking()
    {
        var d=Document(); var c=new Commands(d); using var host=Host(c,d);
        var result=await host.ExecuteLocalAsync("test.nested",BridgeValue.Parse("{}")); Assert.True(result.Success); Assert.Equal("REENTRANT_REQUEST",result.Data.As<string>()); Assert.Equal(0,c.Called);
    }
    [Fact] public async Task RequestLimitRunsBeforeHandler()
    {
        var d=Document(); var c=new Commands(d); using var host=Host(c,d,new BridgeHostOptions{MaxRequestBytes=1});
        Assert.Equal("REQUEST_LIMIT",(await host.ExecuteLocalAsync("test.admin",BridgeValue.Parse("{}"))).Error!.Code); Assert.Equal(0,c.Called);
    }
}
