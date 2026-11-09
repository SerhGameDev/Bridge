# Basic Usage / AOT smoke

Import this sample from Package Manager. `BridgeAotSmoke` runs automatically when a scene loads in a player and reports `BRIDGE_SMOKE_PASS` or a failure with diagnostics. It checks an explicitly registered DTO converter, JSON/UTF-8 round-trip, LINQ, a field projection, exact numbers and an in-memory document reference. In Editor Play mode it logs the same result without quitting the Editor.

For IL2CPP, select an installed target, set IL2CPP and Managed Stripping Level High, build and run a scene. The sample DTO has `[Preserve]` and its typed constructor/member accesses are explicitly referenced. No runtime reflection serializer is used. A WebGL builder is available through `Bridge.Samples.BridgeSmokeBuild.BuildWebGl`; its player writes the result to DOM `#bridge-smoke-result` as well as the console. Only the sample uses UnityEngine; Bridge.Core itself has no engine references.

v0.2 also checks shared converter registration snapshots, multi-type projection and removed-handle diagnostics. If Beginner TextAsset is imported too, the Editor smoke builder adds that optional component and its TextAsset to the generated scene; otherwise BasicUsage remains independently importable. The optional lookup uses Editor tooling only, never a Core reflection serializer.
