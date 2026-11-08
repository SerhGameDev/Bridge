# Changelog

## 0.1.0 — локальный кандидат, не опубликован

- Строгий Bridge JSON v1, диагностика/лимиты, точные числа и сохранение расширений.
- netstandard2.1 API: загрузка, явный CRUD, immutable значения, DTO converters, UTF-8 deterministic Save.
- LINQ и BridgeQuerySpec/fluent с immutable projections и выборкой полей.
- Именованные ссылки, in-memory/file resolvers с ограничениями путей/графа/кеша.
- Единый исходный код .NET/Unity UPM, EditMode NUnit tests и реально выполненный WebGL IL2CPP smoke с High stripping.
- Console Sample, RU/EN quickstart, CI restore/build/test/pack.

Первый кандидат; прежнего публичного API нет. Unity 7 и другие IL2CPP targets не проверены. Ограничения файлового resolver описаны в docs/references.md.
