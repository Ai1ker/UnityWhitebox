using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox
{
    /// <summary>Bilingual world labels; also adapts the original Level 1 signs without rewriting the scene.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(TextMesh))]
    public class LocalizedWorldText : MonoBehaviour
    {
        [TextArea(1, 8)] public string chinese;
        [TextArea(1, 8)] public string english;
        public TextMesh target;
        public bool useRuntimeCjkFont = true;
        Font generatedFont, originalFont;
        Material originalMaterial;
        MeshRenderer meshRenderer;

        void OnEnable()
        {
            if (!target) target = GetComponent<TextMesh>();
            if (string.IsNullOrEmpty(chinese) && string.IsNullOrEmpty(english) && target)
                chinese = target.text;
            WhiteboxLocalization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() { WhiteboxLocalization.LanguageChanged -= Refresh; }

        public void Refresh()
        {
            if (!Application.isPlaying) return;
            if (!target) target = GetComponent<TextMesh>();
            if (target && useRuntimeCjkFont && !generatedFont)
            {
                originalFont = target.font;
                meshRenderer = target.GetComponent<MeshRenderer>();
                if (meshRenderer) originalMaterial = meshRenderer.sharedMaterial;
                generatedFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, Mathf.Max(32, target.fontSize));
                if (generatedFont)
                {
                    generatedFont.hideFlags = HideFlags.HideAndDontSave;
                    target.font = generatedFont;
                    if (meshRenderer) meshRenderer.sharedMaterial = generatedFont.material;
                }
            }
            if (target) target.text = WhiteboxLocalization.Text(chinese, english);
        }

        void OnDestroy()
        {
            if (!generatedFont) return;
            if (target) target.font = originalFont;
            if (meshRenderer) meshRenderer.sharedMaterial = originalMaterial;
            if (Application.isPlaying) Destroy(generatedFont); else DestroyImmediate(generatedFont);
        }

        public static void BindLegacySigns(Scene scene)
        {
            foreach (var mesh in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
            {
                if (mesh.gameObject.scene != scene || mesh.GetComponentInParent<SceneHintText>() || mesh.GetComponent<LocalizedWorldText>()) continue;
                string original = mesh.text;
                string key = System.Text.RegularExpressions.Regex.Replace((original ?? "").Trim(), @"\s+", " ");
                string translated;
                switch (key)
                {
                    case "MOUSE WHEEL / ZOOM VIEW": translated = "鼠标滚轮 / 缩放视野"; break;
                    case "02 / GRAVITY": translated = "02 / 重力"; break;
                    case "CHECKPOINT": translated = "检查点"; break;
                    case "03 / REDIRECT": translated = "03 / 改向"; break;
                    case "01 / MOVE": translated = "01 / 移动"; break;
                    case "EXIT": translated = "出口"; break;
                    default: continue;
                }
                var localized = mesh.gameObject.AddComponent<LocalizedWorldText>();
                localized.target = mesh;
                localized.chinese = translated;
                localized.english = original;
                localized.Refresh();
            }
        }
    }
}
