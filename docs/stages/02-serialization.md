# Этап 2 — завершён

Добавлены Create/GetBlock/TryGetBlock/AddBlock/RemoveBlock/SetBlockData, Get/Set/Deserialize, детерминированные ToJson и UTF-8 Save(Stream), неизменяемые JSON snapshots и явные DTO-конвертеры BridgeSerializer.Register<T>. Конвертеры используют только заранее скомпилированный код. Отдельного reflection serializer нет; сложный DTO требует регистрации. Расширения документа/блоков сохраняются; ключи упорядочены ordinal, порядок массивов сохранён.

PASS: dotnet test Bridge.sln -c Release --logger trx --results-directory work/test-results/stage2 — 47/47, 0 skipped. PASS: dotnet run --project samples/Bridge.Console.Sample -c Release — настройка изменена, DTO применён явно, Save/Load дал speed=3. Тесты проверяют Unicode, null/scalar/array/nested arrays, числа вне double, extensions, точные имена полей, ru-RU, no-BOM UTF-8, отсутствие скрытой мутации и NaN/Infinity.

Исправлена обнаруженная тестом ошибка диагностики несовместимого string/bool: теперь VALUE_TYPE вместо DTO_CONVERTER_MISSING. Файловая запись производится вызывающим кодом через Stream; Core не выбирает пути.

Git: feature/02-serialization; implementation 91fa261bf1baa0ffe0a59ade0758e73f57ff38fe; git status --short после implementation commit пуст; PR отсутствует (локальный режим). git log -n 3 --oneline:

91fa261 feat(core): support block mutations typed values and deterministic serialization
4f87b8f docs(core): record stage one validation and parser decisions
4d97e19 feat(core): add Bridge JSON v1 document model and strict parser

Следующий этап: LINQ/fluent query после 47/47 PASS.
