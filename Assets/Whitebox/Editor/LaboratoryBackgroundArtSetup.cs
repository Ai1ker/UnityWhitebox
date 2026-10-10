using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox.Editor
{
    public static class LaboratoryBackgroundArtSetup
    {
        public const string ArtFolder = "Assets/Whitebox/ArtAssets/Backgrounds/Laboratory/";
        public const string PrefabFolder = "Assets/Whitebox/Prefabs/Backgrounds/";
        static Material material;

        [MenuItem("Whitebox/Art/Set Up Laboratory Backgrounds")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            System.IO.Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            var walls = Import("lab-background-walls.png", 887f / 8f, new[] {
                Frame("LabBackground_Panels",new Rect(0,0,887,887)),
                Frame("LabBackground_BrokenRebar",new Rect(887,0,887,887)) });
            var glass = Import("lab-glass-layers.png", 684f / 5.8f, new[] {
                Frame("LabGlass_Frame",new Rect(50,155,684,407)),
                Frame("LabGlass_Light",new Rect(774,155,684,407)),
                Frame("LabGlass_Shadow",new Rect(1488,155,684,407)) });
            var screen = Import("lab-display-layers.png", 681f / 4.8f, new[] {
                Frame("LabDisplay_Frame",new Rect(31,223,681,315)),
                Frame("LabDisplay_Cracks",new Rect(755,223,681,315)),
                Frame("LabDisplay_Glitch",new Rect(1479,223,681,315)) });
            BuildWall("BackgroundWall_Panels", walls[0]);
            BuildWall("BackgroundWall_BrokenRebar", walls[1]);
            BuildGlass(glass);
            BuildScreen(screen);
            Debug.Log("Four placeable laboratory background prefabs installed, without colliders or scene edits.");
        }

        static GameObject NewRoot(string name)
        {
            var root = new GameObject(name); root.SetActive(false); return root;
        }

        static SpriteRenderer Layer(Transform parent,string name,Sprite sprite,int order)
        {
            var go = new GameObject(name,typeof(SpriteRenderer)); go.transform.SetParent(parent,false);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = material; renderer.sortingOrder = order;
            return renderer;
        }

        static void Save(GameObject root)
        {
            try { root.SetActive(true); PrefabUtility.SaveAsPrefabAsset(root,PrefabFolder+root.name+".prefab"); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void BuildWall(string name,Sprite sprite)
        {
            var root = NewRoot(name);
            var wall = root.AddComponent<LaboratoryBackgroundWall>();
            wall.wallRenderer = Layer(root.transform,"Wall panels",sprite,name.Contains("BrokenRebar") ? -99 : -100);
            wall.RefreshVisual(); Save(root);
        }

        static void BuildGlass(Sprite[] sprites)
        {
            var root = NewRoot("LaboratoryGlass_Flicker");
            var glass = root.AddComponent<LaboratoryGlassFlicker>();
            glass.glassRenderer = Layer(root.transform,"Frame and glass",sprites[0],-80);
            glass.lightRenderer = Layer(root.transform,"Faulty lamp reflections",sprites[1],-79);
            glass.shadowRenderer = Layer(root.transform,"Shifting industrial shadows",sprites[2],-78);
            glass.glassTint = new Color(.84f,.91f,.95f,.85f);
            glass.lightTint = new Color(.53f,.84f,.93f,.18f);
            glass.shadowTint = new Color(.04f,.06f,.08f,.36f);
            glass.RefreshVisual(); Save(root);
        }

        static void BuildScreen(Sprite[] sprites)
        {
            var root = NewRoot("LaboratoryScreen_Cracked");
            var screen = root.AddComponent<LaboratoryDisplayScreen>();
            // Reuse the existing unit square only for the solid display backing.
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/Mechanics/PressurePlate.prefab");
            var square = source.GetComponent<SpriteRenderer>().sprite;
            screen.panelRenderer = Layer(root.transform,"Dark display panel",square,-71);
            screen.panelRenderer.transform.localScale = new Vector3(4.35f,1.62f,1);
            screen.panelRenderer.transform.localPosition = new Vector3(0,.035f,0);
            screen.frameRenderer = Layer(root.transform,"Silver display casing",sprites[0],-70);
            screen.crackRenderer = Layer(root.transform,"Protective glass cracks and bullet impact",sprites[1],-68);
            screen.glitchRenderer = Layer(root.transform,"Transient electronic interference",sprites[2],-66);
            var canvasObject = new GameObject("Screen Text",typeof(RectTransform),typeof(Canvas));
            canvasObject.transform.SetParent(root.transform,false);
            canvasObject.transform.localScale = Vector3.one * .01f;
            canvasObject.transform.localPosition = new Vector3(0,.035f,0);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true; canvas.sortingOrder = -67;
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(406,132);
            var label = new GameObject("Bilingual display message",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            label.transform.SetParent(canvasObject.transform,false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8,8); rect.offsetMax = new Vector2(-8,-8);
            screen.screenCanvas = canvas; screen.screenText = label.GetComponent<Text>();
            screen.fontSize = 25; screen.minFontSize = 12;
            screen.textColor = new Color(.61f,.89f,.87f,1);
            screen.crackColor = new Color(.7f,.8f,.83f,.32f);
            screen.chineseContent = "实验室信息终端\n设备离线 · 等待维护\n在 Inspector 中填写提示内容。";
            screen.englishContent = "LABORATORY TERMINAL\nSystems offline. Maintenance required.\nEnter your message in the Inspector.";
            screen.RefreshVisual(); Save(root);
        }

        static SpriteRect Frame(string name,Rect rect)
        { return new SpriteRect { name=name,rect=rect,pivot=Vector2.one*.5f,alignment=SpriteAlignment.Custom }; }

        static Sprite[] Import(string file,float ppu,SpriteRect[] rects)
        {
            string path = ArtFolder+file; AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            // Dense background detail is usually smaller than a screen pixel, especially when zoomed out.
            // Filter only the wall atlas so smooth camera movement does not make its fine lines shimmer.
            bool wallTexture = file == "lab-background-walls.png";
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = wallTexture ? FilterMode.Trilinear : FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = wallTexture;
            importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var oldRects = provider.GetSpriteRects();
            foreach (var rect in rects)
            {
                var old = oldRects.FirstOrDefault(r=>r.name==rect.name);
                rect.spriteID = old != null ? old.spriteID : GUID.Generate();
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
            return rects.Select(r=>sprites[r.name]).ToArray();
        }
    }
}
