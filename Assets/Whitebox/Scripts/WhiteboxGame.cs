using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorWhitebox
{
    public class WhiteboxGame : MonoBehaviour
    {
        public static WhiteboxGame Instance { get; private set; }
        public static float GameplayTime => Instance && Instance.pauseMenu ? Instance.pauseMenu.GameplayTime : Time.unscaledTime;
        public WhiteboxPlayer player;
        public DirectionSkills skills;
        public WhiteboxPauseMenu pauseMenu;
        public PressureGate gate;
        public WhiteboxTurret turret;
        public DirectionTarget cube;
        public Camera gameCamera;
        public bool prototypeLevelOne;
        public bool useCameraBounds;
        public Vector2 cameraXBounds = new Vector2(6, 59);
        public bool followPlayerY = true;
        public Vector2 cameraOffset = new Vector2(3, 1.66f);
        public float cameraFixedY = 2.3f;
        public float cameraSmooth = 5f;
        public float minCameraSize = 4f;
        public float maxCameraSize = 15f;
        public float zoomStep = 1.2f;
        public float zoomSmooth = 9f;
        public float CameraZoomTarget { get; private set; }
        public float killBelowY = -20f;
        public float Energy = 100, HP = 100;
        public bool Dead { get; private set; }
        public bool Completed { get; private set; }
        public bool InputLocked => Dead || Completed || SceneFadeTransition.IsTransitioning || (skills && skills.Mode != 0) || (pauseMenu && pauseMenu.IsOpen);
        Vector2 checkpoint;
        bool hasCheckpoint;
        bool resetPuzzlesAtCheckpoint = true;
        float deathTimer, invulnerable, messageUntil;
        string message = "";
        string messageChinese, messageEnglish;
        object[] messageArguments;
        Font font;
        GUIStyle normal, title, small, skillTitle, skillHint, micro, rightAligned, marker;
        void Awake()
        {
            Instance = this; Time.timeScale = 1;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 22);
            if (!pauseMenu) pauseMenu = GetComponent<WhiteboxPauseMenu>();
            if (!pauseMenu) pauseMenu = gameObject.AddComponent<WhiteboxPauseMenu>();
        }
        void Start()
        {
            ResolveSceneReferences();
            LocalizedWorldText.BindLegacySigns(gameObject.scene);
            string sceneName = gameObject.scene.name;
            if (player && WhiteboxSaveGame.TryConsumeResume(sceneName, out var savedPosition, out var savedResetPuzzles))
            {
                checkpoint = savedPosition; hasCheckpoint = true; resetPuzzlesAtCheckpoint = savedResetPuzzles;
                player.ResetMotion(); player.transform.position = savedPosition; player.body.position = savedPosition;
                Physics2D.SyncTransforms();
            }
            if (player && !hasCheckpoint) checkpoint = player.transform.position;
            if (player)
            {
                player.GetComponent<DirectionTarget>()?.RefreshForcedGravity();
                WhiteboxSaveGame.Save(sceneName, checkpoint, resetPuzzlesAtCheckpoint);
            }
            if (player && gameCamera) gameCamera.transform.position = CameraTarget();
            if (gameCamera) CameraZoomTarget = gameCamera.orthographicSize;
        }
        void ResolveSceneReferences()
        {
            if (!player) player = FindAnyObjectByType<WhiteboxPlayer>();
            if (!skills) skills = GetComponent<DirectionSkills>();
            if (!gameCamera) gameCamera = Camera.main;
            if (!cube) cube = FindAnyObjectByType<PressureGate>()?.cube;
            if (!gate) gate = FindAnyObjectByType<PressureGate>();
            if (!turret) turret = FindAnyObjectByType<WhiteboxTurret>();
        }
        public void Message(string text)
        {
            message = text; messageChinese = messageEnglish = null; messageArguments = null;
            messageUntil = Time.unscaledTime + 3.5f;
        }
        public void Message(string chinese, string english, params object[] arguments)
        {
            messageChinese = chinese; messageEnglish = english; messageArguments = arguments;
            messageUntil = Time.unscaledTime + 3.5f;
        }
        string CurrentMessage => messageChinese == null ? message : WhiteboxLocalization.Format(messageChinese, messageEnglish, messageArguments);
        static string L(string chinese, string english) => WhiteboxLocalization.Text(chinese, english);
        void Update()
        {
            if (!player || !skills) { ResolveSceneReferences(); if (!player || !skills) return; }
            if (Completed || (pauseMenu && pauseMenu.IsOpen)) return;
            invulnerable -= Time.deltaTime;
            if (Dead)
            {
                deathTimer -= Time.unscaledDeltaTime;
                if (deathTimer <= 0) Respawn();
                return;
            }
            Energy = Mathf.Min(100, Energy + 10 * Time.deltaTime);
            if (player.transform.position.y < killBelowY) Damage(1000);
        }
        public void SetCheckpoint(Vector2 spawn, bool resetPuzzles)
        {
            checkpoint = spawn;
            hasCheckpoint = true;
            resetPuzzlesAtCheckpoint = resetPuzzles;
            WhiteboxSaveGame.Save(gameObject.scene.name, checkpoint, resetPuzzles);
            Message("检查点已激活", "Checkpoint activated");
        }
        public void BeginExit()
        {
            if (Completed) return;
            Completed = true;
            Dead = false;
            skills.Cancel();
            player.ResetMotion();
            player.body.simulated = false;
            Message("正在传送至下一关", "Traveling to the next level");
        }
        void LateUpdate()
        {
            if (!player || !gameCamera) return;
            if (CameraZoomTarget <= 0) CameraZoomTarget = gameCamera.orthographicSize;
            if (!Dead && !Completed && !SceneFadeTransition.IsTransitioning && !(pauseMenu && pauseMenu.IsOpen) && Mouse.current != null)
                AdjustCameraZoom(Mouse.current.scroll.ReadValue().y);
            gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, CameraZoomTarget,
                1 - Mathf.Exp(-zoomSmooth * Time.unscaledDeltaTime));
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, CameraTarget(), 1 - Mathf.Exp(-cameraSmooth * Time.unscaledDeltaTime));
        }
        public void AdjustCameraZoom(float scroll)
        {
            if (!gameCamera || !gameCamera.orthographic || Dead || Completed || (pauseMenu && pauseMenu.IsOpen) || Mathf.Abs(scroll) < .01f) return;
            if (CameraZoomTarget <= 0) CameraZoomTarget = gameCamera.orthographicSize;
            float notches = Mathf.Sign(scroll) * Mathf.Clamp(Mathf.Abs(scroll) / 120f, .5f, 3f);
            CameraZoomTarget = Mathf.Clamp(CameraZoomTarget - notches * zoomStep,
                Mathf.Min(minCameraSize, maxCameraSize), Mathf.Max(minCameraSize, maxCameraSize));
        }
        Vector3 CameraTarget()
        {
            float x = player.transform.position.x + cameraOffset.x;
            if (useCameraBounds) x = Mathf.Clamp(x, cameraXBounds.x, cameraXBounds.y);
            float y = followPlayerY ? player.transform.position.y + cameraOffset.y : cameraFixedY;
            return new Vector3(x, y, -10);
        }
        public void Damage(float amount)
        {
            if (Dead || Completed || (invulnerable > 0 && amount < 100)) return;
            HP = Mathf.Max(0, HP - amount); invulnerable = .7f;
            if (HP <= 0)
            {
                Dead = true; deathTimer = .85f; skills.Cancel(); player.ResetMotion();
                player.body.simulated = false;
                LaboratoryAudio.Play(LaboratorySound.PlayerDeath, player.transform.position);
                var visual = player.GetComponentInChildren<RobotPlayerVisual>();
                if (visual)
                {
                    visual.PlayDeath();
                    deathTimer = Mathf.Max(deathTimer, visual.DeathDuration);
                }
            }
        }
        public void ResetCube()
        {
            if (gate) gate.ResetPuzzle();
        }
        public void Respawn(bool reviveTurrets = false)
        {
            skills.Cancel(); Dead = false; HP = Energy = 100;
            player.ResetMotion(); player.body.simulated = true;
            player.transform.position = checkpoint; player.body.position = checkpoint;
            player.GetComponentInChildren<RobotPlayerVisual>()?.ResetVisual();
            Physics2D.SyncTransforms(); invulnerable = 1;
            player.GetComponent<DirectionTarget>()?.RefreshForcedGravity();
            foreach (var b in FindObjectsByType<WhiteboxBullet>()) Destroy(b.gameObject);
            foreach (var t in FindObjectsByType<WhiteboxTurret>(FindObjectsInactive.Include))
                if (reviveTurrets && t.reviveOnUnstuck && (t.destroyed || t.gameObject.activeInHierarchy))
                    t.Revive();
                else t.ResetLock();
            foreach (var collapse in FindObjectsByType<CollapseHazard>())
                if (collapse.resetOnRespawn) collapse.ResetHazard();
            if (resetPuzzlesAtCheckpoint)
            {
                foreach (var puzzle in FindObjectsByType<PressureGate>()) puzzle.ResetPuzzle();
                foreach (var switchDoor in FindObjectsByType<BulletSwitchDoor>()) switchDoor.ResetSwitch();
                foreach (var bulletSwitch in FindObjectsByType<BulletSwitch>()) bulletSwitch.ResetSwitch();
                foreach (var timedDoor in FindObjectsByType<TimedDoor>()) timedDoor.ResetDoor();
                foreach (var platformSwitch in FindObjectsByType<PressurePlatformSwitch>()) platformSwitch.ResetSwitch();
            }
        }
        void OnDestroy() { Time.timeScale = 1; Time.fixedDeltaTime = .02f; if (Instance == this) Instance = null; if (font) Destroy(font); }
        void OnGUI()
        {
            if (WhiteboxFrontend.IsEndingOpen || (pauseMenu && pauseMenu.IsOpen)) return;
            if (!player || !skills) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            GUI.color = Color.white;
            float w = Screen.width / scale, h = Screen.height / scale;
            if (normal == null)
            {
                normal = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true, padding = new RectOffset(0, 0, 0, 0) };
                normal.normal.textColor = LaboratoryUiTheme.Text;
                title = new GUIStyle(normal) { fontSize = 28 };
                small = new GUIStyle(normal) { fontSize = 15 };
                micro = new GUIStyle(normal) { fontSize = 12 };
                micro.normal.textColor = LaboratoryUiTheme.Muted;
                skillTitle = new GUIStyle(normal) { fontSize = 19 };
                skillTitle.normal.textColor = WithAlpha(LaboratoryUiTheme.Text, .88f);
                skillHint = new GUIStyle(small) { fontSize = 14 };
                skillHint.normal.textColor = WithAlpha(LaboratoryUiTheme.Text, .8f);
                rightAligned = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
                marker = new GUIStyle(micro) { alignment = TextAnchor.MiddleCenter };
            }
            string skillOneKey = WhiteboxControls.Display(WhiteboxAction.SkillOne);
            string skillTwoKey = WhiteboxControls.Display(WhiteboxAction.SkillTwo);
            Panel(new Rect(20, 18, 300, 148), LaboratoryUiTheme.Panel, LaboratoryUiTheme.Line);
            LaboratoryUiTheme.DrawRule(new Rect(20, 34, 3, 22), LaboratoryUiTheme.Accent);
            GUI.Label(new Rect(36, 29, 266, 18), L("Rotcev / 核心状态", "Rotcev / CORE STATUS"), micro);
            LaboratoryUiTheme.DrawRule(new Rect(36, 53, 268, 1), WithAlpha(LaboratoryUiTheme.Line, .38f));
            GUI.Label(new Rect(36, 63, 85, 21), L("生命", "HP"), small);
            GUI.Label(new Rect(108, 63, 48, 21), Mathf.CeilToInt(HP).ToString(), rightAligned);
            Bar(new Rect(36, 89, 120, 4), HP / 100, LaboratoryUiTheme.Danger);
            GUI.Label(new Rect(182, 63, 85, 21), L("能量", "ENERGY"), small);
            GUI.Label(new Rect(254, 63, 48, 21), Mathf.FloorToInt(Energy).ToString(), rightAligned);
            Bar(new Rect(182, 89, 120, 4), Energy / 100, LaboratoryUiTheme.Accent);
            DrawCooldown(new Rect(36, 108, 120, 40), skillOneKey, L("速度", "VELOCITY"), skills.CooldownRemaining(1));
            DrawCooldown(new Rect(182, 108, 120, 40), skillTwoKey, L("重力", "GRAVITY"), skills.CooldownRemaining(2));
            if (prototypeLevelOne)
            {
                int section = player.transform.position.x < 24 ? 1 : player.transform.position.x < 45 ? 2 : 3;
                string stage = section == 1 ? L("越过地刺", "CROSS THE SPIKES") : section == 2 ? L("改写重力", "REDIRECT GRAVITY") : L("反射子弹", "REFLECT PROJECTILES");
                string hint = TutorialHint(section, skillOneKey, skillTwoKey);
                float headingHeight = Mathf.Max(30, normal.CalcHeight(new GUIContent(stage), 270));
                float bodyHeight = small.CalcHeight(new GUIContent(hint), 310);
                var tutorial = new Rect(w - 366, 18, 346, 16 + headingHeight + 13 + bodyHeight + 18);
                Panel(tutorial, LaboratoryUiTheme.Panel, LaboratoryUiTheme.Line);
                LaboratoryUiTheme.DrawRule(new Rect(tutorial.x + 17, tutorial.y + 14, 27, 27), WithAlpha(LaboratoryUiTheme.Line, .18f));
                GUI.Label(new Rect(tutorial.x + 17, tutorial.y + 14, 27, 27), section.ToString("00"), marker);
                GUI.Label(new Rect(tutorial.x + 58, tutorial.y + 14, 270, headingHeight), stage, normal);
                float bodyY = tutorial.y + 16 + headingHeight + 13;
                LaboratoryUiTheme.DrawRule(new Rect(tutorial.x + 18, bodyY - 7, 310, 1), WithAlpha(LaboratoryUiTheme.Line, .32f));
                GUI.Label(new Rect(tutorial.x + 18, bodyY, 310, bodyHeight + 1), hint, small);
            }
            Panel(new Rect(20, h - 52, w - 40, 32), WithAlpha(LaboratoryUiTheme.PanelSoft, .5f), WithAlpha(LaboratoryUiTheme.Line, .18f));
            GUI.Label(new Rect(36, h - 45, w - 72, 20), L("滚轮缩放    /    鼠标选中 · 拖动转向    /    技能 2：上 · 下 · 左 · 右    /    右键取消    /    Esc 菜单", "Scroll: zoom    /    Click: select · Drag: aim    /    Skill 2: four directions    /    Right-click: cancel    /    Esc: menu"), micro);
            float promptBottom = h - 66;
            if (skills.Mode != 0)
            {
                string target = skills.Selected ? TargetDisplayName(skills.Selected) : L("点击物体选择目标", "Click an object to select a target");
                string instruction = WhiteboxLocalization.Format("{0}\n再按 {1} 释放并扣费  ·  右键取消", "{0}\nPress {1} to cast and spend energy · Right-click to cancel", target, skills.Mode == 1 ? skillOneKey : skillTwoKey);
                float height = Mathf.Max(92, skillHint.CalcHeight(new GUIContent(instruction), 350) + 47);
                var prompt = new Rect(w - 402, promptBottom - height, 382, height);
                Panel(prompt, WithAlpha(LaboratoryUiTheme.PanelSoft, .48f), WithAlpha(LaboratoryUiTheme.Line, .25f));
                LaboratoryUiTheme.DrawRule(new Rect(prompt.x, prompt.y + 14, 2, 24), WithAlpha(LaboratoryUiTheme.Accent, .7f));
                GUI.Label(new Rect(prompt.x + 16, prompt.y + 9, 350, 27), WhiteboxLocalization.Format("慢放 ×{0:0.##}  /  {1}", "Slow ×{0:0.##}  /  {1}", skills.slowScale, skills.Mode == 1 ? L("速度方向", "Velocity") : L("重力方向", "Gravity")), skillTitle);
                GUI.Label(new Rect(prompt.x + 16, prompt.y + 39, 350, prompt.height - 45), instruction, skillHint);
                promptBottom = prompt.y - 10;
                foreach (var t in FindObjectsByType<DirectionTarget>())
                {
                    if (skills.Mode == 2 && (t.isPlayer || !t.gravityEditable || t.GravityLocked)) continue;
                    Vector3 p = gameCamera.WorldToScreenPoint(t.transform.position);
                    if (p.z <= 0) continue;
                    GUI.color = t == skills.Selected ? LaboratoryUiTheme.Accent : WithAlpha(LaboratoryUiTheme.Text, .8f);
                    GUI.Label(new Rect(p.x / scale - 60, (Screen.height - p.y) / scale - 47, 120, 24), t == skills.Selected ? L("[ 选中 ]", "[ SELECTED ]") : L("[ 可选 ]", "[ TARGET ]"), marker);
                    GUI.color = Color.white;
                }
            }
            if (Time.unscaledTime < messageUntil)
            {
                string currentMessage = CurrentMessage;
                float height = Mathf.Max(42, skillHint.CalcHeight(new GUIContent(currentMessage), 350) + 20);
                var notice = new Rect(w - 402, promptBottom - height, 382, height);
                Panel(notice, WithAlpha(LaboratoryUiTheme.PanelSoft, .48f), WithAlpha(LaboratoryUiTheme.Line, .22f));
                LaboratoryUiTheme.DrawRule(new Rect(notice.x, notice.y + 12, 2, height - 24), WithAlpha(LaboratoryUiTheme.Accent, .6f));
                GUI.Label(new Rect(notice.x + 16, notice.y + 10, 350, height - 19), currentMessage, skillHint);
            }
            if (Dead || Completed)
            {
                LaboratoryUiTheme.Fill(new Rect(0, 0, w, h), WithAlpha(LaboratoryUiTheme.Ink, .24f));
                Color statusAccent = Dead ? LaboratoryUiTheme.Danger : LaboratoryUiTheme.Accent;
                string detail = Dead ? L("正在返回最近的检查点…", "Returning to the latest checkpoint…") : L("重力机关与速度反射已完成。按 Esc 打开菜单。", "Gravity and velocity tests complete. Press Esc for the menu.");
                float detailHeight = normal.CalcHeight(new GUIContent(detail), 440);
                var card = new Rect(w / 2 - 280, h / 2 - (110 + detailHeight) / 2, 560, 110 + detailHeight);
                Panel(card, LaboratoryUiTheme.Panel, LaboratoryUiTheme.Line);
                LaboratoryUiTheme.DrawRule(new Rect(card.x, card.y + 24, 3, card.height - 48), statusAccent);
                LaboratoryUiTheme.DrawPanel(new Rect(card.x + 25, card.y + 49, 28, 28), Color.clear, statusAccent, 5);
                GUI.Label(new Rect(card.x + 74, card.y + 19, 440, 20), L("Rotcev / 核心状态", "Rotcev / CORE STATUS"), micro);
                GUI.Label(new Rect(card.x + 74, card.y + 44, 440, 38), Dead ? L("生命耗尽", "CORE OFFLINE") : L("试验完成", "LEVEL CLEAR"), title);
                GUI.Label(new Rect(card.x + 74, card.y + 93, 440, detailHeight + 1), detail, normal);
            }
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }
        string TutorialHint(int section, string skillOneKey, string skillTwoKey)
        {
            if (section == 1)
            {
                string moveKeys = WhiteboxControls.Display(WhiteboxAction.Left) + " / " + WhiteboxControls.Display(WhiteboxAction.Right);
                string jumpKeys = WhiteboxControls.Display(WhiteboxAction.Jump) + " / " + WhiteboxControls.Display(WhiteboxAction.AlternateJump);
                return WhiteboxLocalization.Format("{0} 移动 · {1} 跳跃\n滚轮缩放镜头，查看远处机关\n短按低跳，长按高跳\n{2} 穿过带青色灯条的平台", "{0}: move · {1}: jump\nScroll to zoom and inspect mechanisms\nTap for a low jump; hold for height\n{2}: drop through cyan-lit platforms", moveKeys, jumpKeys, WhiteboxControls.Display(WhiteboxAction.Down));
            }
            if (section == 2)
                return WhiteboxLocalization.Format("推动核心箱到压力板下方\n{0} → 选方块 → 指向上方 → {0}（持续 {1:0.##} 秒）\n碰到压力板会吸附，门持续开启", "Push the core crate under the plate\n{0} → select crate → aim up → {0} ({1:0.##}s)\nThe plate holds the crate and opens the door", skillTwoKey, skills.gravityDurationSeconds);
            return WhiteboxLocalization.Format("红色激光锁定后发射子弹\n{0} → 点击红色能量球 → 朝炮塔拖动 → {0}\n反射子弹命中一次即可摧毁炮塔\n技能 1 也可以对自己使用，改变自身速度方向。", "The red laser locks on before firing\n{0} → select orb → drag toward turret → {0}\nOne reflected hit destroys the turret\nSkill 1 also works on yourself to redirect your velocity.", skillOneKey);
        }
        void DrawCooldown(Rect r, string key, string ability, float seconds)
        {
            Color tint = seconds > 0 ? LaboratoryUiTheme.Muted : LaboratoryUiTheme.Accent;
            GUI.Label(new Rect(r.x, r.y - 2, r.width, 18), ability, micro);
            float keyWidth = Mathf.Clamp(marker.CalcSize(new GUIContent(key)).x + 12, 26, 64);
            var keyRect = new Rect(r.x, r.y + 18, keyWidth, 21);
            LaboratoryUiTheme.DrawPanel(keyRect, Color.clear, WithAlpha(tint, .6f), 4);
            GUIStyle keyStyle = marker;
            float keyTextWidth = marker.CalcSize(new GUIContent(key)).x;
            if (keyTextWidth > keyWidth - 8)
                keyStyle = new GUIStyle(marker) { fontSize = Mathf.Max(8, Mathf.FloorToInt(marker.fontSize * (keyWidth - 8) / keyTextWidth)) };
            GUI.Label(keyRect, key, keyStyle);
            GUI.Label(new Rect(r.x + keyWidth + 8, r.y + 19, r.width - keyWidth - 8, 20), seconds > 0 ? seconds.ToString("0.0") + " s" : L("就绪", "READY"), skillHint);
        }
        static string TargetDisplayName(DirectionTarget target)
        {
            if (target.isPlayer) return L("机器人", "Robot");
            if (target.GetComponent<WhiteboxBullet>()) return L("能量球", "Energy orb");
            if (target.GetComponent<WhiteboxTurret>()) return L("炮塔", "Turret");
            return L("核心箱", "Core crate");
        }
        static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        static void Panel(Rect r, Color color, Color border) => LaboratoryUiTheme.DrawPanel(r, color, border, 8);
        static void Bar(Rect r, float fill, Color color) => LaboratoryUiTheme.DrawBar(r, fill, color);
    }
}
