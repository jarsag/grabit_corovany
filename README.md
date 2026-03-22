# Unity Map Loader

Загрузчик карт из manifest.json с поддержкой GLB моделей через GLTFUtility.

## Описание

Этот инструмент загружает карты из JSON-манифеста и GLB файлов террейна. Используется для проектов, где данные карты хранятся в формате:
- `manifest.json` — данные о зданиях, размещениях, зонах
- `terrain.glb` — 3D модель террейна со спавн-поинтами
- `buildings/*.glb` — 3D модели зданий

## Требования

- Unity 6000.x (6000.3.11f1 или новее)
- Пакет **GLTFUtility** (com.siccity.gltfutility)
- Пакет **Newtonsoft.Json** (входит в состав GLTFUtility)

## Установка

1. Клонируйте репозиторий
2. Откройте проект в Unity
3. Дождитесь импорта пакетов и компиляции

## Использование

### Через редактор

1. Откройте меню **Tools → Map Loader**
2. Выберите папку с картой (например, `PKmap` или `garner2`)
3. Нажмите **"Загрузить карту"**

### Через скрипт

```csharp
using MapLoader;

var loader = FindObjectOfType<MapLoader>();
if (loader == null)
{
    loader = new GameObject("MapLoader").AddComponent<MapLoader>();
}

loader.mapFolderPath = "Map/PKmap";
loader.LoadMap();
```

## Настройки

| Параметр | Описание | По умолчанию |
|----------|----------|--------------|
| `mapFolderPath` | Путь к папке с картой | `Map/PKmap` |
| `useTerrainSpawnPoints` | Использовать спавн-поинты из terrain.glb | `true` |
| `spawnPointPrefix` | Префикс имён спавн-поинтов | `building_` |
| `usePlacementRotation` | Использовать ротацию из манифеста | `true` |
| `rotationOffsetY` | Коррекция ротации (градусы) | `0` |
| `rotationFixIds` | ID зданий для коррекции на 180° | `` |
| `globalScaleMultiplier` | Множитель масштаба | `1.0` |
| `defaultScale` | Масштаб по умолчанию | `1.0` |

## Структура проекта

```
Assets/
├── Map/                    # Карты
│   ├── PKmap/             # Карта PKmap
│   │   ├── manifest.json  # Данные карты
│   │   ├── terrain.glb    # Террейн со спавн-поинтами
│   │   └── buildings/     # Модели зданий
│   └── garner2/           # Карта garner2
├── Scripts/
│   ├── MapLoader.cs       # Основной загрузчик
│   ├── MapData.cs         # Классы данных
│   └── Editor/
│       └── MapLoaderEditor.cs  # Редакторное окно
```

## Формат manifest.json

```json
{
  "map_name": "PKmap",
  "map_width_tiles": 128,
  "map_height_tiles": 128,
  "buildings": {
    "26": {
      "filename": "nml-bd151.lmo",
      "glb": "buildings/nml-bd151.glb"
    }
  },
  "placements": [
    {
      "obj_id": 26,
      "position": [118.2, 0.0, 14.2],
      "rotation_y_degrees": -180,
      "scale": 0
    }
  ]
}
```

## Известные ограничения

1. **Текстуры террейна** — не загружаются, используется стандартный материал Unity
2. **Масштаб** — `scale=0` или `scale=-1` означает "использовать дефолтный"
3. **Аномальный масштаб** — объекты с `scale > 100` пропускаются (настраиваемо)

## Лицензия

MIT
