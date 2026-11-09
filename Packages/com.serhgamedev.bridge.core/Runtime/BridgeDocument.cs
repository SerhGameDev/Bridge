#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public sealed partial class BridgeBlock
    {
        private readonly JObject definition;
        private bool removed;
        internal JObject Definition => !removed ? definition : throw InternalJson.Error("BLOCK_REMOVED", $"Block '{Id}' has been removed.", SourceContext, "/blocks/" + Id.Replace("~", "~0").Replace("/", "~1"), DocumentIdContext);
        internal void MarkRemoved() => removed = true;
        public string Id { get; }
        public string Type => (string)Definition["type"]!;
        public int SchemaVersion { get; }
        public BridgeValue Data => new BridgeValue(Definition["data"]!);
        internal BridgeBlock(string id, JObject definition, int schemaVersion)
        { Id = id; this.definition = definition; SchemaVersion = schemaVersion; }
    }

    public sealed partial class BridgeDocument
    {
        internal JObject Root { get; }
        internal readonly Dictionary<string, BridgeBlock> blocks = new Dictionary<string, BridgeBlock>(StringComparer.Ordinal);
        public int BridgeVersion => 1;
        public string DocumentId { get; }
        public string? Source { get; }
        public BridgeSerializer Serializers { get; } = new BridgeSerializer();
        private IReadOnlyList<BridgeBlock>? sortedBlocks;
        public IReadOnlyList<BridgeBlock> Blocks => sortedBlocks ??= Array.AsReadOnly(blocks.Values.OrderBy(b => b.Id, StringComparer.Ordinal).ToArray());
        private BridgeDocument(JObject root, string? source, BridgeReadOptions options)
        {
            Root = root; Source = source;
            if (!InternalJson.IsInteger(root["bridgeVersion"], out int version)) throw Invalid("bridgeVersion must be integer 1.", "bridgeVersion");
            if (version != 1) throw InternalJson.Error("FORMAT_VERSION", $"Unsupported bridgeVersion {version}.", source, "bridgeVersion");
            DocumentId = RequiredString(root["documentId"], "documentId");
            if (!(root["blocks"] is JObject definitions)) throw Invalid("blocks must be an object.", "blocks");
            if (definitions.Count > options.MaxBlocks) throw InternalJson.Error("INPUT_LIMIT", "Document exceeds MaxBlocks.", source, "blocks", DocumentId);
            foreach (var entry in definitions.Properties())
            {
                if (string.IsNullOrWhiteSpace(entry.Name)) throw Invalid("Block ID must not be empty or whitespace.", entry.Path);
                if (!(entry.Value is JObject definition)) throw Invalid("Block definition must be an object.", entry.Path);
                RequiredString(definition["type"], entry.Path + ".type");
                if (!InternalJson.IsInteger(definition["schemaVersion"], out int schemaVersion) || schemaVersion < 1)
                    throw Invalid("schemaVersion must be a positive integer.", entry.Path + ".schemaVersion");
                if (definition.Property("data", StringComparison.Ordinal) == null) throw Invalid("data is required (null is allowed).", entry.Path + ".data");
                if (definition.Property("refs", StringComparison.Ordinal) != null)
                {
                    if (!(definition["refs"] is JObject refs)) throw Invalid("refs must be an object.", entry.Path + ".refs");
                    foreach (var reference in refs.Properties())
                    {
                        if (string.IsNullOrWhiteSpace(reference.Name) || !(reference.Value is JObject target)) throw Invalid("Reference must have a nonempty name and object value.", reference.Path);
                        if (target["document"]?.Type != JTokenType.String) throw Invalid("Reference document must be a string.", reference.Path + ".document");
                        RequiredString(target["block"], reference.Path + ".block");
                    }
                }
                var block = new BridgeBlock(entry.Name, definition, schemaVersion);
                block.Serializers = Serializers;
                block.DocumentIdContext = DocumentId;
                block.SourceContext = Source;
                blocks.Add(entry.Name, block);
            }
        }
        private string RequiredString(JToken? value, string path)
        {
            if (value?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)value)) throw Invalid("Expected a nonempty string.", path);
            return ((string)value)!;
        }
        private BridgeException Invalid(string message, string path) => InternalJson.Error("FORMAT_SCHEMA", message, Source, path, DocumentId);
        public static BridgeDocument Parse(string json, BridgeReadOptions? options = null, string? source = null)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            options ??= new BridgeReadOptions();
            var token = InternalJson.Parse(json, options, source);
            if (!(token is JObject root)) throw InternalJson.Error("FORMAT_SCHEMA", "Document root must be an object.", source);
            return new BridgeDocument(root, source, options);
        }
        public static BridgeResult<BridgeDocument> TryParse(string json, BridgeReadOptions? options = null, string? source = null)
        {
            try { return BridgeResult<BridgeDocument>.Ok(Parse(json, options, source)); }
            catch (BridgeException ex) { return BridgeResult<BridgeDocument>.Fail(ex); }
        }
        public static BridgeDocument Load(Stream stream, BridgeReadOptions? options = null, string? source = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            options ??= new BridgeReadOptions(); options.Validate();
            try
            {
                using var buffer = new MemoryStream();
                var chunk = new byte[4096]; int count;
                while ((count = stream.Read(chunk, 0, (int)Math.Min(chunk.Length, (long)options.MaxUtf8Bytes + 1 - buffer.Length))) > 0)
                {
                    buffer.Write(chunk, 0, count);
                    if (buffer.Length > options.MaxUtf8Bytes) throw InternalJson.Error("INPUT_LIMIT", "Stream exceeds MaxUtf8Bytes.", source);
                }
                var bytes = buffer.ToArray();
                int offset = bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191 ? 3 : 0;
                return Parse(InternalJson.Utf8.GetString(bytes, offset, bytes.Length - offset), options, source);
            }
            catch (DecoderFallbackException ex) { throw InternalJson.Error("JSON_ENCODING", ex.Message, source); }
            catch (IOException ex) { throw InternalJson.Error("DOCUMENT_IO", ex.Message, source); }
        }
        public static BridgeResult<BridgeDocument> TryLoad(Stream stream, BridgeReadOptions? options = null, string? source = null)
        {
            try { return BridgeResult<BridgeDocument>.Ok(Load(stream, options, source)); }
            catch (BridgeException ex) { return BridgeResult<BridgeDocument>.Fail(ex); }
        }
    }
}
