using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    public static class CollapseHazardSetup
    {
        public const string PrefabPath = "Assets/Whitebox/Prefabs/Mechanics/CollapseHazard.prefab";

        [MenuItem("Whitebox/Create Collapse Hazard Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            // Existing assets may already be arranged by a designer. Re-running is safe.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
            var panel = LoadSprite("Assets/Whitebox/ArtAssets/Environment/Laboratory/lab-wall-panels.png", "LabWall_Panel");
            var beam = LoadSprite("Assets/Whitebox/ArtAssets/Environment/Laboratory/lab-oneway-platform.png", "LabPlatform_Beam");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Whitebox/Art/Square.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            if (!panel || !beam || !square || !material)
                throw new InvalidOperationException("The existing laboratory panel, beam, square or unlit material is missing.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("CollapseHazard");
                SceneManager.MoveGameObjectToScene(root, preview);
                var hazard = root.AddComponent<CollapseHazard>();
                var trigger = root.GetComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(12f, 6f);
                trigger.offset = new Vector2(0, -3.4f);
                hazard.segments = new CollapseDebris[6];
                for (int i = 0; i < hazard.segments.Length; i++)
                {
                    var segment = new GameObject("Ceiling slab " + (i + 1));
                    segment.transform.SetParent(root.transform, false);
                    segment.transform.localPosition = new Vector3(-5f + i * 2f, 0, 0);
                    var debris = segment.AddComponent<CollapseDebris>();
                    var shape = segment.GetComponent<BoxCollider2D>();
                    shape.isTrigger = true;
                    shape.size = new Vector2(1.94f, .62f);
                    shape.enabled = false;
                    var body = segment.GetComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.gravityScale = 0;
                    body.constraints = RigidbodyConstraints2D.FreezeRotation;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    var shell = Visual(segment.transform, "Silver laboratory panel", panel, material,
                        new Vector2(1.94f, .62f), Vector2.zero, new Color(.66f, .71f, .74f), 1);
                    shell.drawMode = SpriteDrawMode.Simple;
                    Visual(segment.transform, "Lower support edge", beam, material,
                        new Vector2(1.94f, .13f), new Vector2(0, -.3f), new Color(.76f, .82f, .85f), 2);
                    debris.warningStrip = Visual(segment.transform, "Warning strip", square, material,
                        new Vector2(.5f, .035f), new Vector2(.58f, -.235f), new Color(.25f, .65f, .68f, .95f), 3);
                    hazard.segments[i] = debris;
                }
                if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath))
                    throw new IOException("Could not save collapse hazard prefab.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            AssetDatabase.SaveAssets();
            Debug.Log("WHITEBOX: CollapseHazard prefab created in Mechanics. Trigger size/offset and Delay Seconds are editable; no scene was modified.");
        }

        static Sprite LoadSprite(string path, string name)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return sprites.FirstOrDefault(sprite => sprite.name == name) ?? sprites.FirstOrDefault();
        }

        static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, Material material,
            Vector2 size, Vector2 position, Color color, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localScale = new Vector3(size.x / Mathf.Max(.0001f, sprite.bounds.size.x),
                size.y / Mathf.Max(.0001f, sprite.bounds.size.y), 1);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
