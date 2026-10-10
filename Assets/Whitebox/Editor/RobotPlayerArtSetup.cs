using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class RobotPlayerArtSetup
    {
        public const string AtlasPath = "Assets/Whitebox/ArtAssets/Characters/HaloRobot/halo-robot-atlas.png";
        public const string VisualPrefabPath = "Assets/Whitebox/Resources/HaloRobotVisual.prefab";
        public const string PlayerPrefabPath = "Assets/Whitebox/Prefabs/Characters/Player.prefab";
        static readonly int[] FootY = {245,245,244,245,243,242,243,243,239,236,237,239,218,235,239,238,221,221,221,221,211,211,211,211};
        static readonly float[] EyeX = {177.5f,177.2f,177.9f,177.6f,196.4f,202.4f,194.5f,206.6f,201.3f,208.2f,201.5f,210.3f,197.3f,180f,157.1f,153.9f,155f,149.7f,156.1f,163.9f,161.8f,131.2f,137.7f,160.2f};

        [MenuItem("Whitebox/Art/Set Up Core Robot Player")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before importing character art.");
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Robot atlas is missing: " + AtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 160;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite data provider is unavailable.");
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects();
            var rects = new SpriteRect[24];
            for (int i = 0; i < rects.Length; i++)
            {
                string name = FrameName(i);
                var old = previous.FirstOrDefault(r => r.name == name);
                rects[i] = new SpriteRect {
                    name = name,
                    rect = new Rect((i % 4) * 256, (5 - i / 4) * 256, 256, 256),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2((EyeX[i] - 36f) / 256f, (256 - FootY[i] - 1) / 256f),
                    spriteID = old != null ? old.spriteID : GUID.Generate()
                };
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s => s.name);
            var ordered = Enumerable.Range(0, 24).Select(i => sprites[FrameName(i)]).ToArray();
            if (!AssetDatabase.IsValidFolder("Assets/Whitebox/Resources"))
                AssetDatabase.CreateFolder("Assets/Whitebox", "Resources");
            var temporary = new GameObject("RobotVisual");
            try
            {
                var renderer = temporary.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
                renderer.sortingOrder = 5; renderer.color = Color.white;
                var visual = temporary.AddComponent<RobotPlayerVisual>();
                AssignFrames(visual, renderer, ordered);
                PrefabUtility.SaveAsPrefabAsset(temporary, VisualPrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var player = root.GetComponent<WhiteboxPlayer>();
                if (!player) throw new InvalidOperationException("Player prefab has no WhiteboxPlayer component.");
                var visual = root.GetComponentInChildren<RobotPlayerVisual>(true);
                if (!visual)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath), root.transform);
                    instance.name = "RobotVisual";
                    visual = instance.GetComponent<RobotPlayerVisual>();
                }
                AssignFrames(visual, visual.GetComponent<SpriteRenderer>(), ordered);
                visual.BindToPlayer(player);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            if (System.IO.File.Exists(RobotDeathArtSetup.AtlasPath)) RobotDeathArtSetup.Setup();
            Debug.Log("Core robot: 24 transparent pixel frames imported and Player prefab updated. Level layouts were not modified.");
        }
        static void AssignFrames(RobotPlayerVisual visual, SpriteRenderer renderer, Sprite[] frames)
        {
            visual.spriteRenderer = renderer;
            visual.idleFrames = frames.Take(4).ToArray();
            visual.runFrames = frames.Skip(4).Take(8).ToArray();
            visual.jumpFrames = new[] {frames[12]}; visual.fallFrames = new[] {frames[13]};
            visual.skill1Frames = frames.Skip(14).Take(5).ToArray();
            visual.skill2Frames = frames.Skip(19).Take(5).ToArray();
            visual.referenceBodyHeight = 203f / 160f;
            renderer.sprite = frames[0]; renderer.color = Color.white;
        }
        static string FrameName(int i)
        {
            if (i < 4) return "CoreRobot_Idle_" + i.ToString("00");
            if (i < 12) return "CoreRobot_Run_" + (i - 4).ToString("00");
            if (i == 12) return "CoreRobot_Jump";
            if (i == 13) return "CoreRobot_Fall";
            if (i < 19) return "CoreRobot_Skill1_" + (i - 14).ToString("00");
            return "CoreRobot_Skill2_" + (i - 19).ToString("00");
        }
    }
}
