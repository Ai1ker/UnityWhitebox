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
        Font font;
        GUIStyle normal, title, small, skillTitle, skillHint;
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
        public void Message(string text) { message = text; messageUntil = Time.unscaledTime + 3.5f; }
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
            Message("检查点已激活");
        }
        public void BeginExit()
        {
            if (Completed) return;
            Completed = true;
            Dead = false;
            skills.Cancel();
            player.ResetMotion();
            player.body.simulated = false;
            Message("正在传送至下一关");
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
            Physics2D.SyncTransforms(); invulnerable = 1;
            player.GetComponent<DirectionTarget>()?.RefreshForcedGravity();
            foreach (var b in FindObjectsByType<WhiteboxBullet>()) Destroy(b.gameObject);
            foreach (var t in FindObjectsByType<WhiteboxTurret>(FindObjectsInactive.Include))
                if (reviveTurrets && t.reviveOnUnstuck && t.destroyed) t.Revive(); else t.ResetLock();
            if (resetPuzzlesAtCheckpoint)
            {
                foreach (var puzzle in FindObjectsByType<PressureGate>()) puzzle.ResetPuzzle();
                foreach (var switchDoor in FindObjectsByType<BulletSwitchDoor>()) switchDoor.ResetSwitch();
                foreach (var bulletSwitch in FindObjectsByType<BulletSwitch>()) bulletSwitch.ResetSwitch();
                foreach (var timedDoor in FindObjectsByType<TimedDoor>()) timedDoor.ResetDoor();
                foreach (var platformSwitch in FindObjectsByType<PressurePlatformSwitch>()) platformSwitch.ResetSwitch();
            }
        }
        void OnDestroy() { Time.timeScale = 1; Time.fixedDeltaTime = .02f; if (Instance == this) Instance = null; }
        void OnGUI()
        {
            if (WhiteboxFrontend.IsEndingOpen) return;
            if (!player || !skills) return;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            if (normal == null)
            {
                normal = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, wordWrap = true };
                normal.normal.textColor = new Color(.84f, .91f, .94f);
                title = new GUIStyle(normal) { fontSize = 30, fontStyle = FontStyle.Bold };
                small = new GUIStyle(normal) { fontSize = 16 };
                skillTitle = new GUIStyle(title) { fontSize = 24 };
                skillHint = new GUIStyle(small) { fontSize = 14 };
                skillTitle.normal.textColor = skillHint.normal.textColor = new Color(.84f, .94f, .92f, .85f);
            }
            Panel(new Rect(20, 18, 325, 143), new Color(.025f, .06f, .09f, .95f));
            GUI.Label(new Rect(36, 25, 310, 26), prototypeLevelOne ? "VECTOR / 01    方向试验场" : "VECTOR / 关卡测试", normal);
            Bar(new Rect(37, 63, 278, 17), HP / 100, new Color(.95f, .36f, .4f));
            GUI.Label(new Rect(37, 82, 140, 23), "生命 " + Mathf.CeilToInt(HP) + " / 100", small);
            Bar(new Rect(37, 111, 278, 9), Energy / 100, new Color(.32f, .9f, .79f));
            GUI.Label(new Rect(178, 82, 145, 23), "能量 " + Mathf.FloorToInt(Energy) + " / 100", small);
            GUI.Label(new Rect(37, 126, 278, 22), "1 冷却 " + skills.CooldownRemaining(1).ToString("0.0") + "s    2 冷却 " + skills.CooldownRemaining(2).ToString("0.0") + "s", small);
            string skillOneKey = WhiteboxControls.Display(WhiteboxAction.SkillOne);
            string skillTwoKey = WhiteboxControls.Display(WhiteboxAction.SkillTwo);
            if (prototypeLevelOne)
            {
                string stage = player.transform.position.x < 24 ? "01 / 越过地刺" : player.transform.position.x < 45 ? "02 / 改写重力" : "03 / 反射子弹";
                string moveKeys = WhiteboxControls.Display(WhiteboxAction.Left) + " / " + WhiteboxControls.Display(WhiteboxAction.Right);
                string jumpKeys = WhiteboxControls.Display(WhiteboxAction.Jump) + " / " + WhiteboxControls.Display(WhiteboxAction.AlternateJump);
                string hint = player.transform.position.x < 24 ? moveKeys + " 移动 · " + jumpKeys + " 跳跃\n滚轮缩放镜头，查看远处机关\n短按低跳，长按高跳\n" + WhiteboxControls.Display(WhiteboxAction.Down) + " 穿过带虚线的蓝色平台" : player.transform.position.x < 45 ? "推动黄色方块到压力板下方\n" + skillTwoKey + " → 选方块 → 指向上方 → " + skillTwoKey + "（持续 " + skills.gravityDurationSeconds.ToString("0.##") + " 秒）\n碰到压力板会吸附，门持续开启" : "红色激光锁定后发射子弹\n" + skillOneKey + " → 点击橙色子弹 → 朝炮塔拖动 → " + skillOneKey + "\n反射子弹命中一次即可摧毁炮塔";
                Panel(new Rect(w - 365, 18, 345, 155), new Color(.025f, .06f, .09f, .93f));
                GUI.Label(new Rect(w - 348, 29, 310, 29), stage, normal);
                GUI.Label(new Rect(w - 348, 67, 310, 99), hint, small);
            }
            Panel(new Rect(20, h - 67, w - 40, 48), new Color(.025f, .06f, .09f, .95f));
            GUI.Label(new Rect(37, h - 57, w - 70, 34), "滚轮缩放视野    |    鼠标选中 · 拖动转向    |    技能 2 仅上 / 下 / 左 / 右    |    右键取消瞄准    Esc 暂停菜单", small);
            float promptBottom = h - 85;
            if (skills.Mode != 0)
            {
                string target = skills.Selected ? skills.Selected.gameObject.name : "点击物体选择目标";
                string instruction = target + "\n再按 " + (skills.Mode == 1 ? skillOneKey : skillTwoKey) + " 释放并扣费  ·  右键取消";
                float height = Mathf.Max(94, skillHint.CalcHeight(new GUIContent(instruction), 392) + 48);
                var prompt = new Rect(w - 440, promptBottom - height, 420, height);
                Panel(prompt, new Color(.035f, .17f, .14f, .45f));
                GUI.Label(new Rect(prompt.x + 14, prompt.y + 8, 392, 30), "慢放 ×" + skills.slowScale.ToString("0.##") + "  /  " + (skills.Mode == 1 ? "速度方向" : "重力方向"), skillTitle);
                GUI.Label(new Rect(prompt.x + 14, prompt.y + 40, 392, prompt.height - 48), instruction, skillHint);
                promptBottom = prompt.y - 10;
                foreach (var t in FindObjectsByType<DirectionTarget>())
                {
                    if (skills.Mode == 2 && (t.isPlayer || !t.gravityEditable || t.GravityLocked)) continue;
                    Vector3 p = gameCamera.WorldToScreenPoint(t.transform.position);
                    if (p.z <= 0) continue;
                    GUI.color = t == skills.Selected ? Color.green : new Color(.5f, 1, .85f);
                    GUI.Label(new Rect(p.x / scale - 26, (Screen.height - p.y) / scale - 48, 90, 28), t == skills.Selected ? "[选中]" : "[可选]", small);
                    GUI.color = Color.white;
                }
            }
            if (Time.unscaledTime < messageUntil)
            {
                float height = Mathf.Max(44, skillHint.CalcHeight(new GUIContent(message), 392) + 20);
                var notice = new Rect(w - 440, promptBottom - height, 420, height);
                Panel(notice, new Color(.025f, .06f, .09f, .45f));
                GUI.Label(new Rect(notice.x + 14, notice.y + 10, 392, height - 20), message, skillHint);
            }
            if (Dead || Completed)
            {
                Panel(new Rect(w / 2 - 270, h / 2 - 75, 540, 150), new Color(.025f, .06f, .09f, .98f));
                GUI.Label(new Rect(w / 2 - 240, h / 2 - 55, 490, 50), Dead ? "生命耗尽" : "试验完成 / LEVEL CLEAR", title);
                GUI.Label(new Rect(w / 2 - 240, h / 2 + 9, 490, 65), Dead ? "正在返回最近的检查点…" : "重力机关与速度反射已完成。按 Esc 打开菜单。", normal);
            }
        }
        static void Panel(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        static void Bar(Rect r, float fill, Color color) { Panel(r, new Color(.12f, .19f, .23f)); r.width *= Mathf.Clamp01(fill); Panel(r, color); }
    }
}
