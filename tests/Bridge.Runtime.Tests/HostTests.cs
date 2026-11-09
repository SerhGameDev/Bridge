using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using Xunit;

namespace Bridge.Runtime.Tests;
public class HostTests
{
    sealed class Source : IBridgeDataSource, IDisposable
    {
        public bool Disposed;
        public Task<BridgeDataSnapshot> ReadAsync(CancellationToken token) => Task.FromResult(new BridgeDataSnapshot(BridgeDocument.Create("d")));
        public void Dispose() => Disposed = true;
    }
    sealed class Dispatcher : IBridgeDispatcher
    {
        public bool Disposed;
        public Task<T> InvokeAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken token) => action(token);
        public void Dispose() => Disposed = true;
    }
    [Fact] public void BuilderValidatesRegistrationAndFreezesAfterBuild()
    {
        var source = new Source(); var builder = BridgeHostBuilder.Create("test").AddDataSource("a", source).AddDocument("A", BridgeDocument.Create("d")).AddCommands(new object());
        Assert.Throws<ArgumentException>(() => builder.AddDataSource("a", source));
        using var host = builder.Build(); Assert.Equal("test", host.Name);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Throws<InvalidOperationException>(() => builder.AddDocument("b", BridgeDocument.Create("d")));
        host.Dispose(); host.Dispose(); Assert.True(host.IsDisposed); Assert.False(source.Disposed);
        Assert.Throws<ArgumentException>(() => BridgeHostBuilder.Create("bad name"));
        Assert.Throws<ArgumentNullException>(() => BridgeHostBuilder.Create("a").AddCommands(null!));
    }
    [Fact] public void HostsAreIndependentAndInjectedDispatcherOwnershipIsExplicit()
    {
        var dispatcher = new Dispatcher(); using var a = BridgeHostBuilder.Create("a").UseDispatcher(dispatcher).Build();
        using var b = BridgeHostBuilder.Create("b").Build(); a.Dispose(); Assert.False(b.IsDisposed); Assert.False(dispatcher.Disposed);
        using var c = BridgeHostBuilder.Create("c").UseDispatcher(dispatcher, ownsDispatcher:true).Build(); c.Dispose(); Assert.True(dispatcher.Disposed);
    }
    [Fact] public async Task DefaultDispatcherSerializesAndCancelsPendingWork()
    {
        using var dispatcher = new SerializedBridgeDispatcher(); int running = 0, maximum = 0;
        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => dispatcher.InvokeAsync(async token =>
        { int count=Interlocked.Increment(ref running); maximum=Math.Max(maximum,count); await Task.Delay(2, token); Interlocked.Decrement(ref running); return 1; }, default)));
        Assert.Equal(1, maximum);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = dispatcher.InvokeAsync(_ => release.Task, default);
        var pending = dispatcher.InvokeAsync(_ => Task.FromResult(2), default); Assert.False(pending.IsCompleted);
        dispatcher.Dispose(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        release.SetResult(1); Assert.Equal(1, await first);
    }
    [Fact] public void RuntimeHasNoUnityOrTransportDependency()
    { Assert.DoesNotContain(typeof(BridgeHost).Assembly.GetReferencedAssemblies(), a => a.Name!.Contains("Unity") || a.Name.Contains("Mcp") || a.Name.Contains("AspNet")); }
}
