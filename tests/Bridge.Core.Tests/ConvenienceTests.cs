using System;
using System.IO;
using System.Linq;
using System.Text;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;

public class ConvenienceTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "bridge-dx-" + Guid.NewGuid().ToString("N"));
    public ConvenienceTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    string FilePath => Path.Combine(root, "factory.bridge.json");
    static BridgeDocument Document()
    {
        var doc = BridgeDocument.Create("factory");
        doc.AddBlock("settings", "settings", BridgeValue.Parse("{\"speed\":1,\"nil\":null,\"off\":false,\"zero\":0,\"empty\":\"\",\"x/y.z\":2}"));
        return doc;
    }
    [Fact] public void ExistenceAndTypedAccessDistinguishMissingNullAndType()
    {
        var doc = Document(); var before = doc.ToJson();
        Assert.True(doc.ContainsBlock("settings")); Assert.False(doc.ContainsBlock("Settings"));
        foreach (var field in new[] { "nil", "off", "zero", "empty", "speed", "x/y.z" }) Assert.True(doc.HasField("settings", field));
        Assert.False(doc.HasField("settings", "Speed")); Assert.False(doc.HasField("absent", "speed"));
        Assert.True(doc.TryGet<string?>("settings", "nil", out var nil)); Assert.Null(nil);
        Assert.False(doc.TryGet<int>("settings", "absent", out var missing)); Assert.Equal(0, missing);
        Assert.False(doc.TryGet<int>("absent", "speed", out _));
        Assert.Equal("VALUE_TYPE", Assert.Throws<BridgeException>(() => doc.TryGet<int>("settings", "empty", out _)).Diagnostics[0].Code);
        var failure = doc.TryRead<int>("settings", "nil"); Assert.False(failure.Success);
        Assert.Equal("factory", failure.Diagnostics[0].DocumentId); Assert.Equal("/blocks/settings/data/nil", failure.Diagnostics[0].Path);
        Assert.Equal("FIELD_NOT_FOUND", doc.TryRead<int>("settings", "absent").Diagnostics[0].Code);
        Assert.Equal("BLOCK_NOT_FOUND", doc.TryRead<int>("absent", "speed").Diagnostics[0].Code);
        Assert.Equal("/blocks/absent/data/speed", doc.TryRead<int>("absent", "speed").Diagnostics[0].Path);
        Assert.ThrowsAny<ArgumentException>(() => BridgeFile.TryLoad("bad\0path"));
        Assert.Equal(before, doc.ToJson());
        doc.Set("settings", "speed", 2d); Assert.Equal(2d, doc.Get<double>("settings", "speed"));
        Assert.False(doc.TryRead<int>("absent", "speed").Success);
        doc.AddBlock("empty", "t", BridgeValue.Parse("{}")); Assert.False(doc.HasField("empty", "x"));
        Assert.Throws<BridgeException>(() => doc.Set("absent", "speed", 3)); Assert.False(doc.ContainsBlock("absent"));
    }
    [Theory][InlineData("null")][InlineData("[]")][InlineData("0")][InlineData("false")]
    public void NonObjectIsNotAFieldContainer(string json)
    {
        var doc = BridgeDocument.Create("d"); doc.AddBlock("b", "t", BridgeValue.Parse(json));
        Assert.False(doc.HasField("b", "x")); Assert.False(doc.GetBlock("b").HasField("x"));
        Assert.Throws<BridgeException>(() => doc.TryGet<int>("b", "x", out _));
        Assert.False(doc.TryRead<int>("b", "x").Success); Assert.Throws<BridgeException>(() => doc.Set("b", "x", 1));
    }
    [Theory][InlineData("")][InlineData(" ")][InlineData(null)]
    public void InvalidArgumentsAreNotAbsence(string? name)
    {
        var doc = Document();
        Assert.ThrowsAny<ArgumentException>(() => doc.ContainsBlock(name!));
        Assert.ThrowsAny<ArgumentException>(() => doc.HasField("settings", name!));
        Assert.ThrowsAny<ArgumentException>(() => doc.TryGet<int>("settings", name!, out _));
        Assert.ThrowsAny<ArgumentException>(() => doc.TryRead<int>("settings", name!));
        Assert.ThrowsAny<ArgumentException>(() => BridgeFile.Exists(name!));
        Assert.ThrowsAny<ArgumentException>(() => BridgeFile.TryLoad(name!));
    }
    [Fact] public void FileMissingCorruptSchemaEncodingAndRaceAreDistinct()
    {
        Assert.False(BridgeFile.Exists(FilePath)); Assert.Equal("FILE_NOT_FOUND", BridgeFile.TryLoad(FilePath).Diagnostics[0].Code);
        File.WriteAllText(FilePath, "{BAD"); Assert.True(BridgeFile.Exists(FilePath));
        Assert.Equal("JSON_SYNTAX", BridgeFile.TryLoad(FilePath).Diagnostics[0].Code);
        File.WriteAllText(FilePath, "{}"); Assert.Equal("FORMAT_SCHEMA", BridgeFile.TryLoad(FilePath).Diagnostics[0].Code);
        File.WriteAllText(FilePath, "{\"bridgeVersion\":2}"); Assert.Equal("FORMAT_VERSION", BridgeFile.TryLoad(FilePath).Diagnostics[0].Code);
        File.WriteAllBytes(FilePath, new byte[] { 0xFF }); Assert.Equal("JSON_ENCODING", BridgeFile.TryLoad(FilePath).Diagnostics[0].Code);
        File.WriteAllText(FilePath, Document().ToJson(), new UTF8Encoding(true));
        Assert.True(BridgeFile.TryLoad(FilePath).Success); Assert.True(BridgeFile.Exists(FilePath));
        File.Delete(FilePath); Assert.Equal("FILE_NOT_FOUND", Assert.Throws<BridgeException>(() => BridgeFile.Load(FilePath)).Diagnostics[0].Code);
    }
    [Fact] public void AtomicSavePreservesLexemesReplacesAndCleansFailedTemps()
    {
        var doc = Document(); doc.Set("settings", "large", BridgeValue.Parse("-1.234567890123456789e1000")); doc.Set("settings", "text", "Привет 🚀");
        BridgeFile.Save(doc, FilePath); Assert.Equal(doc.ToJson(), BridgeFile.Load(FilePath).ToJson());
        doc.Set("settings", "speed", 3); BridgeFile.SaveAtomic(doc, FilePath); Assert.Equal(3, BridgeFile.Load(FilePath).Get<int>("settings", "speed"));
        string original = File.ReadAllText(FilePath);
        if (OperatingSystem.IsWindows())
        {
            using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.False(BridgeFile.TrySaveAtomic(doc, FilePath).Success);
                Assert.Equal("FILE_IO", BridgeFile.TryLoad(FilePath).Diagnostics[0].Code);
            }
            Assert.Equal(original, File.ReadAllText(FilePath));
        }
        var directory = Path.Combine(root, "directory.bridge.json"); Directory.CreateDirectory(directory);
        Assert.False(BridgeFile.TrySaveAtomic(doc, directory).Success);
        Assert.Empty(Directory.GetFiles(root, ".bridge-*.tmp"));
        Assert.False(BridgeFile.TryLoad(directory).Success);
    }
    [Fact] public void ReadOnlyDestinationIsReportedAndOriginalSurvives()
    {
        if (!OperatingSystem.IsWindows()) return; // Unix access bits depend on effective uid; lock/IO cases are covered above.
        BridgeFile.Save(Document(), FilePath); File.SetAttributes(FilePath, FileAttributes.ReadOnly);
        try
        {
            Assert.False(BridgeFile.TrySaveAtomic(Document(), FilePath).Success);
            Assert.Equal(1, BridgeFile.Load(FilePath).Get<int>("settings", "speed"));
            Assert.Empty(Directory.GetFiles(root, ".bridge-*.tmp"));
        }
        finally { File.SetAttributes(FilePath, FileAttributes.Normal); }
    }
    [Fact] public void SerializationFailureNeverTouchesExistingFile()
    {
        var doc = Document(); BridgeFile.Save(doc, FilePath); string original = File.ReadAllText(FilePath);
        doc.AddBlock("invalid", "t", BridgeValue.Object(new[] { new System.Collections.Generic.KeyValuePair<string, BridgeValue>("\uD800", BridgeValue.Null) }));
        Assert.Throws<BridgeException>(() => BridgeFile.SaveAtomic(doc, FilePath));
        Assert.Equal(original, File.ReadAllText(FilePath)); Assert.Empty(Directory.GetFiles(root, ".bridge-*.tmp"));
    }
}
