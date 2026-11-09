using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Bridge.Unity.Demo
{
    public sealed class SceneWorld : IBridgeDataSource
    {
        readonly Dictionary<string, GameObject> cubes = new Dictionary<string, GameObject>();
        readonly int owner = Thread.CurrentThread.ManagedThreadId;
        readonly GameObject prefab;
        public readonly BridgeDocument Config;
        public SceneWorld(GameObject prefab, BridgeDocument config) { this.prefab = prefab; Config = config; }
        public bool IsOwner => Thread.CurrentThread.ManagedThreadId == owner;
        void AssertOwner() { if (!IsOwner) throw new InvalidOperationException("Unity owner thread required."); }
        public string Spawn(float x, float y, float z)
        {
            AssertOwner(); var id = Guid.NewGuid().ToString("N");
            var cube = prefab != null ? UnityEngine.Object.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Bridge Cube " + id; cube.transform.position = new Vector3(x,y,z); cubes.Add(id,cube); return id;
        }
        public bool Move(string id, float x, float y, float z) { AssertOwner(); if (!cubes.TryGetValue(id,out var cube)) return false; cube.transform.position = new Vector3(x,y,z); return true; }
        public bool Delete(string id) { AssertOwner(); if (!cubes.TryGetValue(id,out var cube)) return false; cubes.Remove(id); UnityEngine.Object.Destroy(cube); return true; }
        public Task<BridgeDataSnapshot> ReadAsync(CancellationToken token)
        {
            AssertOwner(); token.ThrowIfCancellationRequested(); var doc = BridgeDocument.Create("private.scene");
            foreach (var pair in cubes)
            {
                var p = pair.Value.transform.position;
                doc.AddBlock(pair.Key,"scene.cube",DemoJson.Object(new Dictionary<string,object> { ["x"]=p.x,["y"]=p.y,["z"]=p.z,["privateNote"]="not published" }));
            }
            return Task.FromResult(new BridgeDataSnapshot(doc));
        }
    }
    [Preserve] public enum DemoMode { Ready, Running }
    [Preserve] public sealed class DemoReply { public bool Owner; }
    [Preserve] public sealed class SceneCommands
    {
        readonly SceneWorld world;
        public SceneCommands(SceneWorld world) { this.world=world; }
        [Preserve, BridgeCommand("scene.spawn_cube",Permission=BridgePermission.Edit)] public string Spawn(float x,float y,float z) => world.Spawn(x,y,z);
        [Preserve, BridgeCommand("scene.move",Permission=BridgePermission.Edit)] public bool Move(string objectId,float x,float y,float z) => world.Move(objectId,x,y,z);
        [Preserve, BridgeCommand("scene.delete",Permission=BridgePermission.Edit)] public bool Delete(string objectId) => world.Delete(objectId);
        [Preserve, BridgeCommand("simulation.set_speed",Permission=BridgePermission.Control)] public float Speed([BridgeRange(0.1,10)] float speed) { world.Config.Set("settings","speed",speed); return speed; }
        [Preserve, BridgeCommand("scene.thread_check",Permission=BridgePermission.Read)] public bool ThreadCheck() => world.IsOwner;
        [Preserve, BridgeCommand("scene.admin_reset",Permission=BridgePermission.Admin)] public bool Admin() => true;
        [Preserve, BridgeCommand("demo.enum",Permission=BridgePermission.Read)] public DemoMode Echo(DemoMode mode) => mode;
        [Preserve, BridgeCommand("demo.async_dto",Permission=BridgePermission.Read)] public async Task<DemoReply> Async(CancellationToken token) { await Task.Yield(); token.ThrowIfCancellationRequested(); return new DemoReply { Owner=world.IsOwner }; }
    }
}
