<!-- Bridge.Core managed wiki source -->
# Формат JSON

Расширение .bridge.json остаётся обычным JSON; bridgeVersion — 1. Минимальный целый документ:

```json
{
  "bridgeVersion": 1,
  "documentId": "factory",
  "blocks": {
    "settings": {
      "type": "simulation.settings",
      "schemaVersion": 1,
      "data": { "speed": 1, "note": null },
      "refs": { "layout": { "document": "", "block": "layout" } }
    },
    "layout": { "type": "scene.layout", "schemaVersion": 1, "data": { "objects": [] } }
  }
}
```

documentId идентифицирует документ; ключи blocks — точные ID, type — назначение данных приложения, schemaVersion — положительная версия схемы приложения. data обязателен и может быть object, array, string, number, boolean или null. refs — необязательные именованные ссылки; пустой document означает этот же документ. Блок layout здесь существует, ссылка разрешается только явным вызовом приложения.

Неизвестные поля сохраняются при round-trip документа. Числовые лексемы сохраняются точно; Get<double> ограничен точностью double, Get<int> требует целочисленную лексему. JSON с комментариями, дублирующимися ключами и NaN не принимается. Ввод имеет настраиваемые ограничения размера, глубины и числа блоков. [Полный контракт](../format-v1.md). [Начало](Home.md).
