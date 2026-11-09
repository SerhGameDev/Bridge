using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Bridge.Core;
using UnityEngine;
using UnityEngine.Scripting;

namespace Bridge.Samples
{
    [Preserve]
    public sealed class SmokeSettings { public double Speed; }

    public static class BridgeAotSmoke
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void BridgeReport(string result);
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RunAtStartup()
        {
            string result;
            try { Run(); result = "BRIDGE_SMOKE_PASS"; }
            catch (Exception ex) { result = "BRIDGE_SMOKE_FAIL: " + ex; }
            Debug.Log(result);
#if UNITY_WEBGL && !UNITY_EDITOR
            BridgeReport(result);
#else
            if (!Application.isEditor) Application.Quit(result == "BRIDGE_SMOKE_PASS" ? 0 : 1);
#endif
        }
        public static void Run()
        {
            var document = BridgeDocument.Create("aot");
            document.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"large\":1.234567890123456789e1000,\"unicode\":\"✓\"}"));
            var serializers = new BridgeSerializer();
            serializers.Register<SmokeSettings>(v => new SmokeSettings { Speed = v.Get<double>("speed") },
                dto => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(dto.Speed)));
            document.UseSerializers(serializers);
            var dto = document.GetBlock("settings").Deserialize<SmokeSettings>(); dto.Speed = 7;
            if (document.GetBlock("settings").Get<double>("speed") != 1) throw new Exception("Implicit mutation.");
            document.SetBlockData("settings", dto);
            using var stream = new MemoryStream(); document.Save(stream); stream.Position = 0;
            var restored = BridgeDocument.Load(stream).UseSerializers(serializers);
            if (restored.GetBlock("settings").Deserialize<SmokeSettings>().Speed != 7) throw new Exception("Shared registry.");
            if (restored.GetBlock("settings").Get<double>("speed") != 7) throw new Exception("DTO round-trip.");
            if (document.Blocks.Where(b => b.Type == "settings").Count() != 1) throw new Exception("LINQ.");
            var selection = document.Query().WhereTypes("settings", "layout").SelectFields("speed").Execute();
            if (selection.Blocks[0].Data.Get<double>("speed") != 7) throw new Exception("Projection.");
            var oldHandle = restored.GetBlock("settings"); restored.RemoveBlock("settings");
            try { oldHandle.Set("speed", 100); throw new Exception("Removed handle accepted mutation."); }
            catch (BridgeException ex) { if (ex.Diagnostics[0].Code != "BLOCK_REMOVED") throw; }
            var other = BridgeDocument.Create("palette"); other.AddBlock("colors", "palette", BridgeValue.From("red"));
            var resolver = new InMemoryBridgeDocumentResolver(); resolver.Set("palette.bridge.json", other);
            document.GetBlock("settings").SetReference("palette", new BridgeReference("palette.bridge.json", "colors"));
            if (!document.ResolveReference("settings", "palette", resolver, "aot.bridge.json").Success) throw new Exception("Reference.");
            if (BridgeValue.Parse("1.234567890123456789e1000").ToJson() != "1.234567890123456789e1000") throw new Exception("Number precision.");
        }
    }
}
