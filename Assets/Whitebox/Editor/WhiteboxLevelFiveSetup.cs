using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    public static class WhiteboxLevelFiveSetup
    {
        const string Level4 = "Assets/Whitebox/Scenes/Level04_Blank.unity";
        const string Level5 = "Assets/Whitebox/Scenes/Level05_Blank.unity";
        const string Prefabs = "Assets/Whitebox/Prefabs/";

        [MenuItem("Whitebox/Initialize Level 5")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var original = SceneManager.GetActiveScene();
            var fourth = SceneManager.GetSceneByPath(Level4);
            if (fourth.isLoaded && fourth.isDirty)
                throw new InvalidOperationException("Save Level 4 before initializing the next level.");
            bool openedFourth = !fourth.isLoaded;
            if (openedFourth) fourth = EditorSceneManager.OpenScene(Level4, OpenSceneMode.Additive);
            try
            {
                var portals = new List<ExitPortal>();
                foreach (var root in fourth.GetRootGameObjects())
                    portals.AddRange(root.GetComponentsInChildren<ExitPortal>(true));
                if (portals.Count == 0) throw new InvalidOperationException("Level 4 has no ExitPortal to connect.");
                if (!File.Exists(Level5)) CreateLevelFive();

                var builds = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                var existing = builds.Find(s => s.path == Level5);
                if (existing == null) builds.Add(new EditorBuildSettingsScene(Level5, true));
                else existing.enabled = true;
                EditorBuildSettings.scenes = builds.ToArray();

                bool changed = false;
                foreach (var portal in portals)
                {
                    if (portal.nextScene == "Level05_Blank") continue;
                    portal.nextScene = "Level05_Blank";
                    PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                    EditorUtility.SetDirty(portal);
                    changed = true;
                }
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(fourth);
                    if (!EditorSceneManager.SaveScene(fourth)) throw new IOException("Could not save Level 4 exit routing.");
                }
            }
            finally
            {
                if (openedFourth) EditorSceneManager.CloseScene(fourth, true);
                if (original.isLoaded) SceneManager.SetActiveScene(original);
            }
            Debug.Log("WHITEBOX: Level 4 exits lead to Level 5. Level 5 is ready for layout design.");
        }

        static GameObject Spawn(string path, Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + path);
            if (!prefab) throw new IOException("Missing prefab: " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        }

        static void CreateLevelFive()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var player = Spawn("Characters/Player.prefab", scene).GetComponent<WhiteboxPlayer>();
                player.transform.position = new Vector3(0, 1, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                var camera = Spawn("Core/Camera2D.prefab", scene).GetComponent<Camera>();
                camera.transform.position = new Vector3(3, 2.66f, -10);
                camera.orthographicSize = 6.8f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                var globalLight = Spawn("Core/GlobalLight2D.prefab", scene);
                globalLight.transform.position = Vector3.zero;
                PrefabUtility.RecordPrefabInstancePropertyModifications(globalLight.transform);
                var mainLight = Spawn("Core/MainLight.prefab", scene);
                mainLight.transform.position = new Vector3(0, 3, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(mainLight.transform);
                var game = Spawn("Core/GameCore.prefab", scene).GetComponent<WhiteboxGame>();
                game.player = player;
                game.gameCamera = camera;
                game.skills = game.GetComponent<DirectionSkills>();
                game.prototypeLevelOne = false;
                game.useCameraBounds = false;
                game.followPlayerY = true;
                game.cameraOffset = new Vector2(3, 1.66f);
                game.killBelowY = -20;
                PrefabUtility.RecordPrefabInstancePropertyModifications(game);
                if (!EditorSceneManager.SaveScene(scene, Level5)) throw new IOException("Could not save Level 5.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
