#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public sealed partial class BridgeValue
    {
        public static BridgeValue Null => new BridgeValue(JValue.CreateNull());
        public static BridgeValue From<T>(T value) => BridgeSerializer.PrimitiveWrite(value);
        public static BridgeValue Object(IEnumerable<KeyValuePair<string, BridgeValue>> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            var obj = new JObject();
            foreach (var entry in properties)
            {
                if (entry.Key == null || entry.Value == null) throw new ArgumentException("JSON object entries must be non-null.");
                if (obj.Property(entry.Key, StringComparison.Ordinal) != null) throw InternalJson.Error("JSON_DUPLICATE_KEY", $"Duplicate property {entry.Key}.");
                obj.Add(entry.Key, entry.Value.Token.DeepClone());
            }
            return new BridgeValue(obj);
        }
        public static BridgeValue Array(IEnumerable<BridgeValue> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            return new BridgeValue(new JArray(items.Select(v => v?.Token.DeepClone() ?? throw new ArgumentException("Use BridgeValue.Null."))));
        }
        public IReadOnlyDictionary<string, BridgeValue> Properties => Token is JObject obj
            ? new ReadOnlyDictionary<string, BridgeValue>(obj.Properties().ToDictionary(p => p.Name, p => new BridgeValue(p.Value), StringComparer.Ordinal))
            : throw InternalJson.Error("VALUE_TYPE", "Expected a JSON object.");
        public IReadOnlyList<BridgeValue> Items => Token is JArray arr
            ? System.Array.AsReadOnly(arr.Select(v => new BridgeValue(v)).ToArray())
            : throw InternalJson.Error("VALUE_TYPE", "Expected a JSON array.");
        public bool TryGetProperty(string name, out BridgeValue? value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (!(Token is JObject obj)) throw InternalJson.Error("VALUE_TYPE", "Expected a JSON object.");
            var property = obj.Property(name, StringComparison.Ordinal);
            value = property == null ? null : new BridgeValue(property.Value);
            return property != null;
        }
        public BridgeValue GetProperty(string name) => TryGetProperty(name, out var value) ? value! : throw InternalJson.Error("FIELD_NOT_FOUND", $"Field '{name}' not found.", path: name);
        public T As<T>() => BridgeSerializer.PrimitiveRead<T>(this);
        public T Get<T>(string name) => GetProperty(name).As<T>();
        public BridgeValue WithField(string name, BridgeValue value)
        {
            if (name == null || value == null) throw new ArgumentNullException(name == null ? nameof(name) : nameof(value));
            if (!(Token.DeepClone() is JObject obj)) throw InternalJson.Error("VALUE_TYPE", "Set requires object data.");
            obj[name] = value.Token.DeepClone(); return new BridgeValue(obj);
        }
    }
}
