using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    // All objects live in a disposable preview scene, so authored levels remain untouched.
    public static class WhiteboxPressurePlatformVerification
    {
        static readonly MethodInfo tick = typeof(PressurePlatformSwitch).GetMethod("Tick",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Whitebox/Verify Pressure Platform Switch")]
        public static void Verify() { Debug.Log(Run()); }

        public static string Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before verifying the pressure platforms.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Whitebox/Prefabs/Mechanics/PressurePlatformSwitch.prefab");
            if (!prefab || tick == null)
                throw new InvalidOperationException("Create the pressure platform switch prefab before verification.");

            var preview = EditorSceneManager.NewPreviewScene();
            var roots = new List<GameObject>();
            var results = new List<string>();
            try
            {
                var pressure = CreateSwitch(prefab, preview, roots, Vector2.zero);
                var platforms = new[] { pressure.platform1, pressure.platform2, pressure.platform3 };
                var plate = pressure.GetComponent<BoxCollider2D>();
                Check(plate && !plate.isTrigger && Near(pressure.activeSeconds, 2),
                    "The prefab has a solid player-supporting plate and a default two-second duration", results);
                foreach (var platform in platforms)
                    Check(platform && platform.transform.IsChildOf(pressure.transform) &&
                        PrefabUtility.IsPartOfPrefabInstance(platform) &&
                        platform.GetComponent<Collider2D>() && platform.GetComponent<PlatformEffector2D>() &&
                        platform.GetComponent<PlatformEffector2D>().useOneWay,
                        "A linked platform is a nested one-way platform with its collider and effector", results);
                pressure.ResetSwitch();
                Check(!pressure.Active && !pressure.Pressed && AllState(platforms, false),
                    "Platforms begin hidden before any contact", results);

                var cube = CreateTarget(preview, roots, "TEST / first cube", new Vector2(0, .62f));
                Tick(pressure, 0);
                Check(pressure.Pressed && pressure.Active && AllState(platforms, true),
                    "An initially contacting cube immediately shows all three platforms", results);
                Tick(pressure, 30);
                Check(pressure.Pressed && AllState(platforms, true),
                    "Continuous cube contact keeps the platforms active beyond two seconds", results);
                SetPosition(cube, new Vector2(100, 100));
                Tick(pressure, 30.01f);
                Check(!pressure.Pressed && pressure.Active,
                    "Removing the cube starts the release countdown without hiding platforms", results);
                Tick(pressure, 32);
                Check(pressure.Active && AllState(platforms, true),
                    "Platforms remain available until the complete two-second release interval ends", results);
                Tick(pressure, 32.02f);
                Check(!pressure.Active && AllState(platforms, false),
                    "Platforms disappear after the release interval", results);

                SetPosition(cube, new Vector2(0, .62f));
                Tick(pressure, 40);
                SetPosition(cube, new Vector2(100, 100));
                Tick(pressure, 40.1f);
                SetPosition(cube, new Vector2(0, .62f));
                Tick(pressure, 41.5f);
                SetPosition(cube, new Vector2(100, 100));
                Tick(pressure, 41.6f);
                Tick(pressure, 42.2f);
                Check(pressure.Active,
                    "Touching again refreshes the countdown instead of using the old deadline", results);
                Tick(pressure, 43.61f);
                Check(!pressure.Active, "The refreshed release countdown also expires", results);

                pressure.ResetSwitch();
                var secondCube = CreateTarget(preview, roots, "TEST / second cube", new Vector2(.6f, .62f));
                SetPosition(cube, new Vector2(-.6f, .62f));
                Tick(pressure, 50);
                SetPosition(cube, new Vector2(100, 100));
                Tick(pressure, 60);
                Check(pressure.Pressed && AllState(platforms, true),
                    "One cube leaving does not stop another cube from holding the plate", results);
                SetPosition(secondCube, new Vector2(105, 100));
                Tick(pressure, 60.1f);
                Tick(pressure, 62.11f);
                Check(!pressure.Active, "The countdown starts only after the last cube leaves", results);

                pressure.ResetSwitch();
                var player = CreateTarget(preview, roots, "TEST / player target", new Vector2(0, .62f));
                player.isPlayer = true;
                Tick(pressure, 70);
                Tick(pressure, 80);
                Check(pressure.Pressed && pressure.Active,
                    "A player standing on the solid plate also keeps the platforms active", results);
                SetPosition(player, new Vector2(110, 100));
                pressure.ResetSwitch();

                SetPosition(cube, new Vector2(0, .62f));
                cube.Body.simulated = false;
                Tick(pressure, 90);
                Tick(pressure, 110);
                Check(pressure.Pressed && AllState(platforms, true),
                    "A latched cube with rigidbody simulation disabled still holds the plate", results);
                SetPosition(cube, new Vector2(100, 100));
                cube.Body.simulated = true;
                pressure.ResetSwitch();

                var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/Bullet.prefab");
                if (!bulletPrefab) throw new InvalidOperationException("The existing Bullet prefab is missing.");
                var bullet = (GameObject)PrefabUtility.InstantiatePrefab(bulletPrefab, preview);
                roots.Add(bullet);
                foreach (var behaviour in bullet.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
                bullet.transform.position = pressure.transform.position;
                var bulletBody = bullet.GetComponent<Rigidbody2D>();
                if (bulletBody) bulletBody.position = pressure.transform.position;
                Physics2D.SyncTransforms();
                Tick(pressure, 120);
                Check(!pressure.Pressed && !pressure.Active && AllState(platforms, false),
                    "A bullet overlapping the plate does not activate platforms", results);
                bullet.transform.position = new Vector3(115, 100, 0);
                if (bulletBody) bulletBody.position = new Vector2(115, 100);

                pressure.transform.rotation = Quaternion.Euler(0, 0, 45);
                pressure.transform.localScale = new Vector3(2, .5f, 1);
                Vector2 center = plate.transform.TransformPoint(plate.offset);
                Vector2 normal = plate.transform.TransformVector(Vector2.up).normalized;
                float halfHeight = plate.transform.TransformVector(Vector2.up * plate.size.y * .5f).magnitude;
                cube.transform.rotation = pressure.transform.rotation;
                SetPosition(cube, center + normal * (halfHeight + .53f));
                Tick(pressure, 130);
                Check(pressure.Pressed && pressure.Active,
                    "Contact follows the plate's rotation and nonuniform scale", results);
                SetPosition(cube, center + normal * 1.1f);
                pressure.ResetSwitch();
                Tick(pressure, 131);
                Check(!pressure.Pressed && !pressure.Active,
                    "An object outside the rotated box does not activate it through the world AABB", results);
                pressure.transform.rotation = Quaternion.identity;
                pressure.transform.localScale = Vector3.one;
                cube.transform.rotation = Quaternion.identity;
                SetPosition(cube, new Vector2(100, 100));

                pressure.platform1 = pressure.platform2 = pressure.platform3 = null;
                pressure.ResetSwitch();
                SetPosition(cube, new Vector2(0, .62f));
                Tick(pressure, 140);
                Check(pressure.Active && pressure.Pressed && AllState(platforms, false),
                    "An unconfigured plate tolerates empty platform references", results);
                pressure.platform1 = pressure.platform2 = platforms[0];
                pressure.platform3 = null;
                Tick(pressure, 141);
                Check(platforms[0].activeSelf && !platforms[1].activeSelf && !platforms[2].activeSelf,
                    "Repeated platform references are safe and optional slots remain unused", results);
                pressure.ResetSwitch();
                pressure.platform1 = pressure.gameObject;
                pressure.platform2 = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Whitebox/Prefabs/Environment/OneWayPlatform_A.prefab");
                bool assetWasActive = pressure.platform2.activeSelf;
                Tick(pressure, 142);
                Check(pressure.gameObject.activeSelf && pressure.platform2.activeSelf == assetWasActive,
                    "Incorrect self and prefab-asset references do not disable the controller or mutate assets", results);

                pressure.platform1 = platforms[0]; pressure.platform2 = platforms[1]; pressure.platform3 = platforms[2];
                pressure.activeSeconds = .75f;
                pressure.ResetSwitch();
                Tick(pressure, 150);
                SetPosition(cube, new Vector2(100, 100));
                Tick(pressure, 150.01f);
                Tick(pressure, 150.75f);
                Check(pressure.Active, "A configured custom duration is honored before expiry", results);
                Tick(pressure, 150.77f);
                Check(!pressure.Active, "A configured custom duration expires at its own deadline", results);
                pressure.activeSeconds = 2;

                var other = CreateSwitch(prefab, preview, roots, new Vector2(20, 0));
                var otherPlatforms = new[] { other.platform1, other.platform2, other.platform3 };
                other.ResetSwitch();
                pressure.ResetSwitch();
                SetPosition(cube, new Vector2(0, .62f));
                Tick(pressure, 160); Tick(other, 160);
                Check(AllState(platforms, true) && !other.Pressed && AllState(otherPlatforms, false),
                    "Two plates control their own platforms without cross activation", results);
                SetPosition(cube, new Vector2(100, 100));
                SetPosition(secondCube, new Vector2(20, .62f));
                Tick(pressure, 160.1f); Tick(other, 160.1f);
                Tick(pressure, 162.11f); Tick(other, 162.11f);
                Check(!pressure.Active && AllState(platforms, false) && other.Pressed && AllState(otherPlatforms, true),
                    "One plate timing out leaves another held plate fully active", results);
                other.ResetSwitch();
                Check(!other.Active && !other.Pressed && AllState(otherPlatforms, false),
                    "ResetSwitch clears activation and immediately hides every linked platform", results);

                results.Add("ALL CHECKS PASSED (" + results.Count + ")");
                return string.Join("\n", results.ToArray());
            }
            finally
            {
                for (int i = roots.Count - 1; i >= 0; i--)
                    if (roots[i]) UnityEngine.Object.DestroyImmediate(roots[i]);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static PressurePlatformSwitch CreateSwitch(GameObject prefab, Scene scene, List<GameObject> roots, Vector2 position)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            roots.Add(root);
            root.transform.position = position;
            var pressure = root.GetComponent<PressurePlatformSwitch>();
            if (!pressure) throw new InvalidOperationException("The prefab lacks PressurePlatformSwitch.");
            pressure.enabled = false;
            return pressure;
        }

        static DirectionTarget CreateTarget(Scene scene, List<GameObject> roots, string name, Vector2 position)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            roots.Add(root);
            root.AddComponent<BoxCollider2D>().size = Vector2.one;
            root.AddComponent<Rigidbody2D>().gravityScale = 0;
            var target = root.AddComponent<DirectionTarget>();
            target.enabled = false;
            root.SetActive(true);
            SetPosition(target, position);
            return target;
        }

        static void SetPosition(DirectionTarget target, Vector2 position)
        {
            target.transform.position = new Vector3(position.x, position.y, 0);
            target.Body.position = position;
        }
        static void Tick(PressurePlatformSwitch pressure, float now)
        { tick.Invoke(pressure, new object[] { now }); }
        static bool Near(float a, float b) { return Mathf.Abs(a - b) < .001f; }
        static bool AllState(GameObject[] platforms, bool active)
        {
            foreach (var platform in platforms) if (!platform || platform.activeSelf != active) return false;
            return true;
        }
        static void Check(bool condition, string message, List<string> results)
        {
            if (!condition) throw new Exception("FAIL: " + message);
            results.Add("PASS: " + message);
        }
    }
}
