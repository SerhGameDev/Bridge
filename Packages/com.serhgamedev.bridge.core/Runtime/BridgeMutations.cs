#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public sealed partial class BridgeBlock
    {
        internal BridgeSerializer Serializers { get; set; } = new BridgeSerializer();
        internal string? DocumentIdContext { get; set; }
        internal string? SourceContext { get; set; }
        private T WithContext<T>(Func<T> action, string? field = null)
        {
            try { return action(); }
            catch (BridgeException ex)
            {
                string Escape(string name) => name.Replace("~", "~0").Replace("/", "~1");
                string path = "/blocks/" + Escape(Id) + "/data" + (field == null ? "" : "/" + Escape(field));
                throw new BridgeException(ex.Diagnostics.Select(d => new BridgeDiagnostic(d.Code, d.Message,
                    d.DocumentId ?? DocumentIdContext, d.Source ?? SourceContext, path, d.Line, d.Column, d.Severity)));
            }
        }
        public T Get<T>(string field) => WithContext(() => Serializers.Deserialize<T>(Data.GetProperty(field)), field);
        public void Set<T>(string field, T value) => WithContext(() => Definition["data"] = Data.WithField(field, Serializers.Serialize(value)).Token.DeepClone(), field);
        public T Deserialize<T>() => WithContext(() => Serializers.Deserialize<T>(Data));
        internal void SetData<T>(T data) => WithContext(() => Definition["data"] = Serializers.Serialize(data).Token.DeepClone());
    }
    public sealed partial class BridgeDocument
    {
        public static BridgeDocument Create(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentException("Document ID must be nonempty.", nameof(documentId));
            var root = new JObject { ["bridgeVersion"] = new JRaw("1"), ["documentId"] = documentId, ["blocks"] = new JObject() };
            return new BridgeDocument(root, null, new BridgeReadOptions());
        }
        public bool TryGetBlock(string id, out BridgeBlock? block)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return blocks.TryGetValue(id, out block);
        }
        public BridgeBlock GetBlock(string id) => TryGetBlock(id, out var block) ? block!
            : throw InternalJson.Error("BLOCK_NOT_FOUND", $"Block '{id}' not found.", Source, "blocks", DocumentId);
        public BridgeBlock AddBlock(string id, string type, BridgeValue data, int schemaVersion = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Block ID and type must be nonempty.");
            if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (blocks.ContainsKey(id)) throw InternalJson.Error("BLOCK_DUPLICATE", $"Block '{id}' already exists.", Source, "blocks", DocumentId);
            var definition = new JObject { ["type"] = type, ["schemaVersion"] = new JRaw(schemaVersion.ToString(CultureInfo.InvariantCulture)), ["data"] = data.Token.DeepClone() };
            ((JObject)Root["blocks"]!).Add(id, definition);
            var block = new BridgeBlock(id, definition, schemaVersion) { Serializers = Serializers, DocumentIdContext = DocumentId, SourceContext = Source };
            blocks.Add(id, block); sortedBlocks = null; return block;
        }
        public bool RemoveBlock(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (!blocks.TryGetValue(id, out var block)) return false;
            block.MarkRemoved(); blocks.Remove(id); sortedBlocks = null;
            ((JObject)Root["blocks"]!).Remove(id); return true;
        }
        public void SetBlockData<T>(string id, T data) => GetBlock(id).SetData(data);
        public string ToJson(bool indented = false) => WriteJson(Root, indented);
        internal static string WriteJson(JToken value, bool indented = false)
        {
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            using var writer = new JsonTextWriter(text) { Formatting = indented ? Formatting.Indented : Formatting.None, Culture = CultureInfo.InvariantCulture, StringEscapeHandling = StringEscapeHandling.EscapeNonAscii };
            Sort(value).WriteTo(writer); writer.Flush(); return text.ToString();
        }
        private static JToken Sort(JToken token)
        {
            if (token is JObject obj) return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p =>
            { InternalJson.EnsureUnicode(p.Name); return new JProperty(p.Name, Sort(p.Value)); }));
            if (token is JArray array) return new JArray(array.Select(Sort));
            if (token.Type == JTokenType.String) InternalJson.EnsureUnicode(((string)token)!);
            return token.DeepClone();
        }
        public void Save(Stream destination, bool indented = false)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            try { var bytes = InternalJson.Utf8.GetBytes(ToJson(indented)); destination.Write(bytes, 0, bytes.Length); }
            catch (IOException ex) { throw InternalJson.Error("DOCUMENT_IO", ex.Message, Source, documentId: DocumentId); }
        }
    }
}
