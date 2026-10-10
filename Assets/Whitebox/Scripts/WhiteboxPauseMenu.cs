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
        System.Func<string> notice;
        Font font;
        GUIStyle heading, label, button, valueButton, toggleButton, small, indexLabel, volumeTrack, volumeThumb;
        float contentWidth;
        struct LevelChoice { public int buildIndex; public string path, title; }
        readonly List<LevelChoice> levels = new List<LevelChoice>();
        Vector2 levelScroll;
        const string WindowModePref = "VectorWhitebox.WindowMode";
        static string L(string chinese, string english) => WhiteboxLocalization.Text(chinese, english);

        void SetNotice(string chinese, string english)
        { notice = () => WhiteboxLocalization.Text(chinese, english); }

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
                        SetNotice("已取消改键。", "Key binding canceled.");
                    }
                    else if (WhiteboxControls.TryBind(awaitingBinding.Value, key.keyCode, out _))
                    {
                        var action = awaitingBinding.Value;
                        var boundKey = key.keyCode;
                        notice = () => WhiteboxLocalization.Format("{0} 已设为 {1}", "{0} is now {1}.", WhiteboxControls.Name(action), WhiteboxControls.Display(boundKey));
                        awaitingBinding = null;
                    }
                    else
                    {
                        var action = awaitingBinding.Value;
                        var rejectedKey = key.keyCode;
                        notice = () => WhiteboxControls.BindingError(action, rejectedKey);
                    }
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
            LaboratoryAudio.SaveVolume();
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
            LaboratoryAudio.SaveVolume();
            if (!game) { Close(); return; }
            IsSettingsOpen = IsLevelSelectOpen = false;
            awaitingBinding = null; notice = null;
        }

        public bool SelectLevel(int buildIndex)
        {
            if (!IsOpen || !IsLevelSelectOpen || SceneFadeTransition.IsTransitioning) return false;
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings || !Application.CanStreamedLevelBeLoaded(buildIndex)
                || !WhiteboxSaveGame.IsLevelScene(SceneUtility.GetScenePathByBuildIndex(buildIndex)))
            { SetNotice("无法进入这个关卡。", "This level is unavailable."); return false; }
            if (!LevelUnlockProgress.IsUnlocked(SceneUtility.GetScenePathByBuildIndex(buildIndex)))
            { SetNotice("此关卡尚未解锁，请先通关前一关。", "This level is locked. Complete the previous level first."); return false; }
            WhiteboxSaveGame.ClearResumeRequest();
            Close();
            if (game) game.BeginExit();
            SceneFadeTransition.Travel(buildIndex);
            return true;
        }

        public void SetTesterUnlockAll(bool enabled)
        {
            LevelUnlockProgress.TesterUnlockAll = enabled;
            var frontend = GetComponent<WhiteboxFrontend>();
            if (frontend) frontend.RefreshSaveStatus();
            SetNotice(enabled ? "测试者模式：全部关卡可选，不计入正式解锁进度。" : "已关闭测试者模式，选关恢复为正式通关进度。",
                enabled ? "Tester mode: all levels available. Test clears do not advance normal progress." : "Tester mode off. Level access follows your normal progress.");
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
            heading = new GUIStyle(GUI.skin.label) { font = font, fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            heading.normal.textColor = LaboratoryUiTheme.Text;
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 19, alignment = TextAnchor.MiddleLeft };
            label.normal.textColor = LaboratoryUiTheme.Text;
            small = new GUIStyle(label) { fontSize = 14, alignment = TextAnchor.UpperLeft, wordWrap = true };
            small.normal.textColor = LaboratoryUiTheme.Muted;
            indexLabel = new GUIStyle(small) { fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            indexLabel.normal.textColor = LaboratoryUiTheme.Accent;
            button = new GUIStyle(GUIStyle.none)
            {
                font = font, fontSize = 20, alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(58, 18, 0, 0), border = new RectOffset(),
                margin = new RectOffset(), wordWrap = false
            };
            foreach (var state in new[] { button.normal, button.hover, button.active, button.focused, button.onNormal, button.onHover, button.onActive, button.onFocused })
            {
                state.background = null;
                state.textColor = LaboratoryUiTheme.Text;
            }
            valueButton = new GUIStyle(button)
            {
                fontSize = 18, alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 0, 0)
            };
            toggleButton = new GUIStyle(valueButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(44, 10, 0, 0)
            };
            volumeTrack = new GUIStyle(GUIStyle.none) { fixedHeight = 24, margin = new RectOffset(), padding = new RectOffset() };
            volumeThumb = new GUIStyle(GUIStyle.none) { fixedWidth = 18, fixedHeight = 24, margin = new RectOffset() };
        }

        static void Fill(Rect rect, Color color)
        {
            LaboratoryUiTheme.Fill(rect, color);
        }

        static void Panel(Rect rect, Color fill, Color edge)
        {
            LaboratoryUiTheme.DrawPanel(rect, fill, edge, 9);
        }

        bool FlatButton(Rect rect, string text, string number = null, bool accent = false)
        {
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Color surface = hover ? Color.Lerp(LaboratoryUiTheme.PanelSoft, LaboratoryUiTheme.Accent, .15f) : LaboratoryUiTheme.PanelSoft;
            Color edge = hover || accent ? LaboratoryUiTheme.Accent : LaboratoryUiTheme.Line;
            Panel(rect, surface, edge);
            if (hover || accent) Fill(new Rect(rect.x, rect.y + 12, 3, rect.height - 24), LaboratoryUiTheme.Accent);
            if (number != null)
            {
                GUI.Label(new Rect(rect.x + 8, rect.y, 38, rect.height), number, indexLabel);
                Fill(new Rect(rect.x + 47, rect.y + 13, 1, rect.height - 26), LaboratoryUiTheme.Line);
            }
            return GUI.Button(rect, text, number == null ? valueButton : button);
        }

        bool FlatCheckbox(Rect rect, string text, bool selected)
        {
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Panel(rect, LaboratoryUiTheme.PanelSoft, hover ? LaboratoryUiTheme.Accent : LaboratoryUiTheme.Line);
            var box = new Rect(rect.x + 13, rect.y + (rect.height - 18) * .5f, 18, 18);
            Panel(box, Color.clear, selected ? LaboratoryUiTheme.Accent : LaboratoryUiTheme.Line);
            if (selected)
            {
                LaboratoryUiTheme.DrawLine(new Vector2(box.x + 4, box.y + 9), new Vector2(box.x + 8, box.y + 13), LaboratoryUiTheme.Accent, 2);
                LaboratoryUiTheme.DrawLine(new Vector2(box.x + 8, box.y + 13), new Vector2(box.x + 15, box.y + 5), LaboratoryUiTheme.Accent, 2);
            }
            return GUI.Button(rect, text, toggleButton) ? !selected : selected;
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -100;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            InitStyles();
            try
            {
                GUI.color = Color.white;
                Color overlay = LaboratoryUiTheme.Ink;
                overlay.a = .92f;
                Fill(new Rect(0, 0, w, h), overlay);
                float panelHeight = IsSettingsOpen ? 668 : IsLevelSelectOpen ? 586 : 612;
                float panelWidth = IsSettingsOpen || IsLevelSelectOpen ? 760 : 660;
                var panel = new Rect((w - panelWidth) / 2, (h - panelHeight) / 2, panelWidth, panelHeight);
                Panel(panel, LaboratoryUiTheme.Panel, LaboratoryUiTheme.Line);
                float x = panel.x + 40, y = panel.y + 25;
                contentWidth = panel.width - 80;
                Fill(new Rect(x, y + 7, 4, 34), LaboratoryUiTheme.Accent);
                GUI.Label(new Rect(x + 20, y, contentWidth - 110, 48), IsSettingsOpen ? L("设置", "Settings") : IsLevelSelectOpen ? L("关卡选择", "Level Select") : L("暂停", "Paused"), heading);
                GUI.Label(new Rect(panel.xMax - 125, y + 6, 85, 34), IsSettingsOpen ? "02" : IsLevelSelectOpen ? "03" : "01", indexLabel);
                Fill(new Rect(x, y + 59, contentWidth, 1), LaboratoryUiTheme.Line);
                if (IsSettingsOpen) DrawSettings(x, panel.y + 96);
                else if (IsLevelSelectOpen) DrawLevelSelect(x, panel.y + 103);
                else DrawMain(x, panel.y + 110);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.depth = oldDepth;
            }
        }

        void DrawMain(float x, float y)
        {
            if (FlatButton(new Rect(x, y, contentWidth, 48), L("继续游戏", "Resume"), "01", true)) Close();
            if (FlatButton(new Rect(x, y + 60, contentWidth, 48), L("关卡选择", "Level Select"), "02")) OpenLevelSelect();
            if (FlatButton(new Rect(x, y + 120, contentWidth, 48), L("脱离卡死 · 返回最近存档点", "Unstuck · Return to Checkpoint"), "03")) Unstuck();
            if (FlatButton(new Rect(x, y + 180, contentWidth, 48), L("设置", "Settings"), "04")) OpenSettings();
            if (FlatButton(new Rect(x, y + 240, contentWidth, 48), L("返回主菜单", "Main Menu"), "05")) WhiteboxFrontend.GoToMainMenu();
            if (FlatButton(new Rect(x, y + 300, contentWidth, 48), L("退出游戏", "Quit Game"), "06"))
            {
                Close();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            Fill(new Rect(x, y + 378, contentWidth, 1), LaboratoryUiTheme.Line);
            GUI.Label(new Rect(x, y + 395, contentWidth, 44), L("Esc 继续游戏\n脱离卡死会恢复生命和能量", "Esc resumes\nUnstuck restores health and energy"), small);
        }

        void DrawLevelSelect(float x, float y)
        {
            GUI.Label(new Rect(x, y, contentWidth, 38), LevelUnlockProgress.TesterUnlockAll
                ? L("测试者模式：全部关卡已临时开放。", "Tester mode: all levels are temporarily available.")
                : L("通关后解锁下一关，已解锁关卡可从出生点重新开始。", "Complete each level to unlock the next. Unlocked levels restart at their spawn point."), small);
            var viewport = new Rect(x, y + 43, contentWidth, 280);
            float rowWidth = contentWidth - 22;
            levelScroll = GUI.BeginScrollView(viewport, levelScroll, new Rect(0, 0, rowWidth, Mathf.Max(280, levels.Count * 58)));
            string currentPath = SceneManager.GetActiveScene().path;
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                bool unlocked = LevelUnlockProgress.IsUnlocked(level.path);
                string text = SceneFadeTransition.GetSceneTitle(level.path) + (!unlocked ? L("  ·  未解锁", "  ·  Locked")
                    : level.path == currentPath ? L("  ·  当前关卡 / 重新开始", "  ·  Current / Restart") : "");
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && unlocked;
                if (FlatButton(new Rect(0, i * 58, rowWidth, 46), text, (i + 1).ToString("00"), unlocked && level.path == currentPath)) SelectLevel(level.buildIndex);
                GUI.enabled = enabled;
            }
            if (levels.Count == 0) GUI.Label(new Rect(0, 30, rowWidth, 40), L("没有可用关卡。", "No levels available."), small);
            GUI.EndScrollView();
            if (FlatButton(new Rect(x, y + 340, contentWidth, 46), L("返回暂停菜单", "Back to Pause Menu"))) ReturnToMain();
            GUI.Label(new Rect(x, y + 404, contentWidth, 42), notice != null ? notice() : L("Esc 返回暂停菜单", "Esc returns to the pause menu"), small);
        }

        void DrawSettings(float x, float y)
        {
            for (int i = 0; i < 7; i++)
            {
                var action = (WhiteboxAction)i;
                float rowY = y + i * 36;
                GUI.Label(new Rect(x, rowY, 32, 38), (i + 1).ToString("00"), indexLabel);
                GUI.Label(new Rect(x + 44, rowY, contentWidth - 342, 38), WhiteboxControls.Name(action), label);
                string text = awaitingBinding == action ? L("请按新按键…", "Press a key…") : WhiteboxControls.Display(action);
                if (FlatButton(new Rect(x + contentWidth - 280, rowY + 1, 280, 32), text, null, awaitingBinding == action))
                { awaitingBinding = action; SetNotice("按 Esc 取消改键。", "Press Esc to cancel."); }
                Fill(new Rect(x + 44, rowY + 35, contentWidth - 44, 1), LaboratoryUiTheme.Line * new Color(1, 1, 1, .45f));
            }
            float bottom = y + 264;
            bool borderless = Screen.fullScreenMode != FullScreenMode.Windowed;
            GUI.Label(new Rect(x + 44, bottom, contentWidth - 342, 38), L("窗口模式", "Display mode"), label);
            if (FlatButton(new Rect(x + contentWidth - 280, bottom + 1, 280, 36), borderless ? L("无边框全屏", "Borderless") : L("窗口化", "Windowed")))
                ApplyWindowMode(!borderless, true);
            GUI.Label(new Rect(x + 44, bottom + 40, contentWidth - 342, 38), L("语言", "Language"), label);
            string language = WhiteboxLocalization.Mode == WhiteboxLanguage.System ? L("跟随系统", "System") :
                WhiteboxLocalization.Mode == WhiteboxLanguage.Chinese ? "中文" : "English";
            if (FlatButton(new Rect(x + contentWidth - 280, bottom + 41, 280, 36), language))
                WhiteboxLocalization.SetMode((WhiteboxLanguage)(((int)WhiteboxLocalization.Mode + 1) % 3));
            GUI.Label(new Rect(x + 44, bottom + 80, contentWidth - 342, 38), L("测试者选项", "Tester options"), label);
            bool tester = LevelUnlockProgress.TesterUnlockAll;
            bool selected = FlatCheckbox(new Rect(x + contentWidth - 280, bottom + 81, 280, 36), L("解锁全部关卡", "Unlock all levels"), tester);
            if (selected != tester) SetTesterUnlockAll(selected);
            GUI.Label(new Rect(x + 44, bottom + 120, contentWidth - 342, 38), L("音量", "Volume"), label);
            DrawVolume(new Rect(x + contentWidth - 280, bottom + 121, 280, 36));
            float halfWidth = (contentWidth - 16) / 2;
            if (FlatButton(new Rect(x, bottom + 166, halfWidth, 44), L("恢复默认按键", "Reset Key Bindings")))
            { WhiteboxControls.ResetDefaults(); awaitingBinding = null; SetNotice("已恢复默认按键。", "Default key bindings restored."); }
            if (FlatButton(new Rect(x + halfWidth + 16, bottom + 166, halfWidth, 44), L("返回", "Back")))
            { LaboratoryAudio.SaveVolume(); ReturnToMain(); }
            GUI.Label(new Rect(x, bottom + 218, contentWidth, 44), notice != null ? notice() : L("点击按键框后按新键；Esc 保留为暂停菜单。", "Click a binding, then press a key. Esc opens the pause menu."), small);
        }

        void DrawVolume(Rect rect)
        {
            var slider = new Rect(rect.x + 9, rect.y + 6, rect.width - 76, 24);
            float volume = GUI.HorizontalSlider(slider, LaboratoryAudio.MasterVolume, 0, 1, volumeTrack, volumeThumb);
            LaboratoryAudio.MasterVolume = volume;
            float left = slider.x + 9, width = slider.width - 18;
            Fill(new Rect(left, slider.center.y - 2, width, 4), LaboratoryUiTheme.Line);
            Fill(new Rect(left, slider.center.y - 2, width * volume, 4), LaboratoryUiTheme.Accent);
            Panel(new Rect(left + width * volume - 8, slider.y + 3, 16, 18), LaboratoryUiTheme.PanelSoft, LaboratoryUiTheme.Accent);
            GUI.Label(new Rect(rect.xMax - 61, rect.y, 61, 36), Mathf.RoundToInt(volume * 100) + "%", valueButton);
            if (Event.current.rawType == EventType.MouseUp) LaboratoryAudio.SaveVolume();
        }
    }
}
