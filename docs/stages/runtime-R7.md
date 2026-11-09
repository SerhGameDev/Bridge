# Runtime R7 — beginner docs, packages, Wiki fallback и CI

Base integration `3ca43ac4a231d3bb5c1760f4a0bc610db433d34a` после R6 PR8; CI42613344261 (PR) и42613306118 (push) Windows/Ubuntu success. Branch `feature/runtime-r7-docs`. План: полный маршрут новичка и12recipes, актуальные RU/EN quickstarts/API; исполняемые snippets, local NuGet consumers; безопасный recursive Wiki preview; Debug/Release CI; отчёт и PR до R8.

## Сделано

14страниц docs/wiki/runtime: Home, установка.NET/Unity, первая команда, документы, dynamic data, dispatchers, security, results, Codex MCP, cookbook/FAQ, API reference, advanced и troubleshooting. Определены document/block/field/source/command/principal. Примеры без файла/MCP и с data policy, async/null/enum/DTO/typed registrar/range/timeout/save/audit. Twelve recipes, рабочие полные программы либо ссылка на полный runnable source; Unity sample компилируется реальным Editor отдельно.

README RU/EN имеют короткий полный quickstart и ссылки. Убраны устаревшие заявления Core feature branch/main0.1/noGitURL из активных guides, Core0.2 принят PR1. Обновлены UPM Runtime/Core README, changelog/roadmap; исходники Core не менялись. Public Runtime signatures не менялись, добавлены XML summaries по lifetime/default deny. API reference сверяется с реальными signatures; SerializedBridgeDispatcher constructor parameter — capacity, snapshot property — CapturedAt.

MCP lifetime уточнён по официальному SDK source: StreamServerTransport начинает чтение supplied streams во время Create, RunAsync запускает обработку server. Поэтому Create вызывается только после explicit startup. BridgeHost.Build по-прежнему не открывает сеть/не запускает SDK. Endpoint/host owned application; streams передаются SDK.

Новый Verify-RuntimePackages.ps1 pack Core/Runtime/MCP, проверяет DLL/README/LICENSE/MIT/nuspec и отсутствие vendored DLL/artifacts; MCP включает third-party notices. Consumer использует **только PackageReference**, local source mapping Bridge.*, fresh isolated NuGet cache, исполняет command/query/policy и загружает SDK server type. Ни NuGet publish, ни тег/Release не делались.

Verify-Snippets теперь подбирает Runtime/MCP ProjectReference по namespace и включает Runtime package README; Markdown/JSON checker включает package READMEs/notices. CI на Windows/Ubuntu дополнен полными Debug build/test, Release сохраняется, local package consumer и Wiki safety dry-run обязательны.

## Wiki

Повторная проверка2026-11-09: repo.has_wiki=true, git ls-remote Bridge.wiki.git → Repository not found. **Wiki не опубликована**. Весь source fallback в docs/wiki,29pages (Core15+Runtime14) доступен через repository links. Runtime pages flatten в Runtime-* для Wiki, internal links переписываются; repository links ведут в main. Sync-Wiki без Publish генерирует reviewable preview. Publish сначала проверяет remote, fresh checkout и managed markers всех collisions; не удаляет неизвестные pages/history, нет force push.

Verify-Wiki проверяет все generated internal links и count, временно создаёт unmanaged collision в своём preview, убеждается в отказе и восстанавливает исходное содержимое. Реальные Wiki pages не изменяются. [Публикация после инициализации](../wiki-sync.md).

## Реальные проверки

* `tools/Verify-Snippets.ps1` — **40/40** отдельных.NET программ compile+run PASS. Диагностические JSON_SYNTAX/REQUEST_TIMEOUT/INVALID_ARGUMENT в output — ожидаемые assertions рецептов, не падения.
* Markdown/JSON checker до добавления этого отчёта:78pages,191local links,8JSON examples PASS. После добавления stage report:79pages,192local links,8JSON examples PASS.
* `tools/Verify-RuntimePackages.ps1` — RUNTIME_NUGET_CONSUMER_PASS, Core0.2/Runtime0.1/MCP0.1 локальные nupkgs, no ProjectReference, MIT/notice/artifact checks PASS.
* `tools/Verify-Wiki.ps1` — WIKI_SAFETY_PASS:29pages, internal links, unmanaged collision rejected, no publish.
* `dotnet test Bridge.sln -c Debug` — Core109/109, Runtime38/38, MCP14/14 PASS.
* Release regression: Core109/109, Runtime38/38, MCP14/14 PASS; дополненный Windows+Ubuntu CI проходит до merge; последняя итоговая матрица/UPM Git install/Engine smoke — R8.

Основные файлы: docs/wiki/runtime/*, root/UPM READMEs, changelog/roadmap/wiki-sync, Sync-Wiki/Verify-Wiki/Verify-Snippets/Verify-RuntimePackages, Bridge.Docs.Checks, CI, Runtime XML summaries, MCP metadata/lifetime docs. Изменений функциональных API и Core C# нет.

Ограничения: package publication отсутствует намеренно; Wiki Git storage требует первоначальной страницы владельца, рабочий fallback завершён. Codex personal connected E2E не подменяется SDK-client PASS; точные действия владельца сохранены. Windows IL2CPP/Unity7 не установлены; доступные реальные Player checks документированы R5 и повторяются по финальной матрице R8. R7 интегрируется только после зелёного CI, main ожидает финальной приёмки.
