#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge.Core;

namespace Bridge.Runtime
{
    /// <summary>A finite JSON Schema subset, never an expression evaluator or remote reference resolver.</summary>
    internal static class BridgeSchema
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>(new[] { "type", "properties", "required", "additionalProperties", "items", "maxItems", "enum", "minimum", "maximum", "description", "anyOf", "maxLength" }, StringComparer.Ordinal);
        internal static BridgeValue Nullable(BridgeValue schema) => Matches(BridgeValue.Null, schema) ? schema
            : Json.Obj(("anyOf", BridgeValue.Array(new[] { schema, Json.Obj(("type", BridgeValue.From("null"))) })));
        internal static void Definition(BridgeValue schema, int depth = 0)
        {
            void Invalid() => throw new ArgumentException("Unsupported or inconsistent JSON Schema subset.", nameof(schema));
            if (depth > 16 || schema.Kind != BridgeValueKind.Object || schema.Properties.Keys.Any(k => !Keywords.Contains(k))) { Invalid(); return; }
            if (schema.TryGetProperty("description", out var description) && description!.Kind != BridgeValueKind.String) Invalid();
            if (schema.TryGetProperty("anyOf", out var alternatives))
            {
                if (schema.Properties.Keys.Any(k => k != "anyOf" && k != "description") || alternatives!.Kind != BridgeValueKind.Array || alternatives.Items.Count != 2) { Invalid(); return; }
                foreach (var choice in alternatives.Items) Definition(choice, depth + 1);
                if (!alternatives.Items.Any(s => s.TryGetProperty("type", out var t) && t!.As<string>() == "null")) Invalid();
                return;
            }
            if (!schema.TryGetProperty("type", out var typeValue) || typeValue!.Kind != BridgeValueKind.String) { Invalid(); return; }
            string type = typeValue.As<string>();
            if (!new[] { "object", "array", "string", "integer", "number", "boolean", "null" }.Contains(type)) Invalid();
            var allowed = new HashSet<string>(new[] { "type", "description", "enum" }, StringComparer.Ordinal);
            if (type == "object") allowed.UnionWith(new[] { "properties", "required", "additionalProperties" });
            if (type == "array") allowed.UnionWith(new[] { "items", "maxItems" });
            if (type == "string") allowed.Add("maxLength");
            if (type == "integer" || type == "number") allowed.UnionWith(new[] { "minimum", "maximum" });
            if (schema.Properties.Keys.Any(k => !allowed.Contains(k))) Invalid();
            if (type == "object")
            {
                if (!schema.TryGetProperty("additionalProperties", out var extra) || extra!.Kind != BridgeValueKind.Boolean || extra.As<bool>()
                    || !schema.TryGetProperty("properties", out var properties) || properties!.Kind != BridgeValueKind.Object || properties.Properties.Count > 128) { Invalid(); return; }
                foreach (var property in properties.Properties.Values) Definition(property, depth + 1);
                if (schema.TryGetProperty("required", out var required) && (required!.Kind != BridgeValueKind.Array || required.Items.Any(v => v.Kind != BridgeValueKind.String || !properties.Properties.ContainsKey(v.As<string>())))) Invalid();
            }
            if (type == "array")
            {
                if (!schema.TryGetProperty("items", out var items) || !schema.TryGetProperty("maxItems", out var maxItems) || maxItems!.Kind != BridgeValueKind.Number || !maxItems.As<int>().Equals(Math.Clamp(maxItems.As<int>(), 0, 10000))) { Invalid(); return; }
                Definition(items!, depth + 1);
            }
            foreach (string key in new[] { "minimum", "maximum" })
                if (schema.TryGetProperty(key, out var bound) && bound!.Kind != BridgeValueKind.Number) Invalid();
            if (schema.TryGetProperty("minimum", out var min) && schema.TryGetProperty("maximum", out var max) && min!.As<double>() > max!.As<double>()) Invalid();
            if (schema.TryGetProperty("maxLength", out var length) && (length!.Kind != BridgeValueKind.Number || length.As<int>() < 0)) Invalid();
            if (schema.TryGetProperty("enum", out var values) && (type != "string" || values!.Kind != BridgeValueKind.Array || values.Items.Count == 0 || values.Items.Any(v => !Matches(v, Json.Obj(("type", BridgeValue.From(type))))))) Invalid();
        }
        internal static bool Matches(BridgeValue value, BridgeValue schema)
        {
            try
            {
                if (schema.TryGetProperty("anyOf", out var choices)) return choices!.Items.Any(choice => Matches(value, choice));
                string type = schema.Get<string>("type");
                bool kind = type == "object" ? value.Kind == BridgeValueKind.Object : type == "array" ? value.Kind == BridgeValueKind.Array
                    : type == "null" ? value.Kind == BridgeValueKind.Null : type == "string" ? value.Kind == BridgeValueKind.String
                    : type == "boolean" ? value.Kind == BridgeValueKind.Boolean : value.Kind == BridgeValueKind.Number;
                if (!kind || (type == "integer" && !IsInteger(value.ToJson()))) return false;
                if (schema.TryGetProperty("enum", out var enums) && !enums!.Items.Any(v => v.ToJson() == value.ToJson())) return false;
                if (schema.TryGetProperty("minimum", out var min) && value.As<double>() < min!.As<double>()) return false;
                if (schema.TryGetProperty("maximum", out var max) && value.As<double>() > max!.As<double>()) return false;
                if (schema.TryGetProperty("maxLength", out var length) && value.As<string>().Length > length!.As<int>()) return false;
                if (type == "object")
                {
                    var properties = schema.GetProperty("properties").Properties;
                    if (schema.TryGetProperty("required", out var required) && required!.Items.Any(v => !value.Properties.ContainsKey(v.As<string>()))) return false;
                    if (value.Properties.Any(p => !properties.ContainsKey(p.Key) || !Matches(p.Value, properties[p.Key]))) return false;
                }
                if (type == "array" && (value.Items.Count > schema.Get<int>("maxItems") || value.Items.Any(v => !Matches(v, schema.GetProperty("items"))))) return false;
                return true;
            }
            catch (BridgeException) { return false; }
        }
        private static bool IsInteger(string lexeme)
        {
            var parts = lexeme.ToLowerInvariant().Split('e');
            var mantissa = parts[0].TrimStart('-'); int dot = mantissa.IndexOf('.');
            int decimals = dot < 0 ? 0 : mantissa.Length - dot - 1;
            string digits = mantissa.Replace(".", "");
            if (digits.All(c => c == '0')) return true;
            long exponent = 0;
            if (parts.Length == 2 && !long.TryParse(parts[1], System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out exponent)) return !parts[1].StartsWith("-", StringComparison.Ordinal);
            if (exponent >= decimals) return true;
            if (exponent < -digits.Length) return false;
            int trailing = digits.Length - digits.TrimEnd('0').Length;
            return trailing >= decimals - exponent;
        }
    }
}
