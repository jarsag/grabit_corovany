using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Режет клипы из исходной анимации Assets/4.gltf по НОМЕРАМ СЭМПЛОВ,
/// а не по кадрам, чтобы не путаться в частотах (в источнике ключи лежат с шагом 30 Гц,
/// хотя сам клип объявляет frameRate 60).
///
/// Номера сэмплов = номера кейфреймов в Blender × 1.25 (24 fps -> 30 Гц).
///   idle : Blender  64.. 99 -> сэмплы  80..124   (45 ключей, поза 0.06°, излом 0.05°)
///   бег  : Blender 100..116 -> сэмплы 125..145   (21 ключ,  поза 0.08°, излом 0.04°)
///
/// ВАЖНО: диапазон Blender 100..132 — это ДВА РАЗНЫХ цикла бега. Склеивать их
/// в один клип нельзя: замыкание получается с рывком 63.7 градуса.
///
/// Существующие .anim перезаписываются НА МЕСТЕ, чтобы сохранить GUID
/// и не порвать ссылки в PlayerAnimation.controller.
/// </summary>
public static class AnimationCutter
{
    private const string SourcePath = "Assets/4.gltf";
    private const string OutputFolder = "Assets/Animation/Clips";
    private const float SampleHz = 30f;

    [MenuItem("Tools/Sandbox/2. ПЕРЕСОБРАТЬ КЛИПЫ: idle 80-124 + бег 125-145 (1 цикл)")]
    public static void RebuildSeamless()
    {
        // Бег: ОДИН цикл 125..145 = Blender 100..116.
        // Поза на 145 совпадает с позой на 125 с точностью 0.08 град,
        // поэтому цикл замыкается без рывка.
        Rebuild(125, 145, "1 цикл бега, Blender 100..116");
    }

