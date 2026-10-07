using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    public static class WhiteboxPressurePlatformBuilder
    {
        public const string PrefabPath = "Assets/Whitebox/Prefabs/Mechanics/PressurePlatformSwitch.prefab";
        const string PlatformPath = "Assets/Whitebox/Prefabs/Environment/OneWayPlatform_A.prefab";

        [MenuItem("Whitebox/Create Pressure Platform Switch Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(PrefabPath)) return;

            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Whitebox/Art/Square.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            var grip = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Whitebox/Art/Grip.physicsMaterial2D");
            var platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPath);
            if (!square || !material || !grip || !platformPrefab)
                throw new InvalidOperationException("Pressure platform switch dependencies are missing.");

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Plate");
                SceneManager.MoveGameObjectToScene(root, preview);
                var pressure = root.AddComponent<PressurePlatformSwitch>();
                var collider = root.GetComponent<BoxCollider2D>();
                collider.isTrigger = false;
                collider.size = new Vector2(2.8f, .24f);
                collider.sharedMaterial = grip;

                var visual = new GameObject("Pressure plate visual");
                SceneManager.MoveGameObjectToScene(visual, preview);
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(2.8f, .24f, 1f);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.sharedMaterial = material;
                renderer.color = new Color(.95f, .63f, .22f, 1f);
                renderer.sortingOrder = 2;
                pressure.plateVisual = renderer;
                pressure.activeSeconds = 2f;
                pressure.platform1 = CreatePlatform(platformPrefab, root.transform, preview, 1, new Vector2(3f, 2f));
                pressure.platform2 = CreatePlatform(platformPrefab, root.transform, preview, 2, new Vector2(6f, 3.5f));
                pressure.platform3 = CreatePlatform(platformPrefab, root.transform, preview, 3, new Vector2(9f, 5f));

                if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath))
                    throw new IOException("Cannot create pressure platform switch prefab.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            Debug.Log("WHITEBOX: Pressure platform switch prefab ready. The solid upward-facing plate supports the player; its three one-way platforms can be moved or replaced independently.");
        }

        static GameObject CreatePlatform(GameObject prefab, Transform parent, Scene scene, int number, Vector2 position)
        {
            var platform = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            platform.name = "OneWayPlatform_" + number;
            platform.transform.SetParent(parent, false);
            platform.transform.localPosition = new Vector3(position.x, position.y, 0f);
            platform.SetActive(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(platform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(platform.transform);
            return platform;
        }
    }
}
