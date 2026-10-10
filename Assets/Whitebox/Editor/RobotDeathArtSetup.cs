using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class RobotDeathArtSetup
    {
        public const string AtlasPath = "Assets/Whitebox/ArtAssets/Characters/HaloRobot/halo-robot-death.png";

        [MenuItem("Whitebox/Art/Set Up Robot Death Animation")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before importing death art.");
            var source = new Texture2D(2, 2);
            SpriteRect[] rects;
            float pixelsPerUnit;
            try
            {
                source.LoadImage(System.IO.File.ReadAllBytes(AtlasPath));
                pixelsPerUnit = 160f * (source.width / 4f) / 256f;
                var pixels = source.GetPixels32();
                rects = new SpriteRect[8];
                for (int i = 0; i < rects.Length; i++)
                {
                    int x0 = Mathf.RoundToInt((i % 4) * source.width / 4f);
                    int x1 = Mathf.RoundToInt((i % 4 + 1) * source.width / 4f);
                    int y0 = Mathf.RoundToInt((1 - i / 4) * source.height / 2f);
                    int y1 = Mathf.RoundToInt((2 - i / 4) * source.height / 2f);
                    int bottom = y1;
                    for (int y = y0; y < y1; y++)
                        for (int x = x0; x < x1; x++)
                        {
                            var pixel = pixels[y * source.width + x];
                            bool spark = pixel.g > pixel.r * 1.7f && pixel.b > pixel.r * 1.7f;
                            if (pixel.a >= 245 && !spark) bottom = Mathf.Min(bottom, y);
                        }
                    if (bottom == y1) throw new InvalidOperationException("Death frame " + i + " has no visible body.");
                    rects[i] = new SpriteRect {
                        name = "CoreRobot_Death_" + i.ToString("00"),
                        rect = new Rect(x0, y0, x1 - x0, y1 - y0),
                        alignment = SpriteAlignment.Custom,
                        // Follow the fixed floor/debris, never the falling core itself.
                        pivot = new Vector2(141.5f / 256f, (bottom - y0) / (float)(y1 - y0)),
                        spriteID = GUID.Generate()
                    };
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }

            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Death atlas importer is missing.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();

            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Death sprite data provider is unavailable.");
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects();
            foreach (var rect in rects)
            {
                var previous = old.FirstOrDefault(r => r.name == rect.name);
                if (previous != null) rect.spriteID = previous.spriteID;
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var frames = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 8) throw new InvalidOperationException("Expected eight death frames.");
            var root = PrefabUtility.LoadPrefabContents(RobotPlayerArtSetup.VisualPrefabPath);
            try
            {
                var visual = root.GetComponent<RobotPlayerVisual>();
                if (!visual) throw new InvalidOperationException("Robot visual prefab is missing its animation component.");
                visual.deathFrames = frames;
                PrefabUtility.SaveAsPrefabAsset(root, RobotPlayerArtSetup.VisualPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Robot death: eight frames assigned. Existing movement, cast animation settings and levels preserved.");
        }
    }
}
