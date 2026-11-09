# Bridge.Core for Unity

Core0.2 is accepted into main. Package Manager → Add package from git URL:

```text
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.core
```

For reproducible installs, append a verified commit SHA. No NuGet/registry/tag release is claimed. Alternatively use Add package from disk and select package.json, or this complete local manifest:

```json
{"dependencies":{"com.serhgamedev.bridge.core":"file:C:/GitHub/Bridge/Packages/com.serhgamedev.bridge.core"}}
```

Unity restores official com.unity.nuget.newtonsoft-json3.2.2; do not copy vendor DLLs into Assets. The .NET project links the same Runtime C# source. Bridge.Core.asmdef has noEngineReferences=true; only samples/tests use Unity APIs.

Import Beginner TextAsset or Basic Usage and AOT Smoke from Package Manager samples. Load TextAsset.text with BridgeDocument.Parse. Add Core to manifest testables and run Bridge.Core.EditorTests; tools/Verify-Unity.ps1 provides a reproducible isolated host. Runtime/commands and its Unity adapter are separate packages.

[Core guide](https://github.com/SerhGameDev/Bridge/blob/main/docs/wiki/Home.md) · [Runtime guide](https://github.com/SerhGameDev/Bridge/blob/main/docs/wiki/runtime/Home.md). Actual Unity/IL2CPP evidence is recorded in versioned verification reports; unavailable engines/targets are not assumed tested.
