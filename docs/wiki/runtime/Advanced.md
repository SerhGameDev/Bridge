<!-- Bridge managed wiki source -->
# Свои DataSource, Dispatcher и Transport

Источник читает состояние приложения на owner dispatcher и возвращает BridgeDataSnapshot. Создавайте detached BridgeDocument либо обеспечьте согласованное владение mutable документом. Framework открывает только policy projection, но не синхронизирует внешние записи в тот же state. [Полный CounterSource](Dynamic-Data.md), Unity SceneWorld в импортируемом sample.

Dispatcher обязан вызвать весь async callback ровно один раз либо отменить его, вернуть Task с реальным результатом и удерживать gate через await. Cancellation не должна разрешать следующую запись, пока предыдущий некооперативный handler фактически не завершился. Ограничьте queued requests, не блокируйте owner thread, закрывайте очередь при Dispose. Проверьте cancel-before-start/dispose-while-queued/exception и nested host requests. [Реальная Unity реализация](../../../Packages/com.serhgamedev.bridge.unity/Runtime/UnityBridgeDispatcher.cs).

Transport аутентифицирует клиента **до** создания BridgeRequestContext. Контекст не десериализуется из JSON; permission map принадлежит приложению. Для MCP используйте RuntimeMcpEndpoint или PlayerMcpEndpoint. Свой HTTP adapter должен отдельно enforce TLS/Host/Origin/auth/request limits; эти обязанности не решает Bridge.Core. Не раскрывайте ExecuteLocalAsync как публичный anonymous route.

Attribute registry отражает только переданный instance. Typed registrar AddCommand<TArguments,TResult> вызывает известный delegate и codecs без reflection на командном пути; TArguments schema должна быть object. Кодеки commands BridgeCodecRegistry и Core document BridgeSerializer — **разные** registries с разной задачей. Один не регистрирует тип автоматически в другом.

Schema описывает именно wire shape, read/write должны ей соответствовать. Extra output fields дают INVALID_RESULT целиком, не частичный ответ. В Unity сохраняйте reflected methods/metadata от stripping и явно инстанцируйте generic codecs. Проверяйте настоящий IL2CPP High Player, а не только Editor. Source generator, dynamic registry, durable queues/deduplication и remote authenticated hosting остаются возможными следующими работами, не скрытыми функциями MVP.

Практические [DTO/async/typed рецепты](Cookbook.md), [ADR](../../runtime/architecture.md), [security](Security.md).
