#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    // Newtonsoft owns structural parsing. This lexical adapter rejects its JavaScript
    // extensions and protects exact JSON numeric lexemes from double/decimal rounding.
    internal static class InternalJson
    {
        private static readonly Regex Number = new Regex(@"\G-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?", RegexOptions.CultureInvariant);
        internal static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static JToken Parse(string json, BridgeReadOptions options, string? source)
        {
            options.Validate();
            try
            {
                if (Utf8.GetByteCount(json) > options.MaxUtf8Bytes)
                    throw Error("INPUT_LIMIT", "Input exceeds MaxUtf8Bytes.", source);
                var normalized = json.ToCharArray();
                var numbers = new Queue<string>();
                char last = '\0';
                for (int i = 0; i < json.Length;)
                {
                    char c = json[i];
                    if (IsWhite(c)) { i++; continue; }
                    int start = i;
                    if (c == '"')
                    {
                        i++;
                        bool closed = false;
                        while (i < json.Length)
                        {
                            c = json[i++];
                            if (c == '"') { closed = true; break; }
                            if (c < 32) throw Lexical("Unescaped control character.", start);
                            if (c != '\\') continue;
                            if (i >= json.Length) throw Lexical("Incomplete escape.", i - 1);
                            c = json[i++];
                            if (c == 'u')
                            {
                                for (int h = 0; h < 4; h++)
                                    if (i >= json.Length || !Uri.IsHexDigit(json[i++]))
                                        throw Lexical("Invalid Unicode escape.", i - 1);
                            }
                            else if ("\"\\/bfnrt".IndexOf(c) < 0) throw Lexical("Invalid string escape.", i - 1);
                        }
                        if (!closed) throw Lexical("Unterminated string.", start);
                        last = 's'; continue;
                    }
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        var match = Number.Match(json, i);
                        if (!match.Success) throw Lexical("Invalid JSON number.", i);
                        i += match.Length;
                        if (i < json.Length && !IsBoundary(json[i])) throw Lexical("Invalid JSON number suffix.", i);
                        if (match.Length > options.MaxNumberCharacters)
                            throw Error("INPUT_LIMIT", "Number exceeds MaxNumberCharacters.", source);
                        numbers.Enqueue(match.Value);
                        normalized[start] = '0';
                        for (int n = start + 1; n < i; n++) normalized[n] = ' ';
                        last = 'n'; continue;
                    }
                    if (c == 't' || c == 'f' || c == 'n')
                    {
                        string literal = c == 't' ? "true" : c == 'f' ? "false" : "null";
                        if (i + literal.Length > json.Length || string.CompareOrdinal(json, i, literal, 0, literal.Length) != 0)
                            throw Lexical("Invalid JSON literal.", i);
                        i += literal.Length;
                        if (i < json.Length && !IsBoundary(json[i])) throw Lexical("Invalid literal suffix.", i);
                        last = 'l'; continue;
                    }
                    if ("{}[],:".IndexOf(c) >= 0)
                    {
                        if ((c == '}' || c == ']') && last == ',') throw Lexical("Trailing commas are not JSON.", i);
                        last = c; i++; continue;
                    }
                    throw Lexical("Unexpected character in strict JSON.", i);
                }
                using var reader = new JsonTextReader(new StringReader(new string(normalized)))
                { DateParseHandling = DateParseHandling.None, MaxDepth = options.MaxDepth, Culture = CultureInfo.InvariantCulture };
                var token = JToken.Load(reader, new JsonLoadSettings
                { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, LineInfoHandling = LineInfoHandling.Load });
                if (reader.Read()) throw Error("JSON_SYNTAX", "Trailing data after JSON value.", source);
                return RestoreNumbers(token, numbers);

                BridgeException Lexical(string message, int index)
                {
                    int line = 1, column = 1;
                    for (int j = 0; j < Math.Min(index, json.Length); j++)
                        if (json[j] == '\n') { line++; column = 1; } else column++;
                    return new BridgeException(new BridgeDiagnostic("JSON_SYNTAX", message, source: source, line: line, column: column));
                }
            }
            catch (JsonReaderException ex)
            {
                string code = ex.Message.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0 ? "JSON_DUPLICATE_KEY"
                    : ex.Message.IndexOf("MaxDepth", StringComparison.OrdinalIgnoreCase) >= 0 ? "INPUT_LIMIT" : "JSON_SYNTAX";
                throw new BridgeException(new BridgeDiagnostic(code, ex.Message, source: source, path: ex.Path,
                    line: ex.LineNumber > 0 ? ex.LineNumber : (int?)null, column: ex.LinePosition > 0 ? ex.LinePosition : (int?)null));
            }
            catch (EncoderFallbackException ex) { throw Error("JSON_SYNTAX", ex.Message, source); }
        }
        private static bool IsWhite(char c) => c == ' ' || c == '\r' || c == '\n' || c == '\t';
        private static bool IsBoundary(char c) => IsWhite(c) || ",]}:".IndexOf(c) >= 0;
        internal static BridgeException Error(string code, string message, string? source = null, string? path = null, string? documentId = null)
            => new BridgeException(new BridgeDiagnostic(code, message, documentId, source, path));
        internal static bool IsInteger(JToken? value, out int result)
        {
            result = 0;
            return value is JRaw raw && int.TryParse((string?)raw.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
        }
        internal static string Write(JToken token) => token.ToString(Formatting.None);
        private static JToken RestoreNumbers(JToken token, Queue<string> numbers)
        {
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) return new JRaw(numbers.Dequeue());
            foreach (var child in token.Children().ToArray())
            {
                var restored = RestoreNumbers(child, numbers);
                if (!ReferenceEquals(restored, child)) child.Replace(restored);
            }
            return token;
        }
    }
}
