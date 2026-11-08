using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;

public class AcceptanceTests
{
    [Theory]
    [InlineData("{\"a\":1,\"\\u0061\":2}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\",\"blocks\":{\"a\":{},\"a\":{}}}")]
    public void DecodedDuplicateKeysAreRejected(string json)
        => Assert.Equal("JSON_DUPLICATE_KEY", BridgeDocument.TryParse(json).Diagnostics[0].Code);

    [Theory]
    [InlineData("{\"bridgeVersion\":1,\"blocks\":{}}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\"}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":3,\"blocks\":{}}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\",\"blocks\":{\"\":{}}}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\",\"blocks\":{\"a\":null}}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\",\"blocks\":{\"a\":{\"type\":\"\",\"schemaVersion\":1,\"data\":null}}}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\",\"blocks\":{\"a\":{\"type\":\"t\",\"schemaVersion\":0,\"data\":null}}}")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"d\",\"blocks\":{\"a\":{\"type\":\"t\",\"schemaVersion\":1,\"data\":null,\"refs\":[]}}}")]
    public void RequiredSchemaIsComplete(string json) => Assert.Equal("FORMAT_SCHEMA", BridgeDocument.TryParse(json).Diagnostics[0].Code);

    [Fact] public void EmptyDocumentBomAndNumberLimit()
    {
        var empty = BridgeDocument.Create("empty"); Assert.Empty(BridgeDocument.Parse(empty.ToJson()).Blocks);
        byte[] bytes = new byte[] { 239, 187, 191 }.Concat(Encoding.UTF8.GetBytes(empty.ToJson())).ToArray();
        using var stream = new MemoryStream(bytes); Assert.Empty(BridgeDocument.Load(stream).Blocks);
        Assert.Equal("INPUT_LIMIT", Assert.Throws<BridgeException>(() => BridgeValue.Parse("1234", new BridgeReadOptions { MaxNumberCharacters = 3 })).Diagnostics[0].Code);
    }
    [Fact] public void UnknownFieldsIncludingReferenceExtensionsSurvive()
    {
        const string json = "{\"bridgeVersion\":1,\"documentId\":\"d\",\"custom\":{\"x\":1e1000},\"blocks\":{\"a\":{\"type\":\"t\",\"schemaVersion\":1,\"data\":{\"$ref\":\"arbitrary-data\",\"$type\":\"NeverActivated\"},\"extension\":[1,2],\"refs\":{\"local\":{\"document\":\"\",\"block\":\"a\",\"future\":true}}}}}";
        var document = BridgeDocument.Parse(json);
        Assert.Equal("arbitrary-data", document.GetBlock("a").Get<string>("$ref"));
        Assert.Equal("NeverActivated", document.GetBlock("a").Get<string>("$type"));
        Assert.Contains("\"future\":true", document.ToJson()); Assert.Contains("1e1000", document.ToJson());
        Assert.Equal(document.ToJson(), BridgeDocument.Parse(document.ToJson()).ToJson());
        Assert.DoesNotContain("future", document.Query().Execute().ToJson());
    }
    [Fact] public void UnpairedSurrogatesAreRejectedInsteadOfSilentlyReplaced()
    {
        Assert.Throws<BridgeException>(() => BridgeValue.Parse("\"\\uD800\""));
        Assert.Throws<BridgeException>(() => BridgeValue.Parse("\"\\uDC00\""));
        Assert.Throws<BridgeException>(() => BridgeValue.From("\ud800"));
        Assert.Equal("🚀", BridgeValue.Parse("\"\\uD83D\\uDE80\"").As<string>());
    }
    [Fact] public void PrimitiveAndDtoDiagnosticsIncludeKnownDocumentContext()
    {
        var doc = BridgeDocument.Parse(DocumentTests.Json, source: "source.bridge.json");
        var diagnostic = Assert.Throws<BridgeException>(() => doc.GetBlock("a").Get<int>("missing/key")).Diagnostics[0];
        Assert.Equal("демо", diagnostic.DocumentId); Assert.Equal("source.bridge.json", diagnostic.Source);
        Assert.Equal("/blocks/a/data/missing~1key", diagnostic.Path); Assert.Null(diagnostic.Line);
        Assert.Equal("DTO_CONVERTER_MISSING", Assert.Throws<BridgeException>(() => doc.GetBlock("a").Deserialize<Unregistered>()).Diagnostics[0].Code);
        Assert.Equal("source.bridge.json", Assert.Throws<BridgeException>(() => doc.SetBlockData("a", new Unregistered())).Diagnostics[0].Source);
    }
    private sealed class Unregistered { }

    [Fact] public void IndependentJsonParserAcceptsGeneratedRoundTrips()
    {
        var random = new Random(6281);
        for (int i = 0; i < 100; i++)
        {
            var input = new Dictionary<string, object?>
            {
                ["a.b/" + i] = new object?[] { random.NextInt64(), null, true, new[] { random.NextDouble(), -random.NextDouble() } },
                ["text"] = "Unicode ✓ 🚀 \n\t\"\\ " + random.Next(),
                [""] = new Dictionary<string, int> { ["nested"] = i }
            };
            string json = JsonSerializer.Serialize(input);
            string output = BridgeValue.Parse(json).ToJson();
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(output)));
        }
    }
    [Theory]
    [InlineData("true false")][InlineData("[1 2]")][InlineData("{\"x\" 1}")]
    [InlineData("{\"x\":}")][InlineData("[1e+]")][InlineData("[--1]")][InlineData("[1\u000b]")]
    public void IndependentStrictParserAgreesOnInvalidSyntax(string json)
    {
        Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(json));
        Assert.Throws<BridgeException>(() => BridgeValue.Parse(json));
    }
    [Theory]
    [InlineData("null")][InlineData("[]")][InlineData("true")][InlineData("1")][InlineData("\"text\"")]
    public void FieldProjectionRejectsEveryNonObjectKind(string json)
    {
        var doc = BridgeDocument.Create("d"); doc.AddBlock("a", "t", BridgeValue.Parse(json));
        Assert.Equal("QUERY_DATA_KIND", doc.Query().SelectFields("x").TryExecute().Diagnostics[0].Code);
    }
    [Fact] public void CoreReferencesOnlyDotnetAndJsonAssemblies()
    {
        var forbidden = new[] { "UnityEngine", "UnityEditor", "Bridge.Runtime", "Mcp" };
        Assert.DoesNotContain(typeof(BridgeDocument).Assembly.GetReferencedAssemblies(), a => forbidden.Any(prefix => a.Name!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }
}
