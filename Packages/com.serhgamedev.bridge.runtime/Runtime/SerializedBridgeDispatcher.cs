#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Runtime
{
    /// <summary>Serializes asynchronous operations, without creating a dedicated thread or promising affinity.</summary>
    public sealed class SerializedBridgeDispatcher : IBridgeDispatcher
    {
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly int capacity;
        private int pending, disposed;
        public SerializedBridgeDispatcher(int capacity = 64)
        { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); this.capacity = capacity; }
        public async Task<T> InvokeAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (Volatile.Read(ref disposed) != 0) throw new ObjectDisposedException(nameof(SerializedBridgeDispatcher));
            if (Interlocked.Increment(ref pending) > capacity) { Interlocked.Decrement(ref pending); throw new BridgeDispatchException("BUSY"); }
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stop.Token);
                await gate.WaitAsync(linked.Token).ConfigureAwait(false);
                try { linked.Token.ThrowIfCancellationRequested(); return await action(linked.Token).ConfigureAwait(false); }
                finally { gate.Release(); }
            }
            finally { Interlocked.Decrement(ref pending); }
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) stop.Cancel();
            // Do not dispose gate under in-flight callbacks. Managed state is reclaimed after completion.
        }
    }
}
