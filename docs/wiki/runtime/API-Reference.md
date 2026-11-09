<!-- Bridge managed wiki source -->
# API reference Runtime0.1

Namespace Bridge.Runtime, netstandard2.1/C#9. Ниже сигнатуры, а не standalone программа. Рабочие программы — [First Command](First-Command.md), [Documents](Documents.md), [Cookbook](Cookbook.md). Core0.2 API сохранён отдельно: [Core reference](../API-Reference.md).

```text
BridgeHostBuilder.Create(string name)
  .AddDocument(string name, BridgeDocument document, BridgeDataPolicy? access = null)
  .AddDataSource(string name, IBridgeDataSource source, BridgeDataPolicy? access = null)
  .AddCommands(object instance)
  .UseCodecs(BridgeCodecRegistry codecs)
  .AddCommand<TArguments,TResult>(string name,
      Func<TArguments,CancellationToken,Task<TResult>> handler,
      BridgePermission permission = BridgePermission.Admin, string description = "")
  .UseDispatcher(IBridgeDispatcher dispatcher, bool ownsDispatcher = false)
  .UseOptions(BridgeHostOptions options)
  .Build() -> BridgeHost

BridgeHost : IDisposable
  string Name; bool IsDisposed
  IReadOnlyList<BridgeCommandDescriptor> CommandsFor(BridgeRequestContext context)
  Task<BridgeCommandResult> DiscoverAsync(BridgeRequestContext context)
  Task<BridgeCommandResult> QueryAsync(string source, BridgeQuerySpec spec, BridgeRequestContext context)
  Task<BridgeCommandResult> ExecuteAsync(string command, BridgeValue arguments, BridgeRequestContext context)
  Task<BridgeCommandResult> ExecuteLocalAsync(string command, BridgeValue arguments, CancellationToken cancellationToken = default)
  IReadOnlyList<BridgeAuditEntry> GetAuditSnapshot()
  void Dispose()

BridgeRequestContext(string principal, BridgePermission permission = BridgePermission.None,
    string? requestId = null, CancellationToken cancellationToken = default)
  Principal; Permission; RequestId; CancellationToken
  bool Allows(BridgePermission minimum)
  static BridgeRequestContext Local(CancellationToken cancellationToken = default)

BridgeDataPolicy.ReadOnly() / DenyAll()
  AllowTypes(params string[] types)
  AllowFields(string type, params string[] fields)
  AllowBlocks(params string[] ids)
  WithLimits(int maxBlocks = 100, int maxResponseBytes = 65536, int maxQueryBytes = 16384)
  MaxBlocks; MaxResponseBytes; MaxQueryBytes

IBridgeDataSource.ReadAsync(CancellationToken cancellationToken) -> Task<BridgeDataSnapshot>
BridgeDataSnapshot(BridgeDocument document, string? revision = null)
  Document; Revision; CapturedAt (DateTimeOffset)
IBridgeDispatcher : IDisposable
  Task<T> InvokeAsync<T>(Func<CancellationToken,Task<T>> action, CancellationToken cancellationToken)
SerializedBridgeDispatcher(int capacity = 64)
BridgeDispatchException(string code): Code

BridgeCodecRegistry()
  Register<T>(BridgeValue schema, Func<BridgeValue,T> read, Func<T,BridgeValue> write)
  RegisterEnum<T>() where T : struct, Enum
  BridgeCodecRegistry Clone()
  BridgeValue SchemaFor<T>()
```

Все fluent методы builder/policy/codec возвращают свой тип. Builder frozen после одного Build; source/command duplicates диагностируются при регистрации/Build. Options копируются при настройке/Build, codecs клонируются; delegate closures остаются ответственностью владельца.

Attributes: BridgeCommand(name) с Description и Permission (default Admin); BridgeRange(minimum,maximum), BridgeDescription(description), BridgeAllowNull — на parameters. Имена host/source/command ordinal, case-sensitive, pattern `^[A-Za-z][A-Za-z0-9_.-]{0,127}$`; block/type/field allowlists хранят точные непустые строки Core. Переименование/изменение required args — breaking application contract; используйте новое command name/version и оставьте старый adapter при миграции. Bridge JSON bridgeVersion=1 не меняется.

BridgeHostOptions defaults: MaxRequestBytes65536, MaxResponseBytes1048576 (minimum1024), MaxPendingRequests64, RequestTimeout30s, AuditCapacity0 (max10000). Policy limits отдельные и ограничивают Query projection. [Семантика безопасности](Security.md).

BridgeCommandDescriptor: Name/Description/Permission/InputSchema/OutputSchema. BridgeCommandResult: Status/RequestId/OperationId/Data/Error/Success, ToValue()/ToJson(). BridgeCommandStatus: Completed, Queued (не выдаётся), Rejected, Failed, Cancelled. BridgeAuditEntry: Principal/RequestId/OperationId/Operation/Status/ErrorCode/ExceptionType/Timestamp; нет arguments/exception message/stack. Audit доступен локальному C#, не объявлен network tool.

Binder поддерживает string/bool, CLR integer/float/double/decimal и nullable value types, зарегистрированные enums/DTO, void/Task/Task<T>, optional parameters и injected CancellationToken. Нет object/Type/Delegate/open generic/ref/out/pointer/params/async void/ValueTask MVP. Reference null на input требует BridgeAllowNull либо default null; output должен соответствовать codec schema. JSON Schema subset: type/properties/required/additionalProperties=false, enum, numeric range, maxLength, bounded arrays, nullable anyOf, description. Неизвестные schema keywords отклоняются; не обещается весь JSON Schema.

Bridge.Unity: `UnityBridgeDispatcher : MonoBehaviour, IBridgeDispatcher`, OwnerThreadId, Capacity(default64,1..10000), Dispose. Runtime не содержит этот тип. [Где выполняется handler](Dispatchers.md).

Bridge.Transport.Mcp net8: IBridgeMcpEndpoint (DiscoverAsync/QueryAsync/ExecuteAsync → Task<BridgeValue>), RuntimeMcpEndpoint(host,principal,permission), PlayerMcpEndpoint(token,port=19731), BridgeMcpServer.Create(Stream input,Stream output,IBridgeMcpEndpoint endpoint) → SDK McpServer. **SDK transport начинает читать переданные streams при Create**; RunAsync запускает обработку. Вызывайте Create только на explicit startup, владение streams передаётся SDK, endpoint/host принадлежат приложению. Никакой socket/listener при BridgeHost.Build. [Полный transport guide](../../runtime/mcp.md).
