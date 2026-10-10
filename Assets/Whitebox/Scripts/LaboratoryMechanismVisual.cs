using UnityEngine;

namespace VectorWhitebox
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaboratoryMechanismVisual : MonoBehaviour
    {
        public SpriteRenderer geometrySource;
        public SpriteRenderer tintSource;
        public SpriteRenderer shellRenderer;
        public SpriteRenderer lightRenderer;
        public SpriteRenderer[] legacyRenderers;
        public LineRenderer[] legacyLines;
        public bool shellFollowsAlpha = true;
        [Min(.0001f)] public float alphaReference = 1f;
        public SpriteDrawMode drawMode = SpriteDrawMode.Sliced;

        void OnEnable() { RefreshVisual(); }
        void LateUpdate() { RefreshVisual(); }

        public void RefreshVisual()
        {
            // Keep the original renderer's geometry intact for mechanism detection.
            if (!geometrySource || !shellRenderer || transform.parent != geometrySource.transform ||
                shellRenderer.transform != transform) return;

            Bounds localBounds = geometrySource.localBounds;
            Vector3 sourceScale = geometrySource.transform.lossyScale;
            float widthScale = Mathf.Abs(sourceScale.x);
            float heightScale = Mathf.Abs(sourceScale.y);
            Vector3 position = localBounds.center;
            Vector3 scale = new Vector3(1f / Mathf.Max(.0001f, widthScale),
                1f / Mathf.Max(.0001f, heightScale), 1f);
            Vector2 size = new Vector2(localBounds.size.x * widthScale,
                localBounds.size.y * heightScale);

            if (transform.localPosition != position) transform.localPosition = position;
            if (transform.localRotation != Quaternion.identity) transform.localRotation = Quaternion.identity;
            if (transform.localScale != scale) transform.localScale = scale;

            Color tint = tintSource ? tintSource.color : geometrySource.color;
            float alpha = Mathf.Clamp01(tint.a / Mathf.Max(.0001f, alphaReference));
            ApplyRenderer(shellRenderer, size, new Color(1f, 1f, 1f, shellFollowsAlpha ? alpha : 1f));

            if (lightRenderer && lightRenderer != shellRenderer && lightRenderer != geometrySource)
            {
                Transform lightTransform = lightRenderer.transform;
                if (lightTransform != transform)
                {
                    if (lightTransform.localPosition != Vector3.zero) lightTransform.localPosition = Vector3.zero;
                    if (lightTransform.localRotation != Quaternion.identity) lightTransform.localRotation = Quaternion.identity;
                    if (lightTransform.localScale != Vector3.one) lightTransform.localScale = Vector3.one;
                }
                ApplyRenderer(lightRenderer, size, new Color(tint.r, tint.g, tint.b, alpha));
            }

            if (geometrySource.enabled) geometrySource.enabled = false;
            if (legacyRenderers != null)
                foreach (var legacy in legacyRenderers)
                    if (legacy && legacy != shellRenderer && legacy != lightRenderer && legacy.enabled)
                        legacy.enabled = false;
            if (legacyLines != null)
                foreach (var legacy in legacyLines)
                    if (legacy && legacy.enabled) legacy.enabled = false;
        }

        void ApplyRenderer(SpriteRenderer renderer, Vector2 size, Color color)
        {
            if (renderer.drawMode != drawMode) renderer.drawMode = drawMode;
            if (renderer.size != size) renderer.size = size;
            if (drawMode == SpriteDrawMode.Tiled && renderer.tileMode != SpriteTileMode.Continuous)
                renderer.tileMode = SpriteTileMode.Continuous;
            if (renderer.flipX != geometrySource.flipX) renderer.flipX = geometrySource.flipX;
            if (renderer.flipY != geometrySource.flipY) renderer.flipY = geometrySource.flipY;
            if (renderer.color != color) renderer.color = color;
        }
    }
}
