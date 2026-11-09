<!-- Bridge.Core managed wiki source -->
# Первый пример Unity

Для v0.2 откройте Window → Package Manager → + → Install package from git URL и укажите:

```text
https://github.com/SerhGameDev/Bridge.git?path=/Packages/com.serhgamedev.bridge.core#feature/core-v0.2-developer-experience
```

Файл package.json находится по указанному пути в feature-ветке; main пока содержит v0.1. После интеграции v0.2 можно использовать #main. Версия пакета обновляется локально без опубликованного тега. Зависимость Newtonsoft устанавливается официальным пакетом Unity, не копируйте случайный Newtonsoft DLL в Assets. Альтернатива: Add package from disk и package.json из checkout.

В Package Manager импортируйте sample **Beginner TextAsset**. Создайте сцену, пустой GameObject и добавьте компонент BridgeBeginner из sample. Создайте файл factory.bridge.json с примером из [формата](JSON-Format.md), импортируйте его как TextAsset и назначьте полю Source. Если Unity не распознаёт расширение, используйте factory.bridge.json.txt. Play выведет `BRIDGE_BEGINNER_PASS: speed=2`. Компонент читает TextAsset.text через Parse, поэтому не требует файловой системы.

Исходник MonoBehaviour находится в Samples~/Beginner/BridgeBeginner.cs. Он проверяет наличие блока и поля, меняет speed и проверяет выборку. Код Unity не входит в ядро. Для IO в player выбирайте разрешённый платформой каталог, например Application.persistentDataPath; StreamingAssets/WebGL и Addressables требуют отдельной загрузки приложения.

Для AOT импортируйте Basic Usage and AOT Smoke. Соберите сцену с IL2CPP и Managed Stripping High и запустите player. Ожидается BRIDGE_SMOKE_PASS. Конкретные редакторы и реальные результаты будут в verification-report-v0.2, гарантии Unity 7 без установленного редактора не заявляются. [DTO/AOT](DTO-and-AOT.md). [Начало](Home.md).
