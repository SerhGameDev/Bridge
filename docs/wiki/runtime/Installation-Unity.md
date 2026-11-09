<!-- Bridge managed wiki source -->
# Установка Runtime в Unity

Проверен Unity6000.6.7f1.6000.7.0a2 — alpha Unity6, не Unity7. Runtime/Core — netstandard2.1, MCP SDK в Unity не устанавливается.

Package Manager → Add package from git URL: установите **сначала Core, затем Runtime, затем Unity adapter**, поскольку version-only dependency не опубликована в registry:

```text
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.core
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.runtime
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.unity
```

1. После установки выберите Bridge Unity → Samples → Scene Demo → Import.
2. **Bridge → Create Runtime Demo Scene** создаст scene, Cube prefab и Bridge TextAsset. Никакие настройки scene вручную не пропущены.
3. Откройте созданную сцену, включите Self Test у Bridge Runtime и нажмите Play. Ожидается BRIDGE_RUNTIME_PLAYER_PASS в Console.
4. Для обычной работы Self Test выключите: он создаёт и меняет объекты. Сеть выключена; для готового Windows Player нужны explicit flag и credential.
5. Ваш bootstrap закрывает host/dispatcher в OnDestroy. Собственные reflected methods сохраняйте Unity Preserve/link.xml либо используйте typed registrar и explicit codecs.

Sample — код демонстрации; UnityBridgeDispatcher — adapter; сам BridgeHost относится к Runtime. [Подробный owner-thread guide и команды build/tests](../../runtime/unity.md). Там же воспроизводимые EditMode/PlayMode, Windows Mono Player и отдельный WebGL IL2CPP High smoke. Standalone Windows IL2CPP module в проверенном окружении отсутствовал; успешный WebGL не означает проверку всех AOT targets.

Для воспроизводимого Git install после финальной интеграции используйте commit SHA вместо moving default branch. Перед merge Runtime находится в feature/bridge-runtime-v0.1; при установке до принятия main добавьте `#feature/bridge-runtime-v0.1` к каждому URL. [Готовый Player и MCP](Codex-MCP.md).
