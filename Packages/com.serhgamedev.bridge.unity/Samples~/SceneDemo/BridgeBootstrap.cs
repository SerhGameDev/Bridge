using System;
using System.Linq;
using System.Threading.Tasks;
using Bridge.Core;
using Bridge.Runtime;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Bridge.Unity.Demo
{
    public sealed class BridgeBootstrap : MonoBehaviour
    {
        public GameObject CubePrefab;
        public TextAsset Config;
        public bool SelfTest;
        public BridgeHost Host { get; private set; }
        UnityBridgeDispatcher dispatcher;
#if !UNITY_WEBGL
        PlayerServer server;
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BridgeRuntimeReport(string result);
#endif
        void Awake()
        {
            Application.runInBackground=true;
            dispatcher=gameObject.AddComponent<UnityBridgeDispatcher>();
            var doc=Config != null ? BridgeDocument.Parse(Config.text) : BridgeDocument.Create("demo");
            if(Config==null) doc.AddBlock("settings","simulation.settings",DemoJson.Object(new System.Collections.Generic.Dictionary<string,object>{{"speed",1},{"gravity",9.81},{"privateNote","not published"}}));
            var world=new SceneWorld(CubePrefab,doc);
            var codecs=new BridgeCodecRegistry().RegisterEnum<DemoMode>().Register<DemoReply>(
                BridgeValue.Parse("{\"type\":\"object\",\"properties\":{\"owner\":{\"type\":\"boolean\"}},\"required\":[\"owner\"],\"additionalProperties\":false}"),
                v=>new DemoReply { Owner=v.GetProperty("owner").As<bool>() },v=>DemoJson.Object(new System.Collections.Generic.Dictionary<string,object>{{"owner",v.Owner}}));
            Host=BridgeHostBuilder.Create("unity-demo").UseCodecs(codecs)
                .AddDocument("config",doc,BridgeDataPolicy.ReadOnly().AllowFields("simulation.settings","speed","gravity"))
                .AddDataSource("scene",world,BridgeDataPolicy.ReadOnly().AllowFields("scene.cube","x","y","z"))
                .AddCommands(new SceneCommands(world)).UseDispatcher(dispatcher)
                .AddCommand<DemoReply,DemoReply>("demo.typed",(v,ct)=>Task.FromResult(v),BridgePermission.Read).Build();
#if !UNITY_WEBGL
            if (Environment.GetCommandLineArgs().Contains("--bridge-listen")) server=new PlayerServer(Host,19731,Environment.GetEnvironmentVariable("BRIDGE_PLAYER_TOKEN"),Environment.GetEnvironmentVariable("BRIDGE_READER_TOKEN"));
#endif
        }
        async void Start()
        {
            if (!SelfTest && !Environment.GetCommandLineArgs().Contains("--bridge-self-test")) return;
            bool passed=false;
            try
            {
                // Background caller exercises the actual owner-thread dispatcher.
                async Task Smoke()
                {
                    Debug.Log("BRIDGE_RUNTIME_STEP: dispatch");
                    var ctx=new BridgeRequestContext("player-smoke",BridgePermission.Edit);
                    string[] ids=new string[3];
                    for(int i=0;i<3;i++) { var r=await Host.ExecuteAsync("scene.spawn_cube",BridgeValue.Parse("{\"x\":0,\"y\":1,\"z\":0}"),ctx); Require(r.Success,"spawn"); ids[i]=r.Data.As<string>(); }
                    Debug.Log("BRIDGE_RUNTIME_STEP: spawned=3");
                    Require((await Host.ExecuteAsync("scene.move",DemoJson.Object(new System.Collections.Generic.Dictionary<string,object>{{"objectId",ids[0]},{"x",4},{"y",2},{"z",1}}),ctx)).Success,"move");
                    Require((await Host.ExecuteAsync("scene.delete",DemoJson.Object(new System.Collections.Generic.Dictionary<string,object>{{"objectId",ids[1]}}),ctx)).Data.As<bool>(),"delete");
                    var query=await Host.QueryAsync("scene",new BridgeQuerySpec(),ctx); Require(query.Success,"query");
                    var snapshot=BridgeDocument.Parse(query.Data.ToJson()); Require(snapshot.Blocks.Count==2 && snapshot.Get<float>(ids[0],"x")==4 && !query.ToJson().Contains("privateNote"),"snapshot");
                    Debug.Log("BRIDGE_RUNTIME_STEP: live=2");
                    Require((await Host.ExecuteAsync("scene.thread_check",BridgeValue.Parse("{}"),ctx)).Data.As<bool>(),"owner");
                    Require((await Host.ExecuteAsync("scene.admin_reset",BridgeValue.Parse("{}"),ctx)).Error?.Code=="NOT_FOUND","permission");
                    Require((await Host.QueryAsync("scene",new BridgeQuerySpec(fields:new[]{"privateNote"}),ctx)).Error?.Code=="NOT_FOUND","private field");
                    Require((await Host.ExecuteAsync("demo.enum",BridgeValue.Parse("{\"mode\":\"Ready\"}"),ctx)).Data.As<string>()=="Ready","enum");
                    Require((await Host.ExecuteAsync("demo.async_dto",BridgeValue.Parse("{}"),ctx)).Data.GetProperty("owner").As<bool>(),"async DTO owner");
                    Require((await Host.ExecuteAsync("demo.typed",BridgeValue.Parse("{\"owner\":true}"),ctx)).Data.GetProperty("owner").As<bool>(),"typed AOT");
                    using(var cancel=new System.Threading.CancellationTokenSource())
                    {
                        cancel.Cancel();
                        var cancelled=await Host.ExecuteAsync("scene.thread_check",BridgeValue.Parse("{}"),new BridgeRequestContext("cancel",BridgePermission.Read,cancellationToken:cancel.Token));
                        Require(cancelled.Status==BridgeCommandStatus.Cancelled,"cancelled request");
                    }
                }
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL has no managed worker threads. Requests still traverse Update.
                await Smoke();
#else
                await Task.Run(Smoke);
#endif
                passed=true; Debug.Log("BRIDGE_RUNTIME_PLAYER_PASS: spawn=3 move=1 delete=1 live=2 owner=true enum/DTO/typed=true");
            }
            catch(Exception ex) { Debug.LogError("BRIDGE_RUNTIME_PLAYER_FAIL: "+ex); }
#if UNITY_WEBGL && !UNITY_EDITOR
            BridgeRuntimeReport(passed ? "PASS" : "FAIL");
#endif
            if(Environment.GetCommandLineArgs().Contains("--bridge-exit-after-smoke")) Application.Quit(passed?0:2);
        }
        static void Require(bool ok,string step) { if(!ok) throw new Exception("Smoke failed: "+step); }
        void OnDestroy()
        {
#if !UNITY_WEBGL
            server?.Dispose();
#endif
            Host?.Dispose(); dispatcher?.Dispose();
        }
    }
}
