using System;
using System.IO;
using System.Text;
using Bridge.Core;
using Xunit;

namespace Bridge.Core.Tests;

public class DocumentTests
{
    internal const string Json = "{\"bridgeVersion\":1,\"documentId\":\"демо\",\"blocks\":{\"a\":{\"type\":\"settings\",\"schemaVersion\":1,\"data\":{\"speed\":1.25}}}}";

    [Fact] public void ParseAndStreamHaveSameModelAndLeaveStreamOpen()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json));
        var doc = BridgeDocument.Load(stream, source: "input.bridge.json");
        Assert.True(stream.CanRead); Assert.Equal("демо", doc.DocumentId);
        Assert.Equal("input.bridge.json", doc.Source); Assert.Single(doc.Blocks);
        Assert.Equal("a", doc.Blocks[0].Id); Assert.Equal(BridgeValueKind.Object, doc.Blocks[0].Data.Kind);
    }

    [Theory]
    [InlineData("", "JSON_SYNTAX")]
    [InlineData("{} {}", "JSON_SYNTAX")]
    [InlineData("{\"x\":1,\"x\":2}", "JSON_DUPLICATE_KEY")]
    [InlineData("{\"x\":{\"a\":1,\"a\":2}}", "JSON_DUPLICATE_KEY")]
    [InlineData("[]", "FORMAT_SCHEMA")]
    [InlineData("null", "FORMAT_SCHEMA")]
    [InlineData("{}", "FORMAT_SCHEMA")]
    [InlineData("{\"bridgeVersion\":2}", "FORMAT_VERSION")]
    [InlineData("{\"bridgeVersion\":1.0}", "FORMAT_SCHEMA")]
    [InlineData("{\"bridgeVersion\":true}", "FORMAT_SCHEMA")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\"x\",\"blocks\":[]}", "FORMAT_SCHEMA")]
    [InlineData("{\"bridgeVersion\":1,\"documentId\":\" \u0020\",\"blocks\":{}}", "FORMAT_SCHEMA")]
    public void InvalidInputsReturnNoPartialDocument(string json, string code)
    {
        var result = BridgeDocument.TryParse(json);
        Assert.False(result.Success); Assert.Equal(code, result.Diagnostics[0].Code);
        Assert.Throws<BridgeException>(() => result.Value);
        Assert.Throws<BridgeException>(() => BridgeDocument.Parse(json));
    }

    [Theory]
    [InlineData("{'x':1}")][InlineData("{x:1}")][InlineData("{\"x\":1,}")]
    [InlineData("[1,]")][InlineData("[/*comment*/1]")][InlineData("[NaN]")]
    [InlineData("[Infinity]")][InlineData("[undefined]")][InlineData("[0x10]")]
    [InlineData("[01]")][InlineData("[.5]")][InlineData("[+1]")][InlineData("[1.]")]
    [InlineData("[1e]")][InlineData("[truefalse]")][InlineData("[\u00a0]")]
    public void RejectJavaScriptExtensions(string json) => Assert.Throws<BridgeException>(() => BridgeValue.Parse(json));

    [Theory]
    [InlineData("type")][InlineData("schemaVersion")][InlineData("data")]
    public void MissingBlockFieldRejected(string field)
    {
        var token = Newtonsoft.Json.Linq.JObject.Parse(Json);
        ((Newtonsoft.Json.Linq.JObject)token["blocks"]!["a"]!).Remove(field);
        Assert.False(BridgeDocument.TryParse(token.ToString()).Success);
    }

    [Fact] public void LimitsAndEncodingAreBounded()
    {
        Assert.Equal("INPUT_LIMIT", BridgeDocument.TryParse(Json, new BridgeReadOptions { MaxUtf8Bytes = 20 }).Diagnostics[0].Code);
        Assert.Equal("INPUT_LIMIT", BridgeDocument.TryParse(Json, new BridgeReadOptions { MaxBlocks = 0 }).Diagnostics[0].Code);
        Assert.Equal("INPUT_LIMIT", BridgeDocument.TryParse(Json, new BridgeReadOptions { MaxDepth = 2 }).Diagnostics[0].Code);
        using var input = new MemoryStream(new byte[] { 255, 254, 0 });
        Assert.Equal("JSON_ENCODING", BridgeDocument.TryLoad(input).Diagnostics[0].Code);
        using var large = new MemoryStream(Encoding.UTF8.GetBytes(Json));
        Assert.Equal("INPUT_LIMIT", BridgeDocument.TryLoad(large, new BridgeReadOptions { MaxUtf8Bytes = 5 }).Diagnostics[0].Code);
    }

    [Theory]
    [InlineData("1234567890123456789012345678901234567890")]
    [InlineData("-1.23456789012345678901234567890123456789e1000")]
    [InlineData("1e-1000")][InlineData("-0")]
    public void NumericLexemesNeverRoundOrOverflow(string number) => Assert.Equal(number, BridgeValue.Parse(number).ToJson());

    [Fact] public void JsonDiagnosticsHaveRealCoordinates()
    {
        var result = BridgeDocument.TryParse("{\n  BAD\n}", source: "bad.bridge.json");
        Assert.Equal(2, result.Diagnostics[0].Line); Assert.Equal(3, result.Diagnostics[0].Column);
        Assert.Equal("bad.bridge.json", result.Diagnostics[0].Source);
        Assert.Throws<ArgumentNullException>(() => BridgeDocument.Parse(null!));
    }
}
