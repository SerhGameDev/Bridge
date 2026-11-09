<!-- Bridge.Core managed wiki source -->
# Ошибки и диагностика

Проверяйте Result.Success перед Result.Value. Load/Parse/Get/Set бросают BridgeException для ошибок данных; TryLoad/TryParse/TryRead/TryExecute возвращают тот же набор диагностик. Bool TryGet имеет более узкий смысл: false только missing; при mismatch он бросает. ArgumentException означает некорректный вызов API, а не повреждённый документ.

```csharp
using System;
using Bridge.Core;
var result = BridgeDocument.TryParse("{BROKEN", source: "user.bridge.json");
if (result.Success) throw new Exception("Expected failure");
var error = result.Diagnostics[0];
if (error.Code != "JSON_SYNTAX" || error.Source != "user.bridge.json") throw new Exception("Context");
Console.WriteLine(error.Code + ": " + error.Source);
```

Ожидается JSON_SYNTAX: user.bridge.json. Диагностика содержит Code, Message, Severity, DocumentId, Source, Path и известные Line/Column. У ошибок мутации Path — JSON Pointer к данным блока; отдельного свойства BlockId нет, ID блока содержится в пути и сообщении. Parser path может иметь синтаксис Newtonsoft. Координаты не выдумываются для программно созданных значений.

| Код | Что проверить |
| --- | --- |
| FILE_NOT_FOUND | Существует ли файл/каталог после Exists |
| FILE_ACCESS / FILE_IO / DOCUMENT_IO | Права, блокировка и доступность диска/потока |
| JSON_SYNTAX / JSON_ENCODING | Валидный JSON и UTF-8; без комментариев/NaN |
| FORMAT_SCHEMA / FORMAT_VERSION | Обязательные поля и bridgeVersion=1 |
| BLOCK_NOT_FOUND / FIELD_NOT_FOUND | Регистр, ID блока, прямое поле data |
| VALUE_TYPE | JSON kind, null и диапазон CLR-типа |
| DTO_CONVERTER_MISSING / DTO_CONVERSION | Явная регистрация и код converter-а |
| BLOCK_REMOVED | Получите новый handle после удаления/добавления |
| QUERY_SPEC / QUERY_FIELD_NOT_FOUND / QUERY_DATA_KIND | Версия spec, поля и kind выбранных блоков |
| INPUT_LIMIT | Размер/глубина/число блоков/длина числа |
| REFERENCE_PATH / REFERENCE_CYCLE | Разрешённые относительные пути и граф ссылок |

Если Unity сообщает duplicate Newtonsoft assemblies, удалите лишний пользовательский DLL и используйте зависимость UPM. Если DTO пуст, помните: изменения DTO применяются только явным SetBlockData. [FAQ](FAQ.md). [Начало](Home.md).
