using UnityEngine;

namespace VectorWhitebox
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaboratorySurfaceVisual : MonoBehaviour
    {
        public enum SurfaceKind { Panel, OneWay, Spikes, Cube }

        [Tooltip("直接父物体上的原有方形碰撞体；仅用来读取外观尺寸。")]
        public BoxCollider2D shape;
        public SpriteRenderer surfaceRenderer;
        public SpriteRenderer[] legacyRenderers;
        public SurfaceKind kind;

        void OnEnable()
        {
            if (!shape && transform.parent) shape = transform.parent.GetComponent<BoxCollider2D>();
            if (!surfaceRenderer) surfaceRenderer = GetComponent<SpriteRenderer>();
            RefreshVisual();
        }

        void Update() { RefreshVisual(); }
        void OnValidate() { RefreshVisual(); }

        public void RefreshVisual()
        {
            var parent = transform.parent;
            // The art belongs directly under the existing collider root. Never adjust that root.
            if (!parent || !shape || shape.transform != parent || !surfaceRenderer ||
                surfaceRenderer.transform != transform) return;

            if (surfaceRenderer.sprite && legacyRenderers != null)
            {
                foreach (var legacy in legacyRenderers)
                    if (legacy && legacy != surfaceRenderer && legacy.enabled) legacy.enabled = false;
            }

            Vector3 parentScale = parent.lossyScale;
            float widthScale = Mathf.Abs(parentScale.x);
            float heightScale = Mathf.Abs(parentScale.y);
            Vector3 position = new Vector3(shape.offset.x, shape.offset.y, 0f);
            Vector3 scale = new Vector3(1f / Mathf.Max(.0001f, widthScale),
                1f / Mathf.Max(.0001f, heightScale), 1f);
            Vector2 size = new Vector2(shape.size.x * widthScale, shape.size.y * heightScale);
            if ((kind == SurfaceKind.OneWay || kind == SurfaceKind.Spikes) && surfaceRenderer.sprite)
            {
                float artHeight = Mathf.Max(.0001f, surfaceRenderer.sprite.bounds.size.y);
                size.y = artHeight;
                scale.y = shape.size.y / artHeight;
            }
            SpriteDrawMode drawMode = kind == SurfaceKind.Cube ? SpriteDrawMode.Sliced : SpriteDrawMode.Tiled;

            // Positive reciprocal scales keep the parent's negative-scale mirroring intact.
            if (transform.localPosition != position) transform.localPosition = position;
            if (transform.localRotation != Quaternion.identity) transform.localRotation = Quaternion.identity;
            if (transform.localScale != scale) transform.localScale = scale;
            if (surfaceRenderer.drawMode != drawMode) surfaceRenderer.drawMode = drawMode;
            if (surfaceRenderer.size != size) surfaceRenderer.size = size;
            if (drawMode == SpriteDrawMode.Tiled && surfaceRenderer.tileMode != SpriteTileMode.Continuous)
                surfaceRenderer.tileMode = SpriteTileMode.Continuous;
        }
    }
}
