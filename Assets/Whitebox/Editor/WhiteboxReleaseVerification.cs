using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    // Runtime-only fixtures and scene loads: authored scenes are never saved by this check.
    public static class WhiteboxReleaseVerification
    {
        public static string Status = "Not run";
        static IEnumerator routine;
        static double next;
        static readonly List<string> results = new List<string>();
        static readonly List<GameObject> fixtures = new List<GameObject>();
        static WhiteboxGame originalGame;
        static Vector2 originalPosition;
        static float originalKillY, originalGravityScale;
        static bool originalSimulated;

        [MenuItem("Whitebox/Verify Release Workflow")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying || !WhiteboxGame.Instance || routine != null)
                throw new InvalidOperationException("Enter Play Mode in a level before starting the release check.");
            originalGame = WhiteboxGame.Instance; originalPosition = originalGame.player.body.position;
            originalKillY = originalGame.killBelowY; originalGravityScale = originalGame.player.body.gravityScale;
            originalSimulated = originalGame.player.body.simulated;
            originalGame.pauseMenu.Close(); originalGame.skills.Cancel(); originalGame.killBelowY = -10000;
            results.Clear(); fixtures.Clear(); Status = "Running";
            routine = Scenario(); next = 0; EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (!EditorApplication.isPlaying) throw new Exception("Play Mode ended during verification.");
                if (!routine.MoveNext()) { results.Add("ALL CHECKS PASSED"); Finish(); return; }
                next = EditorApplication.timeSinceStartup + (float)routine.Current;
            }
            catch (Exception e) { results.Add("FAIL: " + e.Message); Finish(); }
        }
        static void Finish()
        {
            EditorApplication.update -= Tick; routine = null;
            foreach (var go in fixtures) if (go) UnityEngine.Object.Destroy(go);
            fixtures.Clear();
            if (originalGame && WhiteboxGame.Instance == originalGame)
            {
                originalGame.killBelowY = originalKillY; originalGame.skills.Cancel();
                originalGame.player.ResetMotion(); originalGame.player.body.position = originalPosition;
                originalGame.player.transform.position = originalPosition;
                originalGame.player.GetComponent<DirectionTarget>().RefreshForcedGravity();
                originalGame.player.body.gravityScale = originalGravityScale;
                originalGame.player.body.simulated = originalSimulated;
            }
            Status = string.Join("\n", results); Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/WhiteboxReleaseVerification.txt", Status); Debug.Log(Status);
        }
        static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); results.Add("PASS: " + message); }
        static GameObject Spawn(string path, Vector2 point)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new Exception("Missing prefab: " + path);
            var go = UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity); fixtures.Add(go); return go;
        }
        static void Place(DirectionTarget target, Vector2 point)
        {
            target.Body.position = point; target.transform.position = point;
            target.Body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms(); target.RefreshForcedGravity();
        }
        static IEnumerator WaitScene(string name)
        {
            double deadline = EditorApplication.timeSinceStartup + 8;
            while (SceneManager.GetActiveScene().name != name || SceneFadeTransition.IsTransitioning)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Scene transition timed out: " + name);
                // Freeze a newly loaded empty scene before its player can fall away from the saved point.
                if (SceneManager.GetActiveScene().name == name && WhiteboxGame.Instance)
                { WhiteboxGame.Instance.killBelowY = -10000; WhiteboxGame.Instance.player.body.simulated = false; }
                yield return .02f;
            }
            Check(SceneManager.GetActiveScene().name == name && !SceneFadeTransition.IsTransitioning, "Fade finishes in " + name);
        }
        static IEnumerator Scenario()
        {
            var g = originalGame; var s = g.skills; Vector2 center = new Vector2(500, 50), outside = center + Vector2.right * 10;
            g.player.ResetMotion(); Place(g.player.GetComponent<DirectionTarget>(), center + Vector2.left * 20);
            g.player.body.simulated = false;
            var area = Spawn(WhiteboxReleaseSetup.BoundaryPrefabPath, center).GetComponent<ForcedGravityBoundary>();
            area.size = new Vector2(8, 8); area.direction = CardinalGravityDirection.Up;
            var cube = Spawn("Assets/Whitebox/Prefabs/GravityCube.prefab", center).GetComponent<DirectionTarget>();
            cube.lateralDamping = 0; cube.Body.gravityScale = 0; cube.Body.linearVelocity = new Vector2(3, 4);
            cube.RefreshForcedGravity(); Vector2 axis;
            Check(area.Contains(center) && !area.Contains(outside) && ForcedGravityBoundary.TryGetDirection(cube, out axis) && axis == Vector2.up,
                "Initially internal cube immediately receives the red region direction");
            Check(cube.GravityLocked && cube.EffectiveGravityDirection == Vector2.up && cube.Body.linearVelocity == new Vector2(3, 4),
                "Entry changes gravity without rewriting existing momentum");
            var directions = new[] { CardinalGravityDirection.Down, CardinalGravityDirection.Up, CardinalGravityDirection.Left, CardinalGravityDirection.Right };
            foreach (var direction in directions)
            {
                area.direction = direction; Place(cube, center); yield return .08f;
                Check(Vector2.Dot(cube.Body.linearVelocity, area.GravityDirection) > .4f &&
                    Mathf.Abs(Vector2.Dot(cube.Body.linearVelocity, new Vector2(-area.GravityDirection.y, area.GravityDirection.x))) < .05f,
                    "Physical cube accelerates only toward " + direction);
            }
            var overlap = Spawn(WhiteboxReleaseSetup.BoundaryPrefabPath, center).GetComponent<ForcedGravityBoundary>();
            overlap.size = area.size; overlap.priority = area.priority + 1; overlap.direction = CardinalGravityDirection.Left;
            Place(cube, center);
            Check(cube.EffectiveGravityDirection == Vector2.left, "Higher priority region wins overlap");
            overlap.enabled = false; cube.RefreshForcedGravity();
            Check(cube.EffectiveGravityDirection == Vector2.right, "Disabling the priority region restores the other active region");
            area.direction = CardinalGravityDirection.Up; cube.ApplyTemporaryGravity(Vector2.left, 4); yield return .08f;
            Place(cube, outside);
            Check(!cube.GravityLocked && cube.EffectiveGravityDirection == Vector2.left && cube.GravitySecondsRemaining > 3.5f,
                "Leaving restores an unexpired temporary gravity effect");
            cube.Body.gravityScale = 1.2f; Place(cube, center);
            Check(cube.Body.gravityScale == 0, "Region suppresses native downward gravity");
            area.enabled = false; cube.RefreshForcedGravity();
            Check(!cube.GravityLocked && Mathf.Abs(cube.Body.gravityScale - 1.2f) < .001f, "Disabled boundary restores native gravity scale");
            area.enabled = true; cube.Body.gravityScale = 0; Place(cube, center);
            double cooldownDeadline = EditorApplication.timeSinceStartup + 8;
            while (s.CooldownRemaining(2) > 0) { if (EditorApplication.timeSinceStartup > cooldownDeadline) throw new Exception("Skill cooldown timed out"); yield return .05f; }
            g.Energy = 100;
            Check(s.Begin(2) && !s.Select(cube) && g.Energy == 100, "Skill 2 rejects a locked target without charge"); s.Cancel();
            Place(cube, outside); Check(s.Begin(2) && s.Select(cube), "Skill 2 selects an ordinary target outside the region");
            s.SetAim(Vector2.down); Place(cube, center);
            Check(!s.Commit() && g.Energy == 100 && s.CooldownRemaining(2) == 0, "A selected target entering the region cannot commit or consume energy/cooldown"); s.Cancel();
            cube.Body.linearVelocity = Vector2.right * 5;
            Check(s.Begin(1) && s.Select(cube), "Skill 1 remains selectable inside the region"); s.SetAim(Vector2.up);
            Check(s.Commit() && Mathf.Abs(g.Energy - 70) < .001f && cube.Body.linearVelocity == Vector2.up * 5, "Skill 1 still redirects and pays normally");
            cube.gameObject.SetActive(false);
            var playerTarget = g.player.GetComponent<DirectionTarget>(); g.player.ResetMotion(); g.player.body.simulated = true;
            playerTarget.Body.gravityScale = 2.6f; Place(playerTarget, center);
            Check(playerTarget.GravityLocked && playerTarget.Body.gravityScale == 0 &&
                Mathf.Abs(playerTarget.EffectiveGravityAcceleration - Physics2D.gravity.magnitude * 2.6f) < .01f,
                "Player forced gravity preserves ordinary acceleration strength");
            g.Energy = 100; Check(!s.Begin(2) && g.Energy == 100 && s.CooldownRemaining(2) == 0,
                "Player inside the region cannot begin skill 2");
            yield return .08f; Check(g.player.body.linearVelocity.y > .5f, "Player physically rises under forced upward gravity");
            Place(playerTarget, outside); Check(!playerTarget.GravityLocked && Mathf.Abs(playerTarget.Body.gravityScale - 2.6f) < .001f, "Player exit restores ordinary downward gravity");
            g.player.ResetMotion(); Place(playerTarget, originalPosition); g.player.body.simulated = false;
            var bullet = Spawn("Assets/Whitebox/Prefabs/Bullet.prefab", center).GetComponent<DirectionTarget>();
            bullet.RefreshForcedGravity();
            Check(bullet.GravityLocked && bullet.EffectiveGravityDirection == Vector2.up && Mathf.Abs(bullet.EffectiveGravityAcceleration - 22) < .01f &&
                bullet.GetComponent<WhiteboxBullet>().gravityAltered, "Bullet receives forced gravity and can damage its originating turret");
            foreach (var go in fixtures) if (go) UnityEngine.Object.Destroy(go); fixtures.Clear(); yield return .05f;

            string savedScene = g.gameObject.scene.name; Vector2 savedPoint = new Vector2(550, 80);
            g.SetCheckpoint(savedPoint, false); WhiteboxFrontend.GoToMainMenu(); var waiting = WaitScene(WhiteboxSaveGame.MainMenuScene);
            while (waiting.MoveNext()) yield return waiting.Current;
            var front = UnityEngine.Object.FindAnyObjectByType<WhiteboxFrontend>();
            Check(front && front.mode == WhiteboxFrontend.ScreenMode.MainMenu && front.continueButton.gameObject.activeSelf, "Main menu shows Continue for persisted progress");
            front.settingsButton.onClick.Invoke(); yield return .04f;
            Check(front.settingsMenu.IsSettingsOpen && !front.menuRoot.activeSelf && Time.timeScale == 0, "Main menu settings hide its Canvas content");
            front.settingsMenu.ReturnToMain(); yield return .04f;
            Check(!front.settingsMenu.IsOpen && front.menuRoot.activeSelf && Time.timeScale == 1, "Settings return restores the main menu and clock");
            front.continueButton.onClick.Invoke(); waiting = WaitScene(savedScene); while (waiting.MoveNext()) yield return waiting.Current;
            g = WhiteboxGame.Instance;
            Check(g && Vector2.Distance(g.player.body.position, savedPoint) < .2f && g.HP == 100 && g.Energy == 100,
                "Continue restores the saved level and checkpoint position");
            g.pauseMenu.Open(); g.pauseMenu.Unstuck(); g.player.body.simulated = false;
            Check(Vector2.Distance(g.player.body.position, savedPoint) < .01f, "Unstuck after Continue returns to the restored checkpoint");
            Check(!WhiteboxSaveGame.TryConsumeResume(savedScene, out axis, out var reset), "Resume request is consumed exactly once");
            WhiteboxFrontend.GoToMainMenu(); waiting = WaitScene(WhiteboxSaveGame.MainMenuScene); while (waiting.MoveNext()) yield return waiting.Current;
            front = UnityEngine.Object.FindAnyObjectByType<WhiteboxFrontend>(); front.startButton.onClick.Invoke();
            waiting = WaitScene(WhiteboxSaveGame.FirstLevelScene); while (waiting.MoveNext()) yield return waiting.Current; g = WhiteboxGame.Instance;
            Check(g && Vector2.Distance(g.player.body.position, savedPoint) > 20 && WhiteboxSaveGame.TryGetSavedProgress(out var fresh) &&
                fresh.sceneName == WhiteboxSaveGame.FirstLevelScene && !fresh.finished, "Start Game resets progress and begins Level 1 at its spawn");
            g.pauseMenu.Open(); g.pauseMenu.OpenLevelSelect();
            var levelList = (IList)typeof(WhiteboxPauseMenu).GetField("levels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(g.pauseMenu);
            Check(levelList.Count == 6 && !g.pauseMenu.SelectLevel(0), "Pause level selection lists six levels and rejects MainMenu"); g.pauseMenu.Close();
            var final = Spawn(WhiteboxReleaseSetup.FinalPortalPrefabPath, g.player.body.position).GetComponent<FinalExitPortal>();
            final.requireAllGatesOpen = final.requireTurretDefeated = false;
            Check(final.TryEnter(g.player.shape) && g.Completed && SceneFadeTransition.IsTransitioning,
                "Final portal completes the old level and starts a fade to GameEnd");
            Check(!final.TryEnter(g.player.shape) && !g.skills.Begin(2), "Completed portal and skills reject repeated input during the transition");
            waiting = WaitScene(WhiteboxSaveGame.EndingScene); while (waiting.MoveNext()) yield return waiting.Current;
            yield return .04f;
            front = UnityEngine.Object.FindAnyObjectByType<WhiteboxFrontend>();
            Check(!WhiteboxGame.Instance && WhiteboxFrontend.IsEndingOpen && Time.timeScale == 0 && front &&
                front.mode == WhiteboxFrontend.ScreenMode.Ending && front.uiCanvas && front.returnMainButton && front.quitButton,
                "Independent GameEnd scene displays its ending Canvas and return/quit buttons");
            front.returnMainButton.onClick.Invoke(); waiting = WaitScene(WhiteboxSaveGame.MainMenuScene); while (waiting.MoveNext()) yield return waiting.Current;
            front = UnityEngine.Object.FindAnyObjectByType<WhiteboxFrontend>();
            Check(!WhiteboxFrontend.IsEndingOpen && Time.timeScale == 1 && front.continueButton.gameObject.activeSelf &&
                WhiteboxSaveGame.TryGetSavedProgress(out var finished) && finished.finished, "Ending return reaches a usable main menu with completed save preserved");
        }
    }
}
