<!-- Bridge.Core managed wiki source -->
# Файлы

Exists проверяет только системное наличие: он не читает JSON и не гарантирует доступ при последующем открытии. Между Exists и Load файл может исчезнуть. Для проверки читаемого Bridge-документа вызывайте TryLoad.

```csharp
using System;
using System.IO;
using Bridge.Core;

const string path = "read-example.bridge.json";
File.WriteAllText(path, "{BAD");
if (!BridgeFile.Exists(path)) throw new Exception("Presence");
var bad = BridgeFile.TryLoad(path);
if (bad.Success || bad.Diagnostics[0].Code != "JSON_SYNTAX") throw new Exception("Invalid JSON hidden");
File.Delete(path);
var missing = BridgeFile.TryLoad(path);
if (missing.Success || missing.Diagnostics[0].Code != "FILE_NOT_FOUND") throw new Exception("Missing hidden");
Console.WriteLine("FILES_PASS");
```

Ожидается FILES_PASS; пример создаёт и удаляет read-example.bridge.json. В своём приложении выбирайте путь пользователя. Load закрывает собственный поток и сохраняет полный путь в Source. FILE_NOT_FOUND отличается от FILE_ACCESS/FILE_IO и JSON_SYNTAX/FORMAT_SCHEMA/FORMAT_VERSION/JSON_ENCODING. Ошибки чтения уже открытого потока могут иметь прежний DOCUMENT_IO. Некорректный аргумент пути бросает ArgumentException; TryLoad не маскирует программную ошибку.

Save и SaveAtomic используют одну стратегию: сначала сериализовать, затем создать случайный temp в той же директории, Flush(true), закрыть, File.Replace для существующего файла либо File.Move для нового. Удаления destination перед переносом нет; при отказе старый файл сохраняется и temp очищается. Если очистка также не удалась, FILE_TEMP_CLEANUP указывает путь оставшегося temp. TrySaveAtomic возвращает BridgeResult<bool>. Каталог должен существовать.

Нельзя обещать атомарность для любой сетевой/виртуальной FS или durability при выключении питания. Проверяйте возможности целевой ОС/FS. Конкурентное изменение destination другим процессом приложению следует синхронизировать. Save(Stream) остаётся низкоуровневым API и оставляет поток открытым; ответственность за позицию/сохранность такого потока у вызывающего кода. Обычный BridgeFile не ограничивает пути как resolver refs: это отдельные сценарии. [Начало](Home.md).
