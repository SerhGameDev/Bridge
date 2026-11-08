# Contributing

Core targets netstandard2.1/C# 9. Используйте SDK по global.json. Исходники Runtime находятся только в UPM-пакете и линкуются в csproj. Не добавляйте UnityEngine/Runtime/MCP, выполнение выражений из JSON или генерацию кода для DTO.

Работа идёт по docs/task-checklist.md в отдельных feature-ветках от актуального main, с Conventional Commits. Проверки этапа завершаются до следующего. По текущей команде владельца Git локальный, завершённые ветки включаются в main fast-forward; PR/branch protection пока нет.

Перед интеграцией выполняйте restore, Release build, test и pack, Console Sample и проверку C# snippets документации. Изменения контракта сопровождайте тестами наблюдаемого поведения и документацией. Unity запускается отдельно через tools/Verify-Unity.ps1; фиксируйте точный Editor/target/backend/stripping и результат реального player. Недоступные проверки обозначайте явно.

Не коммитьте bin/obj/Library/Temp/Logs/work, DLL/пакеты, токены и настройки компьютера. Добавляйте Unity .meta к импортируемым файлам. Публикация NuGet, GitHub release и force push требуют отдельного разрешения владельца.
