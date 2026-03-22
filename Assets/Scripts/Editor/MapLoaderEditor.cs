using UnityEngine;
using UnityEditor;
using System.IO;

namespace MapLoader
{
    /// <summary>
    /// Редакторный скрипт для удобной загрузки карт
    /// </summary>
    public class MapLoaderEditor : EditorWindow
    {
        private string selectedMapFolder = "PKmap";
        private string[] availableMaps;
        private MapLoader currentLoader;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Map Loader")]
        public static void ShowWindow()
        {
            GetWindow<MapLoaderEditor>("Map Loader");
        }

        private void OnEnable()
        {
            RefreshAvailableMaps();
            
            // Пытаемся найти существующий MapLoader на сцене
            MapLoader[] loaders = FindObjectsByType<MapLoader>(FindObjectsSortMode.InstanceID);
            if (loaders.Length > 0)
            {
                currentLoader = loaders[0];
            }
        }

        private void RefreshAvailableMaps()
        {
            string mapsPath = Path.Combine(Application.dataPath, "Map");
            
            if (Directory.Exists(mapsPath))
            {
                string[] directories = Directory.GetDirectories(mapsPath);
                availableMaps = new string[directories.Length];
                
                for (int i = 0; i < directories.Length; i++)
                {
                    availableMaps[i] = Path.GetFileName(directories[i]);
                }
            }
            else
            {
                availableMaps = new string[] { "PKmap", "garner2" };
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("Загрузчик карт", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Выбор карты
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Выбор карты", EditorStyles.boldLabel);
            
            selectedMapFolder = EditorGUILayout.TextField("Папка карты", selectedMapFolder);
            
            if (availableMaps != null && availableMaps.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Доступные карты:");
                
                foreach (string map in availableMaps)
                {
                    if (GUILayout.Button(map, GUILayout.Width(100)))
                    {
                        selectedMapFolder = map;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Кнопки управления
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Управление", EditorStyles.boldLabel);
            
            GUI.enabled = !string.IsNullOrEmpty(selectedMapFolder);
            
            if (GUILayout.Button("Загрузить карту", GUILayout.Height(30)))
            {
                LoadSelectedMap();
            }
            
            if (GUILayout.Button("Загрузить и очистить сцену", GUILayout.Height(30)))
            {
                ClearAndLoad();
            }
            
            GUI.enabled = currentLoader != null;
            
            if (GUILayout.Button("Очистить карту", GUILayout.Height(30)))
            {
                ClearMap();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Информация о загруженной карте
            if (currentLoader != null && currentLoader.manifest != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Информация о карте", EditorStyles.boldLabel);
                
                EditorGUILayout.LabelField("Название:", currentLoader.manifest.map_name);
                EditorGUILayout.LabelField("Размер:", $"{currentLoader.manifest.map_width_tiles}x{currentLoader.manifest.map_height_tiles}");
                EditorGUILayout.LabelField("Buildings:", currentLoader.manifest.buildings?.Count.ToString() ?? "0");
                EditorGUILayout.LabelField("Placements:", currentLoader.manifest.placements?.Count.ToString() ?? "0");
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            
            // Инструкция
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Инструкция", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "1. Выберите папку с картой\n" +
                "2. Нажмите 'Загрузить карту'\n" +
                "3. Для загрузки GLB файлов установите плагин (GLTFUtility или UnityGLTF)\n" +
                "4. Без плагина будут созданы placeholder-объекты (кубы)",
                MessageType.Info);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }

        private void LoadSelectedMap()
        {
            if (currentLoader == null)
            {
                // Создаём новый GameObject с MapLoader
                GameObject loaderObj = new GameObject("MapLoader");
                currentLoader = loaderObj.AddComponent<MapLoader>();
            }
            
            Undo.RegisterCreatedObjectUndo(currentLoader.gameObject, "Create MapLoader");
            
            currentLoader.mapFolderPath = $"Map/{selectedMapFolder}";
            currentLoader.LoadMap();
            
            Debug.Log($"Загрузка карты '{selectedMapFolder}' началась...");
        }

        private void ClearAndLoad()
        {
            // Очищаем сцену от предыдущей карты
            ClearMap();
            
            LoadSelectedMap();
        }

        private void ClearMap()
        {
            if (currentLoader != null)
            {
                currentLoader.ClearMap();
                
                if (currentLoader.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(currentLoader.gameObject);
                    currentLoader = null;
                }
            }
            
            // Также ищем и удаляем оставшиеся объекты карты
            MapLoader[] loaders = FindObjectsByType<MapLoader>(FindObjectsSortMode.None);
            foreach (MapLoader loader in loaders)
            {
                if (loader.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(loader.gameObject);
                }
            }
            
            Debug.Log("Сцена очищена от объектов карты");
        }
    }

    /// <summary>
    /// Кастомный инспектор для MapLoader
    /// </summary>
    [CustomEditor(typeof(MapLoader))]
    public class MapLoaderInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            MapLoader loader = (MapLoader)target;
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Загрузить карту", GUILayout.Height(40)))
            {
                loader.LoadMap();
            }
            
            if (GUILayout.Button("Очистить", GUILayout.Height(40)))
            {
                loader.ClearMap();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (loader.manifest != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Карта: {loader.manifest.map_name}");
                EditorGUILayout.LabelField($"Объектов: {loader.manifest.placements?.Count ?? 0}");
            }
        }
    }
}
