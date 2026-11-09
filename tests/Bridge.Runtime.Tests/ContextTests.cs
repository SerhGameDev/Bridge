using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using Bridge.Runtime;
using Xunit;
namespace Bridge.Runtime.Tests;

public sealed class ContextTests
{
    sealed class Pump : SynchronizationContext
    {
        readonly ConcurrentQueue<(SendOrPostCallback callback,object? state)> queue=new();
        public override void Post(SendOrPostCallback callback,object? state)=>queue.Enqueue((callback,state));
        public void Drain() { while(queue.TryDequeue(out var item))item.callback(item.state); }
    }
    [Fact] public async Task SerializedDispatcherRespectsCallerContextAcrossContendedGate()
    {
        var previous=SynchronizationContext.Current;var pump=new Pump();int owner=Environment.CurrentManagedThreadId;
        SynchronizationContext.SetSynchronizationContext(pump);
        Task<int> first,second;
        try
        {
            using var dispatcher=new SerializedBridgeDispatcher();var release=new TaskCompletionSource<int>();
            first=dispatcher.InvokeAsync(async ct=>{await release.Task;Assert.Equal(owner,Environment.CurrentManagedThreadId);return 1;},default);
            second=dispatcher.InvokeAsync(ct=>{Assert.Equal(owner,Environment.CurrentManagedThreadId);Assert.Same(pump,SynchronizationContext.Current);return Task.FromResult(2);},default);
            Assert.False(second.IsCompleted);release.SetResult(1);var timer=Stopwatch.StartNew();
            while((!first.IsCompleted||!second.IsCompleted)&&timer.Elapsed<TimeSpan.FromSeconds(5)) {pump.Drain();Thread.Yield();}
            Assert.True(first.IsCompleted&&second.IsCompleted);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        Assert.Equal(1,await first);Assert.Equal(2,await second);
    }
}
