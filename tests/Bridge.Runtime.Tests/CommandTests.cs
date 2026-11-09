using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using Xunit;

namespace Bridge.Runtime.Tests;
public class CommandTests
{
    public enum Mode { Slow, Fast }
    public sealed class Settings { public int Speed; }
    public sealed class Commands
    {
        public int Called;
        public int Unmarked() => 99;
        [BridgeCommand("private.secret")] private int Private() => 99;
        [BridgeCommand("math.add", Permission=BridgePermission.Read, Description="Add integers")]
        public int Add([BridgeDescription("First number")] int a, int b = 2) { Called++; return a+b; }
        [BridgeCommand("control.range", Permission=BridgePermission.Control)]
        public double Range([BridgeRange(0.1,10)] double speed) => speed;
        [BridgeCommand("value.null", Permission=BridgePermission.Read)]
        public string? Null([BridgeAllowNull] string? text) => text;
        [BridgeCommand("value.nullint", Permission=BridgePermission.Read)] public int? NullInt(int? value) => value;
        [BridgeCommand("value.nullbool", Permission=BridgePermission.Read)] public bool? NullBool(bool? value) => value;
        [BridgeCommand("value.mode", Permission=BridgePermission.Read)] public Mode EnumValue(Mode mode) => mode;
        [BridgeCommand("value.dto", Permission=BridgePermission.Edit)] public Settings Dto(Settings settings) => new Settings { Speed=settings.Speed+1 };
        [BridgeCommand("async.add", Permission=BridgePermission.Read)] public async Task<int> Async(int a, CancellationToken cancellationToken) { await Task.Delay(1,cancellationToken); return a+1; }
        [BridgeCommand("async.void", Permission=BridgePermission.Read)] public async Task NoResult(CancellationToken cancellationToken) { await Task.Delay(1,cancellationToken); Called++; }
        [BridgeCommand("sync.void", Permission=BridgePermission.Read)] public void Void() => Called++;
        [BridgeCommand("admin.secret")] public string Secret() => "secret";
        [BridgeCommand("errors.throw", Permission=BridgePermission.Read)] public int Throw() => throw new InvalidOperationException("sensitive details");
    }
    static BridgeCodecRegistry Codecs()
    {
        var c = new BridgeCodecRegistry().RegisterEnum<Mode>();
        c.Register(BridgeValue.Parse("{\"type\":\"object\",\"properties\":{\"speed\":{\"type\":\"integer\"}},\"required\":[\"speed\"],\"additionalProperties\":false}"),
            v => new Settings { Speed=(int)v.Get<decimal>("speed") }, s => BridgeValue.Parse("{}").WithField("speed",BridgeValue.From(s.Speed)));
        return c;
    }
    static BridgeHost Host(Commands instance) => BridgeHostBuilder.Create("test").UseCodecs(Codecs()).AddCommands(instance).Build();
    static Task<BridgeValue> Invoke(BridgeHost host,string command,string args,CancellationToken token=default)
    { Assert.True(host.Registry.TryGet(command,out var entry)); return entry.Invoke(BridgeValue.Parse(args), token); }
    [Fact] public async Task ExplicitMethodsHaveSchemaAndSyncAsyncVoidEnumDtoBinding()
    {
        var commands = new Commands(); using var host=Host(commands);
        var catalog=host.CommandsFor(new BridgeRequestContext("reader",BridgePermission.Read));
        Assert.DoesNotContain(catalog,c=>c.Name.Contains("secret") || c.Name=="control.range" || c.Name=="value.dto");
        Assert.False(host.Registry.TryGet("Unmarked",out _)); Assert.False(host.Registry.TryGet("private.secret",out _));
        var add=Assert.Single(catalog,c=>c.Name=="math.add");
        Assert.Equal("Add integers",add.Description); Assert.False(add.InputSchema.Get<bool>("additionalProperties"));
        Assert.Equal("First number",add.InputSchema.GetProperty("properties").GetProperty("a").Get<string>("description"));
        Assert.Single(add.InputSchema.GetProperty("required").Items);
        Assert.Equal(5,(await Invoke(host,"math.add","{\"a\":3}")).As<int>());
        Assert.Equal(4,(await Invoke(host,"math.add","{\"a\":1.0,\"b\":3e0}")).As<int>());
        Assert.Equal(BridgeValueKind.Null,(await Invoke(host,"value.null","{\"text\":null}")).Kind);
        Assert.Equal(BridgeValueKind.Null,(await Invoke(host,"value.nullint","{\"value\":null}")).Kind);
        Assert.Equal(BridgeValueKind.Null,(await Invoke(host,"value.nullbool","{\"value\":null}")).Kind);
        Assert.Equal("Fast",(await Invoke(host,"value.mode","{\"mode\":\"Fast\"}")).As<string>());
        Assert.Equal(3,(await Invoke(host,"value.dto","{\"settings\":{\"speed\":2}}")).Get<int>("speed"));
        Assert.Equal(3,(await Invoke(host,"async.add","{\"a\":2}")).As<int>());
        Assert.Equal(BridgeValueKind.Null,(await Invoke(host,"async.void","{}")).Kind);
        Assert.Equal(BridgeValueKind.Null,(await Invoke(host,"sync.void","{}")).Kind);
        using var stop=new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Invoke(host,"async.add","{\"a\":2}",stop.Token));
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>Invoke(host,"errors.throw","{}")); Assert.Equal("sensitive details",error.Message);
    }
    [Theory]
    [InlineData("math.add","{}")] [InlineData("math.add","{\"a\":1,\"extra\":2}")]
    [InlineData("math.add","{\"a\":\"1\"}")] [InlineData("math.add","{\"a\":null}")]
    [InlineData("math.add","{\"a\":1.000000000000000000000000000001}")]
    [InlineData("math.add","{\"a\":2147483648}")] [InlineData("math.add","{\"a\":1e1000}")]
    [InlineData("control.range","{\"speed\":0.09}")] [InlineData("control.range","{\"speed\":10.1}")]
    [InlineData("value.mode","{\"mode\":\"fast\"}")] [InlineData("value.mode","{\"mode\":1}")]
    [InlineData("value.dto","{\"settings\":{\"speed\":2,\"secret\":true}}")]
    [InlineData("async.add","{\"a\":1,\"cancellationToken\":null}")]
    public async Task InvalidValuesNeverInvokeHandler(string command,string args)
    { var c=new Commands(); using var host=Host(c); var error=await Assert.ThrowsAsync<BridgeBindingException>(()=>Invoke(host,command,args)); Assert.Equal("INVALID_ARGUMENT",error.Code); Assert.Equal(0,c.Called); }
    public sealed class Generic { [BridgeCommand("bad.generic")] public T Run<T>(T value)=>value; }
    public sealed class Ref { [BridgeCommand("bad.ref")] public void Run(ref int value)=>value++; }
    public sealed class Static { [BridgeCommand("bad.static")] public static void Run() {} }
    public sealed class Params { [BridgeCommand("bad.params")] public void Run(params int[] values) {} }
    public sealed class Object { [BridgeCommand("bad.object")] public object Run(object value)=>value; }
    public sealed class AsyncVoid { [BridgeCommand("bad.asyncvoid")] public async void Run() { await Task.Yield(); } }
    public sealed class ValueTaskReturn { [BridgeCommand("bad.valuetask")] public ValueTask<int> Run()=>new ValueTask<int>(1); }
    public sealed class BadRange { [BridgeCommand("bad.range")] public int Run([BridgeRange(4,2)] int value)=>value; }
    [Fact] public void InvalidRegistrationsAndDuplicateNamesFailBuild()
    {
        foreach(var instance in new object[]{new Generic(),new Ref(),new Static(),new Params(),new Object(),new AsyncVoid(),new ValueTaskReturn(),new BadRange()})
            Assert.Throws<ArgumentException>(()=>BridgeHostBuilder.Create("bad").AddCommands(instance).Build());
        var registry=Codecs(); Assert.Throws<ArgumentException>(()=>BridgeHostBuilder.Create("dup").UseCodecs(registry).AddCommands(new Commands()).AddCommands(new Commands()).Build());
        Assert.Throws<ArgumentException>(()=>registry.Register(BridgeValue.Parse("{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}"), _=>new Settings(), _=>BridgeValue.Null));
        Assert.Throws<ArgumentException>(()=>new BridgeCodecRegistry().Register<object>(BridgeValue.Parse("{\"type\":\"string\"}"), _=>new object(), _=>BridgeValue.Null));
    }
    [Fact] public async Task TypedRegistrarUsesExplicitCodecsAndNoMethodDiscovery()
    {
        var codecs=Codecs();
        using var host=BridgeHostBuilder.Create("typed").UseCodecs(codecs)
            .AddCommand<Settings,int>("typed.speed", (settings,_)=>Task.FromResult(settings.Speed*2),BridgePermission.Read).Build();
        Assert.Equal(4,(await Invoke(host,"typed.speed","{\"speed\":2}")).As<int>());
        Assert.Single(host.CommandsFor(BridgeRequestContext.Local()));
        Assert.Empty(host.CommandsFor(new BridgeRequestContext("untrusted")));
    }
}
