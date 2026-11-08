# Unity / AOT compatibility

## Фактические проверки, 2026-11-08

| Среда | Проверка | Результат |
| --- | --- | --- |
| Windows, .NET SDK 8.0.408, NuGet Newtonsoft.Json 13.0.4 | netstandard2.1, unit tests, console | PASS; подробности в отчётах этапов |
| Unity 6000.6.7f1 (b22767f04f04) | Local UPM import, 10 EditMode NUnit tests | PASS 10/10, 0 skipped |
| Unity 6000.7.0a2 (83605ec097c0) | Local UPM import, 10 EditMode NUnit tests | PASS 10/10, 0 skipped; alpha редактор |
| Unity 7 (7000.x) | Editor / player | SKIPPED: редактор не установлен |
| Unity 6000.6.7f1 WebGL IL2CPP, High stripping | Build + player execution | PASS: Build exit 0; браузер WebGL 2.0, BRIDGE_SMOKE_PASS (повторный запуск 2026-11-08 22:07 MSK) |

Проверенные номера приведены точно по executable metadata. Успешная alpha-проверка не обещает совместимость с будущими final версиями. Unity 7 не заявляется проверенным.

## Зависимость JSON

UPM использует официальный `com.unity.nuget.newtonsoft-json` **3.2.2**. В реально импортированном пакете DLL имеет FileVersion **13.0.2**, ProductVersion `13.0.2+cf0a246981c33ae00121dfe58b850aefeac1aac0`. NuGet путь отдельно проверен с **13.0.4**. DLL не копируются в репозиторий. `Bridge.Core.asmdef` использует explicit precompiled reference `Newtonsoft.Json.dll` и `noEngineReferences: true`. EditMode тест проверяет реальные assembly references Core на отсутствие UnityEngine.

## Один источник кода

Код находится в `Packages/com.serhgamedev.bridge.core/Runtime`. `src/Bridge.Core/Bridge.Core.csproj` компилирует эти же файлы как linked Compile items. Unity компилирует их по asmdef. Папка `Samples~` содержит Unity-зависимый пример отдельно от Core; импорт sample обычным способом создаёт копию только примера.

## Установка и запуск проверок

1. В Package Manager выберите Add package from disk и `Packages/com.serhgamedev.bridge.core/package.json` этого checkout. Для manifest dependency используйте `file:` с локальным абсолютным путём. Git URL пока не опубликован.
2. Добавьте `"testables": ["com.serhgamedev.bridge.core"]` в manifest host-проекта. Запустите `Bridge.Core.EditorTests` в EditMode Test Runner.
3. Для отдельного чистого host-проекта выполните из корня репозитория:

```powershell
.\tools\Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -Mode EditMode
.\tools\Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.7.0a2\Editor\Unity.exe' -Mode EditMode
```

Helper создаёт host в игнорируемой `work/unity/<version>`, подключает локальный пакет без копирования Core, импортирует sample и пишет XML/логи. Наличие `Passed` и числа тестов обязательно; старый XML удаляется перед запуском.

## AOT и stripping

Core не использует `dynamic`, Reflection.Emit, expression compilation или reflection serializer. Для каждого пользовательского DTO зарегистрируйте пару типизированных функций через `document.Serializers.Register<T>(read, write)`. Сериализатор принадлежит документу; при повторной загрузке регистрацию следует выполнить заново. Конструктор и члены DTO вызываются из обычного C# кода, что даёт IL2CPP статические ссылки. Если DTO/конвертеры достигаются только через внешнюю регистрацию, добавьте `[UnityEngine.Scripting.Preserve]` или собственный `link.xml` в приложение; Core не может сохранять неизвестные пользовательские типы автоматически. Пример использует `[Preserve]` и явный generic вызов.

Для ручной проверки импортируйте Basic Usage and AOT Smoke, создайте пустую сцену и соберите player с IL2CPP и Managed Stripping Level High. Пример при старте проверяет явный DTO, отсутствие скрытой мутации, JSON/UTF-8 round-trip, LINQ, projection, точные числа и in-memory reference; ожидаемый результат `BRIDGE_SMOKE_PASS` в player log. Для WebGL он также пишет результат в DOM `#bridge-smoke-result`.

```powershell
.\tools\Verify-Unity.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -Mode WebGLBuild
python -m http.server 8765 --bind 127.0.0.1 --directory work/unity/webgl-smoke
```

Откройте `http://127.0.0.1:8765/` в WebGL-совместимом браузере. Builder задаёт IL2CPP, High stripping и отключает WebGL compression, чтобы стандартный локальный HTTP server обслуживал артефакты без дополнительных MIME/Content-Encoding настроек. Build artifacts не входят в Git.


