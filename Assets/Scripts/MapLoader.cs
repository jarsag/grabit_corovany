using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Siccity.GLTFUtility;
using Newtonsoft.Json;

namespace MapLoader
{
    /// <summary>
    /// Загрузчик карты из manifest.json и terrain.glb
    /// Использует спавн-поинты из terrain.glb для размещения объектов
    /// </summary>
    public class MapLoader : MonoBehaviour
    {
        [Header("Настройки карты")]
        [Tooltip("Путь к папке с картой (относительно Assets)")]
        public string mapFolderPath = "Map/PKmap";

        [Header("Настройки спавна игрока")]
        [Tooltip("Префаб игрока для спавна")]
        public GameObject playerPrefab;

        [Tooltip("Спавнить игрока в точке spawn_point из террейна (building_*)")]
        public bool spawnPlayerAtSpawnPoint = true;

        [Tooltip("ID спавн-поинта для игрока (например, 0 = building_0). -1 = первый доступный")]
        public int playerSpawnPointId = -1;

        [Tooltip("Смещение позиции игрока относительно точки спавна")]
        public Vector3 playerSpawnOffset = Vector3.zero;

        [Header("Настройки спавна")]
        [Tooltip("Использовать спавн-поинты из terrain.glb (вместо placements из манифеста)")]
        public bool useTerrainSpawnPoints = true;
        
        [Tooltip("Префикс имён спавн-поинтов в terrain.glb")]
        public string spawnPointPrefix = "building_";
        
        [Tooltip("Использовать ротацию из placements манифеста (вместо ротации спавн-поинтов)")]
        public bool usePlacementRotation = true;
        
        [Tooltip("Коррекция ротации для всех объектов (градусы по Y)")]
        public float rotationOffsetY = 0f;
        
        [Tooltip("ID зданий, которым нужно развернуть ротацию на 180° (через запятую)")]
        public string rotationFixIds = "";
        
        [Tooltip("Множитель масштаба для всех объектов")]
        public float globalScaleMultiplier = 1.0f;
        
        [Tooltip("Масштаб по умолчанию при scale=0/-1")]
        public float defaultScale = 1.0f;
        
        [Tooltip("Максимальный scale в manifest.json (объекты с большим scale будут пропущены)")]
        public float maxPlacementScale = 100f;
        
        [Tooltip("Логировать объекты с аномальным scale")]
        public bool logAnomalousScale = true;

        [Header("Загруженные данные")]
        public Manifest manifest;
        public Transform objectsParent;
        public Transform terrainParent;
        public GameObject spawnedPlayer;

        private Dictionary<int, string> buildingPaths = new();
        private List<Transform> allSpawnPoints = new();
        private Dictionary<int, List<Placement>> placementsByObjId = new();
        private HashSet<int> rotationFixSet = new HashSet<int>();
        private string fullMapPath;

        /// <summary>
        /// Загрузить карту из указанной папки
        /// </summary>
        public void LoadMap(string mapFolderName)
        {
            mapFolderPath = $"Map/{mapFolderName}";
            LoadMap();
        }

        /// <summary>
        /// Загрузить карту используя текущий mapFolderPath
        /// </summary>
        public void LoadMap()
        {
            fullMapPath = Path.Combine(Application.dataPath, mapFolderPath);

            if (!Directory.Exists(fullMapPath))
            {
                Debug.LogError($"Папка карты не найдена: {fullMapPath}");
                return;
            }

            // Загружаем манифест
            string manifestPath = Path.Combine(fullMapPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"manifest.json не найден: {manifestPath}");
                return;
            }

            string jsonContent = File.ReadAllText(manifestPath);
            manifest = JsonConvert.DeserializeObject<Manifest>(jsonContent);

            if (manifest == null)
            {
                Debug.LogError("Не удалось распарсить manifest.json");
                return;
            }

            Debug.Log($"Загрузка карты: {manifest.map_name}");
            Debug.Log($"Размер карты: {manifest.map_width_tiles}x{manifest.map_height_tiles} тайлов");
            Debug.Log($"Объектов buildings: {manifest.buildings?.Count ?? 0}");
            Debug.Log($"Размещений (placements): {manifest.placements?.Count ?? 0}");

            // Парсим ID для коррекции ротации
            ParseRotationFixIds();

            // Создаём родительский объект для объектов карты
            if (objectsParent == null)
            {
                GameObject parentObj = new GameObject($"{manifest.map_name}_Objects");
                objectsParent = parentObj.transform;
            }

            // Загружаем террейн (со спавн-поинтами)
            LoadTerrain();

            // Загружаем placements из манифеста (для ротаций)
            LoadPlacementsRotation();

            // Спавним объекты
            if (useTerrainSpawnPoints)
            {
                SpawnFromTerrainPoints();
            }
            else
            {
                SpawnPlacements();
            }

