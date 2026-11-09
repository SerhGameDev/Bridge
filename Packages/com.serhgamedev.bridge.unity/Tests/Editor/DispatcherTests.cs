using System;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Runtime;
using NUnit.Framework;
using UnityEngine;
namespace Bridge.Unity.EditorTests
{
    public sealed class DispatcherTests
    {
        [Test] public void DisposeCancelsQueuedWithoutUpdate()
        {
            var go=new GameObject("dispatcher test");var dispatcher=go.AddComponent<UnityBridgeDispatcher>();
            var task=dispatcher.InvokeAsync(ct=>Task.FromResult(1),CancellationToken.None);dispatcher.Dispose();
            Assert.That(task.IsCanceled);Assert.Throws<ObjectDisposedException>(()=>dispatcher.InvokeAsync(ct=>Task.FromResult(2),CancellationToken.None));
            UnityEngine.Object.DestroyImmediate(go);
        }
        [Test] public void CapacityIsBounded()
        {
            var go=new GameObject("dispatcher test");var dispatcher=go.AddComponent<UnityBridgeDispatcher>();dispatcher.Capacity=1;
            dispatcher.InvokeAsync(ct=>Task.FromResult(1),CancellationToken.None);
            Assert.Throws<BridgeDispatchException>(()=>dispatcher.InvokeAsync(ct=>Task.FromResult(2),CancellationToken.None));dispatcher.Dispose();UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
