# Bridge.Unity 0.1

Тонкий UPM adapter для Bridge.Runtime: `UnityBridgeDispatcher` исполняет команды и чтение данных из `Update` на Unity owner thread. Core и Runtime не зависят от Unity. MIT.

## Установка и сцена

Установите через Package Manager → Add package from git URL **все три** пакета, сначала Core, затем Runtime, затем Unity (пока пакеты не опубликованы в registry):

```text
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.core
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.runtime
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.unity
```

Импортируйте Samples → Scene Demo и выберите **Bridge → Create Runtime Demo Scene**. Меню создаёт настоящие сцену, Cube prefab и `factory.bridge.json.txt`. Откройте сцену и нажмите Play. Само создание host не включает сеть. Флаг `Self Test` включает проверку spawn/move/delete, разрешений, enum/DTO/typed registrar.

Для своего host:

```csharp
var dispatcher = gameObject.AddComponent<Bridge.Unity.UnityBridgeDispatcher>();
var host = Bridge.Runtime.BridgeHostBuilder.Create("scene")
    .AddCommands(myCommands).UseDispatcher(dispatcher).Build();
// OnDestroy: host.Dispose(); dispatcher.Dispose();
```

Создавайте dispatcher на главном потоке. Не блокируйте `Update` через `.Wait()`/`.Result`. Async handler должен сохранять UnitySynchronizationContext перед обращением к Unity API; не используйте там `ConfigureAwait(false)`. Отмена кооперативная, без rollback: gate занят до фактического окончания handler. Очередь ограничена (`Capacity=64`); синхронная долгая команда задержит кадр. Host не владеет переданным dispatcher по умолчанию.

## Данные и команды демонстрации

`config` — документ с `simulation.settings` (speed/gravity). `scene` — свежий снимок реальных Transform (`scene.cube`: x/y/z), стабильные GUID ID. `privateNote` закрыт. Commands: scene.spawn_cube, scene.move, scene.delete (Edit), simulation.set_speed (Control), scene.thread_check и AOT examples (Read); scene.admin_reset (Admin) закрыт demo editor.

Методы демонстрации помечены Unity `[Preserve]`. DTO имеет явный codec; альтернативный typed registrar также реально вызывается smoke. Пользовательские reflected методы нужно сохранять от stripping самостоятельно. Успешный sample не гарантирует сохранение произвольного пользовательского типа.

## Воспроизведение проверок из checkout

```powershell
./tools/Verify-RuntimeUnity.ps1 -Mode EditMode
./tools/Verify-RuntimeUnity.ps1 -Mode PlayMode
./tools/Verify-RuntimeUnity.ps1 -Mode WindowsBuild
./tools/Verify-RuntimeUnity.ps1 -Mode WebGLBuild
```

По умолчанию Unity 6000.6.7f1; путь переопределяется `-UnityEditor`. Изолированный host и артефакты находятся в `work/runtime-unity/`. Windows Player использует Mono. WebGL — IL2CPP High; откройте сборку через HTTP server и дождитесь `BRIDGE_RUNTIME_PLAYER_PASS` внизу страницы. WebGL не открывает IPC listener и не имеет managed worker threads: запросы smoke всё равно проходят Update.

Windows Player smoke: запустите `Bridge.Runtime.Demo.exe -batchmode -nographics --bridge-self-test --bridge-exit-after-smoke -logFile smoke.log`; ожидаются exit 0 и маркер PASS. Для внешнего клиента задайте случайный `BRIDGE_PLAYER_TOKEN` (минимум 32 символа) через environment и запускайте с `--bridge-listen`. Токен не передавайте в CLI/логи. Не запускайте self-test одновременно с внешним сценарием: он изменяет сцену.

Demo IPC — фиксированный TCP literal loopback 127.0.0.1:19731, максимум 16 connections, deadline 15 s. Это отдельный прикладной протокол, не MCP: magic `BRIDGE/1\n`, big-endian длина, strict UTF-8 JSON, <=64 KiB request/1 MiB response. Токен назначает Edit, отдельный необязательный `BRIDGE_READER_TOKEN` — Read; клиент не выбирает principal/permission. Нет HTTP, Origin или DNS endpoint. Неправильный magic/длина закрывают соединение; неверный токен даёт UNAUTHORIZED. Прикладной listener предназначен для локальной демонстрации, без удалённого bind/TLS. MCP gateway реализуется отдельным .NET приложением.
