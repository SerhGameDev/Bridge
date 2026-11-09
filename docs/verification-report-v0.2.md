# Проверка Bridge.Core v0.2

Дата: 2026-11-09, Europe/Moscow. Исходный main: d7d5ac2f8c923756a055943b181dfbb5104fee1a. Работа последовательно выполнена в feature/core-v0.2-developer-experience. Исходный baseline — 92/92 .NET tests. Реализация и проверки подэтапов фиксировались отдельными коммитами до перехода далее; пользователь разрешил все оставшиеся подэтапы без промежуточных остановок.

Код финальной проверки: c6b392667b1320f454d793b4ce4bf7fefacd1180. Последующие изменения отчётные/документационные. PR: https://github.com/SerhGameDev/Bridge/pull/1. Main сохраняет v0.1 до принятия PR. Версии csproj/UPM — 0.2.0, локальный кандидат; NuGet/Release/tag не публиковались.

## Окружение

- Windows 10.0.22631 x64, Git 2.45.1.windows.1, .NET SDK 8.0.408/VSTest17.11.1; Core netstandard2.1/C#9, tests/console net8.0.
- NuGet Newtonsoft.Json13.0.4. Unity package com.unity.nuget.newtonsoft-json3.2.2 (DLL13.0.2), noEngineReferences=true. Один Runtime source для .NET/UPM, MIT сохранён.
- Unity6000.6.7f1 (b22767f04f04) и 6000.7.0a2 (83605ec097c0). Unity7 отсутствует; Windows standalone IL2CPP module отсутствует, WebGL IL2CPP установлен.

## Фактические результаты

| Проверка | Результат |
| --- | --- |
| Restore + Release solution build | PASS, 0 предупреждений/ошибок |
| .NET xUnit после всех code changes | PASS 109/109, 0 failed/0 skipped |
| Новый clean clone, restore/build/test/beginner | PASS 109/109, build 0 warnings/errors, BEGINNER_PASS |
| Старый скомпилированный Console.Sample v0.1 с заменённой Core DLL0.2, без перекомпиляции | PASS: round-trip/LINQ/projection/refs/sandbox |
| Независимый потребитель локального NuGet0.2.0 | PASS LOCAL_NUGET_PACKAGE_PASS, отдельный cache и package source mapping, новый convenience/multi-type API |
| Console.Beginner end-to-end | PASS: создание/save/exists/tryload/check/get/set/LINQ/multi-type/projection/save/reload, speed=2 |
| C# примеры README/docs/wiki | PASS25/25 compilation + execution (также hosted Windows/Linux) |
| JSON examples и локальные Markdown links | PASS: 44 Markdown pages, 95 local links, 3 JSON examples через tools/Bridge.Docs.Checks |
| Unity6000.6.7f1 EditMode | PASS12/12, 0 failed/skipped |
| Unity6000.7.0a2 EditMode | PASS12/12, 0 failed/skipped; alpha Editor |
| Новый Unity6000.6.7f1 host с Git UPM URL feature-ветки | PASS12/12; Package Manager реально разрешил Git URL с ?path= и #feature/core-v0.2-developer-experience |
| Unity6000.6.7f1 WebGL/IL2CPP High | PASS build exit0 и фактический browser player |
| WebGL runtime | BRIDGE_SMOKE_PASS, 09:58:37.409 UTC /12:58 MSK; BRIDGE_BEGINNER_PASS: speed=2, 09:58:39.433 UTC |
| GitHub Actions Windows + Ubuntu | PASS оба job в run42597108203, проверен статус success |
| Артефакты, credential patterns, история больших blobs | PASS: нет tracked bin/obj/Library/work/DLL/EXE/WASM/NUPKG, известных token/private key patterns, blobs>1MiB |
| Core assembly boundary | PASS существующий .NET/Unity test: нет UnityEngine references; runtime source без dynamic/Reflection.Emit/Expression.Compile |
| Wiki publication | SKIPPED: has_wiki=true, отдельный Bridge.wiki.git ещё не инициализирован. Полный fallback docs/wiki и Sync-Wiki подготовлены |
| Unity7 / другие IL2CPP targets | SKIPPED: редактор/модули отсутствуют |

Реальный CI: https://github.com/SerhGameDev/Bridge/actions/runs/42597108203 . Запущен workflow_dispatch на указанном source commit, оба jobs завершились success. CI выполняет restore/build/test, оба console samples, C# snippets/JSON/links, pack и independent package consumer. Unity не выдаётся за часть hosted workflow. Анонимный REST сначала достиг rate limit; итоговый статус получен аутентифицированным GitHub API без раскрытия credentials.

Логи/результаты остаются игнорируемыми: work/test-results/v02, work/snippets/results.json, work/unity/<Editor>/test-results.xml и EditMode.log, work/unity/6000.6.7f1/WebGLBuild.log, work/unity/webgl-smoke. Browser реально загрузил новый wasm/framework/data и сообщил оба PASS; успешная сборка не подменяет runtime-проверку.

## Приёмка A–N

