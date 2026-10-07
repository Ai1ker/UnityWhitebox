#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace VectorWhitebox
{
    /// <summary>Creates new frontend assets without replacing existing authored scenes or prefabs.</summary>
    public static class WhiteboxFrontendBuilder
    {
        public const string MainPrefabPath = "Assets/Whitebox/Prefabs/UI/MainMenu.prefab";
        public const string EndingPrefabPath = "Assets/Whitebox/Prefabs/UI/EndingMenu.prefab";
        public const string MainScenePath = "Assets/Whitebox/Scenes/MainMenu.unity";
        public const string EndingScenePath = "Assets/Whitebox/Scenes/GameEnd.unity";

        [MenuItem("Whitebox/Build Main Menu and Ending UI")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before building frontend assets.");
            EnsureFolder("Assets/Whitebox/Prefabs/UI");
            EnsureFolder("Assets/Whitebox/Scenes");
            Scene previous = SceneManager.GetActiveScene();
            Scene staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(staging);
                var mainPrefab = BuildPrefab(MainPrefabPath, WhiteboxFrontend.ScreenMode.MainMenu);
                var endingPrefab = BuildPrefab(EndingPrefabPath, WhiteboxFrontend.ScreenMode.Ending);
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath))
                {
                    CreateSceneCore();
                    PrefabUtility.InstantiatePrefab(mainPrefab, staging);
                    if (!EditorSceneManager.SaveScene(staging, MainScenePath))
                        throw new System.IO.IOException("Could not save MainMenu scene.");
                }
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(EndingScenePath))
                {
                    // Reuse the temporary scene after clearing any main-menu contents.
                    foreach (var root in staging.GetRootGameObjects()) Object.DestroyImmediate(root);
                    CreateSceneCore();
                    PrefabUtility.InstantiatePrefab(endingPrefab, staging);
                    if (!EditorSceneManager.SaveScene(staging, EndingScenePath))
                        throw new System.IO.IOException("Could not save GameEnd scene.");
                }
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (staging.IsValid() && staging.isLoaded) EditorSceneManager.CloseScene(staging, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Frontend UI prefabs, MainMenu, and GameEnd scenes are ready.");
        }

        static void CreateSceneCore()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .045f, .065f);
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.GetComponent<Light>().type = LightType.Directional;
            lightObject.GetComponent<Light>().intensity = 1;
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
            var eventObject = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static GameObject BuildPrefab(string path, WhiteboxFrontend.ScreenMode mode)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;
            var frontend = WhiteboxFrontend.CreateDefault(mode);
            try { return PrefabUtility.SaveAsPrefabAsset(frontend.gameObject, path); }
            finally { Object.DestroyImmediate(frontend.gameObject); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
#endif
