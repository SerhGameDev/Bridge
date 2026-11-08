using System;
using System.IO;
using System.Diagnostics;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;

public class ReferenceTests
{
    private static BridgeDocument Doc(string id)
    { var doc = BridgeDocument.Create(id); doc.AddBlock("x", "t", BridgeValue.Parse("{\"color\":\"red\"}")); return doc; }
    private sealed class Counter : IBridgeDocumentResolver
    {
        internal readonly InMemoryBridgeDocumentResolver Inner = new(); internal int Loads;
        public StringComparer PathComparer => StringComparer.Ordinal;
        public BridgeResult<BridgeDocument> Load(string path) { Loads++; return Inner.Load(path); }
    }
    [Fact] public void CrossFileLocalReferencesAndOperationCache()
    {
        var a = Doc("a"); var b = Doc("b"); var c = Doc("c"); var resolver = new Counter();
        a.GetBlock("x").SetReference("b", new BridgeReference("assets/b.bridge.json", "x"));
        b.GetBlock("x").SetReference("c1", new BridgeReference("../c.bridge.json", "x"));
        b.GetBlock("x").SetReference("c2", new BridgeReference(".././c.bridge.json", "x"));
        resolver.Inner.Set("assets/b.bridge.json", b); resolver.Inner.Set("c.bridge.json", c);
        var result = a.ResolveReference("x", "b", resolver, "a.bridge.json");
        Assert.True(result.Success); Assert.Equal("b", result.Value.Document.DocumentId); Assert.Equal(2, resolver.Loads);
        resolver.Inner.Set("assets/b.bridge.json", Doc("b-updated"));
        Assert.Equal("b-updated", a.ResolveReference("x", "b", resolver, "a.bridge.json").Value.Document.DocumentId);
        a.AddBlock("local", "t", BridgeValue.Null); a.GetBlock("x").SetReference("local", new BridgeReference("", "local"));
        Assert.Equal("local", a.ResolveReference("x", "local", resolver, "a.bridge.json").Value.Block.Id);
        Assert.Contains("assets/b.bridge.json", a.ToJson()); Assert.DoesNotContain("b-updated", a.ToJson());
    }
    [Fact] public void CyclesMissingBlocksAndLimitsAreDistinct()
    {
        var a = Doc("a"); var b = Doc("b"); var r = new InMemoryBridgeDocumentResolver(); r.Set("b.bridge.json", b);
        a.GetBlock("x").SetReference("self", new BridgeReference("", "x"));
        Assert.Equal("REFERENCE_CYCLE", a.ResolveReference("x", "self", r, "a.bridge.json").Diagnostics[0].Code);
        a.GetBlock("x").SetReference("b", new BridgeReference("b.bridge.json", "x"));
        b.GetBlock("x").SetReference("a", new BridgeReference("a.bridge.json", "x"));
        Assert.Equal("REFERENCE_CYCLE", a.ResolveReference("x", "b", r, "a.bridge.json").Diagnostics[0].Code);
        Assert.Equal("REFERENCE_DEPTH", a.ResolveReference("x", "b", r, "a.bridge.json", new BridgeResolutionOptions { MaxDepth = 1 }).Diagnostics[0].Code);
        Assert.Equal("REFERENCE_DOCUMENT_LIMIT", a.ResolveReference("x", "b", r, "a.bridge.json", new BridgeResolutionOptions { MaxDocuments = 1 }).Diagnostics[0].Code);
        Assert.Equal("REFERENCE_SIZE_LIMIT", a.ResolveReference("x", "b", r, "a.bridge.json", new BridgeResolutionOptions { MaxTotalUtf8Bytes = 1 }).Diagnostics[0].Code);
        Assert.Equal("REFERENCE_EDGE_LIMIT", a.ResolveReference("x", "b", r, "a.bridge.json", new BridgeResolutionOptions { MaxVisitedEdges = 1 }).Diagnostics[0].Code);
        a.GetBlock("x").SetReference("missing", new BridgeReference("missing.bridge.json", "x"));
        Assert.Equal("REFERENCE_DOCUMENT_NOT_FOUND", a.ResolveReference("x", "missing", r, "a.bridge.json").Diagnostics[0].Code);
        a.GetBlock("x").SetReference("badblock", new BridgeReference("", "missing"));
        Assert.Equal("REFERENCE_BLOCK_NOT_FOUND", a.ResolveReference("x", "badblock", r, "a.bridge.json").Diagnostics[0].Code);
        Assert.Equal("REFERENCE_NOT_FOUND", a.ResolveReference("x", "unknown", r, "a.bridge.json").Diagnostics[0].Code);
    }
    [Theory]
    [InlineData("../escape.bridge.json")][InlineData("C:\\outside.bridge.json")]
    [InlineData("/absolute.bridge.json")][InlineData("https://example.com/a.bridge.json")]
    [InlineData("a.txt")][InlineData("a.bridge.json:stream")][InlineData("\\\\server\\share\\a.bridge.json")]
    [InlineData("CON.bridge.json")][InlineData("dir. /a.bridge.json")]
    public void UnsafePathsAreRejected(string path)
    {
        var a = Doc("a"); a.GetBlock("x").SetReference("bad", new BridgeReference(path, "x"));
        Assert.Equal("REFERENCE_PATH", a.ResolveReference("x", "bad", new InMemoryBridgeDocumentResolver(), "a.bridge.json").Diagnostics[0].Code);
        Assert.Equal("REFERENCE_PATH", new FileBridgeDocumentResolver(AppContext.BaseDirectory).Load(path).Diagnostics[0].Code);
    }
    [Fact] public void FileLoadFailureRecoveryAndPerDocumentSizeLimit()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "work", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        string file = Path.Combine(root, "b.bridge.json"); var resolver = new FileBridgeDocumentResolver(root);
        Assert.False(resolver.Load("b.bridge.json").Success);
        File.WriteAllText(file, Doc("b").ToJson()); Assert.Equal("b", resolver.Load("b.bridge.json").Value.DocumentId);
        File.WriteAllText(file, Doc("new").ToJson()); Assert.Equal("new", resolver.Load("b.bridge.json").Value.DocumentId);
        Assert.Equal("INPUT_LIMIT", new FileBridgeDocumentResolver(root, new BridgeReadOptions { MaxUtf8Bytes = 10 }).Load("b.bridge.json").Diagnostics[0].Code);
        File.WriteAllText(file, "not json"); Assert.Equal("JSON_SYNTAX", resolver.Load("b.bridge.json").Diagnostics[0].Code);
        File.WriteAllText(file, Doc("recovered").ToJson()); Assert.True(resolver.Load("b.bridge.json").Success);
    }
    [Fact] public void DirectoryLinksCannotEscapeRoot()
    {
        string parent = Path.Combine(AppContext.BaseDirectory, "work", Guid.NewGuid().ToString("N"));
        string root = Path.Combine(parent, "root"), target = Path.Combine(parent, "outside"), link = Path.Combine(root, "link");
        Directory.CreateDirectory(root); Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "secret.bridge.json"), Doc("outside").ToJson());
        if (OperatingSystem.IsWindows())
        {
            using var process = Process.Start(new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false, CreateNoWindow = true,
                ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", $"New-Item -ItemType Junction -Path '{link}' -Target '{target}' -ErrorAction Stop | Out-Null" }
            })!;
            process.WaitForExit(); Assert.Equal(0, process.ExitCode);
        }
        else Directory.CreateSymbolicLink(link, target);
        Assert.Equal("REFERENCE_PATH", new FileBridgeDocumentResolver(root).Load("link/secret.bridge.json").Diagnostics[0].Code);
        Assert.Equal("REFERENCE_PATH", new FileBridgeDocumentResolver(link).Load("secret.bridge.json").Diagnostics[0].Code);
    }
}
