using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    /// <summary>Creates the new test assets without rebuilding any authored level.</summary>
    public static class WhiteboxReleaseSetup
    {
        public const string LevelFivePath = "Assets/Whitebox/Scenes/Level05_Blank.unity";
        public const string LevelSixPath = "Assets/Whitebox/Scenes/Level06_Blank.unity";
        public const string BoundaryPrefabPath = "Assets/Whitebox/Prefabs/Mechanics/ForcedGravityBoundary.prefab";
        public const string FinalPortalPrefabPath = "Assets/Whitebox/Prefabs/Mechanics/FinalExitPortal.prefab";
        public const string ArtFolderPath = "Assets/Whitebox/ArtAssets";
        const string MainMenuPath = "Assets/Whitebox/Scenes/MainMenu.unity";
        const string EndingScenePath = "Assets/Whitebox/Scenes/GameEnd.unity";
        const string EndingMenuPrefabPath = "Assets/Whitebox/Prefabs/UI/EndingMenu.prefab";
        const string Prefabs = "Assets/Whitebox/Prefabs/";

        [MenuItem("Whitebox/Initialize Level 6 and Game Menus")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before initializing the assets.");
            var original = SceneManager.GetActiveScene();
            var fifth = SceneManager.GetSceneByPath(LevelFivePath);
            if (fifth.isLoaded && fifth.isDirty)
                throw new InvalidOperationException("Save Level 5 before connecting its exit to Level 6. Unsaved layout changes were preserved.");

            bool openedFifth = !fifth.isLoaded;
            if (openedFifth) fifth = EditorSceneManager.OpenScene(LevelFivePath, OpenSceneMode.Additive);
            try
            {
                var portals = new List<ExitPortal>();
                foreach (var root in fifth.GetRootGameObjects())
                    portals.AddRange(root.GetComponentsInChildren<ExitPortal>(true));
                if (portals.Count == 0)
                    throw new InvalidOperationException("Level 5 has no ExitPortal to connect.");

                WhiteboxFrontendBuilder.Build();
                if (!File.Exists(LevelSixPath)) CreateLevelSix();
                CreateBoundaryPrefab();
                CreateFinalPortalPrefab();
                if (!AssetDatabase.IsValidFolder(ArtFolderPath))
                {
                    if (string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/Whitebox", "ArtAssets")))
                        throw new IOException("Could not create the empty ArtAssets folder.");
                }
                UpdateBuildScenes();

                bool changed = false;
                foreach (var portal in portals)
                {
                    if (portal.nextScene == "Level06_Blank") continue;
                    portal.nextScene = "Level06_Blank";
                    PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                    EditorUtility.SetDirty(portal);
                    changed = true;
                }
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(fifth);
                    if (!EditorSceneManager.SaveScene(fifth))
                        throw new IOException("Could not save Level 5 exit routing.");
                }
            }
            finally
            {
                if (openedFifth) EditorSceneManager.CloseScene(fifth, true);
                if (original.isLoaded) SceneManager.SetActiveScene(original);
            }
            Debug.Log("WHITEBOX: Level 6, red gravity boundary, final portal, game menus, and empty ArtAssets folder are ready. Existing layouts were preserved.");
        }

        static void CreateLevelSix()
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
                if (!EditorSceneManager.SaveScene(scene, LevelSixPath))
                    throw new IOException("Could not save the blank Level 6 scene.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        static GameObject Spawn(string path, Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + path);
            if (!prefab) throw new IOException("Missing prefab: " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        }

        static void CreateBoundaryPrefab()
        {
            if (File.Exists(BoundaryPrefabPath)) return;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("ForcedGravityBoundary");
                SceneManager.MoveGameObjectToScene(root, preview);
                var boundary = root.AddComponent<ForcedGravityBoundary>();
                boundary.direction = CardinalGravityDirection.Down;
                boundary.size = new Vector2(12, 8);
                boundary.showBoundary = true;
                boundary.lineWidth = .065f;
                boundary.boundaryColor = new Color(1f, .16f, .16f, .9f);
                boundary.outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
                if (!boundary.outlineMaterial) throw new IOException("Missing whitebox outline material.");
                var area = root.GetComponent<BoxCollider2D>();
                if (!area) area = root.AddComponent<BoxCollider2D>();
                area.isTrigger = true;
                area.size = boundary.size;
                if (!PrefabUtility.SaveAsPrefabAsset(root, BoundaryPrefabPath))
                    throw new IOException("Could not create the red gravity boundary prefab.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        static void CreateFinalPortalPrefab()
        {
            if (File.Exists(FinalPortalPrefabPath)) return;
            var endingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EndingMenuPrefabPath);
            if (!endingPrefab || !endingPrefab.GetComponent<WhiteboxFrontend>())
                throw new IOException("Missing EndingMenu prefab or WhiteboxFrontend component.");
            var root = PrefabUtility.LoadPrefabContents(Prefabs + "Mechanics/ExitPortal.prefab");
            try
            {
                var oldPortal = root.GetComponent<ExitPortal>();
                if (!oldPortal) throw new IOException("ExitPortal prefab has no ExitPortal component.");
                var core = oldPortal.core;
                var halo = oldPortal.halo;
                var innerRing = oldPortal.innerRing;
                var outerRing = oldPortal.outerRing;
                UnityEngine.Object.DestroyImmediate(oldPortal);
                root.name = "FinalExitPortal";
                var portal = root.AddComponent<FinalExitPortal>();
                portal.requireTurretDefeated = false;
                portal.requireAllGatesOpen = false;
                portal.core = core;
                portal.halo = halo;
                portal.innerRing = innerRing;
                portal.outerRing = outerRing;
                portal.endingMenuPrefab = endingPrefab.GetComponent<WhiteboxFrontend>();
                if (!PrefabUtility.SaveAsPrefabAsset(root, FinalPortalPrefabPath))
                    throw new IOException("Could not create the final exit portal prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void UpdateBuildScenes()
        {
            var previous = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var ordered = new List<EditorBuildSettingsScene>();
            var required = new[]
            {
                MainMenuPath,
                "Assets/Whitebox/Scenes/Level01_Whitebox.unity",
                "Assets/Whitebox/Scenes/Level02_Blank.unity",
                "Assets/Whitebox/Scenes/Level03_Blank.unity",
                "Assets/Whitebox/Scenes/Level04_Blank.unity",
                LevelFivePath,
                LevelSixPath,
                EndingScenePath
            };
            foreach (var path in required)
            {
                if (!File.Exists(path)) throw new IOException("Missing build scene: " + path);
                var existing = previous.Find(s => s.path == path);
                bool enabled = existing == null || existing.enabled;
                if (path == MainMenuPath) enabled = true;
                ordered.Add(new EditorBuildSettingsScene(path, enabled));
                previous.RemoveAll(s => s.path == path);
            }
            ordered.AddRange(previous);
            EditorBuildSettings.scenes = ordered.ToArray();
        }
    }
}
