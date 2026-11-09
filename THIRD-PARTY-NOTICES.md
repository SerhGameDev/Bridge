# Third party dependencies

Bridge source is MIT. Dependencies retain their own licenses and are restored using NuGet/Unity package mechanisms; vendor DLLs are not copied into this repository.

* Newtonsoft.Json13.0.4 — MIT, [upstream license](https://github.com/JamesNK/Newtonsoft.Json/blob/13.0.4/LICENSE.md). Existing Core JSON backend; see Core package metadata.
* ModelContextProtocol.Core2.4.0 — Apache-2.0, copyright Model Context Protocol contributors, [upstream license](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.4.0/LICENSE), [release](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.4.0). Only the separate .NET MCP gateway/client uses this SDK. Apache-2.0 is compatible with distributing Bridge's own MIT code; the SDK is not relicensed as MIT.
* SDK transitive Microsoft.Extensions.*, System.* dependencies retain Microsoft MIT licenses in their NuGet package metadata. Exact restore graph is recorded by NuGet assets and can be inspected with `dotnet list src/Bridge.Transport.Mcp package --include-transitive`.
* xUnit, Microsoft.NET.Test.Sdk and Unity Test Framework are test dependencies; they are not included as Bridge runtime implementations.

Redistributors of dependency binaries must preserve the original package license/notice obligations. Local NuGet packages include this notice; actual third party package licenses ship through their own package mechanisms. No SDK source has been vendored or modified here.

