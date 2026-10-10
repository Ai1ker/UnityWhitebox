using System.Collections.Generic;
using UnityEngine;

namespace VectorWhitebox
{
    public enum CardinalGravityDirection { Down, Up, Left, Right }

    [DefaultExecutionOrder(-200), RequireComponent(typeof(BoxCollider2D))]
    public class ForcedGravityBoundary : MonoBehaviour
    {
        [InspectorName("强制重力方向")] public CardinalGravityDirection direction;
        public Vector2 size = new Vector2(12, 8);
        [Tooltip("重叠区域中优先使用数值较大的区域；相同优先级使用先启用的区域。")]
        public int priority;
        public bool showBoundary = true;
        [Min(.01f)] public float lineWidth = .065f;
        public Color boundaryColor = new Color(1f, .15f, .2f, .85f);
        public Material outlineMaterial;
        static readonly HashSet<ForcedGravityBoundary> active = new HashSet<ForcedGravityBoundary>();
        static int sequence;
        int order;
        LineRenderer outline;
        Material generatedMaterial;
        readonly Vector3[] corners = new Vector3[4];

        public Vector2 GravityDirection => direction == CardinalGravityDirection.Up ? Vector2.up :
            direction == CardinalGravityDirection.Left ? Vector2.left :
            direction == CardinalGravityDirection.Right ? Vector2.right : Vector2.down;
        void OnEnable() { order = ++sequence; active.Add(this); SyncCollider(); }
        void OnDisable() { active.Remove(this); if (outline) outline.enabled = false; }
        void OnDestroy() { active.Remove(this); if (generatedMaterial) Destroy(generatedMaterial); }
        void OnValidate() { SyncCollider(); }
        void SyncCollider()
        {
            var shape = GetComponent<BoxCollider2D>();
            if (!shape) return;
            shape.isTrigger = true; shape.size = new Vector2(Mathf.Max(.1f, size.x), Mathf.Max(.1f, size.y));
        }
        public bool Contains(Vector2 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
            return Mathf.Abs(local.x) <= Mathf.Max(.1f, size.x) * .5f && Mathf.Abs(local.y) <= Mathf.Max(.1f, size.y) * .5f;
        }
        public static bool TryGetDirection(DirectionTarget target, out Vector2 result)
        {
            result = Vector2.down;
            if (!target || !target.gameObject.activeInHierarchy) return false;
            ForcedGravityBoundary best = null;
            foreach (var area in active)
            {
                if (!area || !area.isActiveAndEnabled || area.gameObject.scene != target.gameObject.scene || !area.Contains(target.Body.position)) continue;
                if (!best || area.priority > best.priority || (area.priority == best.priority && area.order < best.order)) best = area;
            }
            if (!best) return false;
            result = best.GravityDirection;
            return true;
        }
        void OnTriggerEnter2D(Collider2D other) { AffectBody(other); }
        void OnTriggerStay2D(Collider2D other) { AffectBody(other); }
        void AffectBody(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (!body || body.bodyType != RigidbodyType2D.Dynamic || !Contains(body.position)) return;
            var target = body.GetComponent<DirectionTarget>();
            if (!target)
            {
                target = body.gameObject.AddComponent<DirectionTarget>();
                target.isPlayer = body.GetComponent<WhiteboxPlayer>();
                target.gravityEditable = false;
            }
            target.RefreshForcedGravity();
        }
        LineRenderer MakeOutline()
        {
            var child = new GameObject("Red gravity boundary"); child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.sortingOrder = 3;
            return line;
        }
        void LateUpdate()
        {
            if (!showBoundary) { if (outline) outline.enabled = false; return; }
            if (!outline) { outline = MakeOutline(); outline.loop = true; outline.positionCount = 4; }
            if (!outlineMaterial && !generatedMaterial) generatedMaterial = new Material(Shader.Find("Sprites/Default"));
            outline.enabled = true;
            outline.sharedMaterial = outlineMaterial ? outlineMaterial : generatedMaterial;
            outline.startColor = outline.endColor = boundaryColor;
            outline.startWidth = outline.endWidth = lineWidth;
            float x = Mathf.Max(.1f, size.x) * .5f, y = Mathf.Max(.1f, size.y) * .5f;
            corners[0] = new Vector3(-x, -y, -.05f); corners[1] = new Vector3(x, -y, -.05f);
            corners[2] = new Vector3(x, y, -.05f); corners[3] = new Vector3(-x, y, -.05f);
            outline.SetPositions(corners);
            BoundaryOutlineGlow.Ensure(outline);
        }
        void OnDrawGizmos()
        {
            var previous = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix; Gizmos.color = boundaryColor;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(Mathf.Max(.1f, size.x), Mathf.Max(.1f, size.y), 0));
            Gizmos.matrix = previous;
        }
    }
}
