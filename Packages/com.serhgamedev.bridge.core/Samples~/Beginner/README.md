# Beginner TextAsset

Import this sample with Package Manager, create an empty GameObject, attach BridgeBeginner and assign factory.bridge.json.txt as Source. Press Play. Expected console: BRIDGE_BEGINNER_PASS: speed=2. The source TextAsset is read through Parse; it is not overwritten. Uses explicit field checks, Get/Set and a projection; no filesystem access or runtime spawning. Requires input settings.data.speed=1 and type simulation.settings.
