using System;
using UnityEngine;

namespace VectorWhitebox
{
    /// <summary>Visual-only flicker for a glass panel and its light/shadow layers.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaboratoryGlassFlicker : MonoBehaviour
    {
        public SpriteRenderer glassRenderer;
        public SpriteRenderer lightRenderer;
        public SpriteRenderer shadowRenderer;
        public Color glassTint = new Color(.72f, .86f, .92f, .7f);
        public Color lightTint = new Color(.45f, .82f, .91f, .28f);
        public Color shadowTint = new Color(.015f, .025f, .035f, .3f);

        public bool enableFlicker = true;
        public bool pauseWithGame = true;
        [Tooltip("损坏灯开始闪烁的随机间隔（秒）。")]
        public Vector2 flickerIntervalSeconds = new Vector2(2.5f, 7f);
        [Tooltip("每次闪烁持续的随机时间（秒）。")]
        public Vector2 flickerDurationSeconds = new Vector2(.12f, .48f);
        [Range(0f, 1f)] public float flickerIntensity = .85f;
        [Min(0f)] public float shadowJitterDistance = .025f;
        [NonSerialized] public bool previewAnimation;

        System.Random random;
        float untilFlicker, flickerRemaining, untilStep, currentIntensity = 1f;
        Vector3 shadowPosition;
        SpriteRenderer capturedShadow;
        bool needsRefresh;

        public bool IsFlickering => (Application.isPlaying || previewAnimation) && flickerRemaining > 0f;

        void OnEnable()
        {
            random = new System.Random(Guid.NewGuid().GetHashCode());
            untilFlicker = SampleRange(flickerIntervalSeconds, .05f) * (float)random.NextDouble();
            flickerRemaining = untilStep = 0f;
            currentIntensity = 1f;
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
            if (!enableFlicker)
            {
                if (flickerRemaining > 0f || currentIntensity != 1f)
                {
                    flickerRemaining = 0f;
                    currentIntensity = 1f;
                    ApplyColors();
                    RestoreShadowPosition();
                }
                return;
            }

            // Use real seconds during skill slow motion, but stop while the pause menu is open.
            if (flickerRemaining <= 0f)
            {
                untilFlicker -= delta;
                if (untilFlicker > 0f) return;
                flickerRemaining = SampleRange(flickerDurationSeconds, .02f);
                untilStep = 0f;
            }

            flickerRemaining -= delta;
            if (flickerRemaining <= 0f)
            {
                currentIntensity = 1f;
                untilFlicker = SampleRange(flickerIntervalSeconds, .05f);
                RestoreShadowPosition();
                ApplyColors();
                return;
            }

            untilStep -= delta;
            if (untilStep > 0f) return;
            untilStep = Mathf.Lerp(.025f, .09f, Next01());
            currentIntensity = Mathf.Lerp(1f - flickerIntensity, 1f, Next01() > .3f ? Next01() : 0f);
            if (capturedShadow)
            {
                float offset = (Next01() * 2f - 1f) * shadowJitterDistance;
                capturedShadow.transform.localPosition = shadowPosition + Vector3.right * offset;
            }
            ApplyColors();
        }

        public void RefreshVisual()
        {
            needsRefresh = false;
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(this)) return;
#endif
            if (capturedShadow != shadowRenderer)
            {
                RestoreShadowPosition();
                capturedShadow = shadowRenderer;
                if (capturedShadow) shadowPosition = capturedShadow.transform.localPosition;
            }
            ApplyColors();
        }

        void ApplyColors()
        {
            if (glassRenderer) glassRenderer.color = glassTint;
            if (lightRenderer)
                lightRenderer.color = new Color(lightTint.r, lightTint.g, lightTint.b,
                    lightTint.a * currentIntensity);
            if (shadowRenderer)
                shadowRenderer.color = new Color(shadowTint.r, shadowTint.g, shadowTint.b,
                    shadowTint.a * Mathf.Lerp(.55f, 1.3f, 1f - currentIntensity));
        }

        void RestoreShadowPosition()
        {
            if (capturedShadow) capturedShadow.transform.localPosition = shadowPosition;
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
            flickerRemaining = 0f;
            currentIntensity = 1f;
            RestoreShadowPosition();
            ApplyColors();
        }
    }
}
