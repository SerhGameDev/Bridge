#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public sealed class BridgeQuerySpec
    {
        public string? Type => Types?.Count == 1 ? Types[0] : null;
        public IReadOnlyList<string>? Types { get; }
        public string? Id { get; }
        public IReadOnlyList<string>? Fields { get; }
        public int? Limit { get; }
        public BridgeQuerySpec(string? type = null, string? id = null, IEnumerable<string>? fields = null, int? limit = null)
            : this(type == null ? null : new[] { type }, id, fields, limit, true) { }
        private BridgeQuerySpec(IEnumerable<string>? types, string? id, IEnumerable<string>? fields, int? limit, bool _)
        {
            if (id != null && string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID filter must be nonempty.");
            if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
            if (types != null)
            {
                var names = types.ToArray();
                if (names.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Types must contain nonempty strings.", nameof(types));
                Types = Array.AsReadOnly(names.Distinct(StringComparer.Ordinal).ToArray());
            }
            Id = id; Limit = limit;
            if (fields != null)
            {
                var names = fields.ToArray();
                if (names.Any(n => n == null)) throw new ArgumentException("Fields must not contain null.");
                Fields = Array.AsReadOnly(names.Distinct(StringComparer.Ordinal).ToArray());
            }
        }
        public static BridgeQuerySpec ForTypes(IEnumerable<string> types, string? id = null, IEnumerable<string>? fields = null, int? limit = null)
            => Build(types ?? throw new ArgumentNullException(nameof(types)), id, fields, limit);
        internal static BridgeQuerySpec Build(IEnumerable<string>? types, string? id, IEnumerable<string>? fields, int? limit)
            => new BridgeQuerySpec(types, id, fields, limit, true);
        public string ToJson()
        {
            bool multiple = Types != null && Types.Count != 1;
            var json = new JObject { ["queryVersion"] = new JRaw(multiple ? "2" : "1") };
            if (multiple) json["types"] = new JArray(Types!);
            else if (Type != null) json["type"] = Type;
            if (Id != null) json["id"] = Id;
            if (Fields != null) json["fields"] = new JArray(Fields);
            if (Limit != null) json["limit"] = new JRaw(Limit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return BridgeDocument.WriteJson(json);
        }
        public static BridgeQuerySpec FromJson(string json, BridgeReadOptions? options = null)
        {
            var token = InternalJson.Parse(json ?? throw new ArgumentNullException(nameof(json)), options ?? new BridgeReadOptions(), null);
            BridgeException Invalid() => InternalJson.Error("QUERY_SPEC", "Expected queryVersion 1 (type) or 2 (types) and only declared operations.");
            if (!(token is JObject obj) || !InternalJson.IsInteger(obj["queryVersion"], out int version) || (version != 1 && version != 2)) throw Invalid();
            string typeKey = version == 1 ? "type" : "types";
            if (obj.Properties().Any(p => !new[] { "queryVersion", typeKey, "id", "fields", "limit" }.Contains(p.Name))) throw Invalid();
            string[]? types = null;
            if (version == 2)
            {
                if (!(obj["types"] is JArray values) || values.Any(v => v.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)v))) throw Invalid();
                types = values.Select(v => ((string)v)!).ToArray();
            }
            foreach (string name in new[] { "type", "id" })
                if (obj.Property(name) != null && (obj[name]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)obj[name]))) throw Invalid();
            string[]? fields = null;
            if (obj.Property("fields") != null)
            {
                if (!(obj["fields"] is JArray arr) || arr.Any(v => v.Type != JTokenType.String)) throw Invalid();
                fields = arr.Select(v => ((string)v)!).ToArray();
            }
            int? limit = null;
            if (obj.Property("limit") != null)
            {
                if (!InternalJson.IsInteger(obj["limit"], out int count) || count < 0) throw Invalid();
                limit = count;
            }
            return version == 1 ? new BridgeQuerySpec((string?)obj["type"], (string?)obj["id"], fields, limit)
                : ForTypes(types!, (string?)obj["id"], fields, limit);
        }
    }

    public sealed class BridgeQuery
    {
        private readonly BridgeDocument document;
        public BridgeQuerySpec Spec { get; }
        internal BridgeQuery(BridgeDocument document, BridgeQuerySpec spec) { this.document = document; Spec = spec; }
        public BridgeQuery WhereType(string type) => WhereTypes(type ?? throw new ArgumentNullException(nameof(type)));
        public BridgeQuery WhereTypes(params string[] types) => new BridgeQuery(document, BridgeQuerySpec.ForTypes(types, Spec.Id, Spec.Fields, Spec.Limit));
        public BridgeQuery WhereId(string id) => new BridgeQuery(document, BridgeQuerySpec.Build(Spec.Types, id, Spec.Fields, Spec.Limit));
        public BridgeQuery SelectFields(params string[] fields) => new BridgeQuery(document, BridgeQuerySpec.Build(Spec.Types, Spec.Id, fields ?? throw new ArgumentNullException(nameof(fields)), Spec.Limit));
        public BridgeQuery Limit(int limit) => new BridgeQuery(document, BridgeQuerySpec.Build(Spec.Types, Spec.Id, Spec.Fields, limit));
        public BridgeSelection Execute()
        {
            var selected = document.Blocks.Where(b => (Spec.Types == null || Spec.Types.Contains(b.Type, StringComparer.Ordinal)) && (Spec.Id == null || b.Id == Spec.Id));
            if (Spec.Limit.HasValue) selected = selected.Take(Spec.Limit.Value);
            var results = new List<BridgeProjection>();
            foreach (var block in selected)
            {
                var data = block.Data;
                if (Spec.Fields != null)
                {
                    if (data.Kind != BridgeValueKind.Object) throw InternalJson.Error("QUERY_DATA_KIND", $"SelectFields requires object data in block '{block.Id}'.", document.Source, documentId: document.DocumentId);
                    var fields = new Dictionary<string, BridgeValue>(StringComparer.Ordinal);
                    foreach (var name in Spec.Fields)
                    {
                        if (!data.TryGetProperty(name, out var value)) throw InternalJson.Error("QUERY_FIELD_NOT_FOUND", $"Field '{name}' not found in block '{block.Id}'.", document.Source, name, document.DocumentId);
                        fields.Add(name, value!);
                    }
                    data = BridgeValue.Object(fields);
                }
                results.Add(new BridgeProjection(block.Id, block.Type, block.SchemaVersion, data));
            }
            return new BridgeSelection(document.DocumentId, results);
        }
        public BridgeResult<BridgeSelection> TryExecute()
        {
            try { return BridgeResult<BridgeSelection>.Ok(Execute()); }
            catch (BridgeException ex) { return BridgeResult<BridgeSelection>.Fail(ex); }
        }
    }

    public sealed class BridgeProjection
    {
        public string Id { get; }
        public string Type { get; }
        public int SchemaVersion { get; }
        public BridgeValue Data { get; }
        internal BridgeProjection(string id, string type, int schemaVersion, BridgeValue data)
        { Id = id; Type = type; SchemaVersion = schemaVersion; Data = new BridgeValue(data.Token); }
    }
    public sealed class BridgeSelection
    {
        public string DocumentId { get; }
        public IReadOnlyList<BridgeProjection> Blocks { get; }
        internal BridgeSelection(string documentId, IEnumerable<BridgeProjection> blocks)
        { DocumentId = documentId; Blocks = Array.AsReadOnly(blocks.ToArray()); }
        public string ToJson(bool indented = false)
        {
            var projected = BridgeDocument.Create(DocumentId);
            foreach (var block in Blocks) projected.AddBlock(block.Id, block.Type, block.Data, block.SchemaVersion);
            return projected.ToJson(indented);
        }
    }
    public sealed partial class BridgeDocument
    {
        public BridgeQuery Query(BridgeQuerySpec? spec = null) => new BridgeQuery(this, spec ?? new BridgeQuerySpec());
    }
}
