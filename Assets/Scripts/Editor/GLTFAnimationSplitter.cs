using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using Siccity.GLTFUtility;

/// <summary>
/// Инструмент для нарезки анимаций из GLTF модели
/// Меню: Tools → GLTF Animation Splitter
/// </summary>
public class GLTFAnimationSplitter : EditorWindow
{
    [Header("Настройки")]
    public string gltfPath = "Assets/4.gltf";
    public string outputFolder = "Assets/Animation/Clips";
    
    [Header("Анимация Idle")]
    public string idleName = "Player_Idle";
    public int idleStartFrame = 80;
    public int idleEndFrame = 124;
    public float frameRate = 24f;
    public bool idleLoop = true;
    
    [Header("Анимация Walk")]
    public string walkName = "Player_Walk";
    public int walkStartFrame = 125;
    public int walkEndFrame = 169;
    public bool walkLoop = true;

    [MenuItem("Tools/GLTF Animation Splitter")]
    static void ShowWindow()
    {
        var window = GetWindow<GLTFAnimationSplitter>("Animation Splitter");
        window.minSize = new Vector2(400, 400);
    }

    void OnGUI()
    {
        GUILayout.Label("Нарезка анимаций из GLTF", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Настройки
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Настройки", EditorStyles.boldLabel);
        gltfPath = EditorGUILayout.TextField("GLTF файл:", gltfPath);
        outputFolder = EditorGUILayout.TextField("Папка вывода:", outputFolder);
        EditorGUILayout.EndVertical();

        // Idle
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Анимация Idle", EditorStyles.boldLabel);
        idleName = EditorGUILayout.TextField("Имя:", idleName);
        idleStartFrame = EditorGUILayout.IntField("Start Frame:", idleStartFrame);
        idleEndFrame = EditorGUILayout.IntField("End Frame:", idleEndFrame);
        idleLoop = EditorGUILayout.Toggle("Loop:", idleLoop);
        EditorGUILayout.EndVertical();

        // Walk
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Анимация Walk", EditorStyles.boldLabel);
        walkName = EditorGUILayout.TextField("Имя:", walkName);
        walkStartFrame = EditorGUILayout.IntField("Start Frame:", walkStartFrame);
        walkEndFrame = EditorGUILayout.IntField("End Frame:", walkEndFrame);
        walkLoop = EditorGUILayout.Toggle("Loop:", walkLoop);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Кнопки
        if (GUILayout.Button("1. Создать Idle Clip", GUILayout.Height(35)))
        {
            CreateAnimationClip(idleName, idleStartFrame, idleEndFrame, frameRate, idleLoop);
        }

        if (GUILayout.Button("2. Создать Walk Clip", GUILayout.Height(35)))
        {
            CreateAnimationClip(walkName, walkStartFrame, walkEndFrame, frameRate, walkLoop);
        }

        if (GUILayout.Button("3. Создать Animator Controller", GUILayout.Height(35)))
        {
            CreateAnimatorController();
        }

        GUILayout.Space(15);
        EditorGUILayout.HelpBox(
            "1. Укажи путь к GLTF файлу\n" +
            "2. Настрой фреймы для Idle и Walk\n" +
            "3. Нажми 'Создать Idle Clip'\n" +
            "4. Нажми 'Создать Walk Clip'\n" +
            "5. Нажми 'Создать Animator Controller'\n" +
            "6. Перетащи контроллер на Animator игрока",
            MessageType.Info);
    }

    void CreateAnimationClip(string name, int startFrame, int endFrame, float fps, bool loop)
    {
        if (!File.Exists(gltfPath))
        {
            Debug.LogError($"❌ GLTF файл не найден: {gltfPath}");
            return;
        }

        Directory.CreateDirectory(outputFolder);

        // Загружаем все ассеты из GLTF
        UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(gltfPath);
        
        // Ищем AnimationClip
        AnimationClip sourceClip = null;
        foreach (var obj in allAssets)
        {
            if (obj is AnimationClip clip)
            {
                sourceClip = clip;
                Debug.Log($"📁 Найдена анимация: {clip.name}");
                Debug.Log($"   Длительность: {clip.length:F3} сек");
                Debug.Log($"   Frame Rate: {clip.frameRate} FPS");
                Debug.Log($"   Всего кадров: {clip.length * clip.frameRate:F0}");
                break;
            }
        }

        if (sourceClip == null)
        {
            Debug.LogError("❌ Не найдено анимаций в GLTF!");
            Debug.LogError("💡 Проверь, что GLTF модель содержит анимации");
            return;
        }

        // Проверяем диапазон
        float totalFrames = sourceClip.length * sourceClip.frameRate;
        Debug.Log($"\n📊 Информация об исходной анимации:");
        Debug.Log($"   Всего кадров в источнике: {totalFrames:F0}");
        Debug.Log($"   Выбранный диапазон: {startFrame} - {endFrame} ({endFrame - startFrame + 1} кадров)");
        
        if (startFrame < 0 || startFrame >= totalFrames)
        {
            Debug.LogError($"❌ Start Frame ({startFrame}) вне диапазона! Максимум: {totalFrames:F0}");
            return;
        }
        if (endFrame > totalFrames)
        {
            Debug.LogWarning($"⚠️ End Frame ({endFrame}) больше чем кадров в анимации ({totalFrames:F0})");
            endFrame = Mathf.FloorToInt(totalFrames);
        }
        if (startFrame >= endFrame)
        {
            Debug.LogError($"❌ Start Frame ({startFrame}) должен быть меньше End Frame ({endFrame})");
            return;
        }

        // Создаём новый клип
        AnimationClip newClip = new AnimationClip();
        newClip.name = name;
        newClip.frameRate = fps;

        float startTime = startFrame / fps;
        float endTime = endFrame / fps;
        float duration = endTime - startTime;

        Debug.Log($"\n✂️ Нарезка:");
        Debug.Log($"   Start Time: {startTime:F3} сек (кадр {startFrame})");
        Debug.Log($"   End Time: {endTime:F3} сек (кадр {endFrame})");
        Debug.Log($"   Duration: {duration:F3} сек");

        // Копируем все кривые из источника
        var bindings = AnimationUtility.GetCurveBindings(sourceClip);
        Debug.Log($"\n🔧 Кривых в источнике: {bindings.Length}");

        int curvesCopied = 0;
        int totalKeysBefore = 0;
        int totalKeysAfter = 0;

        foreach (var binding in bindings)
        {
            AnimationCurve srcCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (srcCurve != null && srcCurve.keys.Length > 0)
            {
                totalKeysBefore += srcCurve.keys.Length;
                
                // Вырезаем диапазон и сдвигаем время к началу
                var newKeys = new System.Collections.Generic.List<Keyframe>();
                foreach (var key in srcCurve.keys)
                {
                    if (key.time >= startTime && key.time <= endTime)
                    {
                        var newKey = key;
                        newKey.time -= startTime;
                        newKeys.Add(newKey);
                    }
                }

                if (newKeys.Count > 0)
                {
                    var newCurve = new AnimationCurve(newKeys.ToArray());
                    AnimationUtility.SetEditorCurve(newClip, binding, newCurve);
                    curvesCopied++;
                    totalKeysAfter += newKeys.Count;
                }
            }
        }

        Debug.Log($"✅ Скопировано кривых: {curvesCopied} из {bindings.Length}");
        Debug.Log($"🔑 Ключевых кадров: {totalKeysBefore} → {totalKeysAfter}");

        // Настройки клипа
        var settings = AnimationUtility.GetAnimationClipSettings(newClip);
        settings.loopTime = loop;
        settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        // Сохраняем
        string path = $"{outputFolder}/{name}.anim";
        AssetDatabase.CreateAsset(newClip, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅✅ Создан клип: {path}");
        Debug.Log($"   Фреймы: {startFrame}-{endFrame}, длительность: {duration:F2} сек, Loop: {loop}");
    }

    void CreateAnimatorController()
    {
        // Ищем созданные клипы
        string[] idleFiles = Directory.GetFiles(outputFolder, $"{idleName}*.anim");
        string[] walkFiles = Directory.GetFiles(outputFolder, $"{walkName}*.anim");

        if (idleFiles.Length == 0)
        {
            Debug.LogError($"❌ Idle клип не найден! Сначала создай '{idleName}'");
            return;
        }

        if (walkFiles.Length == 0)
        {
            Debug.LogError($"❌ Walk клип не найден! Сначала создай '{walkName}'");
            return;
        }

        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleFiles[0]);
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkFiles[0]);

        // Создаём контроллер
        string controllerPath = "Assets/Animation/PlayerAnimator.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Добавляем параметр Speed
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var sm = controller.layers[0].stateMachine;

        // Создаём состояния
        var idle = sm.AddState("Idle", new Vector3(200, 0, 0));
        idle.motion = idleClip;

        var walk = sm.AddState("Walk", new Vector3(200, 100, 0));
        walk.motion = walkClip;

        // Переход Idle -> Walk (Speed > 0.1)
        var t1 = idle.AddTransition(walk);
        t1.hasExitTime = false;
        t1.duration = 0.1f;
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        // Переход Walk -> Idle (Speed < 0.1)
        var t2 = walk.AddTransition(idle);
        t2.hasExitTime = false;
        t2.duration = 0.1f;
        t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Any State -> Idle
        var t3 = sm.AddAnyStateTransition(idle);
        t3.hasExitTime = false;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅✅✅ Animator Controller создан: {controllerPath}");
        Debug.Log("👉 Перетащи контроллер на компонент Animator игрока!");
        
        Selection.activeObject = controller;
    }
}
