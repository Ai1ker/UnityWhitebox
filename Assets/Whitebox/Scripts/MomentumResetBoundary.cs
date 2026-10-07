using System.Collections.Generic;
using UnityEngine;

namespace VectorWhitebox
{
    public class MomentumResetBoundary : MonoBehaviour
    {
        [Tooltip("箱子进入橙色范围后，离开时才会归位。开局在范围外不会传送；归位到范围外后，需要再次进入才能启用。")]
        public DirectionTarget cube;
        [Tooltip("留空或选择箱子自身（含其子对象）时，回到箱子开局的位置；指定独立对象时，回到该对象当前位置。")]
        public Transform returnPoint;
        public Vector2 size = new Vector2(16, 13);
        public bool showBoundary = true;
        [Min(.01f)] public float lineWidth = .065f;
        public Color boundaryColor = new Color(1f, .48f, .08f, .8f);
        public Material outlineMaterial;
        public Vector2 SpawnPosition { get { CaptureSpawnIfNeeded(); return spawnPosition; } }
        static readonly HashSet<MomentumResetBoundary> active = new HashSet<MomentumResetBoundary>();
        DirectionTarget trackedCube;
        Vector2 spawnPosition;
        bool hasEnteredBoundary;
        LineRenderer outline;
        Material generatedMaterial;
        readonly Vector3[] corners = new Vector3[4];

        public static bool Controls(DirectionTarget target)
        {
            if (!target) return false;
            foreach (var boundary in active)
                if (boundary && boundary.isActiveAndEnabled && boundary.cube == target) return true;
            return false;
        }
        void OnEnable()
        {
            active.Add(this);
            CaptureSpawnIfNeeded();
            hasEnteredBoundary = cube && !cube.isPlayer && Contains(cube.Body.position);
        }
        void OnDisable() { active.Remove(this); if (outline) outline.enabled = false; }
        void CaptureSpawnIfNeeded()
        {
            if (cube == trackedCube) return;
            trackedCube = cube;
            if (cube) spawnPosition = cube.Body.position;
            hasEnteredBoundary = cube && !cube.isPlayer && Contains(cube.Body.position);
        }
        public bool Contains(Vector2 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
            return Mathf.Abs(local.x) <= Mathf.Max(.1f, size.x) * .5f &&
                Mathf.Abs(local.y) <= Mathf.Max(.1f, size.y) * .5f;
        }
        void Update() { CheckAndReturn(); }
        public bool CheckAndReturn()
        {
            CaptureSpawnIfNeeded();
            if (!cube || cube.isPlayer) return false;
            if (Contains(cube.Body.position))
            {
                hasEnteredBoundary = true;
                return false;
            }
            if (!hasEnteredBoundary || !cube.Body.simulated) return false;
            return ReturnCube();
        }
        public bool ReturnCube()
        {
            CaptureSpawnIfNeeded();
            if (!cube || cube.isPlayer || !cube.Body.simulated) return false;
            var body = cube.Body;
            Vector2 velocity = body.linearVelocity;
            float angularVelocity = body.angularVelocity;
            // A marker attached to the cube moves with it, so its current position cannot serve as a return destination.
            bool attachedToCube = returnPoint && returnPoint.IsChildOf(cube.transform);
            Vector2 destination = returnPoint && !attachedToCube ? (Vector2)returnPoint.position : spawnPosition;
            body.position = destination;
            cube.transform.position = new Vector3(destination.x, destination.y, cube.transform.position.z);
            Physics2D.SyncTransforms();
            body.linearVelocity = velocity;
            cube.ClampSpeed();
            body.angularVelocity = angularVelocity;
            body.WakeUp();
            // Returning outside the region starts a new wait for entry instead of teleporting every frame.
            hasEnteredBoundary = Contains(destination);
            return true;
        }
        void LateUpdate()
        {
            if (!showBoundary) { if (outline) outline.enabled = false; return; }
            if (!outline)
            {
                var go = new GameObject("Orange momentum boundary"); go.transform.SetParent(transform, false);
                outline = go.AddComponent<LineRenderer>();
                outline.useWorldSpace = false; outline.loop = true; outline.positionCount = 4;
                outline.sortingOrder = 2;
            }
            if (!outlineMaterial && !generatedMaterial) generatedMaterial = new Material(Shader.Find("Sprites/Default"));
            outline.sharedMaterial = outlineMaterial ? outlineMaterial : generatedMaterial;
            outline.enabled = true;
            outline.startColor = outline.endColor = boundaryColor;
            outline.startWidth = outline.endWidth = lineWidth;
            float x = Mathf.Max(.1f, size.x) * .5f, y = Mathf.Max(.1f, size.y) * .5f;
            corners[0] = new Vector3(-x, -y, -.05f); corners[1] = new Vector3(x, -y, -.05f);
            corners[2] = new Vector3(x, y, -.05f); corners[3] = new Vector3(-x, y, -.05f);
            outline.SetPositions(corners);
        }
        void OnDrawGizmos()
        {
            var previous = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = boundaryColor;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(Mathf.Max(.1f, size.x), Mathf.Max(.1f, size.y), 0));
            Gizmos.matrix = previous;
        }
        void OnDestroy()
        {
            active.Remove(this);
            if (generatedMaterial) Destroy(generatedMaterial);
        }
    }
}
