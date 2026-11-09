<!-- Bridge managed wiki source -->
# Где выполняется команда

Default SerializedBridgeDispatcher удерживает Semaphore gate через весь async handler и чтение. Собственного потока не создаёт, caller SynchronizationContext сохраняется; это **не** гарантия Unity thread. Несколько independent host имеют независимые очереди. Доступ к тому же состоянию в обход host должен синхронизировать владелец.

UnityBridgeDispatcher — компонент отдельного Bridge.Unity пакета. Создайте на owner thread, передайте UseDispatcher. Update запускает максимум одну живую операцию; gate остаётся занят до фактического окончания async handler. PlayMode проверяет, что второй handler не вклинивается через await и первый остаётся на owner thread.

Не вызывайте `.Wait()`/`.Result` на Unity/UI owner thread. Async Unity handler не должен терять context перед Transform API через свой Task.Run/ConfigureAwait(false). Runtime не может исправить ошибку внутри handler. Синхронная долгая операция задерживает кадр; отмена не прерывает её насильно.

UseDispatcher(dispatcher) по умолчанию **не передаёт ownership** host. Host.Dispose отменяет свои запросы; owner закрывает переданный dispatcher. С ownsDispatcher=true dispatcher закрывает host. Sources и command instances принадлежат приложению, не уничтожаются автоматически.

В WebGL нет managed ThreadPool: completion dispatcher inline на Update, caller context сохраняется. Держите continuations короткими. Demo включает runInBackground; network IPC в WebGL выключен. [Проверенный Unity путь](Installation-Unity.md), [lifetime/cancellation](Results.md).
