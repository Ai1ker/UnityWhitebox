using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class LaboratoryMechanismArtSetup
    {
        public const string ArtFolder = "Assets/Whitebox/ArtAssets/Mechanisms/Laboratory/";
        const string PrefabFolder = "Assets/Whitebox/Prefabs/Mechanics/";
        static Sprite[] hardware, beacons;

        [MenuItem("Whitebox/Art/Set Up Laboratory Mechanisms")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            hardware = Import("lab-mechanism-layers.png", 274f, new[] {
                Frame("LabDoor_Shell",new Rect(191,934,178,551),new Vector4(36,45,36,45)),
                Frame("LabDoor_Light",new Rect(669,934,178,551),new Vector4(36,45,36,45)),
                Frame("LabPlate_Shell",new Rect(52,677,436,80),new Vector4(72,12,72,12)),
                Frame("LabPlate_Light",new Rect(540,677,436,80),new Vector4(72,12,72,12)),
                Frame("LabReceiver_Shell",new Rect(89,138,376,365)),
                Frame("LabReceiver_Light",new Rect(577,138,376,365)) });
            beacons = Import("lab-beacon-layers.png", 300f, new[] {
                Frame("LabPortal_Shell",new Rect(162,630,426,564)),
                Frame("LabPortal_Energy",new Rect(698,630,426,564)),
                Frame("LabCheckpoint_Shell",new Rect(131,29,479,581)),
                Frame("LabCheckpoint_Energy",new Rect(670,29,479,581)) });
            foreach (string name in new[] {"SensorDoor","TimedDoor","PressurePlate","BulletSwitch",
                "BulletSwitchDoor","GravityPuzzle","PressurePlatformSwitch","Checkpoint","ExitPortal","FinalExitPortal"})
                Configure(PrefabFolder+name+".prefab",name);
            Debug.Log("Laboratory art installed on 10 mechanism prefabs. Original sensing geometry, connections and scenes preserved.");
        }

        static void Configure(string path,string name)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (name == "SensorDoor" || name == "TimedDoor") AddArt(root.GetComponent<SpriteRenderer>(),0);
                else if (name == "PressurePlate") AddArt(root.GetComponent<SpriteRenderer>(),1);
                else if (name == "BulletSwitch") AddArt(root.GetComponent<BulletSwitch>().switchVisual,2);
                else if (name == "BulletSwitchDoor")
                {
                    var mechanism=root.GetComponentInChildren<BulletSwitchDoor>(true);
                    AddArt(mechanism.doorVisual,0); AddArt(mechanism.switchVisual,2);
                }
                else if (name == "GravityPuzzle")
                {
                    var gate=root.GetComponentInChildren<PressureGate>(true);
                    AddArt(gate.doorVisual,0); AddArt(gate.plateVisual,1);
                    var ceiling=root.GetComponentsInChildren<BoxCollider2D>(true).First(c=>c.name=="Puzzle ceiling");
                    AddCeiling(ceiling);
                }
                else if (name == "PressurePlatformSwitch") AddArt(root.GetComponent<PressurePlatformSwitch>().plateVisual,1);
                else if (name == "Checkpoint")
                {
                    CheckpointFlatArtSetup.Configure(root);
                }
                else
                {
                    SpriteRenderer halo, core;
                    var exit=root.GetComponent<ExitPortal>();
                    if(exit) { halo=exit.halo; core=exit.core; }
                    else { var final=root.GetComponent<FinalExitPortal>(); halo=final.halo; core=final.core; }
                    var art=AddArt(halo,3);
                    art.tintSource=core; art.shellFollowsAlpha=false; art.alphaReference=1.15f;
                    art.legacyRenderers=new[]{halo,core};
                    art.legacyLines=root.GetComponentsInChildren<LineRenderer>(true);
                    art.lightRenderer.sortingOrder=art.shellRenderer.sortingOrder-1;
                    art.RefreshVisual();
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static LaboratoryMechanismVisual AddArt(SpriteRenderer source,int kind)
        {
            if(!source) throw new InvalidOperationException("Missing legacy visual.");
            var child=source.transform.Find("MechanismArt");
            if(!child) { child=new GameObject("MechanismArt").transform; child.SetParent(source.transform,false); }
            var art=child.GetComponent<LaboratoryMechanismVisual>();
            if(!art) art=child.gameObject.AddComponent<LaboratoryMechanismVisual>();
            var shell=child.GetComponent<SpriteRenderer>(); if(!shell) shell=child.gameObject.AddComponent<SpriteRenderer>();
            var lights=child.Find("StatusLight");
            if(!lights) { lights=new GameObject("StatusLight").transform; lights.SetParent(child,false); }
            var light=lights.GetComponent<SpriteRenderer>(); if(!light) light=lights.gameObject.AddComponent<SpriteRenderer>();
            shell.sprite=kind<3?hardware[kind*2]:beacons[(kind-3)*2];
            light.sprite=kind<3?hardware[kind*2+1]:beacons[(kind-3)*2+1];
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            shell.sharedMaterial=light.sharedMaterial=material;
            shell.sortingLayerID=light.sortingLayerID=source.sortingLayerID;
            shell.sortingOrder=Mathf.Max(3,source.sortingOrder); light.sortingOrder=shell.sortingOrder+1;
            art.geometrySource=art.tintSource=source; art.shellRenderer=shell; art.lightRenderer=light;
            art.shellFollowsAlpha=kind==0; art.alphaReference=kind==0?.95f:1f;
            art.drawMode=kind==0?SpriteDrawMode.Tiled:SpriteDrawMode.Sliced;
            art.legacyRenderers=source.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r=>!r.transform.IsChildOf(child)).ToArray();
            art.RefreshVisual(); return art;
        }

        static void AddCeiling(BoxCollider2D shape)
        {
            var child=shape.transform.Find("LaboratoryArt");
            if(!child) { child=new GameObject("LaboratoryArt").transform; child.SetParent(shape.transform,false); }
            var renderer=child.GetComponent<SpriteRenderer>(); if(!renderer) renderer=child.gameObject.AddComponent<SpriteRenderer>();
            var art=child.GetComponent<LaboratorySurfaceVisual>(); if(!art) art=child.gameObject.AddComponent<LaboratorySurfaceVisual>();
            renderer.sprite=AssetDatabase.LoadAllAssetsAtPath(LaboratoryArtSetup.ArtFolder+"lab-wall-panels.png").OfType<Sprite>().First();
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(LaboratoryArtSetup.PanelMaterialPath);
            renderer.sortingOrder=0; renderer.color=Color.white;
            art.shape=shape; art.surfaceRenderer=renderer; art.kind=LaboratorySurfaceVisual.SurfaceKind.Panel;
            art.legacyRenderers=shape.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!r.transform.IsChildOf(child)).ToArray();
            art.RefreshVisual();
        }

        static SpriteRect Frame(string name,Rect rect,Vector4 border=default(Vector4))
        { return new SpriteRect {name=name,rect=rect,pivot=Vector2.one*.5f,border=border,alignment=SpriteAlignment.Custom}; }

        static Sprite[] Import(string file,float ppu,SpriteRect[] rects)
        {
            string path=ArtFolder+file; AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=4096;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape=false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories(); factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous=provider.GetSpriteRects();
            foreach(var rect in rects) { var old=previous.FirstOrDefault(r=>r.name==rect.name); rect.spriteID=old!=null?old.spriteID:GUID.Generate(); }
            provider.SetSpriteRects(rects);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if(names!=null) names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
            return rects.Select(r=>sprites[r.name]).ToArray();
        }
    }
}
