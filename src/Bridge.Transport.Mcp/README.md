# Bridge.Transport.Mcp 0.1

Separate .NET8 gateway using official ModelContextProtocol.Core2.4.0 (Apache-2.0). Bridge code remains MIT. Runtime/Core/Unity do not reference MCP. No HTTP/ASP.NET, automatic listener, assembly scanning or filesystem tools.

`BridgeMcpServer.Create(input, output, endpoint)` creates the SDK transport and starts reading the supplied streams; the application explicitly starts server processing with `RunAsync`. Call Create only during explicit startup. Endpoint identity is trusted server configuration (`RuntimeMcpEndpoint`) or the Player's token mapping (`PlayerMcpEndpoint`). Incoming JSON never selects principal/permission. Three tools: bridge_discover (authorized command names/descriptions/formal schemas and source policies), bridge_query (Core QuerySpec v1/v2), bridge_execute (explicit registered command and arguments). Commands retain their native schemas inside discovery instead of duplicating hundreds of handwritten MCP tools.

Business errors are tool results with isError=true and sanitized Bridge envelope in both structuredContent and text. Invalid outer tool name/arguments are JSON-RPC InvalidParams; unsupported protocol methods are handled by the SDK. Execute awaits actual completion, with no automatic retries or fake queued acknowledgement. Input JSON line limit128KiB, tool arguments64KiB, Bridge result256KiB,16active calls and15s cooperative deadline. Cancellation does not roll back side effects. Stream/endpoint errors do not disclose exception messages or credentials.

The executable sample requires explicit `--enable-stdio --player` (environment BRIDGE_PLAYER_TOKEN) or `--enable-stdio --local-demo` (Read-only in-process demo). Protocol bytes alone go to stdout; diagnostics go to stderr. See [full Player/Codex setup](https://github.com/SerhGameDev/Bridge/blob/main/docs/runtime/mcp.md).

The official SDK handles MCP2026-07-28 negotiation/discovery and older initialize-based clients. Real SDK protocol tests and a built Unity Player E2E are required before claiming interoperability. No custom imitation of MCP framing/handshake is used. The Player uses separate authenticated loopback IPC; it contains no MCP SDK.
