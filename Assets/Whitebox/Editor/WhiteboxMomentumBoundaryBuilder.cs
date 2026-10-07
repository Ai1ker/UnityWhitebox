using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    public static class WhiteboxMomentumBoundaryBuilder
    {
        public const string PrefabPath = "Assets/Whitebox/Prefabs/Mechanics/MomentumResetBoundary.prefab";
        [MenuItem("Whitebox/Create Orange Momentum Boundary Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(PrefabPath)) return;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("MomentumResetBoundary"); SceneManager.MoveGameObjectToScene(root, preview);
                var boundary = root.AddComponent<MomentumResetBoundary>();
                boundary.outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
                if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath)) throw new IOException("Cannot create boundary prefab.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Debug.Log("WHITEBOX: Orange momentum boundary prefab ready. Assign Cube in the Inspector.");
        }
    }
}
