using System;
using UnityEditor;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class BoundaryGlowSetup
    {
        public const string ShaderPath = "Assets/Whitebox/Resources/BoundaryLineGlow.shader";
        public const string MaterialPath = "Assets/Whitebox/Resources/BoundaryLineGlow.mat";

        [MenuItem("Whitebox/Art/Set Up Boundary Glow")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!shader) throw new InvalidOperationException("Boundary glow shader is missing: " + ShaderPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                material = new Material(shader) { name = "BoundaryLineGlow" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetFloat("_Falloff", 2.5f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            Debug.Log("Boundary glow material ready. Original boundary colors, sizes and puzzle behavior are unchanged.");
        }
    }
}
