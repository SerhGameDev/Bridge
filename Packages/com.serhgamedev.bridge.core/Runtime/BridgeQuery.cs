#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public sealed class BridgeQuerySpec
    {
        public string? Type { get; }
        public string? Id { get; }
        public IReadOnlyList<string>? Fields { get; }
        public int? Limit { get; }
        public BridgeQuerySpec(string? type = null, string? id = null, IEnumerable<string>? fields = null, int? limit = null)
        {
            if (type != null && string.IsNullOrWhiteSpace(type) || id != null && string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Type and ID filters must be nonempty.");
            if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
            Type = type; Id = id; Limit = limit;
            if (fields != null)
            {
                var names = fields.ToArray();
                if (names.Any(n => n == null)) throw new ArgumentException("Fields must not contain null.");
                Fields = Array.AsReadOnly(names.Distinct(StringComparer.Ordinal).ToArray());
            }
        }
        public string ToJson()
        {
            var json = new JObject { ["queryVersion"] = new JRaw("1") };
            if (Type != null) json["type"] = Type;
            if (Id != null) json["id"] = Id;
            if (Fields != null) json["fields"] = new JArray(Fields);
            if (Limit != null) json["limit"] = new JRaw(Limit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return BridgeDocument.WriteJson(json);
        }
        public static BridgeQuerySpec FromJson(string json, BridgeReadOptions? options = null)
        {
            var token = InternalJson.Parse(json ?? throw new ArgumentNullException(nameof(json)), options ?? new BridgeReadOptions(), null);
            BridgeException Invalid() => InternalJson.Error("QUERY_SPEC", "Expected queryVersion 1 and only type/id/fields/limit operations.");
            if (!(token is JObject obj) || obj.Properties().Any(p => !new[] { "queryVersion", "type", "id", "fields", "limit" }.Contains(p.Name))
                || !InternalJson.IsInteger(obj["queryVersion"], out int version) || version != 1) throw Invalid();
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
            return new BridgeQuerySpec((string?)obj["type"], (string?)obj["id"], fields, limit);
        }
    }

    public sealed class BridgeQuery
    {
        private readonly BridgeDocument document;
        public BridgeQuerySpec Spec { get; }
        internal BridgeQuery(BridgeDocument document, BridgeQuerySpec spec) { this.document = document; Spec = spec; }
        public BridgeQuery WhereType(string type) => new BridgeQuery(document, new BridgeQuerySpec(type, Spec.Id, Spec.Fields, Spec.Limit));
        public BridgeQuery WhereId(string id) => new BridgeQuery(document, new BridgeQuerySpec(Spec.Type, id, Spec.Fields, Spec.Limit));
        public BridgeQuery SelectFields(params string[] fields) => new BridgeQuery(document, new BridgeQuerySpec(Spec.Type, Spec.Id, fields ?? throw new ArgumentNullException(nameof(fields)), Spec.Limit));
        public BridgeQuery Limit(int limit) => new BridgeQuery(document, new BridgeQuerySpec(Spec.Type, Spec.Id, Spec.Fields, limit));
        public BridgeSelection Execute()
        {
            var selected = document.Blocks.Where(b => (Spec.Type == null || b.Type == Spec.Type) && (Spec.Id == null || b.Id == Spec.Id));
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
