using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class SceneHintText : MonoBehaviour
    {
        [TextArea(3, 10)] public string content = "在这里填写给玩家的提示。\n支持中文、多行和自动换行。";
        [Tooltip("English text. Leave empty to use a built-in translation when available, otherwise the original text.")]
        [TextArea(3, 10)] public string contentEnglish = "";
        public Vector2 boxSize = new Vector2(7, 2.4f);
        [Range(14, 80)] public int fontSize = 36;
        [Range(0, 50)] public float padding = 18;
        public Color textColor = new Color(.85f, 1f, .95f);
        public Color backgroundColor = new Color(.025f, .075f, .09f, .8f);
        public bool showBackground = true;
        public Font customFont;
        [HideInInspector] public Material backgroundMaterial;
        Text label;
        Image background;
        Font generatedFont;
        Material generatedBackgroundMaterial;
        bool needsRefresh;

        void OnEnable()
        {
            WhiteboxLocalization.LanguageChanged += Refresh;
            Refresh();
        }
        void OnValidate() { needsRefresh = true; }
        void Update() { if (needsRefresh) Refresh(); }
        public void Refresh()
        {
            needsRefresh = false;
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(this)) return;
#endif
            if (!label) CreateVisuals();
            var rect = GetComponent<RectTransform>();
            if (rect) rect.sizeDelta = new Vector2(Mathf.Max(.5f, boxSize.x), Mathf.Max(.3f, boxSize.y)) * 100;
            if (background)
            {
                if (!backgroundMaterial && !generatedBackgroundMaterial)
                {
                    generatedBackgroundMaterial = new Material(Shader.Find("Sprites/Default"));
                    generatedBackgroundMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                background.material = backgroundMaterial ? backgroundMaterial : generatedBackgroundMaterial;
                background.enabled = showBackground; background.color = backgroundColor;
            }
            if (!label) return;
            if (!customFont && !generatedFont)
            {
                generatedFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, fontSize);
                if (generatedFont) generatedFont.hideFlags = HideFlags.HideAndDontSave;
            }
            label.font = customFont ? customFont : generatedFont;
            // The font atlas stores glyph coverage in alpha; its text shader renders Chinese correctly in the 2D pipeline.
            label.material = label.font ? label.font.material : null;
            label.text = WhiteboxLocalization.Text(content,
                string.IsNullOrWhiteSpace(contentEnglish) ? DefaultEnglish(content) : contentEnglish);
            label.fontSize = fontSize;
            label.color = textColor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = fontSize;
            label.rectTransform.offsetMin = Vector2.one * padding;
            label.rectTransform.offsetMax = Vector2.one * -padding;
        }
        static string DefaultEnglish(string original)
        {
            // Existing scene overrides keep their Chinese source and their authored line breaks.
            // Only known project text is translated here; new hints can supply contentEnglish.
            switch ((original ?? "").Trim())
            {
                case "在这里填写给玩家的提示。\n支持中文、多行和自动换行。":
                    return "Write a hint for the player here.\nSupports multiple lines and automatic word wrapping.";
                case "有时候炮塔也可以帮助你推开BOX":
                    return "Sometimes a turret can help you move a box.";
                case "tips:技能1可以对自己释放":
                    return "Tip: Skill 1 can also target yourself.";
                case "推进器：在推进器的\n橙色边界内可以不断\n加速箱子":
                    return "Accelerator: the orange boundary\nlets the box keep building momentum.";
                case "第四关":
                    return "Level 4";
                case "引力流:强制把任何进入其\n中的物体拉向一个方向":
                    return "Gravity field: forces every object\ninside toward its assigned direction.";
                default:
                    return "";
            }
        }
        void CreateVisuals()
        {
            // Keep procedural font/visual references out of scene and prefab serialization.
            var panel = new GameObject("Hint background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.hideFlags = HideFlags.HideAndDontSave;
            panel.transform.SetParent(transform, false);
            background = panel.GetComponent<Image>(); background.raycastTarget = false;
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            var text = new GameObject("Hint content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            text.hideFlags = HideFlags.HideAndDontSave;
            text.transform.SetParent(transform, false);
            label = text.GetComponent<Text>(); label.alignment = TextAnchor.MiddleCenter; label.lineSpacing = 1.15f;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        }
        void OnDestroy()
        {
            if (label) { label.font = null; label.material = null; }
            if (generatedFont) { if (Application.isPlaying) Destroy(generatedFont); else DestroyImmediate(generatedFont); }
            if (generatedBackgroundMaterial)
            { if (Application.isPlaying) Destroy(generatedBackgroundMaterial); else DestroyImmediate(generatedBackgroundMaterial); }
        }
        void OnDisable()
        {
            WhiteboxLocalization.LanguageChanged -= Refresh;
            if (label)
            {
                label.font = null; label.material = null;
                if (Application.isPlaying) Destroy(label.gameObject); else DestroyImmediate(label.gameObject);
                label = null;
            }
            if (background)
            {
                if (Application.isPlaying) Destroy(background.gameObject); else DestroyImmediate(background.gameObject);
                background = null;
            }
        }
    }
}
