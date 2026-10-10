using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class LaboratoryArtSetup
    {
        public const string ArtFolder = "Assets/Whitebox/ArtAssets/Environment/Laboratory/";
        public const string EnvironmentFolder = "Assets/Whitebox/Prefabs/Environment/";
        public const string CubePath = "Assets/Whitebox/Prefabs/GravityCube.prefab";
        public const string PanelMaterialPath = ArtFolder + "WorldAlignedPanels.mat";

        [MenuItem("Whitebox/Art/Set Up Laboratory Environment And Cube")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var wall = Import("lab-wall-panels.png",1254f/4f,
                new[] {Frame("LabWall_Panel",new Rect(0,0,1254,1254),Vector2.one*.5f)});
            var cube = Import("lab-cube-layers.png",743f,new[] {
                Frame("LabCube_Shell",new Rect(144,95,743,698),new Vector2(.5f,345f/698f)),
                Frame("LabCube_Core",new Rect(964,86,743,698),new Vector2(.5f,.5f)) });
            var platform = Import("lab-oneway-platform.png",91f/.22f,
                new[] {Frame("LabPlatform_Beam",new Rect(56,382,1662,91),Vector2.one*.5f,new Vector4(78,0,78,0))});
            var spikes = Import("lab-spike-module.png",1133f,
                new[] {Frame("LabSpike_Module",new Rect(11,61,1232,1133),Vector2.one*.5f)});
            AssetDatabase.ImportAsset(ArtFolder+"WorldAlignedPanels.shader",ImportAssetOptions.ForceSynchronousImport);
            var panelMaterial = AssetDatabase.LoadAssetAtPath<Material>(PanelMaterialPath);
            var panelShader = AssetDatabase.LoadAssetAtPath<Shader>(ArtFolder+"WorldAlignedPanels.shader");
            if (!panelMaterial) { panelMaterial = new Material(panelShader); AssetDatabase.CreateAsset(panelMaterial,PanelMaterialPath); }
            panelMaterial.shader = panelShader;
            panelMaterial.SetTexture("_MainTex",wall[0].texture); panelMaterial.SetFloat("_PanelSize",4f);
            EditorUtility.SetDirty(panelMaterial);
            foreach (var path in System.IO.Directory.GetFiles(EnvironmentFolder,"*.prefab"))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                var kind = name.StartsWith("OneWayPlatform") ? LaboratorySurfaceVisual.SurfaceKind.OneWay :
                    name == "SpikePit" ? LaboratorySurfaceVisual.SurfaceKind.Spikes : LaboratorySurfaceVisual.SurfaceKind.Panel;
                ConfigurePrefab(path,kind,kind == LaboratorySurfaceVisual.SurfaceKind.OneWay ? platform[0] :
                    kind == LaboratorySurfaceVisual.SurfaceKind.Spikes ? spikes[0] : wall[0],null);
            }
            ConfigurePrefab(CubePath,LaboratorySurfaceVisual.SurfaceKind.Cube,cube[0],cube[1]);
            AssetDatabase.SaveAssets();
            Debug.Log("Laboratory panels, platforms, spikes and color-linked cube core installed. Scene files and puzzle physics were not modified.");
        }

        static void ConfigurePrefab(string path,LaboratorySurfaceVisual.SurfaceKind kind,Sprite sprite,Sprite coreSprite)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var shape = root.GetComponent<BoxCollider2D>();
                if (!shape || shape.autoTiling) throw new InvalidOperationException("Expected an existing manually sized BoxCollider2D: "+path);
                var art = root.GetComponentInChildren<LaboratorySurfaceVisual>(true);
                if (!art)
                {
                    var child = new GameObject("LaboratoryArt"); child.transform.SetParent(root.transform,false);
                    child.AddComponent<SpriteRenderer>(); art = child.AddComponent<LaboratorySurfaceVisual>();
                }
                var renderer = art.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.color = Color.white;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(kind == LaboratorySurfaceVisual.SurfaceKind.Panel ?
                    PanelMaterialPath : "Assets/Whitebox/Art/Unlit.mat");
                renderer.sortingOrder = kind == LaboratorySurfaceVisual.SurfaceKind.Cube ? 4 : kind == LaboratorySurfaceVisual.SurfaceKind.Spikes ? 1 : 0;
                art.shape = shape; art.surfaceRenderer = renderer; art.kind = kind;
                art.legacyRenderers = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r => !r.transform.IsChildOf(art.transform)).ToArray();
                art.RefreshVisual();
                if (coreSprite)
                {
                    var core = art.GetComponentInChildren<LaboratoryCubeCoreVisual>(true);
                    if (!core)
                    {
                        var child = new GameObject("ColorLinkedCore"); child.transform.SetParent(art.transform,false);
                        child.AddComponent<SpriteRenderer>(); core = child.AddComponent<LaboratoryCubeCoreVisual>();
                    }
                    var coreRenderer = core.GetComponent<SpriteRenderer>(); coreRenderer.sprite = coreSprite;
                    coreRenderer.sharedMaterial = renderer.sharedMaterial; coreRenderer.sortingOrder = 5;
                    core.tintSource = root.GetComponent<SpriteRenderer>(); core.shellRenderer = renderer; core.coreRenderer = coreRenderer;
                    core.RefreshVisual();
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static SpriteRect Frame(string name,Rect rect,Vector2 pivot,Vector4 border = default(Vector4))
        { return new SpriteRect {name=name,rect=rect,pivot=pivot,border=border,alignment=SpriteAlignment.Custom}; }

        static Sprite[] Import(string filename,float ppu,SpriteRect[] rects)
        {
            string path = ArtFolder+filename;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects();
            foreach(var rect in rects)
            {
                var old = previous.FirstOrDefault(r=>r.name==rect.name);
                rect.spriteID = old != null ? old.spriteID : GUID.Generate();
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if(names != null) names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
            return rects.Select(r=>sprites[r.name]).ToArray();
        }
    }
}
