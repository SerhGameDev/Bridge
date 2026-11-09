#nullable enable
using System;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    internal static class BridgeArguments
    {
        internal static void Name(string value, string parameter)
        {
            if (value == null) throw new ArgumentNullException(parameter);
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty or whitespace.", parameter);
        }
    }

    public sealed partial class BridgeBlock
    {
        /// <summary>Checks an exact top-level data key. JSON null is present.</summary>
        public bool HasField(string fieldName)
        {
            BridgeArguments.Name(fieldName, nameof(fieldName));
            return Definition["data"] is JObject data && data.Property(fieldName, StringComparison.Ordinal) != null;
        }
    }

    public sealed partial class BridgeDocument
    {
        private BridgeBlock FieldBlock(string blockId, string fieldName)
        {
            if (blocks.TryGetValue(blockId, out var block)) return block;
            string Escape(string value) => value.Replace("~", "~0").Replace("/", "~1");
            throw InternalJson.Error("BLOCK_NOT_FOUND", $"Block '{blockId}' not found while accessing field '{fieldName}'.", Source,
                "/blocks/" + Escape(blockId) + "/data/" + Escape(fieldName), DocumentId);
        }
        /// <summary>Copies registrations. Later registration changes are local to each document.</summary>
        public BridgeDocument UseSerializers(BridgeSerializer serializers)
        {
            Serializers.CopyFrom(serializers); return this;
        }
        public bool ContainsBlock(string id)
        {
            BridgeArguments.Name(id, nameof(id));
            return blocks.ContainsKey(id);
        }
        public bool HasField(string blockId, string fieldName)
        {
            BridgeArguments.Name(blockId, nameof(blockId));
            BridgeArguments.Name(fieldName, nameof(fieldName));
            return blocks.TryGetValue(blockId, out var block) && block.HasField(fieldName);
        }
        public T Get<T>(string blockId, string fieldName)
        {
            BridgeArguments.Name(blockId, nameof(blockId));
            BridgeArguments.Name(fieldName, nameof(fieldName));
            return FieldBlock(blockId, fieldName).Get<T>(fieldName);
        }
        public void Set<T>(string blockId, string fieldName, T value)
        {
            BridgeArguments.Name(blockId, nameof(blockId));
            BridgeArguments.Name(fieldName, nameof(fieldName));
            FieldBlock(blockId, fieldName).Set(fieldName, value);
        }
        /// <summary>False only for missing block/field. Type/conversion failures throw BridgeException.</summary>
        public bool TryGet<T>(string blockId, string fieldName, out T value)
        {
            BridgeArguments.Name(blockId, nameof(blockId));
            BridgeArguments.Name(fieldName, nameof(fieldName));
            if (!blocks.TryGetValue(blockId, out var block)) { value = default!; return false; }
            // A non-object is a type failure, not an absent field.
            if (block.Definition["data"] is JObject && !block.HasField(fieldName)) { value = default!; return false; }
            value = block.Get<T>(fieldName); return true;
        }
        public BridgeResult<T> TryRead<T>(string blockId, string fieldName)
        {
            try { return BridgeResult<T>.Ok(Get<T>(blockId, fieldName)); }
            catch (BridgeException ex) { return BridgeResult<T>.Fail(ex); }
        }
    }
}
