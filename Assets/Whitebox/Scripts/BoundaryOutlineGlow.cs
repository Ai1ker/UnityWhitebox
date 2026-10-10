using UnityEngine;
using UnityEngine.Rendering;

namespace VectorWhitebox
{
    [ExecuteAlways, DefaultExecutionOrder(1000), DisallowMultipleComponent]
    public sealed class BoundaryOutlineGlow : MonoBehaviour
    {
        public LineRenderer source;
        [Tooltip("只扩大柔光，不改变边界原有线宽或判定范围。"), Min(1f)]
        public float glowWidth = 7f;
        [Tooltip("柔光沿用原边界颜色。"), Range(0f, 1f)]
        public float glowOpacity = .45f;
        public Material glowMaterial;

        LineRenderer halo;
        Material generatedMaterial;
        Vector3[] positions = new Vector3[0];
        readonly Gradient haloGradient = new Gradient();

        public static BoundaryOutlineGlow Ensure(LineRenderer line)
        {
            if (!line) return null;
            var glow = line.GetComponent<BoundaryOutlineGlow>();
            if (!glow) glow = line.gameObject.AddComponent<BoundaryOutlineGlow>();
            glow.source = line;
            glow.SyncGlow();
            return glow;
        }

        void OnEnable()
        {
            if (!source) source = GetComponent<LineRenderer>();
        }

        void LateUpdate() { SyncGlow(); }

        public void SyncGlow()
        {
            if (!source || !isActiveAndEnabled || !source.enabled || !source.gameObject.activeInHierarchy || source.positionCount < 2)
            {
                if (halo) halo.enabled = false;
                return;
            }

            var material = GlowMaterial();
            if (!material)
            {
                if (halo) halo.enabled = false;
                return;
            }
            if (!halo)
            {
                var child = new GameObject("Boundary light halo");
                child.hideFlags = HideFlags.HideAndDontSave;
                child.transform.SetParent(source.transform, false);
                halo = child.AddComponent<LineRenderer>();
                halo.shadowCastingMode = ShadowCastingMode.Off;
                halo.receiveShadows = false;
            }
            if (halo.transform.parent != source.transform) halo.transform.SetParent(source.transform, false);
            halo.transform.localPosition = Vector3.zero;
            halo.transform.localRotation = Quaternion.identity;
            halo.transform.localScale = Vector3.one;
            halo.gameObject.layer = source.gameObject.layer;
            halo.sharedMaterial = material;
            halo.useWorldSpace = source.useWorldSpace;
            halo.loop = source.loop;
            halo.alignment = source.alignment;
            halo.textureMode = source.textureMode;
            halo.widthCurve = source.widthCurve;
            halo.widthMultiplier = source.widthMultiplier * Mathf.Max(1f, glowWidth);
            halo.numCapVertices = Mathf.Max(4, source.numCapVertices);
            halo.numCornerVertices = Mathf.Max(4, source.numCornerVertices);
            halo.sortingLayerID = source.sortingLayerID;
            halo.sortingOrder = source.sortingOrder - 1;
            halo.forceRenderingOff = source.forceRenderingOff;

            var gradient = source.colorGradient;
            var alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++) alphaKeys[i].alpha *= Mathf.Clamp01(glowOpacity);
            haloGradient.SetKeys(gradient.colorKeys, alphaKeys);
            haloGradient.mode = gradient.mode;
            halo.colorGradient = haloGradient;
            if (positions.Length != source.positionCount) positions = new Vector3[source.positionCount];
            source.GetPositions(positions);
            halo.positionCount = positions.Length;
            halo.SetPositions(positions);
            halo.enabled = true;
        }

        Material GlowMaterial()
        {
            if (glowMaterial) return glowMaterial;
            var resource = Resources.Load<Material>("BoundaryLineGlow");
            if (resource) return resource;
            if (!generatedMaterial)
            {
                var shader = Resources.Load<Shader>("BoundaryLineGlow");
                if (!shader) return null;
                generatedMaterial = new Material(shader) { name = "BoundaryLineGlow (runtime)", hideFlags = HideFlags.HideAndDontSave };
            }
            return generatedMaterial;
        }

        void OnDisable() { RemoveHalo(); }
        void OnDestroy()
        {
            RemoveHalo();
            if (generatedMaterial)
            {
                if (Application.isPlaying) Destroy(generatedMaterial);
                else DestroyImmediate(generatedMaterial);
                generatedMaterial = null;
            }
        }

        void RemoveHalo()
        {
            if (!halo) return;
            halo.enabled = false;
            var child = halo.gameObject;
            halo = null;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }
}
