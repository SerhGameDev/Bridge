#nullable enable
using Newtonsoft.Json.Linq;

namespace Bridge.Core
{
    public enum BridgeValueKind { Object, Array, String, Number, Boolean, Null }

    // A detached immutable JSON value. The JSON backend never appears in public signatures.
    public sealed partial class BridgeValue
    {
        internal JToken Token { get; }
        internal BridgeValue(JToken token) { Token = token.DeepClone(); }
        public BridgeValueKind Kind => Token.Type switch
        {
            JTokenType.Object => BridgeValueKind.Object, JTokenType.Array => BridgeValueKind.Array,
            JTokenType.String => BridgeValueKind.String, JTokenType.Boolean => BridgeValueKind.Boolean,
            JTokenType.Null => BridgeValueKind.Null, _ => BridgeValueKind.Number
        };
        public static BridgeValue Parse(string json, BridgeReadOptions? options = null)
            => new BridgeValue(InternalJson.Parse(json, options ?? new BridgeReadOptions(), null));
        public string ToJson() => InternalJson.Write(Token);
    }
}
