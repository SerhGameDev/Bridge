using System;
using System.IO;
using System.Linq;
using Bridge.Core;
using NUnit.Framework;

namespace Bridge.Core.EditorTests
{
    public class BridgeEditorTests
    {
        private sealed class Settings { public double Speed; }
        [Test] public void CoreHasNoUnityEngineAssemblyReference()
        { Assert.IsFalse(typeof(BridgeDocument).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("UnityEngine", StringComparison.Ordinal))); }
        [Test] public void StrictJsonRejectsDuplicateKeysAndTrailingCommas()
        {
            Assert.Throws<BridgeException>(() => BridgeValue.Parse("{\"a\":1,\"a\":2}"));
            Assert.Throws<BridgeException>(() => BridgeValue.Parse("[1,]"));
        }
        [Test] public void ExactNumbersAndUnicodeRoundTrip()
        {
            var doc = BridgeDocument.Create("Юникод"); doc.AddBlock("a", "t", BridgeValue.Parse("{\"number\":1.234567890123456789e1000,\"text\":\"✓\"}"));
            Assert.AreEqual(doc.ToJson(), BridgeDocument.Parse(doc.ToJson()).ToJson());
            Assert.IsTrue(doc.ToJson().Contains("1.234567890123456789e1000"));
        }
        [Test] public void RegisteredDtoUsesExplicitReadAndWrite()
        {
            var doc = BridgeDocument.Create("dto"); doc.AddBlock("a", "t", BridgeValue.Parse("{\"speed\":2}"));
            doc.Serializers.Register<Settings>(v => new Settings { Speed = v.Get<double>("speed") }, s => BridgeValue.Parse("{}").WithField("speed", BridgeValue.From(s.Speed)));
            var dto = doc.GetBlock("a").Deserialize<Settings>(); dto.Speed = 3;
            Assert.AreEqual(2, doc.GetBlock("a").Get<double>("speed"));
            doc.SetBlockData("a", dto); Assert.AreEqual(3, doc.GetBlock("a").Get<double>("speed"));
        }
        [Test] public void LinqAndProjectionWorkWithoutMutatingSource()
        {
            var doc = BridgeDocument.Create("query"); doc.AddBlock("a", "t", BridgeValue.Parse("{\"visible\":1,\"secret\":2}"));
            Assert.AreEqual(1, doc.Blocks.Where(b => b.Type == "t").Count());
            var selection = doc.Query().SelectFields("visible").Execute();
            doc.GetBlock("a").Set("visible", 9);
            Assert.AreEqual(1, selection.Blocks[0].Data.Get<int>("visible")); Assert.IsFalse(selection.ToJson().Contains("secret"));
        }
        [Test] public void InMemoryReferencesAndCycleDiagnosticsWork()
        {
            var a = BridgeDocument.Create("a"); var b = BridgeDocument.Create("b");
            a.AddBlock("x", "t", BridgeValue.Null).SetReference("b", new BridgeReference("b.bridge.json", "x"));
            b.AddBlock("x", "t", BridgeValue.Null);
            var resolver = new InMemoryBridgeDocumentResolver(); resolver.Set("b.bridge.json", b);
            Assert.IsTrue(a.ResolveReference("x", "b", resolver, "a.bridge.json").Success);
            b.GetBlock("x").SetReference("a", new BridgeReference("a.bridge.json", "x"));
            Assert.AreEqual("REFERENCE_CYCLE", a.ResolveReference("x", "b", resolver, "a.bridge.json").Diagnostics[0].Code);
        }
        [Test] public void Utf8StreamAndLimitsWork()
        {
            var doc = BridgeDocument.Create("stream"); using var stream = new MemoryStream(); doc.Save(stream); stream.Position = 0;
            Assert.AreEqual("stream", BridgeDocument.Load(stream).DocumentId); Assert.IsTrue(stream.CanRead);
            Assert.AreEqual("INPUT_LIMIT", BridgeDocument.TryParse(doc.ToJson(), new BridgeReadOptions { MaxUtf8Bytes = 1 }).Diagnostics[0].Code);
        }
        [Test] public void QuerySpecRejectsArbitraryExpression()
        { Assert.Throws<BridgeException>(() => BridgeQuerySpec.FromJson("{\"queryVersion\":1,\"expression\":\"x => true\"}")); }
        [Test] public void MalformedUnicodeIsRejectedWithoutReplacement()
        {
            Assert.Throws<BridgeException>(() => BridgeValue.Parse("\"\\uD800\""));
            Assert.AreEqual("🚀", BridgeValue.Parse("\"\\uD83D\\uDE80\"").As<string>());
        }
        [Test] public void FieldDiagnosticsIncludeKnownContext()
        {
            var doc = BridgeDocument.Parse("{\"bridgeVersion\":1,\"documentId\":\"context\",\"blocks\":{\"a\":{\"type\":\"t\",\"schemaVersion\":1,\"data\":{}}}}", source: "context.bridge.json");
            var error = Assert.Throws<BridgeException>(() => doc.GetBlock("a").Get<int>("missing"));
            Assert.AreEqual("context", error.Diagnostics[0].DocumentId);
            Assert.AreEqual("context.bridge.json", error.Diagnostics[0].Source);
        }
    }
}
