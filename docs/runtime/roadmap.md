# Roadmap после Runtime0.1

Реализованы host/registry/schema/binder, permissions/data policy/discovery/query/execute, async/serial ownership/cancel/audit, Unity dispatcher/scene/Player и отдельный официальный MCP gateway. Документация/packaging завершены в R7; [итоговая матрица R8](../verification-report-runtime.md) содержит реальные проверки и ограничения. [Этапные отчёты](../stages/runtime-R0.md) сохраняют точные фактические проверки каждого шага.

Возможные **необязательные будущие** работы: отдельный Bridge.Testing, industrial/PLC plugin, Python SDK, source generator, durable operation polling/deduplication, HTTP authenticated deployment. Они не входят в0.1 и сейчас не реализуются. Runtime не становится движком симуляции, физикой или visual editor. ValueTask/dynamic registry — только по отдельному совместимому предложению API.

Публикация NuGet, тега и GitHub Release требует отдельного решения владельца; merge исходников в main не является публикацией пакетов. Bridge JSON сохраняет bridgeVersion=1 и Core0.2 public contract.
