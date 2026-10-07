using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VectorWhitebox.Editor
{
    // An editor-only, repeatable integration check against actual Play Mode physics.
    public static class WhiteboxVerification
    {
        static IEnumerator routine;
        static double next;
        static Keyboard keyboard;
        static Mouse mouse;
        static readonly List<string> results = new List<string>();
        public static string Status = "Not run";
        [MenuItem("Whitebox/Run Play Mode Verification")]
        public static void Start()
        { StartRoutine(Scenario()); }
        [MenuItem("Whitebox/Verify Gravity Bullet Damage")]
        public static void StartGravityBullet()
        { StartRoutine(GravityBulletScenario()); }
        [MenuItem("Whitebox/Verify Rotated Pressure Plate")]
        public static void StartRotatedPlate()
        { StartRoutine(RotatedPlateScenario()); }
        [MenuItem("Whitebox/Verify Orange Boundary and Cube Pressure")]
        public static void StartMomentumBoundary()
        { StartRoutine(MomentumBoundaryScenario()); }
        [MenuItem("Whitebox/Verify Cube Speed and Pressure Limits")]
        public static void StartCubeLimits()
        { StartRoutine(CubeLimitsScenario()); }
        [MenuItem("Whitebox/Verify Player Air Inertia")]
        public static void StartPlayerInertia()
        { StartRoutine(PlayerInertiaScenario()); }
        [MenuItem("Whitebox/Verify Pause Level Selection")]
        public static void StartLevelSelection()
        { StartRoutine(LevelSelectionScenario()); }
        static void StartRoutine(IEnumerator scenario)
        {
            if (!EditorApplication.isPlaying || !WhiteboxGame.Instance) throw new InvalidOperationException("Open Level01 and enter Play Mode first.");
            if (routine != null) throw new InvalidOperationException("Verification already running.");
            results.Clear(); Status = "Running";
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            routine = scenario; next = 0; EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (!EditorApplication.isPlaying) throw new Exception("Play Mode ended during verification.");
                if (!routine.MoveNext()) { Finish(); return; }
                next = EditorApplication.timeSinceStartup + (float)routine.Current;
            }
            catch (Exception e) { results.Add("FAIL: " + e.Message); Finish(); }
        }
        static void Finish()
        {
            if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
            if (mouse != null) { InputSystem.RemoveDevice(mouse); mouse = null; }
            if (WhiteboxGame.Instance) WhiteboxGame.Instance.skills.Cancel();
            EditorApplication.update -= Tick; routine = null;
            Status = string.Join("\n", results);
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/WhiteboxVerification.txt", Status);
            Debug.Log("WHITEBOX VERIFICATION\n" + Status);
        }
        static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); results.Add("PASS: " + message); }
        static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        static void Place(WhiteboxGame g, Vector2 position)
        { g.player.ResetMotion(); g.player.body.position = position; Physics2D.SyncTransforms(); }
        static IEnumerator Scenario()
        {
            var g = WhiteboxGame.Instance; var s = g.skills;
            Keys(); g.Respawn(); Place(g, new Vector2(0, 1));
            yield return 1f;
            Check(g.player.Grounded, "Player settles on spawn ground");
            var checkpoints = UnityEngine.Object.FindObjectsByType<WhiteboxCheckpoint>();
            Check(checkpoints.Length == 2 && checkpoints[0].halo && checkpoints[0].marker && checkpoints[1].halo && checkpoints[1].marker,
                "Checkpoint prefab displays translucent markers");
            float originalZoom = g.CameraZoomTarget;
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) }); yield return .15f;
            Check(g.CameraZoomTarget < originalZoom && g.gameCamera.orthographicSize < originalZoom,
                "Mouse wheel zooms the camera in during normal movement");
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, -120) }); yield return .15f;
            Check(Mathf.Abs(g.CameraZoomTarget - originalZoom) < .01f,
                "Mouse wheel zooms the camera back out");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            float startX = g.player.body.position.x;
            Keys(Key.D); yield return .4f; Keys(); yield return .1f;
            Check(g.player.body.position.x > startX + 1, "D moves player using Input System");
            Place(g, new Vector2(0, .7f)); yield return .2f;
            Keys(Key.W); yield return .25f;
            Check(g.player.body.position.y > 2, "W jumps from floor");
            Keys(); yield return 1f;
            Place(g, new Vector2(0, .7f)); yield return .2f;
            Keys(Key.Space); yield return .25f;
            Check(g.player.body.position.y > 2, "Space jumps from floor as an alternate binding");
            Keys(); yield return .8f;
            Place(g, new Vector2(4, 2.5f)); yield return .5f;
            Check(g.player.Grounded && g.player.body.position.y > 2, "One-way platform supports player from above");
            Keys(Key.S); yield return .15f; Keys(); yield return .65f;
            Check(g.player.body.position.y < 1 && g.player.Grounded, "S drops through platform onto safe ground");
            Keys(Key.S); yield return .25f; Keys();
            Check(g.player.body.position.y > .5f, "S cannot pass through solid ground");
            Place(g, new Vector2(29, .7f)); g.ResetCube();
            yield return .3f; Keys(Key.D); yield return .65f; Keys(); yield return .1f;
            Check(g.cube.Body.position.x > 31.3f, "Player physically pushes gravity cube");
            float releasedCubeX = g.cube.Body.position.x;
            Place(g, new Vector2(27, .7f)); yield return .5f;
            Check(g.cube.Body.position.x - releasedCubeX < 1f, "Cube stops sliding shortly after release");
            g.ResetCube(); Place(g, new Vector2(27, .7f)); yield return .3f;
            g.Energy = 100; Keys(Key.Digit2); yield return .1f; Keys(); yield return .1f;
            Check(s.Mode == 2 && g.Energy == 100, "Digit 2 starts slow aim through keyboard input");
            Vector2 point = g.gameCamera.WorldToScreenPoint(g.cube.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
            yield return .1f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return .1f;
            Check(s.Selected == g.cube && s.Aim.y < -.99f, "Mouse selects cube and initializes current downward gravity direction");
            float aimingZoom = g.CameraZoomTarget;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, scroll = new Vector2(0, 120) }); yield return .1f;
            Check(s.Aim == Vector2.down && g.CameraZoomTarget < aimingZoom,
                "Mouse wheel only zooms the camera while aiming");
            s.SetAim(new Vector2(.8f, .2f));
            Check(s.Aim == Vector2.right, "Gravity skill snaps diagonal aim to horizontal");
            s.SetAim(new Vector2(.2f, .8f));
            Check(s.Aim == Vector2.up, "Gravity skill snaps diagonal aim to vertical");
            point = g.gameCamera.WorldToScreenPoint(g.cube.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 }); yield return .1f;
            point = g.gameCamera.WorldToScreenPoint(g.cube.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point + Vector2.up * 100, buttons = 1 }); yield return .1f;
            Check(s.Aim.y > .98f, "Mouse drag changes selected direction upward");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point + Vector2.up * 100 });
            Keys(Key.Digit2); yield return .1f; Keys();
            Check(s.Mode == 0 && g.cube.gravityDirection.y > .98f && g.Energy >= 60 && g.Energy < 61, "Second Digit 2 releases mouse-selected gravity skill and spends energy");
            Check(!s.Begin(2) && s.CooldownRemaining(2) > 0, "Skill 2 cannot be reused during its one-second cooldown");
            Check(s.Begin(1), "Skill 1 has an independent cooldown"); s.Cancel();
            g.ResetCube();
            Place(g, new Vector2(0, 1)); yield return .3f;
            g.Energy = 100;
            Check(s.Begin(1) && Mathf.Approximately(Time.timeScale, .06f) && g.Energy == 100, "Skill entry slows time without spending energy");
            float beforeX = g.player.body.position.x;
            Keys(Key.D, Key.W); yield return .25f; Keys();
            Check(Mathf.Abs(g.player.body.position.x - beforeX) < .1f, "Movement input blocked while aiming");
            Check(!s.Commit() && g.Energy == 100 && s.Mode == 1, "No target cannot cast or consume energy");
            g.cube.Body.linearVelocity = Vector2.zero;
            s.Select(g.cube); s.SetAim(Vector2.up);
            Check(!s.Commit() && g.Energy == 100, "Velocity skill rejects stationary object without spending");
            s.Cancel(); Check(Time.timeScale == 1 && g.Energy == 100, "Cancel restores time and costs no energy");
            g.Energy = 29; Check(!s.Begin(1) && s.Mode == 0, "Skill 1 requires 30 energy to enter");
            Check(s.CooldownRemaining(1) == 0, "Failed or cancelled casts do not start cooldown");
            yield return 1.05f;
            g.Energy = 100; s.Begin(2);
            Check(!s.Select(g.player.GetComponent<DirectionTarget>()), "Gravity skill rejects player");
            g.cube.Body.position = new Vector2(35, .65f); g.cube.Body.linearVelocity = Vector2.zero;
            s.Select(g.cube); s.SetAim(Vector2.up);
            Check(s.Commit() && Mathf.Approximately(g.Energy, 60) && Time.timeScale == 1, "Gravity cast spends exactly 40 only on release");
            g.pauseMenu.Open();
            float pausedGravity = g.cube.GravitySecondsRemaining, pausedCooldown = s.CooldownRemaining(2);
            yield return .3f;
            Check(Mathf.Abs(g.cube.GravitySecondsRemaining - pausedGravity) < .01f && Mathf.Abs(s.CooldownRemaining(2) - pausedCooldown) < .01f,
                "Pause menu freezes temporary gravity duration and skill cooldown");
            g.pauseMenu.Close();
            yield return .5f;
            Check(g.cube.Body.linearVelocity.y > 8f, "Upward gravity accelerates cube without vertical damping");
            yield return .9f;
            Check(g.gate.Open && g.gate.Latched && !g.gate.doorCollider.enabled && !g.cube.Body.simulated, "Upward gravity snaps cube to ceiling plate and opens door");
            yield return 4.2f;
            Check(g.cube.GravitySecondsRemaining == 0 && g.cube.gravityDirection == Vector2.down && g.gate.Latched && g.gate.Open,
                "Gravity expires after five real seconds while the latched cube keeps door open");
            g.ResetCube(); yield return .2f;
            Check(!g.gate.Latched && !g.gate.Open && g.gate.doorCollider.enabled && g.cube.Body.simulated,
                "Resetting cube releases plate and closes door");
            g.Energy = 40; yield return 1f;
            Check(g.Energy > 49f && g.Energy < 52f, "Energy regenerates at 10 per game second");
            g.Energy = 100; g.player.body.linearVelocity = new Vector2(3, 4); s.Begin(1); s.Select(g.player.GetComponent<DirectionTarget>()); s.SetAim(Vector2.left);
            Check(s.Commit() && Mathf.Abs(g.player.body.linearVelocity.magnitude - 5) < .01f && g.player.body.linearVelocity.x < -4.9f && g.Energy == 70, "Player redirect preserves speed magnitude and spends 30");
            Check(!s.Begin(1) && s.CooldownRemaining(1) > 1.4f, "First airborne self redirect has 1.5 seconds of cooldown");
            for (int i = 0; i < 9; i++) { Place(g, new Vector2(10, 15)); yield return .2f; }
            g.Energy = 100; g.player.body.linearVelocity = new Vector2(3, 4);
            Check(s.Begin(1), "Self redirect becomes available after its cooldown");
            s.Select(g.player.GetComponent<DirectionTarget>()); s.SetAim(Vector2.left);
            Check(s.Commit() && g.Energy == 70 && s.CooldownRemaining(1) > 1.9f,
                "Second airborne self redirect increases cooldown to 2 seconds");
            Place(g, new Vector2(24.3f, 1)); yield return .5f;
            Check(g.player.Grounded && s.CooldownRemaining(1) <= 1f,
                "Landing resets the self redirect cooldown to the one-second base");
            Check(UnityEngine.Object.FindObjectsByType<WhiteboxCheckpoint>()[0].Activated || UnityEngine.Object.FindObjectsByType<WhiteboxCheckpoint>()[1].Activated,
                "Entering checkpoint activates its save marker");
            Place(g, new Vector2(30, .7f)); Keys(Key.R); yield return .15f; Keys(); yield return .1f;
            Check(g.player.body.position.x > 29 && g.player.body.position.x < 31,
                "R no longer respawns or reloads the level");
            Keys(Key.Escape); yield return .15f; Keys(); yield return .1f;
            Check(g.pauseMenu.IsOpen && Time.timeScale == 0, "Escape opens the pause menu and freezes gameplay");
            g.pauseMenu.Unstuck(); yield return .1f;
            Check(g.player.body.position.x > 24 && g.player.body.position.x < 27,
                "Pause menu unstuck returns to the activated checkpoint without reloading the scene");
            Check(!g.pauseMenu.IsOpen && Time.timeScale == 1, "Unstuck closes the pause menu and resumes gameplay");
            Place(g, new Vector2(15, -2.7f)); yield return .2f;
            Check(g.Dead && g.HP == 0 && Time.timeScale == 1, "Spikes kill instantly and restore normal time");
            yield return 1f;
            Check(!g.Dead && g.HP == 100 && g.player.body.position.x > 24, "Death respawns at last checkpoint");
            g.cube.Body.position = new Vector2(35, .65f); g.cube.gravityDirection = Vector2.up;
            yield return 1.2f;
            Place(g, new Vector2(47, .8f)); g.turret.ResetLock();
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/GravityCube.prefab");
            var sightBox = UnityEngine.Object.Instantiate(cratePrefab, new Vector3(53, 1, 0), Quaternion.identity);
            sightBox.GetComponent<DirectionTarget>().enabled = false;
            sightBox.GetComponent<Rigidbody2D>().gravityScale = 0;
            Physics2D.SyncTransforms(); yield return .15f;
            Check(g.turret.laser.enabled, "Movable cube does not block turret target lock");
            sightBox.SetActive(false); UnityEngine.Object.Destroy(sightBox);
            var sightWall = new GameObject("Temporary turret sight wall");
            sightWall.transform.position = new Vector3(53, 1, 0);
            sightWall.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 4f);
            Physics2D.SyncTransforms(); yield return .15f;
            Check(!g.turret.laser.enabled, "Solid wall blocks turret target lock");
            sightWall.SetActive(false); UnityEngine.Object.Destroy(sightWall);
            g.turret.ResetLock();
            // Await the event rather than assuming editor wall-clock time equals simulated time.
            double deadline = EditorApplication.timeSinceStartup + 6;
            while (UnityEngine.Object.FindObjectsByType<WhiteboxBullet>().Length == 0 && EditorApplication.timeSinceStartup < deadline) yield return .05f;
            var bullets = UnityEngine.Object.FindObjectsByType<WhiteboxBullet>();
            Check(bullets.Length > 0 && g.turret.laser.enabled, "Turret laser locks player and fires a projectile");
            var b = bullets[0]; float speed = b.GetComponent<Rigidbody2D>().linearVelocity.magnitude;
            g.Energy = 100; s.Begin(1); s.Select(b.GetComponent<DirectionTarget>()); s.SetAim((Vector2)g.turret.transform.position - (Vector2)b.transform.position);
            Check(s.Commit() && b.reflected && Mathf.Abs(b.GetComponent<Rigidbody2D>().linearVelocity.magnitude - speed) < .01f && g.Energy == 70 && s.CooldownRemaining(1) <= 1.01f,
                "Bullet redirect preserves speed, spends 30, and uses the base cooldown");
            yield return 2f;
            Check(g.turret.destroyed, "One reflected projectile destroys turret through physics collision");
            Place(g, new Vector2(65.4f, 1)); yield return .2f;
            Check(g.Completed, "Entering exit portal after destroying turret locks player for transition");
            yield return 1.1f;
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level02_Blank", "Fade transition loads blank Level 2 scene");
            results.Add("ALL CHECKS PASSED");
        }
        static IEnumerator GravityBulletScenario()
        {
            var g = WhiteboxGame.Instance; var s = g.skills;
            Keys(); g.Respawn(); Place(g, new Vector2(47, .8f));
            g.turret.range = 0; // Keep the test focused on one in-flight projectile.
            var bullet = UnityEngine.Object.Instantiate(g.turret.bulletPrefab, new Vector3(55, 1.25f, 0), Quaternion.identity);
            bullet.owner = g.turret;
            bullet.GetComponent<Rigidbody2D>().linearVelocity = Vector2.left * 9;
            Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), g.turret.GetComponent<Collider2D>(), true);
            yield return .1f;
            g.Energy = 100;
            Check(s.Begin(2) && s.Select(bullet.GetComponent<DirectionTarget>()), "Skill 2 can select an in-flight turret bullet");
            s.SetAim(Vector2.right);
            Check(s.Commit() && bullet.gravityAltered && !bullet.reflected && Mathf.Approximately(g.Energy, 60), "Gravity cast marks bullet as player manipulated and spends 40");
            Check(!s.Begin(2) && s.CooldownRemaining(2) > 0, "Skill 2 cooldown starts only after successful release");
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (!g.turret.destroyed && EditorApplication.timeSinceStartup < deadline) yield return .05f;
            Check(g.turret.destroyed, "Gravity-directed bullet returns and destroys its own turret");
            results.Add("ALL CHECKS PASSED");
        }
        static IEnumerator MomentumBoundaryScenario()
        {
            var g = WhiteboxGame.Instance;
            Vector2 originalPosition = g.player.body.position;
            float originalGravity = g.player.body.gravityScale;
            var cubeGo = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/GravityCube.prefab"), new Vector3(500, 50, 0), Quaternion.identity);
            var boundaryGo = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(WhiteboxMomentumBoundaryBuilder.PrefabPath), new Vector3(500, 50, 0), Quaternion.identity);
            var cube = cubeGo.GetComponent<DirectionTarget>();
            var boundary = boundaryGo.GetComponent<MomentumResetBoundary>();
            var socket = new GameObject("TEST / return point");
            var floor = new GameObject("TEST / pressure floor");
            var gateGo = new GameObject("TEST / legacy cube reset");
            try
            {
                Keys();
                boundary.cube = cube; boundary.size = new Vector2(4, 4);
                Vector2 spawn = boundary.SpawnPosition;
                cube.ApplyTemporaryGravity(Vector2.right, 5);
                cube.Body.constraints = RigidbodyConstraints2D.None;
                cube.Body.position = spawn + Vector2.right * 3;
                cube.Body.linearVelocity = new Vector2(8, -6); cube.Body.angularVelocity = 7;
                Check(boundary.CheckAndReturn() && Vector2.Distance(cube.Body.position, spawn) < .001f && cube.Body.linearVelocity == new Vector2(8, -6) && cube.Body.angularVelocity == 7,
                    "Orange boundary returns cube while preserving linear and angular velocity");
                Check(cube.gravityDirection == Vector2.right && cube.GravitySecondsRemaining > 4.9f,
                    "Boundary preserves the current temporary gravity and duration");
                boundary.returnPoint = cube.transform;
                cube.Body.position = spawn + Vector2.right * 3;
                Check(boundary.CheckAndReturn() && Vector2.Distance(cube.Body.position, spawn) < .001f && cube.Body.linearVelocity == new Vector2(8, -6),
                    "Selecting the cube itself as return point uses its initial position without any PressureGate");
                var attachedMarker = new GameObject("TEST / attached return marker");
                attachedMarker.transform.SetParent(cube.transform, false);
                boundary.returnPoint = attachedMarker.transform;
                cube.Body.position = spawn + Vector2.right * 3;
                Check(boundary.CheckAndReturn() && Vector2.Distance(cube.Body.position, spawn) < .001f,
                    "A return point parented under the cube also uses the cube initial position");
                socket.transform.position = spawn + Vector2.up;
                boundary.returnPoint = socket.transform;
                Check(boundary.ReturnCube() && Vector2.Distance(cube.Body.position, socket.transform.position) < .001f,
                    "An explicit return point overrides the cube spawn location");
                boundary.transform.rotation = Quaternion.Euler(0, 0, 90);
                boundary.transform.localScale = new Vector3(2, 1, 1);
                Check(boundary.Contains(boundary.transform.TransformPoint(new Vector3(1.9f, 0))) && !boundary.Contains(boundary.transform.TransformPoint(new Vector3(2.1f, 0))),
                    "Orange boundary detection follows its rotation and scale");
                var gate = gateGo.AddComponent<PressureGate>(); gate.cube = cube; gate.resetHorizontalDistance = .01f;
                cube.Body.linearVelocity = Vector2.zero;
                yield return .12f;
                Check(cube.Body.position.x > 490 && MomentumResetBoundary.Controls(cube),
                    "Assigned orange boundary overrides the legacy cyan automatic reset for that cube");

                boundary.enabled = false; gateGo.SetActive(false);
                cube.ResetGravity(); cube.lateralDamping = 0;
                cube.Body.constraints = RigidbodyConstraints2D.FreezeRotation; cube.Body.rotation = 0;
                floor.transform.position = new Vector3(500, 48.5f, 0);
                floor.AddComponent<BoxCollider2D>().size = new Vector2(12, 1);
                Place(g, new Vector2(500, 49.63f));
                cube.Body.position = new Vector2(500, 50.83f); cube.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms(); yield return .4f;
                Check(g.player.CubeDownwardForce > 30 && g.player.CubeDownwardForce < 32,
                    "Cube resting on the player head applies one mass-weighted downward load");
                Check(g.player.Grounded && g.player.body.position.y > 49.5f,
                    "Solid ground supports the player under cube pressure without penetration");
                g.player.body.gravityScale = 0; floor.SetActive(false);
                float before = g.player.body.linearVelocity.y;
                yield return .08f;
                Check(g.player.body.linearVelocity.y < before - .2f,
                    "Cube pressure accelerates an unsupported player downward even with player gravity disabled");
                Place(g, new Vector2(500, 52));
                cube.Body.position = new Vector2(500.93f, 52); cube.gravityDirection = Vector2.right;
                cube.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms(); yield return .1f;
                Check(g.player.CubeDownwardForce == 0, "Side contact with a cube does not create head pressure");
                results.Add("ALL CHECKS PASSED");
            }
            finally
            {
                boundaryGo.SetActive(false); cubeGo.SetActive(false); floor.SetActive(false); gateGo.SetActive(false);
                UnityEngine.Object.Destroy(boundaryGo); UnityEngine.Object.Destroy(cubeGo); UnityEngine.Object.Destroy(floor);
                UnityEngine.Object.Destroy(gateGo); UnityEngine.Object.Destroy(socket);
                g.player.body.gravityScale = originalGravity; Place(g, originalPosition);
            }
        }
        static IEnumerator CubeLimitsScenario()
        {
            var g = WhiteboxGame.Instance;
            Vector2 originalPosition = g.player.body.position;
            float originalGravity = g.player.body.gravityScale;
            float originalMultiplier = g.player.cubePressureMultiplier;
            float originalSpeedMultiplier = g.player.cubeSpeedPressureMultiplier;
            float originalPressureCap = g.player.maxCubeDownwardForce;
            var playerTarget = g.player.GetComponent<DirectionTarget>();
            float originalPlayerCap = playerTarget.maxSpeed;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/GravityCube.prefab");
            var cubeGo = UnityEngine.Object.Instantiate(prefab, new Vector3(500, 50, 0), Quaternion.identity);
            var secondGo = UnityEngine.Object.Instantiate(prefab, new Vector3(500, 65, 0), Quaternion.identity);
            var bulletGo = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/Bullet.prefab"), new Vector3(500, 70, 0), Quaternion.identity);
            var boundaryGo = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(WhiteboxMomentumBoundaryBuilder.PrefabPath), new Vector3(500, 50, 0), Quaternion.identity);
            var floor = new GameObject("TEST / limited pressure floor");
            var cube = cubeGo.GetComponent<DirectionTarget>();
            var second = secondGo.GetComponent<DirectionTarget>();
            var bullet = bulletGo.GetComponent<DirectionTarget>();
            var boundary = boundaryGo.GetComponent<MomentumResetBoundary>();
            try
            {
                Keys(); Place(g, new Vector2(490, 52));
                g.player.body.gravityScale = 0;
                cube.maxSpeed = 9; cube.lateralDamping = 0;
                cube.Body.constraints = RigidbodyConstraints2D.None;
                second.maxSpeed = 3; second.lateralDamping = 0;
                boundary.cube = cube; boundary.size = new Vector2(4, 4);
                cube.ApplyTemporaryGravity(Vector2.right, 5);
                second.ApplyTemporaryGravity(Vector2.right, 5);
                cube.Body.linearVelocity = new Vector2(30, -40);
                cube.Body.angularVelocity = 11;
                Check(boundary.ReturnCube() && Mathf.Abs(cube.Body.linearVelocity.magnitude - 9) < .001f &&
                    Vector2.Distance(cube.Body.linearVelocity.normalized, new Vector2(.6f, -.8f)) < .001f && cube.Body.angularVelocity == 11,
                    "Orange return caps total speed while preserving velocity direction and angular velocity");
                cube.Body.linearVelocity = new Vector2(3, -4);
                Check(boundary.ReturnCube() && cube.Body.linearVelocity == new Vector2(3, -4),
                    "Orange return preserves velocity below the configured limit");
                cube.Body.linearVelocity = Vector2.zero;
                float lastX = cube.Body.position.x;
                int returns = 0;
                double deadline = EditorApplication.timeSinceStartup + 1;
                while (EditorApplication.timeSinceStartup < deadline)
                {
                    yield return .02f;
                    if (cube.Body.linearVelocity.magnitude > 9.01f || second.Body.linearVelocity.magnitude > 3.01f)
                        throw new Exception("Gravity acceleration exceeded a cube speed limit");
                    float x = cube.Body.position.x;
                    if (x < lastX - .5f) returns++;
                    lastX = x;
                }
                Check(returns > 0 && cube.Body.linearVelocity.magnitude > 8.8f && second.Body.linearVelocity.magnitude > 2.8f,
                    "Repeated orange returns and continuous gravity respect separate limits on each cube");
                cubeGo.GetComponent<BulletLaunchReceiver>().launchImpulse = 1000;
                cubeGo.GetComponent<BulletLaunchReceiver>().Launch(Vector2.right, Vector2.left);
                yield return .04f;
                Check(cube.Body.linearVelocity.magnitude <= 9.01f,
                    "Bullet launch impulse cannot exceed the cube speed limit");
                cube.maxSpeed = 2;
                yield return .04f;
                Check(cube.Body.linearVelocity.magnitude <= 2.01f,
                    "Changing Max Speed during play immediately lowers the cube speed");

                secondGo.SetActive(false); boundary.enabled = false;
                cube.Body.position = new Vector2(500, 60); cube.Body.linearVelocity = Vector2.zero;
                playerTarget.maxSpeed = .1f;
                g.player.body.linearVelocity = Vector2.right * 12; g.player.ReleaseMomentum();
                bullet.maxSpeed = .1f; bullet.gravityEditable = false; bullet.Body.linearVelocity = Vector2.right * 45;
                yield return .06f;
                Check(g.player.body.linearVelocity.x > 11.9f && bullet.Body.linearVelocity.x > 44.9f,
                    "Cube limits do not change the player or turret bullet speed");
                playerTarget.maxSpeed = originalPlayerCap;
                bulletGo.SetActive(false);

                floor.transform.position = new Vector3(500, 48.5f, 0);
                var floorShape = floor.AddComponent<BoxCollider2D>(); floorShape.size = new Vector2(12, 1);
                floorShape.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Whitebox/Art/Grip.physicsMaterial2D");
                cube.ResetGravity(); cube.gravityAcceleration = 0; cube.lateralDamping = 0; cube.maxSpeed = 30;
                cube.Body.constraints = RigidbodyConstraints2D.FreezeRotation; cube.Body.rotation = 0; cube.Body.angularVelocity = 0;
                g.player.cubePressureMultiplier = 1; g.player.cubeSpeedPressureMultiplier = 6; g.player.maxCubeDownwardForce = 100;
                float[] speeds = { 3, 8, 25 };
                float[] peaks = new float[3];
                for (int i = 0; i < speeds.Length; i++)
                {
                    Place(g, new Vector2(500, 49.63f));
                    cube.Body.position = new Vector2(500, 50.98f);
                    cube.Body.linearVelocity = Vector2.down * speeds[i];
                    Physics2D.SyncTransforms();
                    deadline = EditorApplication.timeSinceStartup + .35f;
                    while (EditorApplication.timeSinceStartup < deadline)
                    {
                        yield return .01f;
                        peaks[i] = Mathf.Max(peaks[i], g.player.CubeDownwardForce);
                        if (g.player.CubeDownwardForce > 100.01f) throw new Exception("Head pressure exceeded its force limit");
                    }
                }
                Check(peaks[0] > 20 && peaks[1] > peaks[0] + 20,
                    "Faster real head impacts increase downward pressure despite the collision stopping the cube (slow=" + peaks[0] + ", fast=" + peaks[1] + ")");
                Check(Mathf.Abs(peaks[2] - 100) < .01f,
                    "Very fast head impact reaches the pressure cap without exceeding it");
                yield return .2f;
                Check(g.player.CubeDownwardForce < .01f,
                    "Extra speed pressure fades after a stopped cube impact");
                Place(g, new Vector2(500, 49.63f));
                cube.Body.position = new Vector2(501.1f, 49.63f); cube.Body.linearVelocity = Vector2.left * 8;
                Physics2D.SyncTransforms(); yield return .15f;
                Check(g.player.CubeDownwardForce == 0,
                    "Fast side collision does not produce downward head pressure");
                results.Add("ALL CHECKS PASSED");
            }
            finally
            {
                boundaryGo.SetActive(false); cubeGo.SetActive(false); secondGo.SetActive(false); bulletGo.SetActive(false); floor.SetActive(false);
                UnityEngine.Object.Destroy(boundaryGo); UnityEngine.Object.Destroy(cubeGo); UnityEngine.Object.Destroy(secondGo);
                UnityEngine.Object.Destroy(bulletGo); UnityEngine.Object.Destroy(floor);
                g.player.body.gravityScale = originalGravity; playerTarget.maxSpeed = originalPlayerCap;
                g.player.cubePressureMultiplier = originalMultiplier; g.player.cubeSpeedPressureMultiplier = originalSpeedMultiplier;
                g.player.maxCubeDownwardForce = originalPressureCap; Place(g, originalPosition);
            }
        }
        static IEnumerator PlayerInertiaScenario()
        {
            var g = WhiteboxGame.Instance; var player = g.player; var skills = g.skills;
            var target = player.GetComponent<DirectionTarget>();
            Vector2 originalPosition = player.body.position;
            float originalGravity = player.body.gravityScale, originalDamping = player.body.linearDamping;
            float originalAirAcceleration = player.airAcceleration, originalEnergy = g.Energy;
            var floor = new GameObject("TEST / inertia landing floor");
            var wall = new GameObject("TEST / inertia wall");
            var platform = new GameObject("TEST / inertia one-way platform");
            try
            {
                Keys(); skills.Cancel();
                player.body.linearDamping = 0; player.body.gravityScale = 2.6f; player.airAcceleration = 10;
                Place(g, new Vector2(500, 100));
                yield return 1f;
                Check(player.body.linearVelocity.y < -24,
                    "Free fall naturally accelerates beyond the old 22-unit speed clamp");
                while (skills.CooldownRemaining(1) > 0) yield return .05f;
                g.Energy = 100;
                float incomingSpeed = player.body.linearVelocity.magnitude;
                Check(skills.Begin(1) && skills.Select(target), "Skill 1 selects the falling player");
                skills.SetAim(Vector2.up);
                Check(skills.Commit() && Mathf.Abs(player.body.linearVelocity.magnitude - incomingSpeed) < .001f && player.PreservingMomentum,
                    "Real self cast converts falling speed into upward speed without losing magnitude");
                float gravity = -Physics2D.gravity.y * player.body.gravityScale;
                float launchEnergy = .5f * player.body.linearVelocity.sqrMagnitude + gravity * player.body.position.y;
                Keys(WhiteboxControls.Binding(WhiteboxAction.Jump)); yield return .5f;
                Keys(); yield return .1f;
                float flightEnergy = .5f * player.body.linearVelocity.sqrMagnitude + gravity * player.body.position.y;
                Check(player.PreservingMomentum && player.body.linearVelocity.y > 8 && Mathf.Abs(flightEnergy - launchEnergy) < launchEnergy * .01f,
                    "Converted upward motion keeps its ballistic energy beyond 0.38 seconds and ignores jump-key release");

                while (skills.CooldownRemaining(1) > 0) yield return .05f;
                g.Energy = 100; incomingSpeed = player.body.linearVelocity.magnitude;
                Check(skills.Begin(1) && skills.Select(target), "A second airborne self cast becomes available after cooldown");
                skills.SetAim(Vector2.right);
                Check(skills.Commit() && Mathf.Abs(player.body.linearVelocity.x - incomingSpeed) < .001f,
                    "Vertical velocity converts into equal horizontal speed");
                player.body.gravityScale = 0;
                float launchX = player.body.linearVelocity.x;
                Check(launchX > player.runSpeed, "Converted horizontal speed exceeds the ordinary running speed");
                Keys(); yield return .65f;
                Check(player.PreservingMomentum && Mathf.Abs(player.body.linearVelocity.x - launchX) < .01f,
                    "Releasing movement in midair preserves horizontal inertia beyond the old timeout");
                Keys(WhiteboxControls.Binding(WhiteboxAction.Right)); yield return .25f;
                Check(Mathf.Abs(player.body.linearVelocity.x - launchX) < .01f,
                    "Same-direction air input does not reduce high launch speed to running speed");
                Keys(WhiteboxControls.Binding(WhiteboxAction.Left)); yield return .2f;
                float steeredX = player.body.linearVelocity.x;
                Check(launchX - steeredX > .5f && launchX - steeredX < 5,
                    "Opposite air input changes velocity gradually instead of immediately braking it");
                Keys(); yield return .2f;
                Check(Mathf.Abs(player.body.linearVelocity.x - steeredX) < .01f,
                    "Letting go after air steering preserves the resulting velocity");
                player.airAcceleration = 0;
                Keys(WhiteboxControls.Binding(WhiteboxAction.Left)); yield return .15f;
                Check(Mathf.Abs(player.body.linearVelocity.x - steeredX) < .01f,
                    "Setting Air Acceleration to zero disables air steering");
                player.airAcceleration = 10; Keys();

                wall.transform.position = player.body.position + Vector2.right * 2;
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.4f, 8);
                player.body.linearVelocity = Vector2.right * 12;
                Physics2D.SyncTransforms(); yield return .3f;
                Check(Mathf.Abs(player.body.linearVelocity.x) < .1f && player.PreservingMomentum,
                    "Wall collisions still stop velocity through the physics solver");
                wall.SetActive(false);
                floor.transform.position = new Vector3(500, 48.5f, 0);
                var floorShape = floor.AddComponent<BoxCollider2D>(); floorShape.size = new Vector2(80, 1);
                floorShape.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Whitebox/Art/Grip.physicsMaterial2D");
                player.body.gravityScale = 2.6f;
                player.body.position = new Vector2(500, 52); player.body.linearVelocity = new Vector2(12, -8);
                Physics2D.SyncTransforms(); yield return .6f;
                Check(player.Grounded && !player.PreservingMomentum && Mathf.Abs(player.body.linearVelocity.x) < .1f,
                    "Actual landing ends skill inertia and restores normal ground braking");
                Check(skills.CooldownRemaining(1) <= 1f,
                    "Landing still resets the extra cooldown from repeated self casts");
                float groundX = player.body.position.x;
                Keys(WhiteboxControls.Binding(WhiteboxAction.Right)); yield return .3f;
                Check(player.body.position.x > groundX + 1 && Mathf.Abs(player.body.linearVelocity.x - player.runSpeed) < .1f,
                    "Ground movement retains responsive acceleration and running speed");
                Keys(); yield return .15f;
                Check(Mathf.Abs(player.body.linearVelocity.x) < .1f, "Releasing movement on ground still stops promptly");
                Keys(WhiteboxControls.Binding(WhiteboxAction.AlternateJump)); yield return .15f;
                float risingSpeed = player.body.linearVelocity.y;
                Check(risingSpeed > 5, "Alternate jump binding still starts a normal jump");
                Keys(); yield return .05f;
                Check(player.body.linearVelocity.y < risingSpeed * .75f,
                    "Releasing a normal jump still gives a shorter jump");

                platform.transform.position = new Vector3(500, 52, 0);
                var platformShape = platform.AddComponent<BoxCollider2D>(); platformShape.size = new Vector2(5, .2f); platformShape.usedByEffector = true;
                var effector = platform.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
                Place(g, new Vector2(500, 53)); Physics2D.SyncTransforms(); yield return .35f;
                Check(player.Grounded && player.body.position.y > 52, "One-way platform still supports the player");
                Keys(WhiteboxControls.Binding(WhiteboxAction.Down)); yield return .1f; Keys(); yield return .6f;
                Check(player.Grounded && player.body.position.y < 50,
                    "Down input still drops through a one-way platform onto solid ground");
                results.Add("ALL CHECKS PASSED");
            }
            finally
            {
                floor.SetActive(false); wall.SetActive(false); platform.SetActive(false);
                UnityEngine.Object.Destroy(floor); UnityEngine.Object.Destroy(wall); UnityEngine.Object.Destroy(platform);
                skills.Cancel(); g.Energy = originalEnergy;
                player.body.gravityScale = originalGravity; player.body.linearDamping = originalDamping;
                player.airAcceleration = originalAirAcceleration; Place(g, originalPosition);
            }
        }
        static IEnumerator LevelSelectionScenario()
        {
            var game = WhiteboxGame.Instance;
            var menu = game.pauseMenu;
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            int first = BuildIndexForScenePath("Assets/Whitebox/Scenes/Level01_Whitebox.unity");
            int second = BuildIndexForScenePath("Assets/Whitebox/Scenes/Level02_Blank.unity");
            int third = BuildIndexForScenePath("Assets/Whitebox/Scenes/Level03_Blank.unity");
            int fourth = BuildIndexForScenePath("Assets/Whitebox/Scenes/Level04_Blank.unity");
            Check(first >= 0 && second >= 0 && third >= 0 && fourth >= 0,
                "All four authored levels are available by scene path regardless of main-menu build order");
            Keys(); game.Energy = 100;
            Check(game.skills.Begin(2), "Enter slow motion before opening the pause menu");
            Keys(Key.Escape); yield return .1f; Keys(); yield return .05f;
            Check(menu.IsOpen && game.skills.Mode == 0 && Time.timeScale == 0 && Mathf.Abs(Time.fixedDeltaTime - .02f) < .001f,
                "Esc cancels skill slow motion and opens the paused menu");
            menu.OpenLevelSelect();
            Check(menu.IsLevelSelectOpen && !menu.IsSettingsOpen, "Level selection opens as a separate paused menu page");
            float energy = game.Energy, clock = WhiteboxGame.GameplayTime;
            yield return .2f;
            Check(Mathf.Abs(game.Energy - energy) < .01f && Mathf.Abs(WhiteboxGame.GameplayTime - clock) < .01f,
                "Level selection keeps energy and gameplay timers paused");
            Check(!menu.SelectLevel(-1) && !menu.SelectLevel(count) && menu.IsOpen && Time.timeScale == 0,
                "Invalid level indices leave the game safely paused");
            Keys(Key.Escape); yield return .1f; Keys(); yield return .05f;
            Check(menu.IsOpen && !menu.IsLevelSelectOpen && Time.timeScale == 0,
                "Esc from the level page returns to the paused main menu");
            Keys(Key.Escape); yield return .1f; Keys(); yield return .05f;
            Check(!menu.IsOpen && Time.timeScale == 1, "A second Esc resumes the game");

            // Visit every authored level, including a full restart of level 2.
            int[] targets = { second, second, first, third, fourth };
            for (int i = 0; i < targets.Length; i++)
            {
                game = WhiteboxGame.Instance; menu = game.pauseMenu;
                int targetIndex = targets[i];
                int originalAliveTurrets = 0;
                if (i == 1)
                {
                    game.SetCheckpoint(new Vector2(600, 600), true); game.Energy = 12; game.Damage(25);
                    foreach (var turret in UnityEngine.Object.FindObjectsByType<WhiteboxTurret>())
                        if (!turret.destroyed) { originalAliveTurrets++; turret.Hit(); }
                }
                menu.Open(); menu.OpenLevelSelect();
                Check(menu.SelectLevel(targetIndex) && SceneFadeTransition.IsTransitioning && Time.timeScale == 1 && game.Completed,
                    "Selecting build index " + targetIndex + " closes pause and starts the fade transition");
                Check(!menu.SelectLevel((targetIndex + 1) % count), "Repeated selection is ignored during the same transition");
                Keys(Key.Escape); yield return .1f; Keys();
                Check(!menu.IsOpen, "Esc cannot reopen the old pause menu during fade-out");
                bool checkedFadeIn = false;
                double deadline = EditorApplication.timeSinceStartup + 15;
                while (SceneFadeTransition.IsTransitioning && EditorApplication.timeSinceStartup < deadline)
                {
                    var nextGame = WhiteboxGame.Instance;
                    if (!checkedFadeIn && nextGame && !ReferenceEquals(nextGame, game))
                    {
                        nextGame.pauseMenu.Open();
                        Check(nextGame.InputLocked && !nextGame.pauseMenu.IsOpen && !nextGame.skills.Begin(1),
                            "The new level ignores menu and skill input until fade-in completes");
                        checkedFadeIn = true;
                    }
                    yield return .05f;
                }
                var loaded = WhiteboxGame.Instance;
                Check(!SceneFadeTransition.IsTransitioning && checkedFadeIn && loaded && !ReferenceEquals(loaded, game) &&
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == targetIndex &&
                    !loaded.InputLocked && !loaded.pauseMenu.IsOpen && Time.timeScale == 1 && Mathf.Abs(Time.fixedDeltaTime - .02f) < .001f,
                    "Build index " + targetIndex + " loads with fresh gameplay, a following camera, and normal time");
                Check(loaded.player && loaded.gameCamera && loaded.skills.Mode == 0 && loaded.HP == 100 && loaded.Energy == 100,
                    "Selected level has fresh player, camera, health and energy");
                if (i == 1)
                {
                    loaded.Respawn();
                    Check(Vector2.Distance(loaded.player.body.position, new Vector2(600, 600)) > 20,
                        "Restarting the current level clears its previous checkpoint");
                    int alive = 0;
                    foreach (var turret in UnityEngine.Object.FindObjectsByType<WhiteboxTurret>()) if (!turret.destroyed) alive++;
                    Check(alive == originalAliveTurrets, "Restarting the current level restores its original turrets");
                }
            }
            results.Add("ALL CHECKS PASSED");
        }
        static int BuildIndexForScenePath(string path)
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
                if (UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i) == path) return i;
            return -1;
        }
        static IEnumerator RotatedPlateScenario()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Whitebox/Prefabs/Mechanics/GravityPuzzle.prefab");
            var assembly = UnityEngine.Object.Instantiate(prefab, new Vector3(120, 0, 0), Quaternion.identity);
            assembly.name = "TEST / rotated pressure plate";
            try
            {
                var gate = assembly.GetComponentInChildren<PressureGate>();
                gate.resetHorizontalDistance = 30;
                gate.resetAboveSpawn = 30;
                var plate = gate.plateVisual;
                var cube = gate.cube;
                foreach (var collider in assembly.GetComponentsInChildren<Collider2D>())
                    if (collider != cube.GetComponent<Collider2D>() && collider != gate.doorCollider)
                        collider.enabled = false;
                plate.transform.localScale = new Vector3(5, .24f, 1);
                plate.transform.rotation = Quaternion.Euler(0, 0, 45);
                Vector2 center = plate.transform.position;
                cube.gravityDirection = Vector2.up;
                cube.Body.position = center + new Vector2(1.7f, -1.7f);
                cube.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return .12f;
                Check(!gate.Latched, "Rotated plate ignores a cube in its axis-aligned bounding-box corner");

                cube.Body.position = center + new Vector2(1f, 1f);
                cube.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return .12f;
                Check(gate.Latched, "Rotated plate latches cube touching its angled surface");
                Vector2 down = -plate.transform.up;
                Vector2 extents = cube.GetComponent<Collider2D>().bounds.extents;
                float radius = Mathf.Abs(down.x) * extents.x + Mathf.Abs(down.y) * extents.y;
                Vector2 expected = center + down * (.12f + radius);
                Check(Vector2.Distance(cube.transform.position, expected) < .1f,
                    "Cube snaps beneath the rotated plate instead of the world-axis bottom");

                gate.ResetPuzzle();
                plate.transform.localScale = new Vector3(2.8f, .24f, 1);
                Vector2 edge = center + (Vector2)plate.transform.right * 2.7f;
                cube.gravityDirection = Vector2.up;
                cube.Body.position = edge;
                cube.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return .12f;
                Check(!gate.Latched, "Narrow plate does not reach a distant cube");
                plate.transform.localScale = new Vector3(5f, .24f, 1);
                yield return .12f;
                Check(gate.Latched, "Increasing plate scale extends its detection range");
                results.Add("ALL CHECKS PASSED");
            }
            finally { UnityEngine.Object.Destroy(assembly); }
        }
    }
}
