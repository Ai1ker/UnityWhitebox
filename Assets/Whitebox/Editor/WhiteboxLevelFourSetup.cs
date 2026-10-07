using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VectorWhitebox.Editor
{
    public static class WhiteboxLevelFourSetup
    {
        const string Level3 = "Assets/Whitebox/Scenes/Level03_Blank.unity";
        const string Level4 = "Assets/Whitebox/Scenes/Level04_Blank.unity";
        public const string HintPrefab = "Assets/Whitebox/Prefabs/SceneHintText.prefab";

        [MenuItem("Whitebox/Initialize Level 4 and Hint Text Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var original = SceneManager.GetActiveScene();
            var third = SceneManager.GetSceneByPath(Level3);
            bool openedThird = !third.isLoaded;
            if (openedThird) third = EditorSceneManager.OpenScene(Level3, OpenSceneMode.Additive);
            try
            {
                int exits = 0;
                foreach (var root in third.GetRootGameObjects())
                    foreach (var portal in root.GetComponentsInChildren<ExitPortal>(true))
                    {
                        exits++;
                        if (portal.nextScene == "Level04_Blank") continue;
                        portal.nextScene = "Level04_Blank";
                        PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                        EditorUtility.SetDirty(portal);
                        EditorSceneManager.MarkSceneDirty(third);
                    }
                if (exits == 0) throw new InvalidOperationException("Level 3 has no ExitPortal to connect.");
                if (!EditorSceneManager.SaveScene(third)) throw new IOException("Could not save Level 3 exit routing.");
            }
            finally { if (openedThird) EditorSceneManager.CloseScene(third, true); }

            if (!File.Exists(Level4)) CreateLevelFour();
            var builds = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existing = builds.Find(s => s.path == Level4);
            if (existing == null) builds.Add(new EditorBuildSettingsScene(Level4, true));
            else existing.enabled = true;
            EditorBuildSettings.scenes = builds.ToArray();
            CreateHintPrefab();
            if (original.isLoaded) SceneManager.SetActiveScene(original);
            Debug.Log("WHITEBOX: Level 3 exits lead to Level 4. Level 4 and SceneHintText prefab are ready.");
        }

        static GameObject Spawn(string path, Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new IOException("Missing prefab: " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        }
        static void CreateLevelFour()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var player = Spawn("Assets/Whitebox/Prefabs/Characters/Player.prefab", scene).GetComponent<WhiteboxPlayer>();
                player.transform.position = new Vector3(0, 1, 0);
                var camera = Spawn("Assets/Whitebox/Prefabs/Core/Camera2D.prefab", scene).GetComponent<Camera>();
                camera.transform.position = new Vector3(3, 2.66f, -10);
                camera.orthographicSize = 6.8f;
                Spawn("Assets/Whitebox/Prefabs/Core/GlobalLight2D.prefab", scene).transform.position = Vector3.zero;
                var mainLight = Spawn("Assets/Whitebox/Prefabs/Core/MainLight.prefab", scene);
                mainLight.transform.position = new Vector3(0, 3, 0);
                var game = Spawn("Assets/Whitebox/Prefabs/Core/GameCore.prefab", scene).GetComponent<WhiteboxGame>();
                game.player = player;
                game.gameCamera = camera;
                game.skills = game.GetComponent<DirectionSkills>();
                game.prototypeLevelOne = false;
                game.useCameraBounds = false;
                game.followPlayerY = true;
                game.cameraOffset = new Vector2(3, 1.66f);
                game.killBelowY = -20;
                PrefabUtility.RecordPrefabInstancePropertyModifications(game);
                if (!EditorSceneManager.SaveScene(scene, Level4)) throw new IOException("Could not save Level 4.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        public static void CreateHintPrefab(bool overwrite = false)
        {
            if (File.Exists(HintPrefab) && !overwrite) return;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("SceneHintText", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.localScale = Vector3.one * .01f;
                root.transform.position = new Vector3(0, 3, -1);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 20;
                var hint = root.AddComponent<SceneHintText>();
                hint.backgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
                hint.Refresh();
                if (!PrefabUtility.SaveAsPrefabAsset(root, HintPrefab)) throw new IOException("Could not save hint prefab.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
