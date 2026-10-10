using System;
using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox
{
    /// <summary>A placeable bilingual laboratory monitor with readable, pulsing diagnostics.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaboratoryDisplayScreen : MonoBehaviour
    {
        public Canvas screenCanvas;
        public Text screenText;
        public SpriteRenderer frameRenderer;
        public SpriteRenderer crackRenderer;
        public SpriteRenderer panelRenderer;
        public SpriteRenderer glitchRenderer;

        [TextArea(3, 12)] public string chineseContent = "实验室信息终端\n在 Inspector 中填写显示内容。";
        [TextArea(3, 12)] public string englishContent = "LABORATORY TERMINAL\nEnter the message in the Inspector.";
        [Min(8)] public int fontSize = 30;
        [Min(8)] public int minFontSize = 14;
        public TextAnchor textAlignment = TextAnchor.UpperLeft;
        public Color textColor = new Color(.59f, .91f, .9f, 1f);
        public Color panelColor = new Color(.025f, .07f, .085f, 1f);
        public Color crackColor = new Color(.62f, .81f, .84f, .5f);
        public Color glitchColor = new Color(.65f, .93f, 1f, .3f);
        public Color diagnosticColor = new Color(1f, .3f, .21f, 1f);
        [Tooltip("可选自定义字体；运行时未指定时使用系统中英文字体。")]
        public Font customFont;

        public bool enableDiagnostics = true;
        public bool pauseWithGame = true;
        [Tooltip("屏幕随机出现 WARNING / ERROR 的间隔（秒）。")]
        public Vector2 glitchIntervalSeconds = new Vector2(7f, 17f);
        [Tooltip("每次警报持续的随机时间（秒）；警报结束后恢复自定义正文。")]
        public Vector2 glitchDurationSeconds = new Vector2(2.2f, 3.2f);
        [Range(0f, 1f)] public float glitchStrength = .75f;
        [Tooltip("警报亮暗脉冲的周期（秒）。文字保持可读，不会完全闪灭。"), Min(.25f)]
        public float diagnosticPulseSeconds = 1.1f;
        [Tooltip("警报标题相对正文字号的倍率；标题会自动适应屏幕大小。"), Range(1f, 2.5f)]
        public float diagnosticFontScale = 1.65f;
        public Color diagnosticPanelColor = new Color(.2f, .025f, .015f, 1f);
        [NonSerialized] public bool previewAnimation;

        System.Random random;
        Font generatedFont;
        Text capturedText;
        Vector2 textPosition;
        bool capturedTextEnabled;
        RectTransform diagnosticRoot;
        Text diagnosticTitle;
        LaboratoryWarningSymbolGraphic diagnosticSymbol;
        Image diagnosticTopBar, diagnosticBottomBar;
        float untilGlitch, glitchRemaining, diagnosticElapsed, pulse = 1f;
        bool diagnosticError, needsRefresh;

        public bool IsGlitching => (Application.isPlaying || previewAnimation) && glitchRemaining > 0f;

        void OnEnable()
        {
            random = new System.Random(Guid.NewGuid().GetHashCode());
            untilGlitch = SampleRange(glitchIntervalSeconds, .1f) * Mathf.Lerp(.2f, 1f, Next01());
            glitchRemaining = diagnosticElapsed = 0f;
            pulse = 1f;
            WhiteboxLocalization.LanguageChanged += RefreshVisual;
            RefreshVisual();
        }

        void OnValidate() { needsRefresh = true; }

        void Update()
        {
            if (needsRefresh) RefreshVisual();
            if (Application.isPlaying || previewAnimation) AdvanceAnimation(Time.unscaledDeltaTime);
        }

        public void AdvanceAnimation(float delta)
        {
            if (!Application.isPlaying && !previewAnimation) return;
            if (pauseWithGame && Application.isPlaying && Time.timeScale <= 0f) return;
            if (float.IsNaN(delta) || float.IsInfinity(delta)) return;
            delta = Mathf.Clamp(delta, 0f, 1f);
            if (!enableDiagnostics)
            {
                if (glitchRemaining > 0f) FinishDiagnostic();
                return;
            }

            if (glitchRemaining <= 0f)
            {
                untilGlitch -= delta;
                if (untilGlitch <= 0f) TriggerDiagnostic(Next01() < .5f);
                return;
            }

            glitchRemaining -= delta;
            if (glitchRemaining <= 0f)
            {
                FinishDiagnostic();
                return;
            }

            diagnosticElapsed += delta;
            pulse = .5f + .5f * Mathf.Cos(diagnosticElapsed * Mathf.PI * 2f /
                Mathf.Max(.25f, diagnosticPulseSeconds));
            ApplyDisplay();
        }

        public void TriggerDiagnostic(bool error)
        {
            if ((!Application.isPlaying && !previewAnimation) || !enableDiagnostics) return;
            if (glitchRemaining <= 0f && capturedText)
            {
                textPosition = capturedText.rectTransform.anchoredPosition;
                capturedTextEnabled = capturedText.enabled;
            }
            diagnosticError = error;
            glitchRemaining = SampleRange(glitchDurationSeconds, .02f);
            diagnosticElapsed = 0f;
            pulse = 1f;
            ApplyDisplay();
        }

        void FinishDiagnostic()
        {
            glitchRemaining = 0f;
            untilGlitch = SampleRange(glitchIntervalSeconds, .1f);
            pulse = 1f;
            RestoreTextPosition();
            ApplyDisplay();
        }

        public void RefreshVisual()
        {
            needsRefresh = false;
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(this)) return;
#endif
            if (!screenCanvas) screenCanvas = GetComponentInChildren<Canvas>(true);
            if (!screenText) screenText = GetComponentInChildren<Text>(true);
            if (capturedText != screenText)
            {
                RestoreTextPosition();
                if (capturedText) capturedText.enabled = capturedTextEnabled;
                ReleaseDiagnosticOverlay();
                capturedText = screenText;
                if (capturedText)
                {
                    textPosition = capturedText.rectTransform.anchoredPosition;
                    capturedTextEnabled = capturedText.enabled;
                }
            }
            else if (capturedText && !IsGlitching)
            {
                textPosition = capturedText.rectTransform.anchoredPosition;
                capturedTextEnabled = capturedText.enabled;
            }
            if (screenCanvas) screenCanvas.renderMode = RenderMode.WorldSpace;
            if (screenText)
            {
                Font font = ResolveFont();
                screenText.font = font;
                screenText.material = font ? font.material : null;
                screenText.fontSize = Mathf.Max(8, fontSize);
                screenText.resizeTextForBestFit = true;
                screenText.resizeTextMinSize = Mathf.Clamp(minFontSize, 8, Mathf.Max(8, fontSize));
                screenText.resizeTextMaxSize = Mathf.Max(8, fontSize);
                screenText.horizontalOverflow = HorizontalWrapMode.Wrap;
                screenText.verticalOverflow = VerticalWrapMode.Truncate;
                screenText.alignment = textAlignment;
                screenText.lineSpacing = 1.15f;
                screenText.raycastTarget = false;
                screenText.supportRichText = false;
            }
            ApplyDisplay();
        }

        Font ResolveFont()
        {
            if (customFont) return customFont;
            // Keep transient OS fonts out of the saved prefab and scene data.
            if (!Application.isPlaying) return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!generatedFont)
            {
                generatedFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, Mathf.Max(8, fontSize));
                if (generatedFont) generatedFont.hideFlags = HideFlags.HideAndDontSave;
            }
            return generatedFont;
        }

        void ApplyDisplay()
        {
            bool diagnostic = (Application.isPlaying || previewAnimation) && glitchRemaining > 0f;
            if (screenText)
            {
                screenText.text = diagnostic
                    ? (diagnosticError ? WhiteboxLocalization.Text("ERROR · 错误", "ERROR") : WhiteboxLocalization.Text("WARNING · 警告", "WARNING"))
                    : WhiteboxLocalization.Text(chineseContent,
                        string.IsNullOrWhiteSpace(englishContent) ? chineseContent : englishContent);
                screenText.color = diagnostic ? diagnosticColor : textColor;
                // The warning has its own layout, so the designer's normal text rect,
                // font size and alignment never need to be changed or reconstructed.
                screenText.enabled = diagnostic ? false : capturedTextEnabled;
            }
            ApplyDiagnosticOverlay(diagnostic);
            if (crackRenderer) crackRenderer.color = crackColor;
            if (panelRenderer)
            {
                Color color = panelColor;
                if (diagnostic)
                    color = Color.Lerp(color, diagnosticPanelColor,
                        Mathf.Lerp(.72f, 1f, pulse) * Mathf.Lerp(.75f, 1f, glitchStrength));
                panelRenderer.color = color;
            }
            if (glitchRenderer)
            {
                Color color = diagnostic ? Color.Lerp(glitchColor, diagnosticColor, .65f) : glitchColor;
                color.a = diagnostic ? glitchColor.a * glitchStrength * Mathf.Lerp(.18f, .55f, pulse) : 0f;
                glitchRenderer.color = color;
            }
        }

        void ApplyDiagnosticOverlay(bool visible)
        {
            if (!visible)
            {
                if (diagnosticRoot) diagnosticRoot.gameObject.SetActive(false);
                return;
            }
            if (!screenText) return;
            if (!diagnosticRoot) CreateDiagnosticOverlay();
            if (!diagnosticRoot) return;

            var bounds = screenText.rectTransform;
            diagnosticRoot.anchorMin = bounds.anchorMin;
            diagnosticRoot.anchorMax = bounds.anchorMax;
            diagnosticRoot.pivot = bounds.pivot;
            diagnosticRoot.sizeDelta = bounds.sizeDelta;
            diagnosticRoot.anchoredPosition = textPosition;
            diagnosticRoot.localScale = bounds.localScale;
            diagnosticRoot.localRotation = bounds.localRotation;
            diagnosticRoot.gameObject.SetActive(true);
            diagnosticRoot.SetAsLastSibling();

            float symbolSize = Mathf.Min(diagnosticRoot.rect.width * .15f,
                diagnosticRoot.rect.height * .58f);
            diagnosticSymbol.rectTransform.sizeDelta = Vector2.one * Mathf.Max(8f, symbolSize);
            diagnosticTitle.font = screenText.font;
            diagnosticTitle.material = screenText.material;
            diagnosticTitle.fontSize = Mathf.Max(8, Mathf.RoundToInt(fontSize * diagnosticFontScale));
            diagnosticTitle.resizeTextMinSize = Mathf.Min(Mathf.Max(8, minFontSize), diagnosticTitle.fontSize);
            diagnosticTitle.resizeTextMaxSize = diagnosticTitle.fontSize;
            string subtitleSize = Mathf.Max(8, Mathf.RoundToInt(diagnosticTitle.fontSize * .65f)).ToString();
            diagnosticTitle.text = diagnosticError
                ? WhiteboxLocalization.Text("ERROR\n<size=" + subtitleSize + ">错误</size>", "ERROR")
                : WhiteboxLocalization.Text("WARNING\n<size=" + subtitleSize + ">警告</size>", "WARNING");

            Color color = diagnosticColor;
            color.a *= Mathf.Lerp(.9f, 1f, pulse);
            diagnosticTitle.color = color;
            color.a = diagnosticColor.a * Mathf.Lerp(.64f, 1f, pulse);
            diagnosticSymbol.color = color;
            color.a = diagnosticColor.a * Mathf.Lerp(.45f, .95f, pulse);
            diagnosticTopBar.color = diagnosticBottomBar.color = color;
        }

        void CreateDiagnosticOverlay()
        {
            if (!screenText || (!Application.isPlaying && !previewAnimation)) return;
            diagnosticRoot = CreateOverlayObject("Diagnostic warning", screenText.transform.parent);
            diagnosticTitle = CreateOverlayObject("Warning title", diagnosticRoot).gameObject.AddComponent<Text>();
            diagnosticTitle.raycastTarget = false;
            diagnosticTitle.supportRichText = true;
            diagnosticTitle.alignment = TextAnchor.MiddleCenter;
            diagnosticTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            diagnosticTitle.verticalOverflow = VerticalWrapMode.Truncate;
            diagnosticTitle.resizeTextForBestFit = true;
            diagnosticTitle.lineSpacing = .92f;
            Stretch(diagnosticTitle.rectTransform, new Vector2(.22f, .16f), new Vector2(.98f, .91f));

            diagnosticSymbol = CreateOverlayObject("Warning symbol", diagnosticRoot).gameObject
                .AddComponent<LaboratoryWarningSymbolGraphic>();
            diagnosticSymbol.raycastTarget = false;
            diagnosticSymbol.rectTransform.anchorMin = diagnosticSymbol.rectTransform.anchorMax = new Vector2(.11f, .5f);
            diagnosticSymbol.rectTransform.anchoredPosition = Vector2.zero;

            diagnosticTopBar = CreateOverlayObject("Warning top bar", diagnosticRoot).gameObject.AddComponent<Image>();
            diagnosticBottomBar = CreateOverlayObject("Warning bottom bar", diagnosticRoot).gameObject.AddComponent<Image>();
            diagnosticTopBar.raycastTarget = diagnosticBottomBar.raycastTarget = false;
            Stretch(diagnosticTopBar.rectTransform, new Vector2(0f, .97f), Vector2.one);
            Stretch(diagnosticBottomBar.rectTransform, Vector2.zero, new Vector2(1f, .03f));
        }

        static RectTransform CreateOverlayObject(string objectName, Transform parent)
        {
            var result = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            result.hideFlags = HideFlags.HideAndDontSave;
            result.transform.SetParent(parent, false);
            return (RectTransform)result.transform;
        }

        static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        void ReleaseDiagnosticOverlay()
        {
            if (!diagnosticRoot) return;
            if (Application.isPlaying) Destroy(diagnosticRoot.gameObject);
            else DestroyImmediate(diagnosticRoot.gameObject);
            diagnosticRoot = null;
            diagnosticTitle = null;
            diagnosticSymbol = null;
            diagnosticTopBar = diagnosticBottomBar = null;
        }

        void RestoreTextPosition()
        {
            if (capturedText) capturedText.rectTransform.anchoredPosition = textPosition;
        }

        float Next01()
        {
            if (random == null) random = new System.Random(Guid.NewGuid().GetHashCode());
            return (float)random.NextDouble();
        }

        float SampleRange(Vector2 range, float minimum)
        {
            float low = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
            float high = Mathf.Max(low, Mathf.Max(range.x, range.y));
            return Mathf.Lerp(low, high, Next01());
        }

        void OnDisable()
        {
            WhiteboxLocalization.LanguageChanged -= RefreshVisual;
            glitchRemaining = 0f;
            pulse = 1f;
            RestoreTextPosition();
            ApplyDisplay();
            ReleaseDiagnosticOverlay();
            ReleaseFont();
        }

        void OnDestroy() { ReleaseDiagnosticOverlay(); ReleaseFont(); }

        void ReleaseFont()
        {
            if (!generatedFont) return;
            if (screenText && screenText.font == generatedFont)
            {
                screenText.font = null;
                screenText.material = null;
            }
            if (Application.isPlaying) Destroy(generatedFont);
            else DestroyImmediate(generatedFont);
            generatedFont = null;
        }
    }

    /// <summary>Geometry-only warning icon, with no extra texture or serialized child objects.</summary>
    sealed class LaboratoryWarningSymbolGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = GetPixelAdjustedRect();
            float size = Mathf.Min(bounds.width, bounds.height);
            Vector2 center = bounds.center;
            Vector2 top = center + new Vector2(0f, size * .48f);
            Vector2 left = center + new Vector2(-size * .48f, -size * .38f);
            Vector2 right = center + new Vector2(size * .48f, -size * .38f);
            Vector2 innerTop = center + new Vector2(0f, size * .28f);
            Vector2 innerLeft = center + new Vector2(-size * .31f, -size * .28f);
            Vector2 innerRight = center + new Vector2(size * .31f, -size * .28f);
            AddQuad(vertices, top, left, innerLeft, innerTop);
            AddQuad(vertices, left, right, innerRight, innerLeft);
            AddQuad(vertices, right, top, innerTop, innerRight);
            AddRectangle(vertices, center, size, -.045f, -.08f, .045f, .18f);
            AddRectangle(vertices, center, size, -.045f, -.22f, .045f, -.13f);
        }

        void AddRectangle(VertexHelper vertices, Vector2 center, float size,
            float left, float bottom, float right, float top)
        {
            AddQuad(vertices, center + new Vector2(left, bottom) * size,
                center + new Vector2(left, top) * size,
                center + new Vector2(right, top) * size,
                center + new Vector2(right, bottom) * size);
        }

        void AddQuad(VertexHelper vertices, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int start = vertices.currentVertCount;
            vertices.AddVert(a, color, Vector2.zero);
            vertices.AddVert(b, color, Vector2.zero);
            vertices.AddVert(c, color, Vector2.zero);
            vertices.AddVert(d, color, Vector2.zero);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }
    }
}
