using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Runtime;
using UnityEngine;

namespace Bridge.Unity
{
    /// <summary>Attach on the Unity owner thread. Update starts one operation and retains the gate until its actual completion.</summary>
    public sealed class UnityBridgeDispatcher : MonoBehaviour, IBridgeDispatcher
    {
        interface IWork { bool Finished { get; } Task Run(); void Cancel(); }
        sealed class Work<T> : IWork
        {
            readonly Func<CancellationToken, Task<T>> action;
            readonly CancellationTokenSource linked;
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL cannot schedule a ThreadPool continuation. Inline completion stays on Update;
            // callers must keep continuations short, just as for any Unity callback.
            readonly TaskCompletionSource<T> result = new TaskCompletionSource<T>();
#else
            readonly TaskCompletionSource<T> result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            readonly CancellationTokenRegistration registration;
            public Work(Func<CancellationToken, Task<T>> action, CancellationToken token, CancellationToken stop)
            {
                this.action = action; linked = CancellationTokenSource.CreateLinkedTokenSource(token, stop);
                registration = linked.Token.Register(() => result.TrySetCanceled());
            }
            public Task<T> Result => result.Task;
            public bool Finished => result.Task.IsCompleted;
            public void Cancel() => linked.Cancel();
            public async Task Run()
            {
                try { linked.Token.ThrowIfCancellationRequested(); result.TrySetResult(await action(linked.Token)); }
                catch (OperationCanceledException) { result.TrySetCanceled(); }
                catch (Exception ex) { result.TrySetException(ex); }
                finally { registration.Dispose(); linked.Dispose(); }
            }
        }
        readonly object sync = new object();
        readonly Queue<IWork> queue = new Queue<IWork>();
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        bool disposed, busy;
        public int OwnerThreadId { get; private set; }
        int capacity = 64;
        public int Capacity
        {
            get => Volatile.Read(ref capacity);
            set { if(value < 1 || value > 10000) throw new ArgumentOutOfRangeException(nameof(value)); Volatile.Write(ref capacity,value); }
        }
        void Awake() => OwnerThreadId = Thread.CurrentThread.ManagedThreadId;
        public Task<T> InvokeAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (sync)
            {
                if (disposed) throw new ObjectDisposedException(nameof(UnityBridgeDispatcher));
                if (queue.Count + (busy ? 1 : 0) >= Capacity) throw new BridgeDispatchException("CAPACITY_EXCEEDED");
                var work = new Work<T>(action, cancellationToken, stop.Token); queue.Enqueue(work); return work.Result;
            }
        }
        async void Update()
        {
            IWork work;
            lock (sync)
            {
                if (disposed || busy) return;
                while(queue.Count != 0 && queue.Peek().Finished) _ = queue.Dequeue().Run();
                if(queue.Count == 0) return;
                work = queue.Dequeue(); busy = true;
            }
            // Deliberately preserve UnitySynchronizationContext across await.
            try { await work.Run(); } finally { lock (sync) busy = false; }
        }
        public void Dispose()
        {
            lock (sync) { if (disposed) return; disposed = true; }
            try { stop.Cancel(); }
            finally
            {
                // Drain cancelled entries, releasing registrations even after Update stops.
                lock (sync) { while (queue.Count != 0) _ = queue.Dequeue().Run(); }
            }
        }
        void OnDestroy() => Dispose();
    }
}
