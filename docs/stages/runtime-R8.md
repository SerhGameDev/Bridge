# Runtime R8 — окончательная проверка и доставка

Base integration после R7 PR9: `7f991e6dabad9ee8ea224d48fbc95341ff6ac194`. Ветка `feature/runtime-r8-verification`. Функциональный commit `f0e968d45266aa59b2a52aa45ad39e442fd4d84f`: `fix(runtime): build bounded projections and bulk scene snapshots without repeated cloning`. Следующий docs commit содержит этот report, итоговую матрицу и обязательный delivery scan в CI.

Обнаружена квадратичная projection1000blocks:1544ms median/1.616GB managed allocations per call. Исправлены Runtime projection и demo scene bulk snapshot без изменения Core. Повторное измерение57ms/30.3MB; это измерения одной машины, не SLA. Добавлены3 tests, Runtime теперь41. Public API не менялся; новый API quickstart не требуется.

После исправления последовательно повторены Debug/Release Core109+Runtime41+MCP14; snippets40; packages isolated consumer; Wiki29; Unity6000.6.7f1 local/Git2+3 tests, alpha6000.7.0a2 local2+3; WindowsMono runtime/IPC; настоящий Player→gateway→officialSDK E2E; WebGLIL2CPPHigh build и браузер runtime PASS. Git UPM pin проверен по package lock для всех3 packages. Локальные artifacts ignored, секреты не сохраняются.

[Итоговый отчёт](../verification-report-runtime.md) содержит точные команды, результаты, performance данные, критерии приёмки и SKIPPED причины. WindowsIL2CPP module/Unity7 отсутствуют; личная Codex connected session не запускалась, compatible client alternative выполнена. Wiki fallback готов, NuGet/tag/Release не публикуются.

R8 PR в integration и затем integration PR в actual main проходят Windows/Ubuntu Debug/Release CI до merge. Источник фактических merge SHA/run links — [GitHub PR history](https://github.com/SerhGameDev/Bridge/pulls?q=is%3Apr). История сохраняется, force push/переписывания/удаления неизвестных веток нет.
