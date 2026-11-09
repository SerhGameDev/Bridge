using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Bridge.Unity.Demo.PlayTests
{
    public sealed class SceneTests
    {
        [UnityTest] public IEnumerator AsyncOperationRetainsOwnerAndSerializationGate()
        {
            var go=new GameObject("serial test");var dispatcher=go.AddComponent<UnityBridgeDispatcher>();
            int owner=Thread.CurrentThread.ManagedThreadId,phase=0;
            var release=new TaskCompletionSource<bool>();
            var first=dispatcher.InvokeAsync(async ct=>{phase=1;await release.Task;Assert.That(Thread.CurrentThread.ManagedThreadId,Is.EqualTo(owner));phase=2;return 1;},CancellationToken.None);
            var second=dispatcher.InvokeAsync(ct=>{Assert.That(phase,Is.EqualTo(2));phase=3;return Task.FromResult(2);},CancellationToken.None);
            yield return null;yield return null;Assert.That(phase,Is.EqualTo(1));Assert.That(second.IsCompleted,Is.False);
            release.SetResult(true);
            for(int i=0;i<120 && !second.IsCompleted;i++)yield return null;
            Assert.That(first.IsCompletedSuccessfully && second.IsCompletedSuccessfully);Assert.That(phase,Is.EqualTo(3));UnityEngine.Object.Destroy(go);
        }
        [UnityTest] public IEnumerator BackgroundCommandsMutateRealTransformsOnOwner()
        {
            var go=new GameObject("demo test");var bootstrap=go.AddComponent<BridgeBootstrap>();
            var task=Task.Run(async()=>
            {
                var ctx=new BridgeRequestContext("test",BridgePermission.Edit);
                var spawn=await bootstrap.Host.ExecuteAsync("scene.spawn_cube",BridgeValue.Parse("{\"x\":1,\"y\":2,\"z\":3}"),ctx);Assert.That(spawn.Success);
                string id=spawn.Data.As<string>();
                var args=BridgeValue.Parse("{\"x\":9,\"y\":8,\"z\":7}").WithField("objectId",BridgeValue.From(id));
                Assert.That((await bootstrap.Host.ExecuteAsync("scene.move",args,ctx)).Data.As<bool>());
                var result=await bootstrap.Host.QueryAsync("scene",new BridgeQuerySpec(),ctx);
                Assert.That(BridgeDocument.Parse(result.Data.ToJson()).Get<float>(id,"x"),Is.EqualTo(9));
                Assert.That(result.ToJson(),Does.Not.Contain("privateNote"));
                Assert.That((await bootstrap.Host.ExecuteAsync("scene.thread_check",BridgeValue.Parse("{}"),ctx)).Data.As<bool>());
                Assert.That((await bootstrap.Host.ExecuteAsync("demo.async_dto",BridgeValue.Parse("{}"),ctx)).Data.Get<bool>("owner"));
                Assert.That((await bootstrap.Host.ExecuteAsync("scene.delete",BridgeValue.Parse("{}").WithField("objectId",BridgeValue.From(id)),ctx)).Data.As<bool>());
                Assert.That(BridgeDocument.Parse((await bootstrap.Host.QueryAsync("scene",new BridgeQuerySpec(),ctx)).Data.ToJson()).Blocks.Count,Is.Zero);
            });
            for(int i=0;i<1200 && !task.IsCompleted;i++)yield return null;
            UnityEngine.Object.Destroy(go);
            Assert.That(task.IsCompleted,"Timed out waiting for Unity dispatcher.");if(task.IsFaulted)throw task.Exception;
        }
        [UnityTest] public IEnumerator CancelledQueueNeverRunsMutation()
        {
            var go=new GameObject("cancel test");var dispatcher=go.AddComponent<UnityBridgeDispatcher>();var cancel=new CancellationTokenSource();bool ran=false;
            var task=dispatcher.InvokeAsync(ct=>{ran=true;return Task.FromResult(1);},cancel.Token);cancel.Cancel();yield return null;yield return null;
            Assert.That(task.IsCanceled);Assert.That(ran,Is.False);UnityEngine.Object.Destroy(go);cancel.Dispose();
        }
    }
}
