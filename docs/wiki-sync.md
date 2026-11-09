# Источник и безопасная публикация Wiki

Единый источник — docs/wiki, включая Runtime route в docs/wiki/runtime. README ведут на эти реальные файлы, пока отдельная Wiki публикация не подтверждена. Не поддерживайте две расходящиеся копии.

Проверка2026-11-09: GitHub repo.has_wiki=true, но git ls-remote Bridge.wiki.git отвечает Repository not found. Wiki git storage ещё не инициализировано. Публикация не заявляется; полный Core+Runtime fallback доступен в репозитории.

## Dry run

`pwsh -File tools/Sync-Wiki.ps1` создаёт work/wiki-preview, проверяет локальные ссылки и генерирует Wiki-compatible pages. Root Core pages сохраняют имена, runtime/Home.md становится Runtime-Home, остальные Runtime pages тоже flatten с Runtime- prefix. Внутренние ссылки переписываются под Wiki, ссылки на repository files — на main. Никакой commit/push в dry run нет. Исходники остаются обычными browsable Markdown с относительными links.

## Публикация после инициализации

Владелец может создать первую Home страницу в GitHub Wiki с одной строкой `<!-- Bridge managed wiki source -->`. Затем `pwsh -File tools/Sync-Wiki.ps1 -Publish` сначала генерирует reviewable preview, проверяет remote и клонирует свежий work/wiki-sync. Неуправляемая страница с совпавшим именем либо существующий sync checkout остановят публикацию. Скрипт принимает новый marker и прежний Bridge.Core marker, заменяет только managed pages, не удаляет неизвестные страницы/историю и не использует force push.

После publish проверьте Home/Sidebar/Runtime-Home на GitHub и обновите статус в исходной документации; до этого README не переключаются на вымышленный Wiki URL. В случае collision вручную согласуйте конкретные страницы, не удаляйте чужую Wiki. [Core route](wiki/Home.md) · [Runtime route](wiki/runtime/Home.md).
