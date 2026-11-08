# Bridge JSON v1

Стандартный JSON (RFC 8259), расширение .bridge.json. Newtonsoft разбирает структуру; лексический адаптер запрещает JavaScript-расширения и сохраняет числовые лексемы без double/decimal округления.

```json
{"bridgeVersion":1,"documentId":"demo","blocks":{"settings":{"type":"settings","schemaVersion":1,"data":{"speed":1},"refs":{"local":{"document":"","block":"another"}}},"another":{"type":"value","schemaVersion":1,"data":null}}}
```

| Поле | Правило |
| --- | --- |
| bridgeVersion | Обязательная целочисленная лексема 1. Неизвестная версия: FORMAT_VERSION; неверный тип/отсутствие: FORMAT_SCHEMA. |
| documentId | Строка, не пустая и не целиком whitespace. |
| blocks | Объект ID -> определение блока, пустой объект разрешён. |
| block ID | Имя свойства, не пустое/не whitespace. |
| type | Обязательная строка, не пустая/не whitespace. |
| schemaVersion | Обязательная положительная целочисленная лексема Int32; предметная валидация приложением. |
| data | Обязательное свойство; любой JSON kind, включая null. |
| refs | Опциональный объект name -> reference; name не пустое/не whitespace. reference имеет document:string и непустой block:string. |

Все ID/type/keys/reference names сравниваются ordinal с учётом регистра; нет trim/Unicode normalization. Пробелы вокруг непустых ID сохраняются. В data допустимы пустые имена свойств и любые JSON escapes. Точки/слеши — буквальные символы ключа.

Дубли свойств отвергаются на любой глубине после JSON decode: "a" и "\u0061" — дубль. Неизвестные поля документа/блока/reference сохраняются как расширения; не исполняются и не интерпретируются. Неизвестный bridgeVersion не мигрируется автоматически. schemaVersion не имеет значения по умолчанию при чтении; при AddBlock C# default равен 1.

## Ссылки

refs.document — относительный .bridge.json путь, пустая строка — текущий документ. refs.block — точный ID. Поле $ref внутри data не имеет особого смысла. Parse проверяет форму refs; политика путей и графа применяется при явном ResolveReference. Ссылки не загружаются сами и не заменяются содержимым при Save. См. [references.md](references.md).

## Числа, строки и запись

Комментарии, single quotes, unquoted keys, trailing commas/data, NaN/Infinity, undefined, hex, ведущие нули/плюс и нестандартные числа отвергаются. Даты остаются строками. Большие целые, длинные дроби и экспоненты сохраняют лексему; typed read отдельно ограничен CLR-типом. Load принимает корректный UTF-8 с/без BOM; Parse не принимает BOM как часть строки JSON.

Parse/Save/Parse сохраняет семантику JSON. Пробелы не сохраняются, массивы не переставляются. ToJson/Save рекурсивно сортирует object keys ordinal, использует invariant culture и UTF-8 без BOM. Это не RFC 8785 canonicalization: 1 и 1.0 могут сохранять разные лексемы. Unknown fields также сохраняются/упорядочиваются.

BridgeReadOptions defaults: 16 MiB MaxUtf8Bytes, MaxDepth 64 (включая служебные объекты), MaxBlocks 100000, MaxNumberCharacters 4096. Лимиты настраиваемые и не являются JSON полями; превышение даёт INPUT_LIMIT. Load читает не больше лимита плюс один байт.

Полный sample: [minimal.bridge.json](../samples/contracts/minimal.bridge.json). Палитра для Console Sample лежит в samples/Bridge.Console.Sample/assets.

Строки должны содержать корректные Unicode scalar values: непарные surrogate escapes отвергаются JSON_SYNTAX, чтобы Newtonsoft не заменял их символом U+FFFD. Корректные surrogate pairs поддержаны. Writer экранирует non-ASCII символы; семантика строк сохраняется.
