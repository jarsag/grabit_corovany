using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Диагностика анимаций: показывает, какие кривые реально есть в клипе,
/// включая root-motion, и какие настройки зацикливания выставлены.
/// Меню: Tools → Sandbox → 0. Диагностика анимаций
/// </summary>
public static class AnimationDiagnostics
{
    private static readonly string[] RootMotionCandidates =
    {
        "m_RootMotionRotationCurves",
        "m_RootMotionPositionCurves",
        "m_RootMotionFloatCurves",
        "m_RootMotionCurves",
        "m_GenericRootTransformCurves",
        "m_HasGenericRootTransform",
        "m_HasMotionFloatCurves"
    };

    [MenuItem("Tools/Sandbox/0. Диагностика анимаций")]
    public static void Run()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== ДИАГНОСТИКА АНИМАЦИЙ ==========");

        // 1. Все клипы внутри 4.gltf
        Object[] all = AssetDatabase.LoadAllAssetsAtPath("Assets/4.gltf");
        sb.AppendLine($"Ассетов внутри Assets/4.gltf: {all.Length}");
        foreach (Object o in all)
        {
            AnimationClip clip = o as AnimationClip;
            if (clip == null) continue;
            DumpClip(sb, clip, "ИСТОЧНИК 4.gltf");
        }

