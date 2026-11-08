# Предложение v0.1.0

Статус: локальный кандидат. NuGet публикация, GitHub repository/release и tag ещё не созданы.

## Содержание

- Bridge JSON v1 с блоками, type/schemaVersion, data любого JSON kind и refs-метаданными.
- Строгий JSON/схема, ограниченное чтение, диагностические Result/Try API и throwing convenience API.
- Явный CRUD, DTO-конвертеры без runtime генерации, immutable snapshots, детерминированная UTF-8 запись.
- LINQ и сериализуемый fluent query с выборкой полей.
- In-memory и ограниченный file resolver, графовые лимиты, operation cache и диагностика циклов.
- Один Runtime исходник для netstandard2.1/Unity UPM, console/Unity samples, tests/docs/CI.

Это первый публичный API; изменений относительно прежнего публичного релиза нет. Смена формата/QuerySpec в будущем потребует явной политики версий. Независимый verification report должен сопровождать кандидат.

## Известные ограничения

Unity 7 не установлен и не проверен. Проверены только точные Editor/IL2CPP targets из unity-compatibility.md; alpha Editor не обещает совместимость со всеми будущими версиями. File resolver требует стабильного доверенного root, без adversarial concurrent directory replacement; ограничения описаны в references.md. DTO требуют явной регистрации в каждом загруженном документе. Непарные Unicode surrogate escapes отклоняются без тихой замены символа. CI настроен, удалённого GitHub run не было; Linux execution пока не подтверждён.

## После отдельной команды владельца

1. Подключить согласованный публичный репозиторий Bridge, проверить владельца и доступы, отправить локальные ветки без force push; настроить main protection при наличии прав.
2. Выполнить remote CI и добавить фактические статусы/ссылки в verification report. Проверить нужные владельцу дополнительные Unity targets.
3. Согласовать доступность NuGet PackageId и лицензионные метаданные, повторить pack/consumer smoke на утверждённом commit.
4. После явного разрешения создать tag v0.1.0, GitHub release и опубликовать согласованный пакет. До этого локальный .nupkg пригоден для проверки через local source.