    private static void Rebuild(int runStart, int runEnd, string runLabel)
    {
        AnimationClip source = FindSourceClip();
        if (source == null)
        {
            EditorUtility.DisplayDialog("Резак", $"Не найден клип внутри {SourcePath}", "ОК");
            return;
        }

        Debug.Log($"[AnimationCutter] Источник '{source.name}': length={source.length:F4}, frameRate={source.frameRate}, кривых={AnimationUtility.GetCurveBindings(source).Length}");

        // Idle: 44 интервала (45 ключей) — ровно столько, сколько в Blender 64..99.
        // Проверено по данным: при 80..124 совпадение позы 0.06° И излом скорости 0.05°.
        // При 80..123 (на один сэмпл короче) поза тоже 0.06°, но излом 2.76° —
        // именно он читается как «idle проскальзывает».
        CutInto(source, "Player_Idle", 80, 124);
        CutInto(source, "Player_Walk", runStart, runEnd);

        // GLTFUtility теряет анимацию корня Bip01 — возвращаем её из готовых данных
        AddRootCurves("Assets/Animation/Clips/Player_Idle.anim",
                      "Assets/Animation/Clips/root_Bip01_Idle.txt", "Player_Idle");
        AddRootCurves("Assets/Animation/Clips/Player_Walk.anim",
                      "Assets/Animation/Clips/root_Bip01_Walk.txt", "Player_Walk");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Резак",
            $"Готово.\n\nIdle: сэмплы 80..124 (Blender 64..99, 1.467 с)\n" +
            $"Бег: сэмплы {runStart}..{runEnd} ({runLabel})\n\n" +
            "Подробности смотри в консоли.", "ОК");
    }

    [MenuItem("Tools/Sandbox/8. Ключи ЛИНЕЙНЫЕ (как в glTF, точно по данным)")]
    public static void MakeAllLinear()
    {
        int a = ApplyTangents("Assets/Animation/Clips/Player_Walk.anim", AnimationUtility.TangentMode.Linear);
        int b = ApplyTangents("Assets/Animation/Clips/Player_Idle.anim", AnimationUtility.TangentMode.Linear);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Касательные = Linear",
            $"Обработано кривых:\nPlayer_Walk: {a}\nPlayer_Idle: {b}\n\n" +
            "Точное воспроизведение данных glTF (там LINEAR). Проверь в Play.", "ОК");
    }

    [MenuItem("Tools/Sandbox/9. Ключи ПЛАВНЫЕ (сглаживание, как Bezier в Blender)")]
    public static void MakeAllSmooth()
    {
        int a = ApplyTangents("Assets/Animation/Clips/Player_Walk.anim", AnimationUtility.TangentMode.ClampedAuto);
        int b = ApplyTangents("Assets/Animation/Clips/Player_Idle.anim", AnimationUtility.TangentMode.ClampedAuto);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Касательные = ClampedAuto (плавные)",
            $"Обработано кривых:\nPlayer_Walk: {a}\nPlayer_Idle: {b}\n\n" +
            "Unity сам построит сглаженные касательные (Catmull-Rom-подобно), как Bezier в Blender.\n" +
            "Это дополнительно сгладит резкие кадры. Проверь в Play и сравни с пунктом 8.", "ОК");
    }

    private static int ApplyTangents(string path, AnimationUtility.TangentMode mode)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[AnimationCutter] не найден {path}");
            return -1;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        int done = 0;

        foreach (EditorCurveBinding b in bindings)
        {
            AnimationCurve c = AnimationUtility.GetEditorCurve(clip, b);
            if (c == null || c.keys.Length == 0) continue;

            MakeTangents(c, mode);
            AnimationUtility.SetEditorCurve(clip, b, c);
            done++;
        }

        EditorUtility.SetDirty(clip);
        Debug.Log($"[AnimationCutter] {System.IO.Path.GetFileName(path)}: касательные -> {mode}, кривых {done}/{bindings.Length}");
        return done;
    }

    /// <summary>
    /// Ключи из glTF приходят с НУЛЕВЫМИ касательными (inSlope=0, outSlope=0, tangentMode=Free).
    /// Для Unity это означает плавный вход и выход в КАЖДОМ ключе — анимация
    /// останавливается на каждом кадре. Отсюда «нет гладкости» и люфт.
    /// Linear = точно как в данных glTF, ClampedAuto = сглаживание.
    /// </summary>
    private static void MakeTangents(AnimationCurve curve, AnimationUtility.TangentMode mode)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, mode);
            AnimationUtility.SetKeyRightTangentMode(curve, i, mode);
        }
    }

    [MenuItem("Tools/Sandbox/10. Убрать вторичные кости (тест на виляние)")]
    public static void StripAccessoryBones()
    {
        int a = StripAccessories("Assets/Animation/Clips/Player_Walk.anim");
        int b = StripAccessories("Assets/Animation/Clips/Player_Idle.anim");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Вторичные кости убраны",
            $"Удалено кривых:\nPlayer_Walk: {a}\nPlayer_Idle: {b}\n\n" +
            "Это ТЕСТ. Если виляние пропало — значит виноваты вторичные кости " +
            "(волосы/хвост/юбка), и мы будем решать, что с ними делать.\n\n" +
            "Вернуть обратно: запусти любой пункт пересборки (2..7) — они режут заново из источника.", "ОК");
    }

    private static int StripAccessories(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[AnimationCutter] не найден {path}");
            return -1;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        int removed = 0;

        foreach (EditorCurveBinding b in bindings)
        {
            if (string.IsNullOrEmpty(b.path)) continue;
            string[] parts = b.path.Split('/');
            string leaf = parts[parts.Length - 1];
            bool isAccessory =
                System.Text.RegularExpressions.Regex.IsMatch(leaf, @"^Bone\d+$") ||
                leaf.StartsWith("Bip01 Ponytail") ||
                leaf.EndsWith("Nub") ||
                leaf == "Bip01 Footsteps";

            if (isAccessory)
            {
                AnimationUtility.SetEditorCurve(clip, b, null);   // null = удалить кривую
                removed++;
            }
        }

        EditorUtility.SetDirty(clip);
        Debug.Log($"[AnimationCutter] {System.IO.Path.GetFileName(path)}: удалено вторичных кривых {removed} из {bindings.Length}");
        return removed;
    }

    [MenuItem("Tools/Sandbox/11. Убрать перевороты кватернионов (глитчи между кадрами)")]
    public static void FixQuaternionFlips()
    {
        int a = FixQuaternionSigns("Assets/Animation/Clips/Player_Walk.anim");
        int b = FixQuaternionSigns("Assets/Animation/Clips/Player_Idle.anim");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Перевороты кватернионов",
            $"Исправлено ключей:\nPlayer_Walk: {a}\nPlayer_Idle: {b}\n\n" +
            "Перевёрнутые кватернионы (q -> -q) давали одинаковую позу в ключах, " +
            "но между кадрами кость выворачивалась наизнанку.\n\n" +
            "Теперь знаки непрерывны. Проверь в Play.", "ОК");
    }

    private static int FixQuaternionSigns(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[AnimationCutter] не найден {path}");
            return -1;
        }

        int fixedKeys = FixQuaternionSigns(clip);
        EditorUtility.SetDirty(clip);
        Debug.Log($"[AnimationCutter] {System.IO.Path.GetFileName(path)}: перевёрнутых ключей исправлено {fixedKeys}");
        return fixedKeys;
    }

    /// <summary>
    /// Убирает перевороты знака кватернионов. Поза q и -q — одна и та же,
    /// поэтому в ключах всё выглядит правильно, но при независимой интерполяции
    /// четырёх компонентов между q и -q кость выворачивается наизнанку.
    /// Идём по ключам и меняем знак там, где скалярное произведение с предыдущим
    /// ключом отрицательное.
    /// </summary>
    private static int FixQuaternionSigns(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        Dictionary<string, AnimationCurve[]> groups = new Dictionary<string, AnimationCurve[]>();
        Dictionary<string, EditorCurveBinding[]> groupBindings = new Dictionary<string, EditorCurveBinding[]>();

        foreach (EditorCurveBinding b in bindings)
        {
            if (!b.propertyName.StartsWith("m_LocalRotation.")) continue;

            if (!groups.ContainsKey(b.path))
            {
                groups[b.path] = new AnimationCurve[4];
                groupBindings[b.path] = new EditorCurveBinding[4];
            }

            int idx = b.propertyName.EndsWith(".x") ? 0
                    : b.propertyName.EndsWith(".y") ? 1
                    : b.propertyName.EndsWith(".z") ? 2 : 3;

            groups[b.path][idx] = AnimationUtility.GetEditorCurve(clip, b);
            groupBindings[b.path][idx] = b;
        }

        int fixedKeys = 0;

        foreach (KeyValuePair<string, AnimationCurve[]> kv in groups)
        {
            AnimationCurve[] cs = kv.Value;
            if (cs[0] == null || cs[1] == null || cs[2] == null || cs[3] == null) continue;

            int n = cs[0].length;
            if (cs[1].length != n || cs[2].length != n || cs[3].length != n) continue;
            if (n < 2) continue;

            Keyframe[][] keys = new Keyframe[4][];
            for (int k = 0; k < 4; k++) keys[k] = cs[k].keys;

            double px = keys[0][0].value, py = keys[1][0].value, pz = keys[2][0].value, pw = keys[3][0].value;

            for (int i = 1; i < n; i++)
            {
                double x = keys[0][i].value, y = keys[1][i].value, z = keys[2][i].value, w = keys[3][i].value;
                double dot = px * x + py * y + pz * z + pw * w;

                if (dot < 0.0)
                {
                    x = -x; y = -y; z = -z; w = -w;
                    keys[0][i].value = (float)x;
                    keys[1][i].value = (float)y;
                    keys[2][i].value = (float)z;
                    keys[3][i].value = (float)w;
                    fixedKeys++;
                }

                px = x; py = y; pz = z; pw = w;
            }

            for (int k = 0; k < 4; k++)
            {
                cs[k].keys = keys[k];
                AnimationUtility.SetEditorCurve(clip, groupBindings[kv.Key][k], cs[k]);
            }
        }

        return fixedKeys;
    }

    [MenuItem("Tools/Sandbox/13. Вернуть анимацию корня Bip01 (убрать наклон, вернуть покачивание)")]
    public static void RestoreRootAnimation()
    {
        int a = AddRootCurves("Assets/Animation/Clips/Player_Idle.anim", "Assets/Animation/Clips/root_Bip01_Idle.txt", "Player_Idle");
        int b = AddRootCurves("Assets/Animation/Clips/Player_Walk.anim", "Assets/Animation/Clips/root_Bip01_Walk.txt", "Player_Walk");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Анимация корня Bip01",
            $"Добавлено ключей:\nPlayer_Idle: {a}\nPlayer_Walk: {b}\n\n" +
            "У корня теперь есть и поворот, и смещение — как в Blender.\n" +
            "Персонаж перестанет быть наклонённым и начнёт покачиваться в такт.\n\n" +
            "ПРОВЕРЬ: не ушёл ли персонаж под землю или в воздух.\n" +
            "Если да — скажи, я поправлю смещение.", "ОК");
    }

    private static readonly string[] RootAttributes =
    {
        "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
        "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
    };

    /// <summary>
    /// GLTFUtility теряет анимацию корневого узла Bip01 — в клипах нет ни одной
    /// кривой с путём ровно "Bip01". Из-за этого персонаж стоит в rest-повороте
    /// (отсюда наклон и «смотрит вбок») и не покачивается корпусом.
    /// Данные берём из Assets/4.gltf и кладём готовые значения из txt-файлов,
    /// которые сгенерированы с пересчётом: позиция Unity = (-x, y, z),
    /// кватернион Unity = (x, -y, -z, w). Формула сверена по rest-позе: 0.0054°.
    /// </summary>
    private static int AddRootCurves(string clipPath, string dataPath, string label)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning($"[AnimationCutter] не найден {clipPath}");
            return -1;
        }

        if (!File.Exists(dataPath))
        {
            Debug.LogWarning($"[AnimationCutter] нет файла данных {dataPath}");
            return -1;
        }

        string[] lines = File.ReadAllLines(dataPath);
        if (lines.Length == 0) return -1;

        // Времена ключей берём из уже существующей кривой клипа — так они точно совпадут
        EditorCurveBinding[] existing = AnimationUtility.GetCurveBindings(clip);
        if (existing.Length == 0)
        {
            Debug.LogWarning($"[AnimationCutter] в {label} нет кривых");
            return -1;
        }

        AnimationCurve sampleCurve = AnimationUtility.GetEditorCurve(clip, existing[0]);
        if (sampleCurve == null || sampleCurve.length != lines.Length)
        {
            Debug.LogError($"[AnimationCutter] {label}: ключей в клипе {(sampleCurve != null ? sampleCurve.length : -1)}, " +
                           $"строк в файле {lines.Length} — не совпало. Сначала пересобери клипы (пункт 2).");
            return -1;
        }

        float[] times = new float[sampleCurve.length];
        for (int i = 0; i < times.Length; i++) times[i] = sampleCurve.keys[i].time;

        Keyframe[][] keys = new Keyframe[7][];
        for (int k = 0; k < 7; k++) keys[k] = new Keyframe[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 7) continue;

            for (int k = 0; k < 7; k++)
            {
                keys[k][i].time = times[i];
                keys[k][i].value = float.Parse(parts[k], CultureInfo.InvariantCulture);
            }
        }

        for (int k = 0; k < 7; k++)
        {
            AnimationCurve curve = new AnimationCurve(keys[k]);
            MakeTangents(curve, AnimationUtility.TangentMode.Linear);
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("Bip01", typeof(Transform), RootAttributes[k]);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        int flips = FixQuaternionSigns(clip);
        EditorUtility.SetDirty(clip);

        Debug.Log($"[AnimationCutter] {label}: добавлено 7 кривых корня 'Bip01', ключей {lines.Length}, " +
                  $"перевёрнутых кватернионов исправлено {flips}");
        return lines.Length;
    }

    private static AnimationClip FindSourceClip()
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(SourcePath);
        foreach (Object o in all)
        {
            AnimationClip c = o as AnimationClip;
            if (c != null) return c;
        }
        return null;
    }

    private static void CutInto(AnimationClip source, string clipName, int startSample, int endSample, bool closeLoop = false)
    {
        float t0 = startSample / SampleHz;
        float t1 = endSample / SampleHz;
        float duration = t1 - t0;

        string path = $"{OutputFolder}/{clipName}.anim";
        AnimationClip target = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool existed = target != null;
        if (!existed)
        {
            target = new AnimationClip();
            AssetDatabase.CreateAsset(target, path);
        }

        target.ClearCurves();

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
        int copied = 0;
        int totalKeys = 0;

        foreach (EditorCurveBinding b in bindings)
        {
            AnimationCurve src = AnimationUtility.GetEditorCurve(source, b);
            if (src == null || src.keys.Length == 0) continue;

            List<Keyframe> keys = new List<Keyframe>();
            foreach (Keyframe k in src.keys)
            {
                if (k.time >= t0 - 1e-4f && k.time <= t1 + 1e-4f)
                {
                    Keyframe nk = k;
                    nk.time = k.time - t0;
                    keys.Add(nk);
                }
            }

            if (keys.Count == 0) continue;

            if (closeLoop && keys.Count > 2)
            {
                // Распределяем расхождение первого и последнего ключа по всему клипу:
                // в начале поправка 0, в конце — ровно на величину расхождения.
                // Так последний ключ становится РАВЕН первому, и цикл замыкается
                // без локальных рывков (поправка на кадр — доли градуса).
                float diff = keys[0].value - keys[keys.Count - 1].value;
                for (int i = 1; i < keys.Count; i++)
                {
                    float t = (float)i / (keys.Count - 1);
                    Keyframe kk = keys[i];
                    kk.value += diff * t;
                    keys[i] = kk;
                }
            }

            // ключи из glTF приходят с нулевыми касательными -> Unity делает
            // остановку в каждом кадре. Переводим в Linear, как в Blender.
            AnimationCurve newCurve = new AnimationCurve(keys.ToArray());
            MakeTangents(newCurve, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetEditorCurve(target, b, newCurve);
            copied++;
            totalKeys += keys.Count;
        }

        target.frameRate = SampleHz;

        // ключи из glTF приходят с перевёрнутыми знаками кватернионов — чиним сразу
        int flips = FixQuaternionSigns(target);

        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(target);
        s.loopTime = true;
        s.startTime = 0f;
        s.stopTime = duration;      // ровно по последнему ключу, без "залипания"
        AnimationUtility.SetAnimationClipSettings(target, s);

        EditorUtility.SetDirty(target);

        float seam = MeasureSeam(target);
        Debug.Log($"[AnimationCutter] {clipName}: сэмплы {startSample}..{endSample} " +
                  $"({startSample / SampleHz:F3}..{endSample / SampleHz:F3} с), " +
                  $"длительность {duration:F4} с, кривых {copied}/{bindings.Length}, ключей {totalKeys}, " +
                  $"frameRate {target.frameRate}, перевёрнутых кватернионов исправлено {flips}, " +
                  $"asset {(existed ? "перезаписан" : "создан")}\n" +
                  $"    макс. расхождение первого и последнего ключа: {seam:E3} " +
                  $"(для кватернионов это не градусы, а разница компоненты; близко к 0 = цикл замкнут)");
    }

    private static float MeasureSeam(AnimationClip clip)
    {
        float worst = 0f;
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding b in bindings)
        {
            AnimationCurve c = AnimationUtility.GetEditorCurve(clip, b);
            if (c == null || c.keys.Length < 2) continue;
            float d = Mathf.Abs(c.keys[c.keys.Length - 1].value - c.keys[0].value);
            if (d > worst) worst = d;
        }
        return worst;
    }
}
