# Этап 4 — завершён

Добавлены BridgeReference, IBridgeDocumentResolver, in-memory/file реализации и ResolveReference с Result<BridgeResolvedBlock>. Явное разрешение проверяет достижимый граф блоков; local ссылку на другой блок допускает, self/cross-file цикл отвергает. Кеш на операцию, без старых данных между вызовами. Лимиты по умолчанию: 32 ребра глубины, 64 документа, 64 MiB канонического JSON суммарно, 100000 посещённых рёбер; per-document read limits также действуют.

PASS: dotnet test Bridge.sln -c Release --logger trx --results-directory work/test-results/stage4 — 64/64, 0 skipped. PASS: dotnet run --project samples/Bridge.Console.Sample -c Release — внешняя палитра red загружена, ../outside отвергнут. Проверены A->B, local, self, A->B->A, missing block/doc/ref, absolute/UNC/URL/ADS/устройства/расширения, depth/docs/bytes/edges, duplicate loads, обновление и восстановление после ошибки. Реальный Windows junction вне корня и junction-root отвергнуты.

Файловый resolver запрещает symlinks/reparse points и проверяет все предки. Он рассчитан на стабильный доверенный root; portable managed API не защищает атомарно от враждебной конкурентной замены дерева (см. references.md). Ссылки сохраняются метаданными, не инлайн-контентом.

Git: feature/04-references; implementation 58929ae578e6385a1c675e96d89307ed914ec9f1; git status --short после implementation commit пуст; PR отсутствует (локальный режим). git log -n 3 --oneline:

58929ae feat(core): add bounded cross-document references and resolvers
26c68a7 docs(core): record query semantics and performance smoke
7b8df70 feat(core): introduce LINQ-friendly blocks and serializable query specifications

Далее: UPM/Unity/AOT после 64/64 PASS.
