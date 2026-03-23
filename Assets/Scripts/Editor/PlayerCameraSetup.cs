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

        GUILayout.Space(20);
        GUILayout.Label("Инструкция:", EditorStyles.boldLabel);
        GUILayout.Label("1. Создайте землю (Plane или Terrain)");
        GUILayout.Label("2. Нажмите 'Создать всё вместе'");
        GUILayout.Label("3. Запустите сцену");
        GUILayout.Label("4. ПКМ для перемещения игрока");
    }

    void CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1, 0);
        
        var controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = player.AddComponent<CharacterController>();
            controller.height = 2;
            controller.radius = 0.5f;
        }

        if (player.GetComponent<PlayerMovement>() == null)
        {
            player.AddComponent<PlayerMovement>();
        }

        player.tag = "Player";
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
}
