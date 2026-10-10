using UnityEngine;

namespace VectorWhitebox
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaboratoryBackgroundWall : MonoBehaviour
    {
        public SpriteRenderer wallRenderer;
        [InspectorName("背景尺寸"), Tooltip("也可以缩放根对象；面板纹理会继续平铺。")]
        public Vector2 size = new Vector2(12, 8);
        public Color tint = Color.white;

        void OnEnable() { RefreshVisual(); }
        void LateUpdate() { RefreshVisual(); }

        public void RefreshVisual()
        {
            if (!wallRenderer || wallRenderer.transform.parent != transform) return;
            var worldScale = transform.lossyScale;
            float x = Mathf.Max(.0001f, Mathf.Abs(worldScale.x));
            float y = Mathf.Max(.0001f, Mathf.Abs(worldScale.y));
            var scale = new Vector3(1f / x, 1f / y, 1);
            var renderSize = new Vector2(Mathf.Max(.1f, size.x) * x, Mathf.Max(.1f, size.y) * y);
            if (wallRenderer.transform.localScale != scale) wallRenderer.transform.localScale = scale;
            if (wallRenderer.drawMode != SpriteDrawMode.Tiled) wallRenderer.drawMode = SpriteDrawMode.Tiled;
            if (wallRenderer.tileMode != SpriteTileMode.Continuous) wallRenderer.tileMode = SpriteTileMode.Continuous;
            if (wallRenderer.size != renderSize) wallRenderer.size = renderSize;
            if (wallRenderer.color != tint) wallRenderer.color = tint;
        }
    }
}