| Сценарии | Подтверждение |
| --- | --- |
| A/B/C: файл без ручного Stream, Exists, missing vs JSON/schema | ConvenienceTests и Console.Beginner: FILE_NOT_FOUND, FILE_ACCESS/FILE_IO, JSON_SYNTAX/JSON_ENCODING, FORMAT_SCHEMA/FORMAT_VERSION; UTF8/BOM, исчезновение после Exists |
| D/E/F/G: блок/поле, null/missing/type, typed read/update/save | ConvenienceTests: ordinal/case, null/false/0/empty string, empty object/scalar/array, TryGet missing=false/type=exception, TryRead diagnostic, context path, repeated load |
| H/I: LINQ + multi-type/spec versions | QueryTests/QueryLifecycleTests: OR types, AND ID, replacement, duplicate/empty/case/limit/order, immutable builder/projection, strict v1/v2 round-trip |
| J: общий registry/AOT | SerializerReuseTests и Unity shared registry test/player: изолированные таблицы, существующие handles, explicit generic DTO; DLL не зависит от Unity |
| K: stale handle | Remove/Add с прежним ID, BLOCK_REMOVED на Get/Set/HasField/refs; immutable Data snapshots остаются валидны; cache Add/Remove invalidation |
| L: beginner route/Wiki fallback | 15 docs/wiki pages, Home/Sidebar, 8 runnable recipes, 14 FAQ, RU/EN README и English quickstart, console + MonoBehaviour/TextAsset sample |
| M: действительные проверки | 109/109 .NET, 25 snippets, package consumer, 12/12 каждого Editor, IL2CPP runtime, hosted Windows/Ubuntu success |
| N: границы Core | bridgeVersion1 сохранён; нет Runtime/MCP/AI/PLC/симуляционного исполнения |

Документы v1 сохраняют неизвестные поля и точные числовые лексемы (существующие AcceptanceTests плюс новые file tests). QuerySpec v1 остаётся строгим: неизвестные операции отвергаются, а не теряются. Сериализованные single-type specs используют прежний v1, multiple/empty types используют v2.

SaveAtomic: сериализация перед IO, случайный same-directory temp, Flush(true), File.Replace либо File.Move, без удаления destination перед переносом. Проверены Windows locks/read-only и cleanup, serialization failure до IO, сохранность исходного файла; Linux реализация прошла в hosted suite. OS-специфический read-only test в Linux не выполняет Windows assertions (в test suite ранний return, не отдельный skipped test). Универсальная power-loss durability не обещается.

## Совместимость и ограничения

- Все публичные v0.1 stream/block entry points сохранены. UseSerializers выбран вместо двусмысленных новых Parse/Load overloads. Registry/table изолированы, но состояние delegate closures не копируется; configure-before-use, stateless converters или собственная синхронизация.
- Document и mutable registry не thread-safe. Blocks — readonly snapshot состава с живыми handles; BridgeValue/projections — detached immutable values. После Remove старый handle больше не изменяет документ, Id/SchemaVersion остаются историческими.
- HasField — прямой literal key. Nested selection API не реализован; docs/query-v2-proposal.md фиксирует RFC следующей версии без eval/JsonPath. Производительность: измерения 1k/10k/50k в отчёте подэтапа3; legacy getter replay и новый getter измерены в одном процессе с прогревом. Это не SLA и не заявление об ускорении всего pipeline; JSON snapshot-копирование остаётся.
- File.Exists — моментальная системная проверка без JSON validation. Файл может исчезнуть перед Load. File facade не заменяет bounded refs resolver. Resolver требует стабильный доверенный root; portable managed API не обещает защиты от враждебной конкурентной подмены каталогов.
- Wiki включена, но публикация требует создания первой Home через авторизованный GitHub UI. Sync-Wiki -Publish был реально запущен и остановлен до изменений remote с Repository not found. В docs/wiki-sync.md точные шаги; README пока ведёт на docs/wiki. Неизвестные чужие Wiki pages скрипт не перезаписывает/не удаляет.
- Unity7/другие target runtimes не проверены; успешный alpha6000.7 test не обещает будущие finalверсии. UPM Git URL указывает feature-ветку, потому что main пока v0.1; package.json проверен по опубликованному path.
- Credential-pattern scan ограничен известными форматами и не является полной security audit. Release/tag/NuGet publish не выполнялись.

## Воспроизведение

```powershell
dotnet restore Bridge.sln
dotnet build Bridge.sln -c Release --no-restore
dotnet test Bridge.sln -c Release --no-build
dotnet run --project samples/Bridge.Console.Beginner -c Release --no-build
dotnet run --project samples/Bridge.Console.Sample -c Release --no-build -- --performance
./tools/Verify-Snippets.ps1
dotnet pack src/Bridge.Core/Bridge.Core.csproj -c Release --no-build -o work/packages
./tools/Verify-Package.ps1
./tools/Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -Mode EditMode
./tools/Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.7.0a2\Editor\Unity.exe' -Mode EditMode
./tools/Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -Mode WebGLBuild
python -m http.server 8765 --bind 127.0.0.1 --directory work/unity/webgl-smoke
```

Откройте localhost:8765 в WebGL2 browser и проверьте DOM/console PASS и beginner log. Clean clone проверки выполнен на source commit c6b3926 в work/clean-v02 с отдельным restore/build/test/sample. Для Wiki отдельно следуйте docs/wiki-sync.md; обычная проверка не публикует страницы.
