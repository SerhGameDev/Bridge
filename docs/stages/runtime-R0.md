# Runtime R0 — аудит и bootstrap

Base: main d7d5ac2 при входе; PR1 head a02a407, mergeable и ready, последний hosted CI42598308031 success Windows/Ubuntu. Main protection endpoint404 и rules[] — действующих правил не обнаружено; origin проверен как SerhGameDev/Bridge. Рабочее дерево было чистым. Свежий dotnet test -c Release: PASS109/109.

PR1 слит через GitHub обычным merge с expected head: main5e06afd76b4e57251cba77c1ef8bce5f417f30c9. Локальный main обновлён --ff-only. Новая integration feature/bridge-runtime-v0.1 от этого SHA; R0 branch feature/runtime-r0-foundation. Не удалялись ветки, не переписывалась история.

Изменены: docs/runtime/architecture.md и этот отчёт. Зафиксированы directed dependencies, default deny, trusted context, permission hierarchy, сериализация query/commands, lifecycle/cancellation, DTO/AOT и реальные доступные Unity targets. Public runtime API пока не реализован; схема приведена в ADR. Следующие подэтапы не начнутся до CI и интеграции этого документа.

Проверка этапа: Core109/109 PASS, документационные ссылки проверяются существующим CI. Hosted run/PR и фактический merge R0 фиксируются в описании PR; недоступные проверки не выдаются за PASS. Release/tag/package publish не выполняются.
