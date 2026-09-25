using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapLoader
{
    /// <summary>
    /// Делает из текущей сцены «лёгкую» копию: удаляет запечённую карту
    /// (террейн, клоны зданий, мусорные маркеры) и оставляет только
    /// MapLoader, Player, камеру, свет и Global Volume.
    ///
    /// Карта после этого собирается из GLB в рантайме — при старте (MapLoader.loadOnStart).
    /// Исходный файл сцены НЕ перезаписывается: результат сохраняется в отдельный файл.
    /// </summary>
    public static class SandboxMinimizer
    {
        private const string TargetPath = "Assets/Scenes/Sandbox_Minimal.unity";

        [MenuItem("Tools/Sandbox/1. Сделать лёгкую сцену")]
        public static void MakeMinimalScene()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Лёгкая сцена", "Нет открытой сцены.", "ОК");
                return;
            }

            MapLoader loader = Object.FindFirstObjectByType<MapLoader>();
            if (loader == null)
            {
                EditorUtility.DisplayDialog(
                    "Лёгкая сцена",
                    $"В сцене «{scene.name}» не найден объект с компонентом MapLoader.",
                    "ОК");
                return;
            }

            string overwriteNote = File.Exists(TargetPath)
                ? "\n\nВНИМАНИЕ: файл уже существует и будет перезаписан."
                : "";

            if (!EditorUtility.DisplayDialog(
                    "Сделать лёгкую сцену?",
                    $"Текущая сцена: {scene.name}\n\n" +
                    $"Будет создана копия без запечённой карты:\n{TargetPath}\n\n" +
                    "Удаляется: террейн, клоны зданий, мусорные маркеры.\n" +
                    "Остаётся: MapLoader, Player, Main Camera, свет, Global Volume.\n\n" +
                    "Исходный файл сцены НЕ перезаписывается." +
                    overwriteNote,
                    "Сделать", "Отмена"))
            {
                return;
            }

            long sizeBefore = FileSize(scene.path);

            // 1. Что удаляем: сначала по ссылкам самого MapLoader, потом страховка по именам
            HashSet<GameObject> toDelete = new HashSet<GameObject>();

            if (loader.objectsParent != null) toDelete.Add(loader.objectsParent.gameObject);
            if (loader.terrainParent != null) toDelete.Add(loader.terrainParent.gameObject);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.EndsWith("_Objects") ||
                    root.name.EndsWith("_Terrain") ||
                    root.name == "DestinationMarker")
                {
                    toDelete.Add(root);
                }
            }

            // 2. Удаляем
            int deleted = 0;
            foreach (GameObject go in toDelete)
            {
                if (go == null) continue;
                int children = go.GetComponentsInChildren<Transform>(true).Length;
                Debug.Log($"[SandboxMinimizer] Удаляю «{go.name}» (объектов внутри: {children})");
                Object.DestroyImmediate(go);
                deleted++;
            }

            // 3. Чистим ссылки и переключаем на рантайм-загрузку
            loader.objectsParent = null;
            loader.terrainParent = null;
            loader.spawnedPlayer = null;
            loader.manifest = null;                 // запечённый манифест больше не нужен
            loader.spawnPlayerAtSpawnPoint = false; // игрок уже стоит в сцене
            loader.loadOnStart = true;              // карта собирается из GLB при старте
            EditorUtility.SetDirty(loader);

            MarkAllDirty(scene);
            EditorSceneManager.MarkSceneDirty(scene);

            // 4. Save As: открытой становится новая сцена, оригинал на диске не трогаем
            if (!EditorSceneManager.SaveScene(scene, TargetPath))
            {
                EditorUtility.DisplayDialog("Лёгкая сцена", "Не удалось сохранить сцену.", "ОК");
                return;
            }

            AssetDatabase.Refresh();

            long sizeAfter = FileSize(TargetPath);

            static string Mb(long bytes) => (bytes / 1024f / 1024f).ToString("F1");

            Debug.Log(
                "[SandboxMinimizer] Готово.\n" +
                $"  Удалено корневых объектов: {deleted}\n" +
                $"  Размер сцены: {Mb(sizeBefore)} МБ -> {Mb(sizeAfter)} МБ\n" +
                $"  Новая сцена: {TargetPath}");

            string extra = sizeAfter > 50L * 1024 * 1024
                ? "\n\nВНИМАНИЕ: файл всё ещё большой — значит в сцене остались " +
                  "неиспользуемые меши/текстуры. Сообщи мне, сделаем сцену с нуля."
                : "";

            EditorUtility.DisplayDialog(
                "Лёгкая сцена",
                $"Готово.\n\n" +
                $"Было: {Mb(sizeBefore)} МБ\nСтало: {Mb(sizeAfter)} МБ\n" +
                $"Удалено объектов: {deleted}\n\n" +
                $"Открыта новая сцена: {TargetPath}\n\n" +
                "Проверь: нажми Play — карта должна собраться из GLB автоматически." +
                extra,
                "ОК");
        }

        private static void MarkAllDirty(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                EditorUtility.SetDirty(root);
            }
        }

        private static long FileSize(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return 0L;
            FileInfo fi = new FileInfo(assetPath);
            return fi.Exists ? fi.Length : 0L;
        }
    }
}
