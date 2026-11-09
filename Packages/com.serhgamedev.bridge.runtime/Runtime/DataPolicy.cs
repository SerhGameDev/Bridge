#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Runtime
{
    /// <summary>Immutable exact allowlists. A type alone never opens all its fields.</summary>
    public sealed class BridgeDataPolicy
    {
        internal HashSet<string> Types { get; }
        internal Dictionary<string, string[]> Fields { get; }
        internal HashSet<string>? Blocks { get; }
        public int MaxBlocks { get; }
        public int MaxResponseBytes { get; }
        public int MaxQueryBytes { get; }
        private BridgeDataPolicy(HashSet<string>? types = null, Dictionary<string, string[]>? fields = null, HashSet<string>? blocks = null, int maxBlocks = 100, int maxResponseBytes = 65536, int maxQueryBytes = 16384)
        {
            Types = new HashSet<string>(types ?? new HashSet<string>(), StringComparer.Ordinal);
            Fields = fields == null ? new Dictionary<string, string[]>(StringComparer.Ordinal) : fields.ToDictionary(p => p.Key, p => (string[])p.Value.Clone(), StringComparer.Ordinal);
            Blocks = blocks == null ? null : new HashSet<string>(blocks, StringComparer.Ordinal);
            MaxBlocks = maxBlocks; MaxResponseBytes = maxResponseBytes; MaxQueryBytes = maxQueryBytes;
        }
        public static BridgeDataPolicy ReadOnly() => new BridgeDataPolicy();
        public static BridgeDataPolicy DenyAll() => new BridgeDataPolicy();
        private static string[] Names(string[] names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (names.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Names cannot be empty.", nameof(names));
            return names.Distinct(StringComparer.Ordinal).ToArray();
        }
        public BridgeDataPolicy AllowTypes(params string[] types)
        {
            var copy = new BridgeDataPolicy(Types, Fields, Blocks, MaxBlocks, MaxResponseBytes, MaxQueryBytes);
            copy.Types.UnionWith(Names(types)); return copy;
        }
        public BridgeDataPolicy AllowFields(string type, params string[] fields)
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));
            var copy = AllowTypes(type);
            copy.Fields[type] = Names(fields); return copy;
        }
        public BridgeDataPolicy AllowBlocks(params string[] ids) => new BridgeDataPolicy(Types, Fields, new HashSet<string>(Names(ids), StringComparer.Ordinal), MaxBlocks, MaxResponseBytes, MaxQueryBytes);
        public BridgeDataPolicy WithLimits(int maxBlocks = 100, int maxResponseBytes = 65536, int maxQueryBytes = 16384)
        {
            if (maxBlocks < 1 || maxResponseBytes < 1 || maxQueryBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBlocks));
            return new BridgeDataPolicy(Types, Fields, Blocks, maxBlocks, maxResponseBytes, maxQueryBytes);
        }
        internal bool HasReadSurface => Fields.Any(p => Types.Contains(p.Key) && p.Value.Length > 0) && (Blocks == null || Blocks.Count > 0);
    }
}
