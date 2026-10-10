using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox
{
    public class SceneFadeTransition : MonoBehaviour
    {
        static SceneFadeTransition instance;
        public static bool IsTransitioning => instance;
        float black;
        string chapter;
        Font font;
        GUIStyle chapterStyle, captionStyle;
        public static void Travel(string sceneName)
        {
            StartTransition(sceneName, -1);
        }
        public static void Travel(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings || !Application.CanStreamedLevelBeLoaded(buildIndex)) return;
            StartTransition(SceneUtility.GetScenePathByBuildIndex(buildIndex), buildIndex);
        }
        public static string GetSceneTitle(string scenePath)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (name.StartsWith("Level") && int.TryParse(name.Substring(5).Split('_')[0], out int level) && level > 0)
            {
                switch (level)
                {
                    case 1: return WhiteboxLocalization.Text("第一关", "Level 1");
                    case 2: return WhiteboxLocalization.Text("第二关", "Level 2");
                    case 3: return WhiteboxLocalization.Text("第三关", "Level 3");
                    case 4: return WhiteboxLocalization.Text("第四关", "Level 4");
                    case 5: return WhiteboxLocalization.Text("第五关", "Level 5");
                    case 6: return WhiteboxLocalization.Text("第六关", "Level 6");
                    default: return WhiteboxLocalization.Format("第 {0} 关", "Level {0}", level);
                }
            }
            if (name == "MainMenu") return WhiteboxLocalization.Text("主菜单", "Main Menu");
            return name == "GameEnd" ? WhiteboxLocalization.Text("游戏结束", "Game Complete") : name;
        }
        static void StartTransition(string sceneName, int buildIndex)
        {
            if (instance) return;
            var go = new GameObject("Scene Fade Transition");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFadeTransition>();
            instance.StartCoroutine(instance.Transition(sceneName, buildIndex));
        }
        IEnumerator Transition(string sceneName, int buildIndex)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 32);
            yield return Fade(0, 1, .65f);
            chapter = sceneName;
            if (buildIndex >= 0) SceneManager.LoadScene(buildIndex);
            else SceneManager.LoadScene(sceneName);
            yield return null;
            yield return new WaitForSecondsRealtime(.55f);
            yield return Fade(1, 0, .9f);
            instance = null;
            Destroy(gameObject);
        }
        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            { black = Mathf.Lerp(from, to, elapsed / seconds); yield return null; }
            black = to;
        }
        void OnGUI()
        {
            if (black <= .001f) return;
            int beforeDepth = GUI.depth;
            GUI.depth = -1000;
            Color before = GUI.color;
            Matrix4x4 beforeMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            LaboratoryUiTheme.Fill(new Rect(0, 0, Screen.width, Screen.height), FadeColor(LaboratoryUiTheme.Ink, black));
            if (!string.IsNullOrEmpty(chapter))
            {
                float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
                float w = Screen.width / scale, h = Screen.height / scale;
                if (chapterStyle == null)
                {
                    chapterStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, font = font, padding = new RectOffset(0, 0, 0, 0) };
                    chapterStyle.normal.textColor = LaboratoryUiTheme.Text;
                    captionStyle = new GUIStyle(chapterStyle) { fontSize = 12 };
                    captionStyle.normal.textColor = LaboratoryUiTheme.Muted;
                }
                Color line = FadeColor(LaboratoryUiTheme.Line, black * .45f);
                LaboratoryUiTheme.DrawRule(new Rect(w / 2 - 182, h / 2 - 47, 153, 1), line);
                LaboratoryUiTheme.DrawRule(new Rect(w / 2 + 29, h / 2 - 47, 153, 1), line);
                LaboratoryUiTheme.DrawPanel(new Rect(w / 2 - 7, h / 2 - 54, 14, 14), Color.clear, FadeColor(LaboratoryUiTheme.Accent, black), 3);
                GUI.color = new Color(1, 1, 1, black);
                GUI.Label(new Rect(w / 2 - 240, h / 2 - 17, 480, 47), GetSceneTitle(chapter), chapterStyle);
                GUI.Label(new Rect(w / 2 - 240, h / 2 + 47, 480, 22), WhiteboxLocalization.Text("Rotcev / 正在切换场景", "Rotcev / SCENE TRANSITION"), captionStyle);
            }
            GUI.color = before;
            GUI.matrix = beforeMatrix;
            GUI.depth = beforeDepth;
        }
        static Color FadeColor(Color color, float alpha) { color.a = alpha; return color; }
        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (font) Destroy(font);
        }
    }
}
