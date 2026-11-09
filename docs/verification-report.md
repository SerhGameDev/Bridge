# Итоговая проверка Bridge.Core v0.1.0 (локальный кандидат)

Дата: 2026-11-08, Europe/Moscow. Репозиторий: C:\GitHub\Bridge. Все этапы выполнялись последовательно, каждый следующий начинался после проверок и локальной fast-forward интеграции предыдущего. Этапы 0–7 завершены в доступном окружении; недоступные платформы обозначены явно. На дату этого исторического отчёта публикаций не было. Обновление 2026-11-09: репозиторий опубликован в https://github.com/SerhGameDev/Bridge; текущая работа v0.2 описана в verification-report-v0.2.md. Утверждения о локальном Git ниже относятся к проверке v0.1 до публикации.

## Окружение

- RuntimeInformation.OSDescription: Microsoft Windows 10.0.22631, X64.
- Git 2.45.1.windows.1; .NET SDK 8.0.408, VSTest 17.11.1. Core: netstandard2.1/C# 9; test/sample: net8.0.
- Newtonsoft.Json NuGet 13.0.4; официальный Unity package 3.2.2, DLL FileVersion 13.0.2, ProductVersion 13.0.2+cf0a246981c33ae00121dfe58b850aefeac1aac0.
- Unity 6000.6.7f1 (b22767f04f04), Unity 6000.7.0a2 (83605ec097c0). Unity 7 не установлен. GitHub CLI отсутствует; Git по команде владельца локальный.

## Результаты после исправлений

| Проверка | Результат |
| --- | --- |
| .NET xUnit, основной checkout | PASS 92/92, 0 failed, 0 skipped |
| Чистый локальный clone, restore/build/test | PASS 92/92, build 0 warnings/0 errors |
| Все C# fences README.md/README.ru.md/docs | PASS 9/9, отдельная compilation/execution каждого |
| Console Sample | PASS: CRUD/DTO/UTF-8, LINQ/fluent, внешняя палитра и sandbox escape |
| .NET pack и независимое приложение с PackageReference | PASS: LOCAL_NUGET_PACKAGE_PASS; source mapping принудительно выбирает локальный Bridge.Core |
| Unity 6000.6.7f1 EditMode | PASS 10/10, 0 skipped |
| Unity 6000.7.0a2 EditMode | PASS 10/10, 0 skipped |
| WebGL IL2CPP, Unity 6000.6.7f1, stripping High | PASS build exit 0 + повторное выполнение исправленного player |
| WebGL runtime DOM/console | BRIDGE_SMOKE_PASS, 2026-11-08 19:07:12.789 UTC / 22:07 MSK |
| Runtime assembly boundaries | PASS: нет UnityEngine/UnityEditor/Bridge.Runtime/MCP references |
| Tracked artifacts / история больших blobs | PASS: нет bin/obj/Library/work/DLL/EXE/WASM/NUPKG в Git; ни одного blob >1 MiB во всех refs |
| Известные credential patterns в tracked файлах | PASS: private keys, GitHub token и AWS access key patterns не обнаружены |
| Hosted GitHub CI / Linux execution | SKIPPED: локальный режим, workflow настроен, удалённого запуска не было |
| Unity 7 / standalone IL2CPP | SKIPPED: Unity 7 и Windows standalone IL2CPP module отсутствуют; WebGL IL2CPP реально проверен |

Сканирование шаблонов секретов не заменяет будущую проверку перед публичной публикацией. NuGet ZIP проверен: netstandard2.1 Bridge.Core.dll/XML, README.md, MIT LICENSE, nuspec; Newtonsoft DLL, тесты и build output в пакет не копируются.

## Acceptance matrix

1. **JSON и схема:** DocumentTests/AcceptanceTests проверяют valid/invalid/trailing data, duplicate decoded keys/ID, обязательные поля и типы, unknown version, Unicode/escapes/null, большие целые/дроби/экспоненты, encoding/BOM, размер/глубину/число блоков/длину числа. 100 детерминированно сгенерированных вложенных JSON-значений сверены независимым System.Text.Json; malformed corpus отклоняется обоими parser.
2. **CRUD и DTO:** MutationTests проверяют add/remove/update, exact case-sensitive ID и field names, отличия missing/null/type error, readonly value snapshots, registered DTO round-trip, отсутствие скрытой мутации и NaN/Infinity. Неизвестные поля документа/блоков/refs сохраняются.
3. **Запросы:** QueryTests/AcceptanceTests проверяют System.Linq, фильтры type+ID, ordering/limit/empty results, compositions, serializable specs, exact field names, missing fields, все non-object kinds, immutable projections и отсутствие исключённых данных/произвольных expression operations.
4. **Ссылки:** ReferenceTests проверяют A->B, local, self, A->B->A, missing doc/block/ref, absolute/URL/UNC/ADS/расширения/устройства/выход за root, depth/files/total size/edge limits, duplicate loads, fresh operation cache и recovery. Настоящий Windows junction и junction-root отвергнуты.
5. **Сериализация:** UTF-8 без BOM, invariant culture ru-RU, nested objects/arrays/arrays of arrays/scalars/null, semantic round-trip, deterministic ordinal keys и пользовательский порядок массивов подтверждены.
6. **Совместимость:** netstandard2.1 без Unity dependencies, .NET console и local NuGet consumer; один Runtime source .NET/UPM; реальные NUnit и IL2CPP WebGL с явно зарегистрированным DTO и Preserve/High stripping.
7. **Документы/CI:** RU/EN quickstart и API/query/ref/AOT/future guides описывают фактический API. 9 snippets компилируются/исполняются. Workflow содержит restore/build/test/sample/snippets/pack/package consumer; Unity не выдаётся за hosted CI.

