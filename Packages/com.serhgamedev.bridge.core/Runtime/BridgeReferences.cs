#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public sealed class BridgeReference
    {
        public string Document { get; }
        public string Block { get; }
        public BridgeReference(string document, string block)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Block = !string.IsNullOrWhiteSpace(block) ? block : throw new ArgumentException("Target block must be nonempty.", nameof(block));
        }
    }
    public interface IBridgeDocumentResolver
    {
        /// <summary>Comparison of normalized document paths, independent of ordinal block IDs.</summary>
        StringComparer PathComparer { get; }
        BridgeResult<BridgeDocument> Load(string documentPath);
    }
    public sealed class BridgeResolutionOptions
    {
        public int MaxDepth { get; set; } = 32;
        public int MaxDocuments { get; set; } = 64;
        public long MaxTotalUtf8Bytes { get; set; } = 64 * 1024 * 1024;
        public int MaxVisitedEdges { get; set; } = 100000;
        internal void Validate()
        {
            if (MaxDepth < 1 || MaxDocuments < 1 || MaxTotalUtf8Bytes < 1 || MaxVisitedEdges < 1) throw new ArgumentOutOfRangeException(nameof(BridgeResolutionOptions));
        }
    }
    public sealed class BridgeResolvedBlock
    {
        public BridgeDocument Document { get; }
        public BridgeBlock Block { get; }
        public string DocumentPath { get; }
        internal BridgeResolvedBlock(BridgeDocument document, BridgeBlock block, string path) { Document = document; Block = block; DocumentPath = path; }
    }
    public sealed partial class BridgeBlock
    {
        public IReadOnlyDictionary<string, BridgeReference> References
        {
            get
            {
                var result = new Dictionary<string, BridgeReference>(StringComparer.Ordinal);
                if (Definition["refs"] is JObject refs)
                    foreach (var p in refs.Properties()) result.Add(p.Name, new BridgeReference((string)p.Value["document"]!, (string)p.Value["block"]!));
                return new ReadOnlyDictionary<string, BridgeReference>(result);
            }
        }
        public void SetReference(string name, BridgeReference reference)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Reference name must be nonempty.", nameof(name));
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (!(Definition["refs"] is JObject refs)) { refs = new JObject(); Definition["refs"] = refs; }
            refs[name] = new JObject { ["document"] = reference.Document, ["block"] = reference.Block };
        }
    }
    public sealed partial class BridgeDocument
    {
        /// <summary>Explicitly resolves one reference and validates the reachable block graph.</summary>
        public BridgeResult<BridgeResolvedBlock> ResolveReference(string blockId, string referenceName,
            IBridgeDocumentResolver resolver, string sourcePath, BridgeResolutionOptions? options = null)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (referenceName == null) throw new ArgumentNullException(nameof(referenceName));
            options ??= new BridgeResolutionOptions(); options.Validate();
            try
            {
                string path = BridgePaths.Normalize(sourcePath);
                var sourceBlock = GetBlock(blockId);
                if (!sourceBlock.References.TryGetValue(referenceName, out var reference)) throw InternalJson.Error("REFERENCE_NOT_FOUND", $"Reference '{referenceName}' not found.", sourcePath, documentId: DocumentId);
                return BridgeResult<BridgeResolvedBlock>.Ok(new Resolution(this, path, resolver, options).Resolve(sourceBlock, reference));
            }
            catch (BridgeException ex) { return BridgeResult<BridgeResolvedBlock>.Fail(ex); }
        }
        private sealed class Resolution
        {
            private readonly IBridgeDocumentResolver resolver;
            private readonly BridgeResolutionOptions limits;
            private readonly Dictionary<string, BridgeDocument> cache;
            private readonly Dictionary<string, HashSet<string>> active;
            private readonly Dictionary<string, HashSet<string>> visited;
            private readonly string sourcePath;
            private long bytes;
            private int edges;
            internal Resolution(BridgeDocument source, string path, IBridgeDocumentResolver resolver, BridgeResolutionOptions limits)
            {
                this.resolver = resolver; this.limits = limits; sourcePath = path;
                cache = new Dictionary<string, BridgeDocument>(resolver.PathComparer);
                active = new Dictionary<string, HashSet<string>>(resolver.PathComparer);
                visited = new Dictionary<string, HashSet<string>>(resolver.PathComparer);
                Cache(path, source);
            }
            internal BridgeResolvedBlock Resolve(BridgeBlock source, BridgeReference reference)
            {
                Mark(active, sourcePath, source.Id);
                return Visit(sourcePath, reference, 1);
            }
            private BridgeResolvedBlock Visit(string from, BridgeReference reference, int depth)
            {
                if (++edges > limits.MaxVisitedEdges) throw InternalJson.Error("REFERENCE_EDGE_LIMIT", "Reference graph exceeds MaxVisitedEdges.", from);
                if (depth > limits.MaxDepth) throw InternalJson.Error("REFERENCE_DEPTH", "Reference graph exceeds MaxDepth.", from);
                string path = reference.Document.Length == 0 ? from : BridgePaths.Normalize(reference.Document, from);
                if (!cache.TryGetValue(path, out var document))
                {
                    if (cache.Count >= limits.MaxDocuments) throw InternalJson.Error("REFERENCE_DOCUMENT_LIMIT", "Reference graph exceeds MaxDocuments.", path);
                    document = resolver.Load(path).Value; Cache(path, document);
                }
                if (!document.TryGetBlock(reference.Block, out var block)) throw InternalJson.Error("REFERENCE_BLOCK_NOT_FOUND", $"Block '{reference.Block}' not found.", path, documentId: document.DocumentId);
                if (Has(active, path, block!.Id)) throw InternalJson.Error("REFERENCE_CYCLE", "Cycle in reachable block references.", path, documentId: document.DocumentId);
                // Revisit a DAG node along each path so a deeper path cannot bypass MaxDepth.
                // Completed leaves need no further work; loaded documents still share operation cache.
                if (!Has(visited, path, block.Id) || block.References.Count != 0)
                {
                    Mark(active, path, block.Id);
                    foreach (var nested in block.References.OrderBy(p => p.Key, StringComparer.Ordinal)) Visit(path, nested.Value, depth + 1);
                    active[path].Remove(block.Id); Mark(visited, path, block.Id);
                }
                return new BridgeResolvedBlock(document, block, path);
            }
            private void Cache(string path, BridgeDocument document)
            {
                bytes += InternalJson.Utf8.GetByteCount(document.ToJson());
                if (bytes > limits.MaxTotalUtf8Bytes) throw InternalJson.Error("REFERENCE_SIZE_LIMIT", "Reference graph exceeds MaxTotalUtf8Bytes.", path);
                cache.Add(path, document);
            }
            private static bool Has(Dictionary<string, HashSet<string>> map, string path, string id) => map.TryGetValue(path, out var ids) && ids.Contains(id);
            private static void Mark(Dictionary<string, HashSet<string>> map, string path, string id)
            { if (!map.TryGetValue(path, out var ids)) map.Add(path, ids = new HashSet<string>(StringComparer.Ordinal)); ids.Add(id); }
        }
    }
    internal static class BridgePaths
    {
        internal static string Normalize(string path, string? from = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            string portable = path.Replace('\\', '/');
            BridgeException Invalid() => InternalJson.Error("REFERENCE_PATH", "Expected a safe relative .bridge.json path inside the allowed root.", path);
            if (portable.Length == 0 || portable.StartsWith("/", StringComparison.Ordinal) || portable.Any(c => c < 32 || ":<>\"|?*".IndexOf(c) >= 0)) throw Invalid();
            var parts = new List<string>();
            if (from != null) parts.AddRange(from.Split('/').Reverse().Skip(1).Reverse());
            foreach (string part in portable.Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == "..") { if (parts.Count == 0) throw Invalid(); parts.RemoveAt(parts.Count - 1); continue; }
                if (part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal)) throw Invalid();
                string device = part.Split('.')[0].ToUpperInvariant();
                if (new[] { "CON", "PRN", "AUX", "NUL" }.Contains(device) || device.Length == 4 && (device.StartsWith("COM", StringComparison.Ordinal) || device.StartsWith("LPT", StringComparison.Ordinal)) && device[3] >= '1' && device[3] <= '9') throw Invalid();
                parts.Add(part);
            }
            string result = string.Join("/", parts);
            if (!result.EndsWith(".bridge.json", StringComparison.OrdinalIgnoreCase)) throw Invalid();
            return result;
        }
    }
}
