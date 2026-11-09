using System.Collections.Generic;
using System.Linq;
using Bridge.Core;
namespace Bridge.Unity.Demo
{
    internal static class DemoJson
    {
        internal static BridgeValue Object(IDictionary<string,object> values) => BridgeValue.Object(values.Select(p=>new KeyValuePair<string,BridgeValue>(p.Key,BridgeValue.From(p.Value))));
    }
}
