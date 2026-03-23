using UnityEngine;
using UnityEditor;

/// <summary>
/// Помощник для настройки игрока и камеры на сцене
/// </summary>
public class PlayerCameraSetup : EditorWindow
{
    [MenuItem("Tools/Setup Player & Camera")]
    static void ShowWindow()
    {
        var window = GetWindow<PlayerCameraSetup>("Player & Camera Setup");
        window.minSize = new Vector2(300, 200);
    }

    void OnGUI()
    {
        GUILayout.Label("Настройка игрока и изометрической камеры", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Создать игрока", GUILayout.Height(40)))
        {
            CreatePlayer();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Создать изометрическую камеру", GUILayout.Height(40)))
        {
            CreateIsometricCamera();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Создать всё вместе", GUILayout.Height(40)))
        {
            CreatePlayerAndCamera();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("После загрузки карты:", MessageType.Info);
        
        if (GUILayout.Button("Поставить игрока на точку спавна", GUILayout.Height(35)))
        {
            PlacePlayerOnSpawnPoint();
        }

        GUILayout.Space(20);
        GUILayout.Label("Инструкция:", EditorStyles.boldLabel);
        GUILayout.Label("1. Создайте землю (Plane или Terrain)");
        GUILayout.Label("2. Нажмите 'Создать всё вместе'");
        GUILayout.Label("3. Загрузите карту через Map Loader");
        GUILayout.Label("4. Нажмите 'Поставить игрока на точку спавна'");
        GUILayout.Label("5. ПКМ для перемещения игрока");
    }

    void CreatePlayer()
    {
        // Создаём пустой объект для игрока
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);
        
        // Добавляем CharacterController
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2;
        controller.radius = 0.5f;
        
        // Добавляем скрипты
        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerAnimation>();
        
        // Устанавливаем тег
        player.tag = "Player";
        
        // Загружаем модель 4.gltf как дочерний объект
        string modelPath = "Assets/4.gltf";
        if (System.IO.File.Exists(modelPath))
        {
            Debug.Log($"Загрузка модели игрока из {modelPath}...");
            // Модель загрузится автоматически через GLTFUtility при запуске
            // В редакторе просто создадим placeholder
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Model";
            model.transform.SetParent(player.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;
            DestroyImmediate(model.GetComponent<CapsuleCollider>());
        }
        else
        {
            // Если модели нет, создаём цилиндр как placeholder
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            placeholder.name = "Model";
            placeholder.transform.SetParent(player.transform);
            placeholder.transform.localPosition = Vector3.zero;
            placeholder.transform.localScale = Vector3.one * 0.5f;
            DestroyImmediate(placeholder.GetComponent<CapsuleCollider>());
        }
        
        Selection.activeGameObject = player;
        Debug.Log("Игрок создан!");
    }

    void CreateIsometricCamera()
    {
        GameObject cameraObj = GameObject.Find("Main Camera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("Main Camera");
            cameraObj.AddComponent<Camera>();
        }

        cameraObj.name = "Main Camera";
        
        var isoCamera = cameraObj.GetComponent<IsometricCamera>();
        if (isoCamera == null)
        {
            isoCamera = cameraObj.AddComponent<IsometricCamera>();
        }

        isoCamera.distance = 12f;
        isoCamera.heightAngle = 35f;
        isoCamera.rotateSpeed = 5f;
        isoCamera.followSpeed = 5f;
        isoCamera.smoothFollow = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            isoCamera.target = player.transform;
        }

        Selection.activeGameObject = cameraObj;
        Debug.Log("Изометрическая камера создана!");
    }

    void CreatePlayerAndCamera()
    {
        CreatePlayer();
        CreateIsometricCamera();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject cameraObj = GameObject.Find("Main Camera");

        if (player != null && cameraObj != null)
        {
            var isoCamera = cameraObj.GetComponent<IsometricCamera>();
            if (isoCamera != null)
            {
                isoCamera.target = player.transform;
            }

            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "DestinationMarker";
                marker.transform.localScale = Vector3.one * 0.3f;
                marker.AddComponent<DestinationMarker>();

                string path = "Assets/Prefabs/DestinationMarker.prefab";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                PrefabUtility.SaveAsPrefabAsset(marker, path);

                playerMovement.destinationMarker = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        Debug.Log("Игрок и камера созданы и связаны!");
    }

    void PlacePlayerOnSpawnPoint()
    {
        // Находим игрока на сцене
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Игрок не найден! Создай игрока сначала.");
            return;
        }

        // Ищем террейн по имени
        Transform terrainParent = null;
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID);
        
        foreach (Transform t in allTransforms)
        {
            if (t.name.Contains("_Terrain") || t.name.Contains("terrain") || t.name.Contains("PKmap"))
            {
                terrainParent = t;
                Debug.Log($"Найден террейн: {t.name}");
                break;
            }
        }

        if (terrainParent == null)
        {
            Debug.LogWarning("Террейн не найден! Загрузи карту через Map Loader.");
            return;
        }

        // Ищем SpawnPoint в иерархии: PKmap_terrain -> map_root -> SpawnPoint
        Transform spawnPoint = FindSpawnPoint(terrainParent);

        if (spawnPoint == null)
        {
            Debug.LogWarning("SpawnPoint не найден в иерархии террейна!");
            return;
        }

        // Перемещаем игрока на точку спавна
        Undo.RecordObject(player.transform, "Move Player to Spawn Point");
        player.transform.position = spawnPoint.position;
        
        Debug.Log($"✓ Игрок перемещён на SpawnPoint: {spawnPoint.position}");
    }

    Transform FindSpawnPoint(Transform terrainRoot)
    {
        // Ищем map_root
        Transform mapRoot = null;
        foreach (Transform child in terrainRoot)
        {
            if (child.name.Contains("map_root") || child.name.Contains("maproot"))
            {
                mapRoot = child;
                Debug.Log($"Найден map_root: {child.name}");
                break;
            }
        }

        // Если не нашли map_root, ищем SpawnPoint напрямую
        Transform spawnPoint = null;
        Transform searchRoot = mapRoot != null ? mapRoot : terrainRoot;

        // Ищем SpawnPoint в детях
        foreach (Transform child in searchRoot)
        {
            if (child.name.Contains("SpawnPoint") || child.name.Contains("spawn"))
            {
                spawnPoint = child;
                Debug.Log($"Найден SpawnPoint: {child.name}");
                break;
            }
        }

        // Если не нашли, ищем во всех дочерних объектах
        if (spawnPoint == null)
        {
            Transform[] allChildren = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t.name.Contains("SpawnPoint") || t.name.Contains("spawn"))
                {
                    spawnPoint = t;
                    Debug.Log($"Найден SpawnPoint (в дочерних): {t.name}");
                    break;
                }
            }
        }

        return spawnPoint;
    }
}
