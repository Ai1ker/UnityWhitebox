using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class TurretArtSetup
    {
        public const string Folder = "Assets/Whitebox/ArtAssets/Turrets/";
        public const string TurretPath = "Assets/Whitebox/Prefabs/Mechanics/Turret.prefab";
        public const string BulletPath = "Assets/Whitebox/Prefabs/Bullet.prefab";
        public const string VisualPath = "Assets/Whitebox/Resources/TurretArtVisual.prefab";
        public const string ExplosionPath = "Assets/Whitebox/Resources/TurretExplosion.prefab";

        [MenuItem("Whitebox/Art/Set Up Turrets And Energy Orbs")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var standing = Import("turret-standing-atlas.png", 6, 3, 0, 18, 255f, "Standing", (i,r,t) => BodyPivot(t,r,158f,127.5f));
            var mounted = Import("turret-mounted-atlas.png", 6, 6, 18, 18, 165f, "Mounted", (i,r,t) => BodyPivot(t,r,104.5f,83f));
            var effects = Import("turret-effects-atlas.png", 4, 3, 0, 12, 300f, "Effects", (i,r,t) =>
                i < 4 ? new Vector2(new[] {68.2f,68.3f,34f,46.4f}[i] / r.width, 157.3f / r.height) : new Vector2(.5f,.5f));
            var orb = Import("energy-orb-atlas.png", 4, 1, 0, 4, 400f, "EnergyOrb", OrbPivot);
            var temporary = new GameObject("TurretArt");
            try
            {
                var body = new GameObject("Body").AddComponent<SpriteRenderer>();
                var gun = new GameObject("AimingGun").AddComponent<SpriteRenderer>();
                body.transform.SetParent(temporary.transform, false); gun.transform.SetParent(temporary.transform, false);
                ConfigureRenderer(body, 3); ConfigureRenderer(gun, 4);
                body.sprite = mounted[0]; gun.sprite = effects[0];
                var art = temporary.AddComponent<TurretArtVisual>();
                art.bodyRenderer = body; art.gunRenderer = gun;
                art.physicsIdle = standing.Take(6).ToArray(); art.physicsLock = standing.Skip(6).Take(6).ToArray(); art.physicsFire = standing.Skip(12).Take(6).ToArray();
                art.fixedIdle = mounted.Take(6).ToArray(); art.fixedLock = mounted.Skip(6).Take(6).ToArray(); art.fixedFire = mounted.Skip(12).Take(6).ToArray();
                art.gunFrames = effects.Take(4).ToArray();
                art.physicsBodyHeight = 1f; art.fixedBodyHeight = 166f / 165f;
                art.gunReferenceLength = 269f / 300f; art.gunWorldLength = .7f;
                PrefabUtility.SaveAsPrefabAsset(temporary, VisualPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
            temporary = new GameObject("TurretExplosion");
            try
            {
                var renderer = temporary.AddComponent<SpriteRenderer>(); ConfigureRenderer(renderer, 12); renderer.sprite = effects[4];
                var animation = temporary.AddComponent<SpriteFrameAnimation>(); animation.spriteRenderer = renderer;
                animation.frames = effects.Skip(4).ToArray(); animation.framesPerSecond = 16; animation.loop = false; animation.destroyWhenFinished = true;
                PrefabUtility.SaveAsPrefabAsset(temporary, ExplosionPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
            var root = PrefabUtility.LoadPrefabContents(TurretPath);
            try
            {
                var turret = root.GetComponent<WhiteboxTurret>();
                var art = root.GetComponentInChildren<TurretArtVisual>(true);
                if (!art)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath), root.transform);
                    art = instance.GetComponent<TurretArtVisual>();
                }
                art.owner = turret; art.RefreshVisual();
                if (!turret.explosionPrefab) turret.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPath);
                PrefabUtility.SaveAsPrefabAsset(root, TurretPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            root = PrefabUtility.LoadPrefabContents(BulletPath);
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = orb[0]; renderer.color = Color.white;
                var animation = root.GetComponent<SpriteFrameAnimation>(); if (!animation) animation = root.AddComponent<SpriteFrameAnimation>();
                animation.spriteRenderer = renderer; animation.frames = orb; animation.framesPerSecond = 10; animation.loop = true; animation.destroyWhenFinished = false;
                PrefabUtility.SaveAsPrefabAsset(root, BulletPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            SaveVariant("Turret_Fixed", false); SaveVariant("Turret_Physics", true);
            AssetDatabase.SaveAssets();
            Debug.Log("Turret art and animated energy orbs installed. Existing scene layouts and physics settings are preserved.");
        }

        static void SaveVariant(string name, bool physics)
        {
            string path = "Assets/Whitebox/Prefabs/Mechanics/" + name + ".prefab";
            // Re-running the importer must not overwrite designer-authored variant settings.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TurretPath));
            try
            {
                instance.name = name;
                if (physics) instance.AddComponent<Rigidbody2D>();
                instance.GetComponentInChildren<TurretArtVisual>(true).RefreshVisual();
                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        static void ConfigureRenderer(SpriteRenderer renderer, int order)
        {
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            renderer.sortingOrder = order; renderer.color = Color.white;
        }

        static Sprite[] Import(string filename, int columns, int rows, int start, int count, float ppu,
            string prefix, Func<int,Rect,Texture2D,Vector2> pivot)
        {
            string path = Folder + filename;
            var source = new Texture2D(2,2);
            try
            {
                source.LoadImage(System.IO.File.ReadAllBytes(path));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
                importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var previous = provider.GetSpriteRects();
                var rects = new SpriteRect[count];
                for (int i = 0; i < count; i++)
                {
                    int cell = start + i, col = cell % columns, row = cell / columns;
                    int x0 = Mathf.RoundToInt((float)col * source.width / columns), x1 = Mathf.RoundToInt((float)(col+1) * source.width / columns);
                    int y0 = Mathf.RoundToInt((float)(rows-row-1) * source.height / rows), y1 = Mathf.RoundToInt((float)(rows-row) * source.height / rows);
                    var rect = new Rect(x0,y0,x1-x0,y1-y0);
                    string name = prefix + "_" + i.ToString("00");
                    var old = previous.FirstOrDefault(r => r.name == name);
                    rects[i] = new SpriteRect { name = name, rect = rect, alignment = SpriteAlignment.Custom,
                        pivot = pivot(i,rect,source), spriteID = old != null ? old.spriteID : GUID.Generate() };
                }
                provider.SetSpriteRects(rects);
                var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
                return rects.Select(r => sprites[r.name]).ToArray();
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        static Vector2 BodyPivot(Texture2D source, Rect rect, float centerX, float halfHeight)
        {
            int foot = (int)rect.height;
            var pixels = source.GetPixels32();
            for (int y = (int)rect.y; y < rect.yMax; y++)
                for (int x = (int)rect.x; x < rect.xMax; x++)
                {
                    var p = pixels[y*source.width+x];
                    if (p.a > 180 && p.r < 220 && p.g > 45 && Mathf.Abs(p.r-p.g) < 35 && Mathf.Abs(p.g-p.b) < 40)
                        foot = Mathf.Min(foot,y-(int)rect.y);
                }
            return new Vector2(centerX/rect.width,(foot+halfHeight)/rect.height);
        }

        static Vector2 OrbPivot(int i, Rect rect, Texture2D source)
        {
            var pixels = source.GetPixels32(); double xSum=0,ySum=0,n=0;
            for (int y=(int)rect.y;y<rect.yMax;y++) for(int x=(int)rect.x;x<rect.xMax;x++)
            {
                var p=pixels[y*source.width+x];
                if(p.a>240 && p.r>240 && p.g>230 && p.b>210) { xSum+=x; ySum+=y; n++; }
            }
            return n > 0 ? new Vector2(((float)(xSum/n)-rect.x)/rect.width,((float)(ySum/n)-rect.y)/rect.height) : new Vector2(.5f,.5f);
        }
    }
}
