# Контроль выполнения Bridge.Core

Этапы выполняются последовательно. Git остаётся локальным по указанию владельца. После проверок этапа его ветка интегрируется в локальный main; следующая ветка начинается от main. Публикация пакетов/релизов не разрешена.

- [x] 0: независимый репозиторий, MIT SershDev/2026, solution, JSON/API контракт, проверка инструментов.
- [x] 1: документ/блок/значение, диагностика, Parse/TryParse/Load/TryLoad, строгая схема, дубли/версии/лимиты, unit-тесты и netstandard2.1.
- [x] 2: CRUD, Get/Set, DTO и явная регистрация, мутабельность, extensions, детерминированные ToJson/Save, round-trip тесты и Console Sample.
- [x] 3: System.Linq, BridgeQuerySpec/fluent, snapshots/projections, полный набор query-тестов, Console Sample, performance smoke 1k/10k.
- [ ] 4: именованные ссылки, in-memory/file resolvers, root sandbox/paths/cycles/depth/files/size/cache/failure recovery, тесты и Console Sample с палитрой.
- [ ] 5: единый исходный код UPM/.NET, package/asmdef, Unity sample/EditMode NUnit, проверка установленных редакторов, фактическая IL2CPP проверка либо явное ограничение и runnable smoke, DTO preservation.
- [ ] 6: RU/EN README и quickstart; формат/API/query/refs/AOT/future docs; CI restore/build/test/pack; CONTRIBUTING/CHANGELOG/LICENSE/examples; реальные проверки.
- [ ] 7: чистая сборка, acceptance matrix, исполняемые quickstart сниппеты, проверка артефактов/секретов/зависимостей, отчёт ОС/SDK/Unity/тесты/Git/ограничения и предложение v0.1.0.

## Матрица acceptance

| Группа | Доказательство |
| --- | --- |
| Строгий JSON, числа, Unicode, дубли, trailing data, лимиты | DocumentTests; дальнейшее расширение на этапах 2/7 |
| Схема, отсутствующие поля, версии | DocumentTests |
| CRUD, DTO, отсутствие тайной мутации | Этап 2 |
| LINQ, фильтры, поля, limit, snapshots, отсутствие произвольных выражений | Этап 3 |
| Локальные/внешние ссылки, циклы, sandbox, лимиты, восстановление | Этап 4 |
| UTF-8, культура, вложенные значения/массивы, extensions, детерминизм | Этап 2 |
| .NET Standard, Console, UPM, Editor, IL2CPP | Этапы 1/5/7 |


