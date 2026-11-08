# AOT / IL2CPP

Core не использует dynamic, Reflection.Emit, Expression.Compile, JIT-only генерацию или reflection serializer для DTO. Встроенное typed чтение ограничено JSON-примитивами и BridgeValue. DTO используют BridgeSerializer.Register<T> с парой функций; полный пример — в [API](api.md).

Регистрация принадлежит документу и выполняется снова после загрузки. Не создавайте CLR-типы по строковым именам из недоверенного JSON. Core не интерпретирует `$type` и не включает TypeNameHandling.

Конвертер должен статически обращаться к конструктору и членам DTO. Sample также помечает DTO `[UnityEngine.Scripting.Preserve]`. Если приложение подключает типы только косвенно, оно отвечает за Preserve/link.xml. Core не может автоматически сохранять неизвестные пользовательские типы. Unity-атрибуты остаются в приложении/sample.

Фактически проверен WebGL IL2CPP c High stripping: registered DTO, JSON round-trip, запросы и in-memory reference. Точные версии/команды — в [unity-compatibility.md](unity-compatibility.md). Другие IL2CPP targets и Unity 7 не проверены; runnable sample подготовлен для их проверки.
