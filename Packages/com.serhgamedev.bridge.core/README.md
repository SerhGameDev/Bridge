# Bridge.Core for Unity

Use Package Manager **Add package from disk** and choose this directory's `package.json`, or add a local dependency in the host project's manifest:

```json
"com.serhgamedev.bridge.core": "file:C:/GitHub/Bridge/Packages/com.serhgamedev.bridge.core"
```

This repository has no published Git URL yet. For another checkout, replace the local path. The dependency is Unity's official `com.unity.nuget.newtonsoft-json` 3.2.2; do not copy Newtonsoft DLLs into Assets.

The same Runtime C# source is compiled by the .NET project. `Bridge.Core.asmdef` sets `noEngineReferences: true`; only samples/tests use Unity APIs. Load `TextAsset.text` through `BridgeDocument.Parse(asset.text)`.

To run package tests, include `"testables": ["com.serhgamedev.bridge.core"]` in the host manifest and run `Bridge.Core.EditorTests` in Test Runner. The repository helper `tools/Verify-Unity.ps1` creates an isolated host under ignored `work/`.

Import **Basic Usage and AOT Smoke** for a runnable explicitly registered DTO example. See the sample README for IL2CPP/code stripping and WebGL build instructions. Compatibility claims are documented in the repository's `docs/unity-compatibility.md`; Unity 7 and other unavailable versions are not assumed tested.
