using System;
using Bridge.Core;
using UnityEngine;

namespace Bridge.Samples
{
    public sealed class BridgeBeginner : MonoBehaviour
    {
        public TextAsset Source;
        private void Start()
        {
            if (Source == null) throw new InvalidOperationException("Assign a Bridge JSON TextAsset to Source.");
            var document = BridgeDocument.Parse(Source.text, source: Source.name);
            if (!document.ContainsBlock("settings") || !document.HasField("settings", "speed"))
                throw new InvalidOperationException("Expected settings.data.speed.");
            document.Set("settings", "speed", document.Get<double>("settings", "speed") * 2);
            var result = document.Query().WhereType("simulation.settings").SelectFields("speed").Execute();
            if (result.Blocks.Count != 1 || document.Get<double>("settings", "speed") != 2)
                throw new InvalidOperationException("Expected input speed=1 and output speed=2.");
            Debug.Log("BRIDGE_BEGINNER_PASS: speed=2");
        }
    }
}