        // 2. Нарезанные клипы
        foreach (string path in new[]
                 {
                     "Assets/Animation/Clips/Player_Idle.anim",
                     "Assets/Animation/Clips/Player_Walk.anim"
                 })
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                sb.AppendLine($"--- {path}: НЕ НАЙДЕН ---");
                continue;
            }
            DumpClip(sb, clip, path);
        }

        Debug.Log(sb.ToString());
    }

    private static void DumpClip(StringBuilder sb, AnimationClip clip, string title)
    {
        sb.AppendLine();
        sb.AppendLine($"========== [{title}] клип '{clip.name}' ==========");
        sb.AppendLine($"  length = {clip.length:F4} с");
        sb.AppendLine($"  frameRate = {clip.frameRate}");
        sb.AppendLine($"  frameCount = {clip.length * clip.frameRate:F1}");

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        sb.AppendLine($"  обычных кривых (GetCurveBindings) = {bindings.Length}");

        Dictionary<string, int> byPath = new Dictionary<string, int>();
        foreach (EditorCurveBinding b in bindings)
        {
            string key = string.IsNullOrEmpty(b.path) ? "<ПУСТОЙ ПУТЬ = корень>" : b.path;
            byPath.TryGetValue(key, out int c);
            byPath[key] = c + 1;
        }
        sb.AppendLine($"  уникальных путей: {byPath.Count}");
        sb.AppendLine("  --- пути ---");
        foreach (KeyValuePair<string, int> kv in byPath)
        {
            sb.AppendLine($"    {kv.Value,3} кривых : '{kv.Key}'");
        }

        sb.AppendLine("  --- есть ли путь ровно 'Bip01' (анимация корня)? ---");
        bool hasRoot = false;
        foreach (EditorCurveBinding b in bindings)
        {
            if (b.path == "Bip01")
            {
                hasRoot = true;
                sb.AppendLine($"    ДА: {b.type.Name}.{b.propertyName}");
            }
        }
        if (!hasRoot) sb.AppendLine("    НЕТ — анимации корневого узла в обычных кривых нет");

        SerializedObject so = new SerializedObject(clip);
        sb.AppendLine("  --- root-motion / служебные поля ---");
        foreach (string name in RootMotionCandidates)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
            {
                sb.AppendLine($"    {name}: поля нет");
                continue;
            }
            if (p.isArray) sb.AppendLine($"    {name}: массив, размер {p.arraySize}");
            else sb.AppendLine($"    {name}: {p.propertyType}");
        }

        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        sb.AppendLine("  --- настройки клипа ---");
        sb.AppendLine($"    loopTime={s.loopTime}  loopBlend={s.loopBlend}");
        sb.AppendLine($"    loopBlendOrientation={s.loopBlendOrientation}  loopBlendPositionY={s.loopBlendPositionY}  loopBlendPositionXZ={s.loopBlendPositionXZ}");
        sb.AppendLine($"    keepOriginalOrientation={s.keepOriginalOrientation}  keepOriginalPositionY={s.keepOriginalPositionY}  keepOriginalPositionXZ={s.keepOriginalPositionXZ}");
        sb.AppendLine($"    heightFromFeet={s.heightFromFeet}  startTime={s.startTime:F4}  stopTime={s.stopTime:F4}");
    }

    [MenuItem("Tools/Sandbox/12. Где анимация корня Bip01")]
    public static void FindRootAnimation()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== ПОИСК АНИМАЦИИ КОРНЯ Bip01 ==========");

        // 1. Префаб: что стоит в трансформе корня в Unity
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/4.gltf");
        if (prefab == null)
        {
            sb.AppendLine("Не удалось загрузить GameObject из Assets/4.gltf");
        }
        else
        {
            Transform[] all = prefab.GetComponentsInChildren<Transform>(true);
            sb.AppendLine($"Объектов в префабе: {all.Length}");
            sb.AppendLine("--- корень и его прямые дети ---");
            foreach (Transform t in all)
            {
                if (t == prefab.transform || t.parent == prefab.transform || t.name == "Bip01")
                {
                    sb.AppendLine($"  '{t.name}'  родитель='{(t.parent != null ? t.parent.name : "нет")}'");
                    sb.AppendLine($"      localPos={t.localPosition}  localEuler={t.localEulerAngles}  localScale={t.localScale}");
                }
            }
        }

        // 2. Клипы: флаги root motion и ВСЕ сериализованные свойства
        DumpClipRoot(sb, "Assets/4.gltf", "ИСТОЧНИК 4.gltf");
        DumpClipRoot(sb, "Assets/Animation/Clips/Player_Idle.anim", "Player_Idle");
        DumpClipRoot(sb, "Assets/Animation/Clips/Player_Walk.anim", "Player_Walk");

        Debug.Log(sb.ToString());
    }

    private static void DumpClipRoot(StringBuilder sb, string assetPath, string title)
    {
        AnimationClip clip = null;

        if (assetPath.EndsWith(".gltf"))
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object o in all)
            {
                AnimationClip c = o as AnimationClip;
                if (c != null) { clip = c; break; }
            }
        }
        else
        {
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        if (clip == null)
        {
            sb.AppendLine($"--- [{title}]: клип не найден ---");
            return;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        int rootExact = 0, emptyPath = 0;
        foreach (EditorCurveBinding b in bindings)
        {
            if (b.path == "Bip01") rootExact++;
            if (string.IsNullOrEmpty(b.path)) emptyPath++;
        }

        sb.AppendLine();
        sb.AppendLine($"--- [{title}] '{clip.name}' ---");
        sb.AppendLine($"  кривых: {bindings.Length},  с путём ровно 'Bip01': {rootExact},  с пустым путём (корень аниматора): {emptyPath}");

        SerializedObject so = new SerializedObject(clip);
        SerializedProperty it = so.GetIterator();

        int total = 0;
        StringBuilder interesting = new StringBuilder();
        while (it.NextVisible(true))
        {
            total++;
            string n = it.propertyPath;
            if (n.IndexOf("Root", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Motion", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Generic", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string val = "";
                if (it.propertyType == SerializedPropertyType.Boolean) val = $" = {it.boolValue}";
                else if (it.isArray) val = $" (массив, размер {it.arraySize})";
                interesting.AppendLine($"    {n}  [{it.propertyType}]{val}");
            }
        }

        sb.AppendLine($"  всего сериализованных свойств: {total}");
        sb.AppendLine("  свойства со словами Root/Motion/Generic:");
        sb.Append(interesting.Length > 0 ? interesting.ToString() : "    (нет)\n");
    }
}
