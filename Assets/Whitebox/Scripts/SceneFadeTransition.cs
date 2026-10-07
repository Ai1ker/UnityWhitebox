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
                    case 1: return "第一关";
                    case 2: return "第二关";
                    case 3: return "第三关";
                    case 4: return "第四关";
                    case 5: return "第五关";
                    case 6: return "第六关";
                    default: return "第 " + level + " 关";
                }
            }
            if (name == "MainMenu") return "主菜单";
            return name == "GameEnd" ? "游戏结束" : name;
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
            chapter = GetSceneTitle(sceneName);
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
            GUI.depth = -1000;
            Color before = GUI.color;
            GUI.color = new Color(.015f, .025f, .045f, black);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            if (!string.IsNullOrEmpty(chapter))
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(32 * Mathf.Min(Screen.width / 1280f, Screen.height / 720f)), font = font };
                style.normal.textColor = new Color(.7f, 1f, .88f, black);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 80), chapter, style);
            }
            GUI.color = before;
        }
        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (font) Destroy(font);
        }
    }
}
