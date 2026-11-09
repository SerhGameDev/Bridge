# Changelog

## Bridge.Runtime0.1 — source candidate, без NuGet/Release/tag

- netstandard2.1 host/builder, explicit command attributes, schemas/binding, typed registrar и DTO/enum codecs.
- Default-deny data policy, Core QuerySpec v1/v2, discovery, fresh snapshots, sanitized results, bounded async dispatcher/cancel/audit.
- Тонкий Unity UPM dispatcher, реальная Cube Scene Demo, EditMode/PlayMode, Windows Mono Player и WebGL IL2CPP High runtime PASS.
- Отдельный .NET8 MCP gateway SDK2.4.0, protocol versions2026-07-28/2025-11-25/2025-06-18, реальный built Player compatible-client E2E.
- Beginner Runtime route,12recipes, runnable snippets, local NuGet consumer checks, safe recursive Wiki preview. Core contract/bridgeVersion=1 сохранены.

## Core0.2.0 — исходники приняты через PR1, NuGet/Release/tag не опубликованы

- BridgeFile.Exists/Load/TryLoad/Save/SaveAtomic/TrySaveAtomic и различимые ошибки файла/JSON/схемы.
- ContainsBlock/HasField и document-level Get/Set/TryGet/TryRead; null/missing/type mismatch документированы.
- UseSerializers и Clone для повторно используемых явных AOT-safe конвертеров с изоляцией таблиц документов.
- WhereTypes, строгий QuerySpec v2 для multiple/empty types, сохранение v1 для старых запросов.
- BLOCK_REMOVED для устаревших handles; кэш ordinal Blocks с инвалидацией Add/Remove.
- Beginner console/Unity TextAsset samples; RU/EN вход, 15 Wiki source pages, 25 исполняемых snippets, проверка JSON/Markdown links.
- Публичный GitHub репозиторий подключён; итоговые фактические проверки и ограничения в docs/verification-report-v0.2.md.

Миграция: старые stream и block API сохранены. После удаления используйте новый handle; изменения старого теперь явно запрещены. Для общего registry применяйте UseSerializers после Parse/Load(Stream) или именованный serializers у BridgeFile.Load. Документ/изменяемый registry не thread-safe. Библиотека сохраняет bridgeVersion=1.

## 0.1.0 — локальный кандидат, не опубликован

- Строгий Bridge JSON v1, диагностика/лимиты, точные числа и сохранение расширений.
- netstandard2.1 API: загрузка, явный CRUD, immutable значения, DTO converters, UTF-8 deterministic Save.
- LINQ и BridgeQuerySpec/fluent с immutable projections и выборкой полей.
- Именованные ссылки, in-memory/file resolvers с ограничениями путей/графа/кеша.
- Единый исходный код .NET/Unity UPM, EditMode NUnit tests и реально выполненный WebGL IL2CPP smoke с High stripping.
- Console Sample, RU/EN quickstart, CI restore/build/test/pack.

Первый кандидат; прежнего публичного API нет. Unity 7 и другие IL2CPP targets не проверены. Ограничения файлового resolver описаны в docs/references.md.