Performance smoke этапа 3: 1000 blocks -> 500 results, 8.037 ms/~1.82 MB allocations; 10000 -> 5000, 68.035 ms/~18.17 MB. Один наблюдательный прогон, без аппаратного SLA.

## Команды воспроизведения

Из корня репозитория:

```powershell
dotnet restore Bridge.sln
dotnet build Bridge.sln -c Release --no-restore
dotnet test Bridge.sln -c Release --logger 'trx;LogFileName=stage7.trx' --results-directory work/test-results/stage7
.\tools\Verify-Snippets.ps1
dotnet run --project samples/Bridge.Console.Sample -c Release -- --performance
dotnet pack src/Bridge.Core/Bridge.Core.csproj -c Release --no-build -o work/packages
.\tools\Verify-Package.ps1
.\tools\Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -Mode EditMode
.\tools\Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.7.0a2\Editor\Unity.exe' -Mode EditMode
.\tools\Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -Mode WebGLBuild
python -m http.server 8765 --bind 127.0.0.1 --directory work/unity/webgl-smoke
```

После HTTP server откройте localhost:8765 и убедитесь в BRIDGE_SMOKE_PASS. При новой сборке reload обязателен. Фактически выполнен fresh reload и прочитаны новый DOM/console сигнал с отметкой 22:07 MSK. Логи/XML/WASM и наблюдение `il2cpp-runtime-final.json` оставлены в игнорируемой work/unity, а не в Git.

Чистая проверка выполнена через `git clone --no-hardlinks --branch feature/07-verification C:\GitHub\Bridge work/clean-checkout` на commit 18d6e98af33afc6a48bd2789186970b4e9cec75b. Внутри clone отдельно выполнены restore/build/test/sample/pack/Verify-Package; git status --short остался пуст. Package consumer не использовал ProjectReference, а восстановил локальный nupkg и Newtonsoft через отдельный package cache/source mapping.

## Исправленные замечания финальной проверки

- Ошибки Get/Set/Deserialize/SetBlockData теперь включают известные DocumentId/Source и JSON Pointer к данным блока.
- Необходимость зарегистрировать DTO теперь диагностируется DTO_CONVERTER_MISSING независимо от JSON kind.
- Непарные Unicode surrogates раньше заменялись Newtonsoft на U+FFFD; теперь они явно отвергаются, корректные пары сохраняются. Writer использует Unicode escapes и проверяет Unicode строк/ключей.
- Все эти изменения покрыты .NET и Unity regression; IL2CPP player после них пересобран и выполнен заново.

Первоначальные неуспешные прогоны обнаруживали ошибки и не считались успешными: stage 1 Raw reader adapter, stage 2 неправильный код ошибки string conversion, stage 7 Unicode replacement. Финальные прогоны: 0 failed.

## Известные ограничения

- Unity 7 и другие IL2CPP targets не проверены; 6000.7.0a2 — alpha. Runnable smoke и ручные инструкции подготовлены.
- File resolver рассчитан на стабильный доверенный root и запрещает symlinks/reparse points. Portable managed API не обеспечивает атомарной защиты при враждебной конкурентной подмене каталогов; нужен собственный handle-based resolver/защита root для такого threat model.
- DTO требуют явной регистрации в каждом загруженном документе. Registry не сериализуется; reflection serializer отсутствует.
- Числа хранятся точно, typed conversion имеет диапазон/точность CLR-типа. Ввод ограничен read options, непарные surrogate escapes отвергаются. Детерминированная запись не является RFC 8785 normalization.
- GitHub CI пока только настроен. Main branch protection не установлен и не заявляется. Пакет/релиз не опубликован.

## Git и этапы

Каждый этап имел отдельную feature-ветку и отчёт в docs/stages. Реализации до документационного финала:

| Этап | Ветка | Содержательные коммиты |
| --- | --- | --- |
| 0 | main + feature/00-foundation | 0cef4ae11dd6eb99ad4587bf4837edea1adb8c15; 8c382597455396e5bcb942570f9cf4884110570d |
| 1 | feature/01-document | 4d97e196aa9d8706baaa772f7786c30522ab3696 |
| 2 | feature/02-serialization | 91fa261bf1baa0ffe0a59ade0758e73f57ff38fe |
| 3 | feature/03-query | 7b8df7002096d2c1521e907f923a6a1a5aeba56b |
| 4 | feature/04-references | 58929ae578e6385a1c675e96d89307ed914ec9f1 |
| 5 | feature/05-unity | 4a561be92909908ed4383117dcb4e1009975bc05 |
| 6 | feature/06-ci-docs | ac2ede8a11913184d294650904da3cf2c7377921; 50771a843cd479dab48e8fda830fb95d21e242db |
| 7 | feature/07-verification | 6806c6a1d12e0879de9bdc07d8f3ff71000bff39; 18d6e98af33afc6a48bd2789186970b4e9cec75b |

Просмотр: `git show <SHA>`; все отчётные коммиты доступны в `git log --all --oneline`. PR URL отсутствуют, remote не настроен по команде работать локально. После фиксации этого отчёта feature/07-verification включается fast-forward в main; итоговые HEAD/status фиксируются при передаче результата. Предложение публикации: [release-proposal-v0.1.0.md](release-proposal-v0.1.0.md); требует отдельной команды владельца.
