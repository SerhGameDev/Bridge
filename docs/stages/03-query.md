# Этап 3 — завершён

BridgeQuerySpec и неизменяемый fluent builder поддерживают type/id/fields/limit. Обычный LINQ работает через IReadOnlyList<BridgeBlock>/IEnumerable. Execute возвращает BridgeSelection/BridgeProjection snapshots; метаданные refs и расширения не попадают в проекцию. Необъектный data и отсутствующее выбранное поле дают QUERY_DATA_KIND/QUERY_FIELD_NOT_FOUND. Повторная установка одного фильтра заменяет прежнее условие этого поля. Фильтры type/id сочетаются AND; сортировка ID ordinal, Limit применяется после фильтрации. Запрос JSON версии 1 отклоняет неизвестные операции и не принимает выражения/лямбды.

PASS: dotnet test Bridge.sln -c Release --logger trx --results-directory work/test-results/stage3 — 51/51, 0 skipped. PASS: dotnet run --project samples/Bridge.Console.Sample -c Release -- --performance — LINQ/fluent и отсутствие исключённого поля подтверждены.

Performance smoke на этом компьютере, без SLA: 1000 блоков -> 500, 8.037 ms, ~1816664 bytes allocations; 10000 -> 5000, 68.035 ms, ~18172616 bytes. Один прогон, прогрев query предыдущим sample; время не является стабильной оценкой производительности других систем.

Git: feature/03-query; implementation 7b8df7002096d2c1521e907f923a6a1a5aeba56b; git status --short после implementation commit пуст; PR отсутствует (локальный режим). git log -n 3 --oneline:

7b8df70 feat(core): introduce LINQ-friendly blocks and serializable query specifications
1a22fdc docs(core): record serialization and DTO validation
91fa261 feat(core): support block mutations typed values and deterministic serialization

Далее: ссылки и ограниченные резолверы, после 51/51 PASS.
