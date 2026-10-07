using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VectorWhitebox.Editor
{
    public static class WhiteboxExpansionBuilder
    {
        const string Level1 = "Assets/Whitebox/Scenes/Level01_Whitebox.unity";
        const string Level2 = "Assets/Whitebox/Scenes/Level02_Blank.unity";
        const string Prefabs = "Assets/Whitebox/Prefabs/";

        [MenuItem("Whitebox/Package Level 1 and Create Level 2")]
        public static void BuildFromOpenLevel()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before packaging the level.");
            if (EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            if (EditorSceneManager.GetActiveScene().path != Level1) EditorSceneManager.OpenScene(Level1);
            Directory.CreateDirectory(Prefabs + "Environment");
            Directory.CreateDirectory(Prefabs + "Mechanics");
            Directory.CreateDirectory(Prefabs + "Characters");
            Directory.CreateDirectory(Prefabs + "Core");

            var detail = Find("DETAIL / placeholder graphics").transform;
            var gameCore = Find("GAME / rules and HUD");
            var skillArrow = Find("Skill direction / green");
            if (skillArrow.transform.parent != gameCore.transform) skillArrow.transform.SetParent(gameCore.transform, true);
            var turret = Find("Turret / one reflected hit");
            var laser = Find("Targeting laser");
            if (laser.transform.parent != turret.transform) laser.transform.SetParent(turret.transform, true);

            var hazard = Find("SPIKES / instant death");
            foreach (Transform child in Children(detail))
                if (child.name == "Spike") child.SetParent(hazard.transform, true);
            Connect(hazard, Prefabs + "Environment/SpikePit.prefab");

            var platforms = new[] {
                new[] { "Training drop-through", "OneWayPlatform_Training" },
                new[] { "Pit platform A", "OneWayPlatform_A" },
                new[] { "Pit platform B", "OneWayPlatform_B" },
                new[] { "Pit platform C", "OneWayPlatform_C" },
                new[] { "Pit platform D", "OneWayPlatform_D" }
            };
            foreach (var entry in platforms)
            {
                var platform = Find(entry[0]);
                float x = platform.transform.position.x, y = platform.transform.position.y;
                float half = platform.transform.localScale.x * .5f + .01f;
                foreach (Transform child in Children(detail))
                    if (child.name == "Drop-through dash" && Mathf.Abs(child.position.y - (y - .25f)) < .02f && Mathf.Abs(child.position.x - x) <= half)
                        child.SetParent(platform.transform, true);
                Connect(platform, Prefabs + "Environment/" + entry[1] + ".prefab");
            }

            string[,] solids = {
                { "Spawn ground", "Ground_Spawn" }, { "Puzzle ground", "Ground_Long" },
                { "Left boundary", "Wall_Left" }, { "Right boundary", "Wall_Right" },
                { "Pit floor", "PitFloor" }
            };
            for (int i = 0; i < solids.GetLength(0); i++)
            {
                var body = Find(solids[i, 0]);
                var edge = Find(solids[i, 0] + " top edge");
                if (edge.transform.parent != body.transform) edge.transform.SetParent(body.transform, true);
                Connect(body, Prefabs + "Environment/" + solids[i, 1] + ".prefab");
            }

            Connect(Find("玩家 / Player"), Prefabs + "Characters/Player.prefab");
            Connect(Find("Main Camera"), Prefabs + "Core/Camera2D.prefab");
            Connect(Find("Global Light 2D"), Prefabs + "Core/GlobalLight2D.prefab");
            Connect(Find("Main Light"), Prefabs + "Core/MainLight.prefab");
            Connect(turret, Prefabs + "Mechanics/Turret.prefab");

            var puzzle = GameObject.Find("GRAVITY PUZZLE / complete assembly");
            if (!puzzle)
            {
                puzzle = new GameObject("GRAVITY PUZZLE / complete assembly");
                puzzle.transform.position = new Vector3(35, 0, 0);
                var cube = Find("重力方块 / Gravity cube");
                Connect(cube, Prefabs + "GravityCube.prefab");
                var door = Find("Sensor door");
                var ceiling = Find("Puzzle ceiling");
                Find("Sensor door top edge").transform.SetParent(door.transform, true);
                Find("Puzzle ceiling top edge").transform.SetParent(ceiling.transform, true);
                PrefabUtility.SaveAsPrefabAsset(door, Prefabs + "Mechanics/SensorDoor.prefab");
                PrefabUtility.SaveAsPrefabAsset(Find("Ceiling pressure plate"), Prefabs + "Mechanics/PressurePlate.prefab");
                PrefabUtility.SaveAsPrefabAsset(ceiling, Prefabs + "Environment/Ceiling.prefab");
                foreach (string name in new[] { "重力方块 / Gravity cube", "Sensor door", "Puzzle ceiling",
                    "Ceiling pressure plate", "Pressure sensor / requires upward gravity", "Pressure circuit" })
                    Find(name).transform.SetParent(puzzle.transform, true);
            }
            Connect(puzzle, Prefabs + "Mechanics/GravityPuzzle.prefab");
            if (Mathf.Abs(puzzle.transform.position.x) < .01f)
            {
                // Older generated scenes used world origin as the prefab pivot.
                var children = new List<Transform>(Children(puzzle.transform));
                var worldPositions = new List<Vector3>();
                foreach (var child in children) worldPositions.Add(child.position);
                puzzle.transform.position = new Vector3(35, 0, 0);
                for (int i = 0; i < children.Count; i++) children[i].position = worldPositions[i];
                PrefabUtility.ApplyPrefabInstance(puzzle, InteractionMode.AutomatedAction);
            }
            PrefabUtility.SaveAsPrefabAsset(gameCore, Prefabs + "Core/GameCore.prefab");
            ConfigureLevelOne(gameCore);
            EnsureCheckpoints();
            EnsureBulletLaunchReceiver();
            EnsureBulletSwitchPrefab();
            ApplyFrictionTuning();

            var portal = FindOptional("EXIT PORTAL / Level 2");
            if (!portal) portal = CreatePortal();
            Connect(portal, Prefabs + "Mechanics/ExitPortal.prefab");
            var oldBeacon = FindOptional("Exit beacon");
            if (oldBeacon) UnityEngine.Object.DestroyImmediate(oldBeacon);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Level1);
            CreateBlankLevelIfMissing();
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(Level1, true),
                new EditorBuildSettingsScene(Level2, true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Level1);
            Selection.activeGameObject = Find("EXIT PORTAL / Level 2");
            Debug.Log("WHITEBOX: Exit portal, Level 2, and reusable prefabs are ready.");
        }

        static void Connect(GameObject instance, string path)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(instance)) return;
            if (!PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction))
                throw new InvalidOperationException("Could not save prefab: " + path);
        }
        static void ConfigureLevelOne(GameObject gameCore)
        {
            var game = gameCore.GetComponent<WhiteboxGame>();
            game.prototypeLevelOne = true;
            game.useCameraBounds = true;
            game.followPlayerY = false;
            game.killBelowY = -4f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(game);
            EditorUtility.SetDirty(game);
        }
        static void EnsureCheckpoints()
        {
            const string path = Prefabs + "Mechanics/Checkpoint.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab)
            {
                var template = new GameObject("Checkpoint");
                var collider = template.AddComponent<BoxCollider2D>();
                collider.isTrigger = true; collider.size = new Vector2(1.1f, 3f);
                template.AddComponent<WhiteboxCheckpoint>();
                prefab = PrefabUtility.SaveAsPrefabAsset(template, path);
                UnityEngine.Object.DestroyImmediate(template);
            }
            RefreshCheckpointVisuals();
            if (!FindOptional("Checkpoint / puzzle"))
            {
                var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                checkpoint.name = "Checkpoint / puzzle";
                checkpoint.transform.position = new Vector3(24, 1, 0);
                checkpoint.GetComponent<WhiteboxCheckpoint>().respawnOffset = new Vector2(1, .2f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint.GetComponent<WhiteboxCheckpoint>());
            }
            if (!FindOptional("Checkpoint / turret"))
            {
                var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                checkpoint.name = "Checkpoint / turret";
                checkpoint.transform.position = new Vector3(45, 1, 0);
                var script = checkpoint.GetComponent<WhiteboxCheckpoint>();
                script.respawnOffset = new Vector2(1, .2f);
                script.resetPuzzlesOnDeath = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(script);
            }
        }
        [MenuItem("Whitebox/Refresh Checkpoint Visuals")]
        public static void RefreshCheckpointVisuals()
        {
            const string path = Prefabs + "Mechanics/Checkpoint.prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            var checkpoint = root.GetComponent<WhiteboxCheckpoint>();
            if (!checkpoint.halo || !checkpoint.beam || !checkpoint.marker)
            {
                var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Whitebox/Art/Circle.png");
                var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Whitebox/Art/Square.png");
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
                if (!circle || !square || !material) throw new InvalidOperationException("Checkpoint placeholder art is missing.");
                checkpoint.halo = CheckpointPart(root.transform, "Translucent checkpoint halo", circle, material,
                    Vector2.zero, new Vector2(1.7f, 2.7f), -1, new Color(.3f, .82f, 1f, .2f));
                checkpoint.beam = CheckpointPart(root.transform, "Checkpoint light beam", square, material,
                    Vector2.zero, new Vector2(.12f, 2.7f), 2, new Color(.3f, .82f, 1f, .28f));
                checkpoint.marker = CheckpointPart(root.transform, "Checkpoint marker", circle, material,
                    new Vector2(0, .7f), new Vector2(.48f, .48f), 3, new Color(.3f, .82f, 1f, .55f));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        static SpriteRenderer CheckpointPart(Transform parent, string name, Sprite sprite, Material material,
            Vector2 position, Vector2 size, int sortingOrder, Color color)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(size.x, size.y, 1);
            var visual = part.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.sharedMaterial = material;
            visual.sortingOrder = sortingOrder;
            visual.color = color;
            return visual;
        }
        static void EnsureBulletLaunchReceiver()
        {
            const string path = Prefabs + "GravityCube.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (!root.GetComponent<BulletLaunchReceiver>())
            {
                root.AddComponent<BulletLaunchReceiver>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        [MenuItem("Whitebox/Apply High Friction")]
        public static void ApplyFrictionTuning()
        {
            const string gripPath = "Assets/Whitebox/Art/Grip.physicsMaterial2D";
            var grip = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(gripPath);
            if (!grip)
            {
                grip = new PhysicsMaterial2D("Grip");
                AssetDatabase.CreateAsset(grip, gripPath);
            }
            grip.friction = 1.2f;
            grip.bounciness = 0;
            EditorUtility.SetDirty(grip);
            var crate = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Whitebox/Art/Crate.physicsMaterial2D");
            if (crate) { crate.friction = 1.2f; crate.bounciness = 0; EditorUtility.SetDirty(crate); }
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs + "Environment" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Add(Prefabs + "Mechanics/GravityPuzzle.prefab");
            paths.Add(Prefabs + "Mechanics/SensorDoor.prefab");
            paths.Add(Prefabs + "Mechanics/BulletSwitchDoor.prefab");
            foreach (var path in paths)
            {
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(path)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var collider in root.GetComponentsInChildren<Collider2D>(true))
                {
                    if (collider.isTrigger || collider.GetComponent<DirectionTarget>()) continue;
                    if (collider.sharedMaterial == grip) continue;
                    collider.sharedMaterial = grip;
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            const string cubePath = Prefabs + "GravityCube.prefab";
            var cubeRoot = PrefabUtility.LoadPrefabContents(cubePath);
            var body = cubeRoot.GetComponent<Rigidbody2D>();
            body.linearDamping = 0f;
            cubeRoot.GetComponent<DirectionTarget>().lateralDamping = 4f;
            if (crate) cubeRoot.GetComponent<Collider2D>().sharedMaterial = crate;
            PrefabUtility.SaveAsPrefabAsset(cubeRoot, cubePath);
            PrefabUtility.UnloadPrefabContents(cubeRoot);
            AssetDatabase.SaveAssets();
        }
        static void EnsureBulletSwitchPrefab()
        {
            const string path = Prefabs + "Mechanics/BulletSwitchDoor.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Whitebox/Art/Square.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            var root = new GameObject("Bullet switch + timed door");
            var switchObject = new GameObject("Bullet switch");
            switchObject.transform.SetParent(root.transform, false);
            switchObject.transform.localPosition = new Vector3(0, 1.5f, 0);
            switchObject.transform.localScale = new Vector3(.75f, .75f, 1);
            var switchVisual = switchObject.AddComponent<SpriteRenderer>();
            switchVisual.sprite = sprite; switchVisual.sharedMaterial = material;
            switchVisual.color = new Color(1, .52f, .22f);
            switchVisual.sortingOrder = 3;
            switchObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var doorObject = new GameObject("Timed door");
            doorObject.transform.SetParent(root.transform, false);
            doorObject.transform.localPosition = new Vector3(6, 3.65f, 0);
            doorObject.transform.localScale = new Vector3(.65f, 7.3f, 1);
            var doorVisual = doorObject.AddComponent<SpriteRenderer>();
            doorVisual.sprite = sprite; doorVisual.sharedMaterial = material;
            doorVisual.color = new Color(1, .52f, .22f);
            var doorCollider = doorObject.AddComponent<BoxCollider2D>();
            var mechanism = switchObject.AddComponent<BulletSwitchDoor>();
            mechanism.switchVisual = switchVisual;
            mechanism.doorVisual = doorVisual;
            mechanism.doorCollider = doorCollider;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }
        static GameObject Find(string name)
        {
            var obj = GameObject.Find(name);
            if (!obj) throw new InvalidOperationException("Level 1 object missing: " + name);
            return obj;
        }
        static GameObject FindOptional(string name) { return GameObject.Find(name); }
        static IEnumerable<Transform> Children(Transform parent)
        {
            var snapshot = new List<Transform>();
            foreach (Transform child in parent) snapshot.Add(child);
            return snapshot;
        }

        static GameObject CreatePortal()
        {
            var root = new GameObject("EXIT PORTAL / Level 2");
            root.transform.position = new Vector3(65.4f, 2f, 0);
            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true; trigger.size = new Vector2(1.55f, 3.5f);
            var portal = root.AddComponent<ExitPortal>();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Whitebox/Art/Circle.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Whitebox/Art/Unlit.mat");
            if (!circle || !material) throw new InvalidOperationException("Whitebox art assets are missing. Build Level 1 first.");
            portal.halo = SpriteChild(root.transform, "Outer glow", circle, material, new Vector2(1.9f, 4.1f), 1);
            portal.core = SpriteChild(root.transform, "Portal core", circle, material, new Vector2(1.05f, 3.05f), 2);
            portal.outerRing = Ring(root.transform, "Outer orbit", material, new Vector2(1.06f, 2.13f), 3);
            portal.innerRing = Ring(root.transform, "Inner orbit", material, new Vector2(.7f, 1.64f), 4);
            return root;
        }
        static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, Material material, Vector2 size, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = order;
            renderer.color = new Color(.4f, 1f, .85f, .6f); return renderer;
        }
        static Transform Ring(Transform parent, string name, Material material, Vector2 radius, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false;
            line.loop = true; line.positionCount = 48; line.startWidth = line.endWidth = .065f;
            line.startColor = line.endColor = new Color(.57f, 1f, .87f, .9f); line.sortingOrder = order;
            for (int i = 0; i < 48; i++)
            { float a = i * Mathf.PI * 2 / 48; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y)); }
            return go.transform;
        }
        static void CreateBlankLevelIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level2)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6.8f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .065f, .10f);
            cameraGo.transform.position = new Vector3(0, 2.3f, -10); cameraGo.AddComponent<AudioListener>();
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>(); light.lightType = Light2D.LightType.Global;
            var sun = new GameObject("Main Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = .3f;
            EditorSceneManager.SaveScene(scene, Level2);
        }
    }
}
