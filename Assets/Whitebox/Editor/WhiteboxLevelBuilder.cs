using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace VectorWhitebox.Editor
{
    public static class WhiteboxLevelBuilder
    {
        const string Root = "Assets/Whitebox/";
        static Sprite square, triangle, circle;
        static Material unlit;
        static PhysicsMaterial2D frictionless, crateFriction, environmentFriction;
        static Transform geometry, decoration;
        static readonly Color Ground = new Color(.22f, .3f, .36f);
        static readonly Color Mint = new Color(.35f, .93f, .76f);
        [MenuItem("Whitebox/Build First Level")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before rebuilding.");
            Directory.CreateDirectory(Root + "Scenes"); Directory.CreateDirectory(Root + "Art"); Directory.CreateDirectory(Root + "Prefabs");
            // Save existing edits before switching to the generated scene.
            var current = EditorSceneManager.GetActiveScene();
            if (current.isDirty && !string.IsNullOrEmpty(current.path)) EditorSceneManager.SaveScene(current);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            square = MakeSprite("Square", 0); triangle = MakeSprite("Spike", 1); circle = MakeSprite("Circle", 2);
            unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/Unlit.mat");
            if (!unlit) { unlit = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(unlit, Root + "Art/Unlit.mat"); }
            frictionless = MakeFriction("Slide", 0);
            crateFriction = MakeFriction("Crate", 1.2f);
            environmentFriction = MakeFriction("Grip", 1.2f);
            geometry = new GameObject("LEVEL / editable geometry").transform;
            decoration = new GameObject("DETAIL / placeholder graphics").transform;
            var systems = new GameObject("GAME / rules and HUD");
            var game = systems.AddComponent<WhiteboxGame>();
            var skills = systems.AddComponent<DirectionSkills>(); game.skills = skills;
            var camObject = new GameObject("Main Camera"); camObject.tag = "MainCamera";
            var cam = camObject.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 6.8f;
            cam.backgroundColor = new Color(.035f, .065f, .10f); cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(6, 2.3f, -10); camObject.AddComponent<AudioListener>(); game.gameCamera = cam;
            var lightObject = new GameObject("Global Light 2D"); var light = lightObject.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Global;
            var mainLight = new GameObject("Main Light").AddComponent<Light>(); mainLight.type = LightType.Directional; mainLight.intensity = .3f;
            for (int x = -8; x <= 72; x += 2)
            {
                Visual("Grid vertical", new Vector2(x, 3), new Vector2(.015f, 20), new Color(.075f, .115f, .15f), decoration, -10);
            }
            for (int y = -4; y <= 12; y += 2) Visual("Grid horizontal", new Vector2(32, y), new Vector2(80, .015f), new Color(.075f, .115f, .15f), decoration, -10);
            Solid("Spawn ground", new Vector2(1, -.7f), new Vector2(14, 1.4f));
            Solid("Puzzle ground", new Vector2(46, -.7f), new Vector2(46, 1.4f));
            Solid("Left boundary", new Vector2(-6.5f, 3), new Vector2(1, 10));
            Solid("Right boundary", new Vector2(69.5f, 3), new Vector2(1, 10));
            Solid("Pit floor", new Vector2(15.5f, -3.8f), new Vector2(15, .8f));
            Platform("Training drop-through", 4, 1.65f, 2.8f);
            Platform("Pit platform A", 10.2f, .6f, 2.5f);
            Platform("Pit platform B", 14.1f, 1.6f, 2.6f);
            Platform("Pit platform C", 18.1f, .7f, 2.7f);
            Platform("Pit platform D", 21.5f, 1.5f, 2.3f);
            var hazard = new GameObject("SPIKES / instant death"); hazard.transform.parent = geometry; hazard.transform.position = new Vector3(15.5f, -2.95f);
            var hc = hazard.AddComponent<BoxCollider2D>(); hc.size = new Vector2(15, 1); hc.isTrigger = true; hazard.AddComponent<WhiteboxHazard>();
            for (float x = 8.3f; x < 23; x += .6f)
                Visual("Spike", new Vector2(x, -2.95f), new Vector2(.55f, .95f), new Color(.95f, .35f, .4f), decoration, 1, triangle);
            var p = Visual("玩家 / Player", new Vector2(0, 1.2f), new Vector2(.72f, 1.25f), Mint, null, 5);
            var pb = p.AddComponent<Rigidbody2D>(); pb.gravityScale = 2.6f; pb.freezeRotation = true; pb.interpolation = RigidbodyInterpolation2D.Interpolate; pb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var pc = p.AddComponent<BoxCollider2D>(); pc.sharedMaterial = frictionless;
            var player = p.AddComponent<WhiteboxPlayer>(); player.body = pb; player.shape = pc; game.player = player;
            var pt = p.AddComponent<DirectionTarget>(); pt.isPlayer = true; pt.gravityEditable = false;
            var eyes = Visual("Visor", new Vector2(.12f, 1.36f), new Vector2(.43f, .15f), new Color(.035f, .12f, .15f), null, 6); eyes.transform.SetParent(p.transform, true);
            var cube = Visual("重力方块 / Gravity cube", new Vector2(31, .65f), Vector2.one * 1.15f, new Color(1, .73f, .28f), null, 3);
            var cb = cube.AddComponent<Rigidbody2D>(); cb.gravityScale = 0; cb.mass = 1.4f; cb.freezeRotation = true; cb.interpolation = RigidbodyInterpolation2D.Interpolate; cb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            cube.AddComponent<BoxCollider2D>().sharedMaterial = crateFriction;
            game.cube = cube.AddComponent<DirectionTarget>(); game.cube.lateralDamping = 4f;
            cube.AddComponent<BulletLaunchReceiver>();
            var mark = Visual("Cube mark", new Vector2(31, .65f), Vector2.one * .38f, new Color(.5f, .29f, .09f), null, 4); mark.transform.SetParent(cube.transform, true);
            PrefabUtility.SaveAsPrefabAsset(cube, Root + "Prefabs/GravityCube.prefab");
            Solid("Puzzle ceiling", new Vector2(34, 7.8f), new Vector2(20, 1));
            // The full-height door seals the ceiling, so jumping cannot bypass the puzzle.
            var door = Solid("Sensor door", new Vector2(43, 3.65f), new Vector2(.65f, 7.3f));
            var plate = Visual("Ceiling pressure plate", new Vector2(35, 7.12f), new Vector2(2.8f, .24f), new Color(1, .5f, .2f), geometry, 3);
            var sensor = new GameObject("Pressure sensor / requires upward gravity"); sensor.transform.position = new Vector3(35, 6.99f); sensor.transform.parent = geometry;
            var gate = sensor.AddComponent<PressureGate>(); gate.cube = game.cube; gate.doorCollider = door.GetComponent<Collider2D>(); gate.doorVisual = door.GetComponent<SpriteRenderer>(); gate.plateVisual = plate.GetComponent<SpriteRenderer>(); game.gate = gate;
            gate.wire = Line("Pressure circuit", new Color(1, .5f, .2f), .055f, geometry, 2);
            gate.wire.positionCount = 4; gate.wire.SetPositions(new[] { new Vector3(35,7.1f), new Vector3(35,7.55f), new Vector3(43,7.55f), new Vector3(43,3.7f) });
            for (int x = 34; x <= 36; x++) Visual("Align cube below plate", new Vector2(x, .035f), new Vector2(.5f, .06f), new Color(1, .7f, .25f), decoration, 2);
            var arrow = Line("Skill direction / green", Color.green, .09f, null, 20); arrow.enabled = false; skills.arrow = arrow;
            var bulletObject = Visual("子弹 / Bullet", Vector2.zero, Vector2.one * .42f, new Color(1, .56f, .24f), null, 8, circle);
            var bb = bulletObject.AddComponent<Rigidbody2D>(); bb.gravityScale = 0; bb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; bb.interpolation = RigidbodyInterpolation2D.Interpolate;
            bulletObject.AddComponent<CircleCollider2D>().sharedMaterial = frictionless;
            var bt = bulletObject.AddComponent<DirectionTarget>(); bt.gravityEditable = true; bt.gravityAcceleration = 0;
            var bullet = bulletObject.AddComponent<WhiteboxBullet>();
            var bulletPrefab = PrefabUtility.SaveAsPrefabAsset(bulletObject, Root + "Prefabs/Bullet.prefab").GetComponent<WhiteboxBullet>(); Object.DestroyImmediate(bulletObject);
            var turretObject = Visual("Turret / one reflected hit", new Vector2(59, 1.25f), new Vector2(1.25f, 1.8f), new Color(.92f, .32f, .36f), null, 3);
            turretObject.AddComponent<BoxCollider2D>();
            var turret = turretObject.AddComponent<WhiteboxTurret>(); turret.visual = turretObject.GetComponent<SpriteRenderer>(); turret.bulletPrefab = bulletPrefab; game.turret = turret;
            var barrelPivot = new GameObject("Barrel pivot"); barrelPivot.transform.position = turretObject.transform.position; barrelPivot.transform.SetParent(turretObject.transform, true); turret.barrel = barrelPivot.transform;
            var barrel = Visual("Barrel", new Vector2(59.7f, 1.25f), new Vector2(1.25f, .22f), new Color(1, .56f, .5f), null, 4); barrel.transform.SetParent(barrelPivot.transform, true);
            turret.laser = Line("Targeting laser", new Color(1, .23f, .3f, .8f), .03f, null, 2); turret.laser.positionCount = 2; turret.laser.enabled = false;
            Visual("Exit beacon", new Vector2(65.4f, 2), new Vector2(.16f, 4), Mint, decoration, 0);
            Label("01 / MOVE", new Vector2(-1, 4.3f), 55, Mint);
            Label("MOUSE WHEEL  /  ZOOM VIEW", new Vector2(4.2f, 4.15f), 25, Mint);
            Label("02 / GRAVITY", new Vector2(26, 5.6f), 48, new Color(1, .73f, .28f));
            Label("03 / REDIRECT", new Vector2(47, 5.6f), 48, new Color(1, .5f, .46f));
            Label("EXIT", new Vector2(64, 4.7f), 45, Mint);
            Label("S", new Vector2(3.8f, 2.4f), 45, new Color(.4f,.75f,1));
            Label("CHECKPOINT", new Vector2(24, .25f), 22, Mint);
            Label("CHECKPOINT", new Vector2(45, .25f), 22, Mint);
            EditorSceneManager.SaveScene(scene, Root + "Scenes/Level01_Whitebox.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "Scenes/Level01_Whitebox.unity", true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(31, 3), Quaternion.identity, 36, true);
            Debug.Log("WHITEBOX: Level01 built and configured as startup scene.");
            WhiteboxExpansionBuilder.BuildFromOpenLevel();
        }
        static PhysicsMaterial2D MakeFriction(string name, float friction)
        {
            string path = Root + "Art/" + name + ".physicsMaterial2D";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (!mat) { mat = new PhysicsMaterial2D(name); AssetDatabase.CreateAsset(mat, path); }
            mat.friction = friction; mat.bounciness = 0; return mat;
        }
        static Sprite MakeSprite(string name, int shape)
        {
            string path = Root + "Art/" + name + ".png";
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                bool inside = shape == 0 || (shape == 1 ? Mathf.Abs(x - 31.5f) <= (63 - y) * .5f : new Vector2(x - 31.5f, y - 31.5f).magnitude <= 31);
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64; importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static GameObject Visual(string name, Vector2 pos, Vector2 size, Color color, Transform parent, int order = 0, Sprite sprite = null)
        {
            var go = new GameObject(name); go.transform.position = pos; go.transform.localScale = new Vector3(size.x, size.y, 1); if (parent) go.transform.SetParent(parent, true);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite ? sprite : square; sr.sharedMaterial = unlit; sr.color = color; sr.sortingOrder = order; return go;
        }
        static GameObject Solid(string name, Vector2 pos, Vector2 size)
        {
            var go = Visual(name, pos, size, Ground, geometry); go.AddComponent<BoxCollider2D>().sharedMaterial = environmentFriction;
            Visual(name + " top edge", pos + Vector2.up * (size.y / 2 - .03f), new Vector2(size.x, .06f), new Color(.45f,.57f,.64f), decoration, 1); return go;
        }
        static void Platform(string name, float x, float y, float width)
        {
            var go = Visual(name, new Vector2(x,y), new Vector2(width,.22f), new Color(.33f,.67f,.88f), geometry, 1);
            var collider = go.AddComponent<BoxCollider2D>(); collider.sharedMaterial = environmentFriction; collider.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.useOneWayGrouping = true; effector.surfaceArc = 160; effector.useSideFriction = false;
            for (float dx = -width/2 + .15f; dx < width/2; dx += .4f) Visual("Drop-through dash", new Vector2(x+dx,y-.25f), new Vector2(.2f,.035f), new Color(.25f,.48f,.65f), decoration);
        }
        static LineRenderer Line(string name, Color color, float width, Transform parent, int order)
        {
            var go = new GameObject(name); if (parent) go.transform.parent = parent;
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = unlit; line.startColor = line.endColor = color; line.startWidth = line.endWidth = width; line.sortingOrder = order; line.numCapVertices = 4; return line;
        }
        static void Label(string text, Vector2 position, int fontSize, Color color)
        {
            var go = new GameObject(text); go.transform.parent = decoration; go.transform.position = position;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = fontSize; label.characterSize = .06f; label.color = color;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        }
    }
}
