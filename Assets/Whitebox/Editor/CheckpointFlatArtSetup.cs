using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class CheckpointFlatArtSetup
    {
        public const string TexturePath = LaboratoryMechanismArtSetup.ArtFolder + "lab-checkpoint-flat-layers.png";
        const string PrefabPath = "Assets/Whitebox/Prefabs/Mechanics/Checkpoint.prefab";

        [MenuItem("Whitebox/Art/Set Up Flat Checkpoint")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            ImportLayers();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Flat checkpoint art installed. Checkpoint triggers, save settings and scene layouts preserved.");
        }

        public static void Configure(GameObject root)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().ToArray();
            if (!sprites.Any(s => s.name == "LabCheckpointFlat_Shell") || !sprites.Any(s => s.name == "LabCheckpointFlat_Light"))
                sprites = ImportLayers();
            var checkpoint = root.GetComponent<WhiteboxCheckpoint>();
            if (!checkpoint || !checkpoint.halo) throw new InvalidOperationException("Missing checkpoint visual references.");
            var child = checkpoint.halo.transform.Find("MechanismArt");
            if (!child) { child = new GameObject("MechanismArt").transform; child.SetParent(checkpoint.halo.transform, false); }
            var art = child.GetComponent<LaboratoryMechanismVisual>();
            if (!art) art = child.gameObject.AddComponent<LaboratoryMechanismVisual>();
            var shell = child.GetComponent<SpriteRenderer>();
            if (!shell) shell = child.gameObject.AddComponent<SpriteRenderer>();
            var lights = child.Find("StatusLight");
            if (!lights) { lights = new GameObject("StatusLight").transform; lights.SetParent(child, false); }
            var light = lights.GetComponent<SpriteRenderer>();
            if (!light) light = lights.gameObject.AddComponent<SpriteRenderer>();
            shell.sprite = sprites.Single(s => s.name == "LabCheckpointFlat_Shell");
            light.sprite = sprites.Single(s => s.name == "LabCheckpointFlat_Light");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            shell.sharedMaterial = light.sharedMaterial = material;
            shell.sortingLayerID = light.sortingLayerID = checkpoint.halo.sortingLayerID;
            shell.sortingOrder = Mathf.Max(3, checkpoint.halo.sortingOrder);
            light.sortingOrder = shell.sortingOrder + 1;
            art.geometrySource = checkpoint.halo; art.tintSource = checkpoint.beam;
            art.shellRenderer = shell; art.lightRenderer = light;
            art.shellFollowsAlpha = false; art.alphaReference = .6f; art.drawMode = SpriteDrawMode.Sliced;
            art.legacyRenderers = new[] { checkpoint.halo, checkpoint.beam, checkpoint.marker };
            art.RefreshVisual();
        }

        static Sprite[] ImportLayers()
        {
            // Both halves share the same crop to retain registration between casing and light.
            var texture = new Texture2D(2, 2);
            Rect crop;
            int halfWidth;
            try
            {
                if (!texture.LoadImage(System.IO.File.ReadAllBytes(TexturePath))) throw new InvalidOperationException("Cannot read checkpoint image.");
                halfWidth = texture.width / 2;
                var pixels = texture.GetPixels32();
                int left = halfWidth, right = -1, bottom = texture.height, top = -1;
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < halfWidth; x++)
                    {
                        if (pixels[y * texture.width + x].a < 128) continue;
                        left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                    }
                if (right < left) throw new InvalidOperationException("Checkpoint casing layer is empty.");
                left = Mathf.Max(0, left - 4); right = Mathf.Min(halfWidth - 1, right + 4);
                bottom = Mathf.Max(0, bottom - 4); top = Mathf.Min(texture.height - 1, top + 4);
                crop = new Rect(left, bottom, right - left + 1, top - bottom + 1);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = crop.height / 2.7f;
            importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects = new[] {
                new SpriteRect { name="LabCheckpointFlat_Shell", rect=crop, pivot=Vector2.one*.5f, alignment=SpriteAlignment.Custom },
                new SpriteRect { name="LabCheckpointFlat_Light", rect=new Rect(crop.x+halfWidth,crop.y,crop.width,crop.height), pivot=Vector2.one*.5f, alignment=SpriteAlignment.Custom }
            };
            var previous = provider.GetSpriteRects();
            foreach (var rect in rects)
            {
                var old = previous.FirstOrDefault(r => r.name == rect.name);
                rect.spriteID = old != null ? old.spriteID : GUID.Generate();
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().ToArray();
        }
    }
}
