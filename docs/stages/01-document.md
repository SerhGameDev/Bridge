# Этап 1 — завершён

Реализованы BridgeDocument, BridgeBlock, неизменяемый BridgeValue, BridgeDiagnostic/BridgeResult/BridgeException, BridgeReadOptions и строковая/потоковая загрузка. Parse/Load бросают BridgeException при ошибках данных; TryParse/TryLoad возвращают Result. Ошибки аргументов остаются ArgumentException. Поток принадлежит вызывающему коду.

PASS: dotnet test Bridge.sln -c Release --logger trx --results-directory work/test-results/stage1 — 38/38, 0 skipped. netstandard2.1 собран. Первый прогон выявил несовместимость Raw-токена с JToken.Load; адаптер исправлен: Newtonsoft разбирает структуру, затем исходные числовые лексемы восстанавливаются без округления.

Лимиты: 16 MiB UTF-8, глубина 64, 100000 блоков, 4096 символов на число; настраиваемые. Newtonsoft.Json NuGet 13.0.4 фактически восстановлен/собран. Unity/IL2CPP пока не проверены.

Git: feature/01-document; implementation 4d97e196aa9d8706baaa772f7786c30522ab3696; git status --short после implementation commit пуст. PR отсутствует по указанию работать локально. Последние коммиты на момент фиксации реализации:

4d97e19 feat(core): add Bridge JSON v1 document model and strict parser
8c38259 docs(core): define Bridge JSON v1 contract
0cef4ae chore(repo): initialize independent Bridge.Core repository

Дальше: этап 2, CRUD/DTO/запись, только после прохождения тестов этапа 1 (38/38 PASS).
