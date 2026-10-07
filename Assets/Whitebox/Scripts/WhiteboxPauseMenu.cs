using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace VectorWhitebox
{
    public class WhiteboxPauseMenu : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public bool IsSettingsOpen { get; private set; }
        public bool IsLevelSelectOpen { get; private set; }
        public float GameplayTime => Time.unscaledTime - pausedSeconds - (IsOpen ? Time.unscaledTime - pauseStartedAt : 0);
        float pausedSeconds, pauseStartedAt;
        WhiteboxGame game;
        WhiteboxAction? awaitingBinding;
        string notice;
        Font font;
        GUIStyle heading, label, button, small;
        struct LevelChoice { public int buildIndex; public string path, title; }
        readonly List<LevelChoice> levels = new List<LevelChoice>();
        Vector2 levelScroll;
        const string WindowModePref = "VectorWhitebox.WindowMode";

        void Awake()
        {
            game = GetComponent<WhiteboxGame>();
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 22);
            if (PlayerPrefs.HasKey(WindowModePref)) ApplyWindowMode(PlayerPrefs.GetInt(WindowModePref) == 1, false);
        }

        void Update()
        {
            if (SceneFadeTransition.IsTransitioning || WhiteboxFrontend.IsEndingOpen) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (awaitingBinding.HasValue)
            {
                foreach (var key in keyboard.allKeys)
                {
                    if (!key.wasPressedThisFrame) continue;
                    if (key.keyCode == Key.Escape)
                    {
                        awaitingBinding = null;
                        notice = "已取消改键。";
                    }
                    else if (WhiteboxControls.TryBind(awaitingBinding.Value, key.keyCode, out var error))
                    {
                        notice = WhiteboxControls.Name(awaitingBinding.Value) + " 已设为 " + WhiteboxControls.Display(key.keyCode);
                        awaitingBinding = null;
                    }
                    else notice = error;
                    return;
                }
                return;
            }
            if (!keyboard.escapeKey.wasPressedThisFrame) return;
            if (!game && !IsOpen) return;
            if (!IsOpen) Open();
            else if (IsSettingsOpen || IsLevelSelectOpen) ReturnToMain();
            else Close();
        }

        public void Open()
        {
            if (IsOpen || SceneFadeTransition.IsTransitioning || WhiteboxFrontend.IsEndingOpen) return;
            if (game && game.skills) game.skills.Cancel();
            pauseStartedAt = Time.unscaledTime;
            IsOpen = true;
            IsSettingsOpen = false;
            IsLevelSelectOpen = false;
            awaitingBinding = null;
            notice = null;
            Time.timeScale = 0;
        }

        public void Close()
        {
            if (IsOpen) pausedSeconds += Time.unscaledTime - pauseStartedAt;
            IsOpen = false;
            IsSettingsOpen = false;
            IsLevelSelectOpen = false;
            awaitingBinding = null;
            Time.timeScale = WhiteboxFrontend.IsEndingOpen && !SceneFadeTransition.IsTransitioning ? 0 : 1;
        }

        public void OpenSettings()
        {
            if (SceneFadeTransition.IsTransitioning || WhiteboxFrontend.IsEndingOpen) return;
            if (!IsOpen) Open();
            IsSettingsOpen = true;
            IsLevelSelectOpen = false;
            awaitingBinding = null;
            notice = null;
        }

        public void Unstuck()
        {
            Close();
            if (game) game.Respawn(reviveTurrets: true);
        }

        public void OpenLevelSelect()
        {
            if (!game || !IsOpen || SceneFadeTransition.IsTransitioning) return;
            IsSettingsOpen = false; IsLevelSelectOpen = true;
            awaitingBinding = null; notice = null; levelScroll = Vector2.zero;
            levels.Clear();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (!WhiteboxSaveGame.IsLevelScene(path)) continue;
                levels.Add(new LevelChoice { buildIndex = i, path = path, title = SceneFadeTransition.GetSceneTitle(path) });
            }
        }

        public void ReturnToMain()
        {
            if (!game) { Close(); return; }
            IsSettingsOpen = IsLevelSelectOpen = false;
            awaitingBinding = null; notice = null;
        }

        public bool SelectLevel(int buildIndex)
        {
            if (!IsOpen || !IsLevelSelectOpen || SceneFadeTransition.IsTransitioning) return false;
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings || !Application.CanStreamedLevelBeLoaded(buildIndex)
                || !WhiteboxSaveGame.IsLevelScene(SceneUtility.GetScenePathByBuildIndex(buildIndex)))
            { notice = "无法进入这个关卡。"; return false; }
            WhiteboxSaveGame.ClearResumeRequest();
            Close();
            if (game) game.BeginExit();
            SceneFadeTransition.Travel(buildIndex);
            return true;
        }

        void ApplyWindowMode(bool borderless, bool save)
        {
            Screen.fullScreenMode = borderless ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (!save) return;
            PlayerPrefs.SetInt(WindowModePref, borderless ? 1 : 0);
            PlayerPrefs.Save();
        }

        void OnDisable() { if (IsOpen) Close(); }

        void InitStyles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { font = font, fontSize = 31, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            heading.normal.textColor = new Color(.85f, 1f, .95f);
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 21, alignment = TextAnchor.MiddleLeft };
            label.normal.textColor = new Color(.85f, .94f, .96f);
            small = new GUIStyle(label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 21, alignment = TextAnchor.MiddleCenter };
            button.normal.textColor = Color.white;
            button.hover.textColor = Color.white;
            button.active.textColor = Color.white;
        }

        static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            GUI.depth = -100;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            InitStyles();
            Fill(new Rect(0, 0, w, h), new Color(0, .025f, .04f, .82f));
            float panelHeight = IsSettingsOpen ? 610 : IsLevelSelectOpen ? 570 : 612;
            var panel = new Rect(w / 2 - 315, h / 2 - panelHeight / 2, 630, panelHeight);
            Fill(panel, new Color(.025f, .075f, .09f, .98f));
            Fill(new Rect(panel.x, panel.y, panel.width, 3), new Color(.3f, .9f, .79f));
            float x = panel.x + 44, y = panel.y + 20;
            GUI.Label(new Rect(x, y, 542, 50), IsSettingsOpen ? "设置" : IsLevelSelectOpen ? "关卡选择" : "暂停菜单", heading);
            if (IsSettingsOpen) DrawSettings(x, y + 67);
            else if (IsLevelSelectOpen) DrawLevelSelect(x, y + 79);
            else DrawMain(x, y + 79);
        }

        void DrawMain(float x, float y)
        {
            if (GUI.Button(new Rect(x, y, 542, 55), "继续游戏", button)) Close();
            if (GUI.Button(new Rect(x, y + 69, 542, 55), "关卡选择", button)) OpenLevelSelect();
            if (GUI.Button(new Rect(x, y + 138, 542, 55), "脱离卡死 · 返回最近存档点", button)) Unstuck();
            if (GUI.Button(new Rect(x, y + 207, 542, 55), "设置", button)) OpenSettings();
            if (GUI.Button(new Rect(x, y + 276, 542, 55), "返回主菜单", button)) WhiteboxFrontend.GoToMainMenu();
            if (GUI.Button(new Rect(x, y + 345, 542, 55), "退出游戏", button))
            {
                Close();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            GUI.Label(new Rect(x, y + 426, 542, 42), "Esc 继续游戏 · 脱离卡死会恢复生命和能量", small);
        }

        void DrawLevelSelect(float x, float y)
        {
            GUI.Label(new Rect(x, y, 542, 40), "选择关卡从出生点开始测试，当前关卡可重新开始。", small);
            var viewport = new Rect(x, y + 48, 542, 280);
            levelScroll = GUI.BeginScrollView(viewport, levelScroll, new Rect(0, 0, 518, Mathf.Max(280, levels.Count * 64)));
            string currentPath = SceneManager.GetActiveScene().path;
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                string text = level.title + (level.path == currentPath ? "  ·  当前关卡 / 重新开始" : "");
                if (GUI.Button(new Rect(0, i * 64, 518, 52), text, button)) SelectLevel(level.buildIndex);
            }
            if (levels.Count == 0) GUI.Label(new Rect(0, 30, 518, 40), "没有可用关卡。", small);
            GUI.EndScrollView();
            if (GUI.Button(new Rect(x, y + 342, 542, 50), "返回暂停菜单", button)) ReturnToMain();
            GUI.Label(new Rect(x, y + 404, 542, 42), notice ?? "Esc 返回暂停菜单", small);
        }

        void DrawSettings(float x, float y)
        {
            for (int i = 0; i < 7; i++)
            {
                var action = (WhiteboxAction)i;
                float rowY = y + i * 48;
                GUI.Label(new Rect(x + 10, rowY, 270, 40), WhiteboxControls.Name(action), label);
                string text = awaitingBinding == action ? "请按新按键…" : WhiteboxControls.Display(action);
                if (GUI.Button(new Rect(x + 296, rowY + 2, 236, 38), text, button))
                { awaitingBinding = action; notice = "按 Esc 取消改键。"; }
            }
            float bottom = y + 352;
            bool borderless = Screen.fullScreenMode != FullScreenMode.Windowed;
            GUI.Label(new Rect(x + 10, bottom, 270, 40), "窗口模式", label);
            if (GUI.Button(new Rect(x + 296, bottom + 2, 236, 38), borderless ? "无边框全屏" : "窗口化", button))
                ApplyWindowMode(!borderless, true);
            if (GUI.Button(new Rect(x, bottom + 59, 260, 43), "恢复默认按键", button))
            { WhiteboxControls.ResetDefaults(); awaitingBinding = null; notice = "已恢复默认按键。"; }
            if (GUI.Button(new Rect(x + 282, bottom + 59, 260, 43), "返回", button))
            { ReturnToMain(); }
            GUI.Label(new Rect(x, bottom + 112, 542, 40), notice ?? "点击按键框后按新键；Esc 保留为暂停菜单。", small);
        }
    }
}