            // Спавн игрока
            if (spawnPlayerAtSpawnPoint)
            {
                SpawnPlayer();
            }

            Debug.Log($"Загрузка карты '{manifest.map_name}' завершена!");
        }

        /// <summary>
        /// Парсинг ID для коррекции ротации
        /// </summary>
        private void ParseRotationFixIds()
        {
            rotationFixSet.Clear();
            
            if (string.IsNullOrWhiteSpace(rotationFixIds))
            {
                return;
            }
            
            string[] parts = rotationFixIds.Split(',');
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int id))
                {
                    rotationFixSet.Add(id);
                }
            }
            
            Debug.Log($"Коррекция ротации для {rotationFixSet.Count} ID: {rotationFixIds}");
        }

        /// <summary>
        /// Загрузка террейна из GLB файла
        /// </summary>
        private void LoadTerrain()
        {
            string terrainGlbPath = Path.Combine(fullMapPath, "terrain.glb");

            if (!File.Exists(terrainGlbPath))
            {
                Debug.LogWarning($"terrain.glb не найден: {terrainGlbPath}");
                return;
            }

            // Создаём родительский объект для террейна
            GameObject terrainObj = new GameObject($"{manifest.map_name}_Terrain");
            terrainParent = terrainObj.transform;

            // Загружаем GLB модель террейна
            Debug.Log($"Загрузка террейна из {terrainGlbPath}...");
            GameObject terrainModel = Importer.LoadFromFile(terrainGlbPath);

            if (terrainModel == null)
            {
                Debug.LogError($"Не удалось загрузить terrain.glb");
                return;
            }

            // Перемещаем модель в наш объект
            terrainModel.transform.SetParent(terrainParent);
            terrainModel.transform.localPosition = Vector3.zero;
            terrainModel.transform.localRotation = Quaternion.identity;
            terrainModel.transform.localScale = Vector3.one;

            // Добавляем Mesh Collider для raycast
            AddMeshCollider(terrainModel);

            // Находим спавн-поинты
            FindSpawnPoints(terrainModel);

            Debug.Log($"Террейн загружен из GLB, найдено спавн-поинтов: {allSpawnPoints.Count}");
        }

        /// <summary>
        /// Добавляет Mesh Collider на модель террейна
        /// </summary>
        private void AddMeshCollider(GameObject terrainModel)
        {
            // Ищем все меши в модели
            MeshFilter[] meshFilters = terrainModel.GetComponentsInChildren<MeshFilter>(true);
            
            int collidersAdded = 0;
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    MeshCollider collider = mf.gameObject.GetComponent<MeshCollider>();
                    if (collider == null)
                    {
                        collider = mf.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = mf.sharedMesh;
                        collider.convex = false;
                        collidersAdded++;
                    }
                }
            }
            
            Debug.Log($"Добавлено Mesh Collider'ов: {collidersAdded}");
        }

        /// <summary>
        /// Загрузка ротаций из placements манифеста
        /// </summary>
        private void LoadPlacementsRotation()
        {
            if (manifest.placements == null)
            {
                return;
            }

            placementsByObjId.Clear();
            
            foreach (Placement placement in manifest.placements)
            {
                if (!placementsByObjId.ContainsKey(placement.obj_id))
                {
                    placementsByObjId[placement.obj_id] = new List<Placement>();
                }
                placementsByObjId[placement.obj_id].Add(placement);
            }
            
            Debug.Log($"Загружено {manifest.placements.Count} placements для ротаций");
        }

        /// <summary>
        /// Поиск спавн-поинтов в террейне
        /// </summary>
        private void FindSpawnPoints(GameObject terrainModel)
        {
            allSpawnPoints.Clear();
            
            // Ищем все объекты с именами типа "building_26", "building_316" и т.д.
            Transform[] allTransforms = terrainModel.GetComponentsInChildren<Transform>(true);
            
            foreach (Transform t in allTransforms)
            {
                if (t.name.StartsWith(spawnPointPrefix))
                {
                    allSpawnPoints.Add(t);
                }
            }
            
            Debug.Log($"Всего найдено спавн-поинтов: {allSpawnPoints.Count}");
        }

        /// <summary>
        /// Спавн объектов из спавн-поинтов террейна
        /// </summary>
        private void SpawnFromTerrainPoints()
        {
            if (allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("Спавн-поинты не найдены в террейне, используем placements из манифеста");
                SpawnPlacements();
                return;
            }

            // Предзагружаем пути к зданиям
            PreloadBuildings();

            int spawnedCount = 0;
            int skippedCount = 0;
            
            Debug.Log($"Начало спавна {allSpawnPoints.Count} объектов из спавн-поинтов...");

            // Спавним для каждого спавн-поинта
            foreach (Transform spawnPoint in allSpawnPoints)
            {
                // Извлекаем ID из имени (например, "building_26" -> 26)
                string idString = spawnPoint.name.Substring(spawnPointPrefix.Length);
                
                if (!int.TryParse(idString, out int objId))
                {
                    Debug.LogWarning($"Не удалось распарсить ID из имени: {spawnPoint.name}");
                    continue;
                }

                if (!buildingPaths.ContainsKey(objId))
                {
                    Debug.LogWarning($"Здание с ID {objId} не найдено в buildings (спавн-поинт: {spawnPoint.name})");
                    continue;
                }

                string glbPath = buildingPaths[objId];

                // Загружаем модель
                GameObject prefab = Importer.LoadFromFile(glbPath);

                if (prefab == null)
                {
                    Debug.LogError($"Не удалось загрузить GLB: {glbPath}");
                    continue;
                }

                prefab.name = Path.GetFileNameWithoutExtension(glbPath);

                // Создаём инстанс в позиции спавн-поинта
                GameObject instance = Instantiate(prefab, objectsParent);
                instance.transform.position = spawnPoint.position;
                
                // Ротация: ищем placement с похожей позицией
                float rotationY = 0f;
                bool foundRotation = false;
                
                if (usePlacementRotation && placementsByObjId.ContainsKey(objId))
                {
                    List<Placement> placements = placementsByObjId[objId];
                    Vector3 spPos = spawnPoint.position;
                    
                    // Ищем placement с ближайшей позицией
                    float minDistance = float.MaxValue;
                    Placement closestPlacement = null;
                    
                    foreach (Placement p in placements)
                    {
                        Vector3 pPos = new Vector3(p.position[0], p.position[1], p.position[2]);
                        float distance = Vector3.Distance(spPos, pPos);
                        
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestPlacement = p;
                        }
                    }
                    
                    // Если нашли близко (< 5 единиц), используем его ротацию
                    if (closestPlacement != null && minDistance < 5f)
                    {
                        rotationY = closestPlacement.rotation_y_degrees;
                        foundRotation = true;
                        
                        // Логирование для отладки (первые 10 объектов)
                        if (spawnedCount < 10)
                        {
                            Debug.Log($"Спавн #{spawnedCount+1}: {spawnPoint.name} (ID={objId}) -> pos={spPos}, rotation={rotationY} (из манифеста, dist={minDistance:F2})");
                        }
                    }
                }
                
                if (!foundRotation)
                {
                    // Используем ротацию спавн-поинта с коррекцией
                    rotationY = spawnPoint.rotation.eulerAngles.y + rotationOffsetY;
                    
                    if (spawnedCount < 10)
                    {
                        Debug.Log($"Спавн #{spawnedCount+1}: {spawnPoint.name} (ID={objId}) -> pos={spawnPoint.position}, rotation={rotationY} (из спавн-поинта)");
                    }
                }
                
                // Применяем коррекцию 180° если нужно
                if (rotationFixSet.Contains(objId))
                {
                    rotationY += 180f;
                    Debug.Log($"ID={objId}: применена коррекция ротации +180° (итог: {rotationY})");
                }
                
                instance.transform.rotation = Quaternion.Euler(0, rotationY, 0);
                
                // Масштаб по умолчанию
                instance.transform.localScale = Vector3.one * defaultScale * globalScaleMultiplier;

                spawnedCount++;
            }

            Debug.Log($"Спавнено объектов: {spawnedCount} из {allSpawnPoints.Count} (пропущено: {skippedCount})");
        }

        /// <summary>
        /// Спавн объектов из placements (резервный метод)
        /// </summary>
        private void SpawnPlacements()
        {
            if (manifest.placements == null)
            {
                Debug.LogWarning("Placements не найдены в манифесте");
                return;
            }

            // Предзагружаем пути к зданиям
            PreloadBuildings();

            int spawnedCount = 0;
            int totalPlacements = manifest.placements.Count;

            Debug.Log($"Начало спавна {totalPlacements} объектов из placements...");

            // Группируем размещения по obj_id для эффективной загрузки
            Dictionary<int, List<Placement>> groupedByObj = new Dictionary<int, List<Placement>>();

            foreach (Placement placement in manifest.placements)
            {
                if (!groupedByObj.ContainsKey(placement.obj_id))
                {
                    groupedByObj[placement.obj_id] = new List<Placement>();
                }
                groupedByObj[placement.obj_id].Add(placement);
            }

            // Загружаем и спавним для каждого типа здания
            foreach (var kvp in groupedByObj)
            {
                int objId = kvp.Key;
                List<Placement> placements = kvp.Value;

                if (!buildingPaths.ContainsKey(objId))
                {
                    Debug.LogWarning($"Здание с ID {objId} не найдено в buildings");
                    continue;
                }

                string glbPath = buildingPaths[objId];

                // Проверяем наличие аномального масштаба
                bool hasAnomalousScale = false;
                foreach (var placement in placements)
                {
                    if (placement.scale > maxPlacementScale)
                    {
                        hasAnomalousScale = true;
                        if (logAnomalousScale)
                        {
                            Debug.LogWarning($"ID={objId}: аномальный scale={placement.scale} > {maxPlacementScale} (position={placement.position[0]}, {placement.position[1]}, {placement.position[2]}). Объект пропущен.");
                        }
                    }
                }

                if (hasAnomalousScale)
                {
                    continue;
                }

                // Загружаем модель
                GameObject prefab = Importer.LoadFromFile(glbPath);

                if (prefab == null)
                {
                    Debug.LogError($"Не удалось загрузить GLB: {glbPath}");
                    continue;
                }

                prefab.name = Path.GetFileNameWithoutExtension(glbPath);

                // Спавним все инстансы этого здания
                foreach (Placement placement in placements)
                {
                    GameObject instance = Instantiate(prefab, objectsParent);

                    Vector3 position = new Vector3(
                        placement.position[0],
                        placement.position[1],
                        placement.position[2]
                    );
                    instance.transform.position = position;

                    instance.transform.rotation = Quaternion.Euler(
                        0,
                        placement.rotation_y_degrees,
                        0
                    );

                    float scale;
                    if (placement.scale <= 0)
                    {
                        scale = defaultScale * globalScaleMultiplier;
                    }
                    else
                    {
                        scale = placement.scale * globalScaleMultiplier;
                    }
                    instance.transform.localScale = Vector3.one * scale;

                    spawnedCount++;
                }

                Debug.Log($"Загружено здание ID={objId}, спавнено {placements.Count} инстансов");
            }

            Debug.Log($"Спавнено объектов: {spawnedCount} из {totalPlacements}");
        }

        /// <summary>
        /// Предварительная загрузка путей к зданиям
        /// </summary>
        private void PreloadBuildings()
        {
            if (manifest.buildings == null)
            {
                Debug.LogWarning("Buildings не найдены в манифесте");
                return;
            }

            foreach (var kvp in manifest.buildings)
            {
                int id = int.Parse(kvp.Key);
                Building building = kvp.Value;

                string glbPath = Path.Combine(fullMapPath, building.glb);

                if (File.Exists(glbPath))
                {
                    buildingPaths[id] = glbPath;
                    Debug.Log($"Добавлено здание ID={id}: {building.filename}");
                }
                else
                {
                    Debug.LogWarning($"GLB файл не найден: {glbPath}");
                }
            }
        }

        /// <summary>
        /// Очистить загруженную карту
        /// </summary>
        public void ClearMap()
        {
            if (terrainParent != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(terrainParent.gameObject);
                }
                else
                {
                    Destroy(terrainParent.gameObject);
                }
                terrainParent = null;
            }

            if (objectsParent != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(objectsParent.gameObject);
                }
                else
                {
                    Destroy(objectsParent.gameObject);
                }
                objectsParent = null;
            }

            if (spawnedPlayer != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(spawnedPlayer);
                }
                else
                {
                    Destroy(spawnedPlayer);
                }
                spawnedPlayer = null;
            }

            buildingPaths.Clear();
            allSpawnPoints.Clear();
            placementsByObjId.Clear();
            rotationFixSet.Clear();
            manifest = null;

            Debug.Log("Карта очищена");
        }

        /// <summary>
        /// Спавн игрока в точке спавна из террейна (building_*)
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("playerPrefab не назначен, игрок не будет заспавнен");
                return;
            }

            if (allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("Спавн-поинты не найдены в террейне, игрок не будет заспавнен");
                return;
            }

            // Находим спавн-поинт для игрока
            Transform spawnPoint = null;

            if (playerSpawnPointId >= 0)
            {
                // Ищем по конкретному ID
                string targetName = $"{spawnPointPrefix}{playerSpawnPointId}";
                foreach (Transform sp in allSpawnPoints)
                {
                    if (sp.name == targetName)
                    {
                        spawnPoint = sp;
                        break;
                    }
                }

                if (spawnPoint == null)
                {
                    Debug.LogWarning($"Спавн-поинт {targetName} не найден, используем первый доступный");
                    spawnPoint = allSpawnPoints[0];
                }
            }
            else
            {
                // Используем первый доступный
                spawnPoint = allSpawnPoints[0];
            }

            Debug.Log($"Спавн игрока в точке: {spawnPoint.name} ({spawnPoint.position})");

            Vector3 spawnPosition = spawnPoint.position + playerSpawnOffset;
            spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            
            Debug.Log($"✓ Игрок заспавнен: {spawnPosition}");

            // Визуализация точки спавна
            Debug.DrawRay(spawnPoint.position, Vector3.up * 2f, Color.green, 5f);
        }
    }
}
