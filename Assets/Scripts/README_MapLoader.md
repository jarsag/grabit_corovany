# Map Loader для Unity

Загрузчик карт из manifest.json с поддержкой GLB моделей через GLTFUtility.

## Установка

1. Убедитесь, что установлен пакет **GLTFUtility** (com.siccity.gltfutility)
2. Убедитесь, что установлен **Newtonsoft.Json** (входит в состав GLTFUtility)
3. Скрипты находятся в `Assets/Scripts/`

## Использование

### Через редактор

1. Откройте меню **Tools → Map Loader**
2. Выберите папку с картой (PKmap или garner2)
3. Нажмите **"Загрузить карту"**

### Через скрипт

```csharp
using MapLoader;

// Получите или создайте компонент MapLoader
var loader = FindObjectOfType<MapLoader>();
if (loader == null)
{
    loader = new GameObject("MapLoader").AddComponent<MapLoader>();
}

// Загрузите карту
loader.mapFolderPath = "Map/PKmap";
loader.LoadMap();

// Или используйте метод с параметром
loader.LoadMap("PKmap");
```

## Структура manifest.json

Загрузчик парсит следующие секции:

- **buildings** — словарь объектов (GLB модели)
- **placements** — массив размещений объектов на карте
- **map_width_tiles / map_height_tiles** — размеры террейна
- **alpha_masks** — текстуры для смешивания слоёв террейна

## Террейн

Террейн создаётся из:
- `grids/terrain_height.png` — карта высот
- `terrain_textures/alpha_mask_*.png` — маски для текстур

## GLB Модели

Модели загружаются через **GLTFUtility.Importer.LoadFromFile()**. 

Если `asyncLoading = true`, модели загружаются асинхронно.

## Настройки

| Параметр | Описание | Значение по умолчанию |
|----------|----------|----------------------|
| mapFolderPath | Путь к папке карты | "Map/PKmap" |
| tileSize | Размер тайла террейна | 1.0 |
| asyncLoading | Асинхронная загрузка GLB | true |

## Известные ограничения

1. **Текстуры террейна** — создаются placeholder-текстуры (серого цвета). Для реальных текстур нужно настроить материал террейна вручную.

2. **Масштаб объектов** — в manifest.json `scale: 0` означает "использовать масштаб по умолчанию".

3. **Производительность** — при большом количестве объектов (1000+) загрузка может занять несколько секунд.

## Отладка

Включите консоль Unity для просмотра логов:
- Количество загруженных buildings
- Количество спавненных placements
- Ошибки при загрузке GLB файлов
