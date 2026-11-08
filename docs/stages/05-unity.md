# Этап 5 — завершён с ограничением доступных версий

Добавлены UPM package.json (0.1.0), noEngineReferences asmdef с явной Newtonsoft DLL-ссылкой, NUnit EditMode tests, .meta, sample с явной регистрацией DTO и Preserve, runnable WebGL IL2CPP smoke и tools/Verify-Unity.ps1. Core исходники не копируются: .NET/Unity компилируют Runtime.

PASS: Unity 6000.6.7f1 local import, EditMode 8/8. PASS: tools/Verify-Unity.ps1 -UnityEditor '<6000.7.0a2>/Editor/Unity.exe' -Mode EditMode, 8/8. Оба теста CoreHasNoUnityEngineAssemblyReference PASS. Первому редактору переданы эквивалентные CLI -batchmode -nographics -projectPath ... -runTests -testPlatform EditMode -testFilter Bridge.Core.EditorTests -testResults ... -logFile ...

PASS: tools/Verify-Unity.ps1 -UnityEditor '<6000.6.7f1>/Editor/Unity.exe' -Mode WebGLBuild, exit 0. Использован настоящий WebGL IL2CPP toolchain, Managed Stripping Level High. PASS исполнения: player запущен с localhost:8765 через Python HTTP server, браузер создал WebGL 2.0 context и DOM/console показали BRIDGE_SMOKE_PASS в 21:48 MSK 2026-11-08. В smoke проверены DTO, Save/Load, отсутствие скрытой мутации, LINQ, projection, exact numbers и in-memory reference. XML, build logs, wasm и runtime observation JSON находятся только в игнорируемой work/unity.

SKIPPED Unity 7: не установлен. 6000.7.0a2 — alpha, не обещание совместимости со всеми будущими версиями. Windows standalone IL2CPP не проверен: отсутствует standalone IL2CPP module; выполнен WebGL IL2CPP. Фактический Unity Newtonsoft package 3.2.2, DLL FileVersion 13.0.2/ProductVersion 13.0.2+cf0a246981c33ae00121dfe58b850aefeac1aac0. .NET путь проверен с NuGet 13.0.4.

Git: feature/05-unity; implementation 4a561be92909908ed4383117dcb4e1009975bc05; git status --short после implementation commit пуст; PR отсутствует (локально). git log -n 3 --oneline:

4a561be feat(unity): provide source-shared UPM package and verified IL2CPP smoke
9e3de76 docs(core): record resolver limits and sandbox verification
58929ae feat(core): add bounded cross-document references and resolvers

Далее: документация и CI после фактических проверок доступного окружения.
