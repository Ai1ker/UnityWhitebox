using UnityEngine;

namespace VectorWhitebox
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class LaboratoryCubeCoreVisual : MonoBehaviour
    {
        [Tooltip("箱子根物体原有的隐藏 SpriteRenderer，保留关卡中设置的核心颜色。")]
        public SpriteRenderer tintSource;
        public SpriteRenderer shellRenderer;
        public SpriteRenderer coreRenderer;

        void OnEnable() { RefreshVisual(); }
        void LateUpdate() { RefreshVisual(); }
        void OnValidate() { RefreshVisual(); }

        public void RefreshVisual()
        {
            if (!tintSource || !shellRenderer || !coreRenderer ||
                coreRenderer.transform != transform || shellRenderer.transform != transform.parent) return;

            if (transform.localPosition != Vector3.zero) transform.localPosition = Vector3.zero;
            if (transform.localRotation != Quaternion.identity) transform.localRotation = Quaternion.identity;
            if (transform.localScale != Vector3.one) transform.localScale = Vector3.one;
            if (coreRenderer.color != tintSource.color) coreRenderer.color = tintSource.color;
            if (coreRenderer.drawMode != SpriteDrawMode.Sliced) coreRenderer.drawMode = SpriteDrawMode.Sliced;
            if (coreRenderer.size != shellRenderer.size) coreRenderer.size = shellRenderer.size;
        }
    }
}
