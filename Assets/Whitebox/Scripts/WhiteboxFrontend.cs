using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace VectorWhitebox
{
    /// <summary>Canvas based frontend; every image, label and button is editable on the UI prefab.</summary>
    public class WhiteboxFrontend : MonoBehaviour
    {
        public enum ScreenMode { MainMenu, Ending }
        public ScreenMode mode;
        public Canvas uiCanvas;
        public GameObject menuRoot;
        public Button startButton, continueButton, settingsButton, quitButton, returnMainButton;
        public Text titleText, subtitleText, statusText;
        public WhiteboxPauseMenu settingsMenu;
        [Tooltip("白盒默认运行时加载中文字体；替换正式字体后可关闭。")]
        public bool useRuntimeCjkFont = true;

        static WhiteboxFrontend endingInstance;
        Font runtimeFont;
        public static bool IsEndingOpen => endingInstance && endingInstance.isActiveAndEnabled;

        void Awake()
        {
            if (!Application.isPlaying) return;
            EnsureEventSystem();
            if (useRuntimeCjkFont)
            {
                runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 24);
                foreach (var text in GetComponentsInChildren<Text>(true)) text.font = runtimeFont;
            }
            if (mode == ScreenMode.MainMenu)
            {
                Time.timeScale = 1;
                if (!settingsMenu) settingsMenu = GetComponent<WhiteboxPauseMenu>();
                if (!settingsMenu) settingsMenu = gameObject.AddComponent<WhiteboxPauseMenu>();
                RefreshSaveStatus();
            }
            else
            {
                endingInstance = this;
                Time.timeScale = 0;
            }
            if (startButton) startButton.onClick.AddListener(StartGame);
            if (continueButton) continueButton.onClick.AddListener(ContinueGame);
            if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton) quitButton.onClick.AddListener(QuitGame);
            if (returnMainButton) returnMainButton.onClick.AddListener(ReturnToMainMenu);
        }

        void Update()
        {
            bool busy = SceneFadeTransition.IsTransitioning;
            if (mode == ScreenMode.MainMenu && menuRoot)
                menuRoot.SetActive(!(settingsMenu && settingsMenu.IsOpen));
            if (startButton) startButton.interactable = !busy;
            if (continueButton) continueButton.interactable = !busy;
            if (settingsButton) settingsButton.interactable = !busy;
            if (quitButton) quitButton.interactable = !busy;
            if (returnMainButton) returnMainButton.interactable = !busy;
            if (mode == ScreenMode.Ending && !busy) Time.timeScale = 0;
        }

        public void RefreshSaveStatus()
        {
            bool available = WhiteboxSaveGame.TryGetSavedProgress(out var progress)
                && Application.CanStreamedLevelBeLoaded(progress.sceneName);
            if (continueButton) continueButton.gameObject.SetActive(available);
            if (!statusText) return;
            statusText.text = available
                ? "最近存档：" + SceneFadeTransition.GetSceneTitle(progress.sceneName) + (progress.finished ? " · 已完成" : "")
                : "";
        }

        public void StartGame()
        {
            if (mode != ScreenMode.MainMenu || SceneFadeTransition.IsTransitioning) return;
            if (!Application.CanStreamedLevelBeLoaded(WhiteboxSaveGame.FirstLevelScene))
            { SetStatus("第一关未加入构建列表。"); return; }
            WhiteboxSaveGame.StartNewGame();
            Time.timeScale = 1;
            Time.fixedDeltaTime = .02f;
            SceneFadeTransition.Travel(WhiteboxSaveGame.FirstLevelScene);
        }

        public void ContinueGame()
        {
            if (mode != ScreenMode.MainMenu || SceneFadeTransition.IsTransitioning) return;
            string scene = WhiteboxSaveGame.RequestContinue();
            if (string.IsNullOrEmpty(scene))
            { RefreshSaveStatus(); SetStatus("没有可继续的存档。"); return; }
            Time.timeScale = 1;
            Time.fixedDeltaTime = .02f;
            SceneFadeTransition.Travel(scene);
        }

        public void OpenSettings()
        {
            if (mode != ScreenMode.MainMenu || SceneFadeTransition.IsTransitioning || !settingsMenu) return;
            settingsMenu.OpenSettings();
            if (menuRoot) menuRoot.SetActive(false);
        }

        void SetStatus(string message) { if (statusText) statusText.text = message; }

        public void ReturnToMainMenu() { GoToMainMenu(); }

        public static void GoToMainMenu()
        {
            if (SceneFadeTransition.IsTransitioning || !Application.CanStreamedLevelBeLoaded(WhiteboxSaveGame.MainMenuScene)) return;
            WhiteboxSaveGame.ClearResumeRequest();
            var game = WhiteboxGame.Instance;
            if (game)
            {
                if (game.pauseMenu) game.pauseMenu.Close();
                game.BeginExit();
            }
            Time.timeScale = 1;
            Time.fixedDeltaTime = .02f;
            SceneFadeTransition.Travel(WhiteboxSaveGame.MainMenuScene);
        }

        public void QuitGame()
        {
            if (SceneFadeTransition.IsTransitioning) return;
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static void ShowEnding(WhiteboxFrontend prefab = null)
        {
            if (IsEndingOpen || SceneFadeTransition.IsTransitioning) return;
            var game = WhiteboxGame.Instance;
            if (game)
            {
                if (game.pauseMenu && game.pauseMenu.IsOpen) game.pauseMenu.Close();
                game.BeginExit();
            }
            WhiteboxSaveGame.MarkFinished();
            if (Application.CanStreamedLevelBeLoaded(WhiteboxSaveGame.EndingScene))
            {
                Time.timeScale = 1;
                Time.fixedDeltaTime = .02f;
                SceneFadeTransition.Travel(WhiteboxSaveGame.EndingScene);
                return;
            }
            if (prefab) endingInstance = Instantiate(prefab);
            else endingInstance = CreateDefault(ScreenMode.Ending);
            Time.timeScale = 0;
            Time.fixedDeltaTime = .02f;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current || FindAnyObjectByType<EventSystem>()) return;
            var system = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        void OnDestroy()
        {
            if (endingInstance == this) endingInstance = null;
            if (runtimeFont) Destroy(runtimeFont);
        }

        /// <summary>Used once by the editor builder, and as a fallback if a final portal has no UI prefab.</summary>
        public static WhiteboxFrontend CreateDefault(ScreenMode screenMode)
        {
            var root = new GameObject(screenMode == ScreenMode.MainMenu ? "Main Menu UI" : "Ending UI", typeof(RectTransform));
            root.SetActive(false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = screenMode == ScreenMode.MainMenu ? 100 : 200;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            var background = MakeImage("Background · Replace Art", root.transform, new Color(.025f, .045f, .065f, .98f));
            Stretch(background.rectTransform);
            var panel = MakeImage("Menu Layout", root.transform, new Color(.05f, .085f, .105f, .98f));
            Center(panel.rectTransform, new Vector2(1120, 580), Vector2.zero);
            var accent = MakeImage("Accent · Replace Art", panel.transform, new Color(.35f, .9f, .78f));
            Center(accent.rectTransform, new Vector2(1120, 3), new Vector2(0, 288));
            var art = MakeImage("Art Area · Empty Placeholder", panel.transform, new Color(.2f, .35f, .38f, .09f));
            Center(art.rectTransform, new Vector2(510, 470), new Vector2(275, 0));
            var content = new GameObject("Menu Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            Center((RectTransform)content.transform, new Vector2(430, 500), new Vector2(-290, 0));
            var title = MakeText("Title", content.transform, screenMode == ScreenMode.MainMenu ? "VECTOR" : "游戏结束", 42, TextAnchor.MiddleLeft);
            Center(title.rectTransform, new Vector2(430, 62), new Vector2(0, 197));
            var subtitle = MakeText("Subtitle", content.transform, screenMode == ScreenMode.MainMenu ? "方向试验场" : "所有试验已完成", 22, TextAnchor.MiddleLeft);
            Center(subtitle.rectTransform, new Vector2(430, 40), new Vector2(0, 142));
            var buttons = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            buttons.transform.SetParent(content.transform, false);
            var buttonsRect = (RectTransform)buttons.transform;
            buttonsRect.anchorMin = buttonsRect.anchorMax = new Vector2(.5f, .5f);
            buttonsRect.pivot = new Vector2(.5f, 1);
            buttonsRect.anchoredPosition = new Vector2(0, 90);
            buttonsRect.sizeDelta = new Vector2(410, 0);
            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            buttons.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var frontend = root.AddComponent<WhiteboxFrontend>();
            frontend.mode = screenMode; frontend.uiCanvas = canvas; frontend.menuRoot = panel.gameObject;
            frontend.titleText = title; frontend.subtitleText = subtitle;
            if (screenMode == ScreenMode.MainMenu)
            {
                frontend.startButton = MakeButton("Start Game", buttons.transform, "开始游戏");
                frontend.continueButton = MakeButton("Continue Game", buttons.transform, "继续游戏");
                frontend.settingsButton = MakeButton("Settings", buttons.transform, "设置");
                frontend.quitButton = MakeButton("Quit Game", buttons.transform, "退出游戏");
                frontend.settingsMenu = root.AddComponent<WhiteboxPauseMenu>();
            }
            else
            {
                frontend.returnMainButton = MakeButton("Return To Main Menu", buttons.transform, "返回主菜单");
                frontend.quitButton = MakeButton("Quit Game", buttons.transform, "退出游戏");
            }
            frontend.statusText = MakeText("Save Status", content.transform, "", 17, TextAnchor.MiddleLeft);
            Center(frontend.statusText.rectTransform, new Vector2(430, 62), new Vector2(0, -206));
            root.SetActive(true);
            return frontend;
        }

        static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color;
            return image;
        }

        static Text MakeText(string name, Transform parent, string content, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>(); text.text = content; text.fontSize = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = new Color(.89f, .97f, .96f); text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        static Button MakeButton(string name, Transform parent, string title)
        {
            var image = MakeImage(name, parent, new Color(.1f, .2f, .23f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.65f, .95f, .88f);
            colors.pressedColor = new Color(.4f, .75f, .66f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var element = image.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 52;
            var label = MakeText("Label", image.transform, title, 23, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return button;
        }

        static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
