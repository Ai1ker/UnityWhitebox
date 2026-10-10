using UnityEngine;

namespace VectorWhitebox
{
    public class PressureGate : MonoBehaviour
    {
        public DirectionTarget cube;
        public Collider2D doorCollider;
        public SpriteRenderer doorVisual, plateVisual;
        public LineRenderer wire;
        public bool Open { get; private set; }
        public bool EverOpened { get; private set; }
        public bool Latched { get; private set; }
        public float resetHorizontalDistance = 8f;
        public float resetBelowSpawn = 5f;
        public float resetAboveSpawn = 8f;
        public bool showResetBoundary = true;
        Vector2 cubeSpawn;
        LineRenderer resetBoundary;
        bool soundReady;
        void Awake()
        {
            if (!cube) return;
            cubeSpawn = cube.transform.position;
            if (showResetBoundary) CreateResetBoundary();
        }
        void CreateResetBoundary()
        {
            var outline = new GameObject("Cube reset boundary (cyan)");
            outline.transform.SetParent(transform, false);
            resetBoundary = outline.AddComponent<LineRenderer>();
            resetBoundary.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            resetBoundary.useWorldSpace = true;
            resetBoundary.loop = true;
            resetBoundary.positionCount = 4;
            resetBoundary.startWidth = resetBoundary.endWidth = .055f;
            resetBoundary.startColor = resetBoundary.endColor = new Color(.2f, .95f, 1f, .5f);
            resetBoundary.sortingOrder = 1;
            float left = cubeSpawn.x - resetHorizontalDistance, right = cubeSpawn.x + resetHorizontalDistance;
            float bottom = cubeSpawn.y - resetBelowSpawn, top = cubeSpawn.y + resetAboveSpawn;
            resetBoundary.SetPositions(new[] {
                new Vector3(left, bottom), new Vector3(right, bottom),
                new Vector3(right, top), new Vector3(left, top)
            });
            BoundaryOutlineGlow.Ensure(resetBoundary);
        }
        void OnDrawGizmosSelected()
        {
            if (!cube) return;
            Vector3 center = Application.isPlaying ? (Vector3)cubeSpawn : cube.transform.position;
            Gizmos.color = new Color(.2f, .95f, 1f, .75f);
            Gizmos.DrawWireCube(center + Vector3.up * (resetAboveSpawn - resetBelowSpawn) * .5f,
                new Vector3(resetHorizontalDistance * 2, resetAboveSpawn + resetBelowSpawn, 0));
            if (plateVisual)
            {
                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = plateVisual.transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, .8f, .25f, .9f);
                Gizmos.DrawWireCube(plateVisual.localBounds.center, plateVisual.localBounds.size);
                Gizmos.matrix = previous;
            }
        }
        void Update()
        {
            bool momentumBoundary = MomentumResetBoundary.Controls(cube);
            if (resetBoundary) resetBoundary.enabled = showResetBoundary && !momentumBoundary;
            if (!cube || Latched || momentumBoundary) return;
            Vector2 delta = (Vector2)cube.transform.position - cubeSpawn;
            if (Mathf.Abs(delta.x) > resetHorizontalDistance || delta.y < -resetBelowSpawn || delta.y > resetAboveSpawn)
                ResetPuzzle();
        }
        void FixedUpdate()
        {
            if (!cube || !doorCollider || !doorVisual || !plateVisual) return;
            bool announceChanges = soundReady;
            soundReady = true;
            // Contact activates either a floor or ceiling plate, regardless of skill use.
            if (!Latched)
            {
                GetPlateBox(out Vector2 center, out Vector2 size, out float angle, out Vector2 normal);
                foreach (var c in Physics2D.OverlapBoxAll(center, size + Vector2.one * .12f, angle))
                    if (c.GetComponent<DirectionTarget>() == cube) { Latch(center, size, normal, announceChanges); break; }
            }
            bool wasOpen = Open;
            Open = Latched; EverOpened |= Open;
            if (announceChanges && Open != wasOpen && Application.isPlaying)
                LaboratoryAudio.Play(Open ? LaboratorySound.DoorOpen : LaboratorySound.DoorClose, doorVisual.transform.position);
            // Never close the door collider through the player.
            var game = WhiteboxGame.Instance;
            bool occupied = game && game.player && game.player.shape.bounds.Intersects(doorVisual.bounds);
            doorCollider.enabled = !Open && !occupied;
            Color col = Open ? new Color(.3f, .95f, .65f) : new Color(1, .51f, .2f);
            plateVisual.color = col;
            if (wire) wire.startColor = wire.endColor = col;
            doorVisual.color = new Color(col.r, col.g, col.b, Open ? .15f : .95f);
        }
        void GetPlateBox(out Vector2 center, out Vector2 size, out float angle, out Vector2 normal)
        {
            Bounds local = plateVisual.localBounds;
            Transform plate = plateVisual.transform;
            Vector3 right = plate.TransformVector(Vector3.right);
            Vector3 up = plate.TransformVector(Vector3.up);
            center = plate.TransformPoint(local.center);
            size = new Vector2(local.size.x * right.magnitude, local.size.y * up.magnitude);
            angle = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;
            normal = (Vector2)up.normalized;
        }
        void Latch(Vector2 plateCenter, Vector2 plateSize, Vector2 normal, bool announceChanges)
        {
            var body = cube.Body;
            Vector2 cubeExtents = cube.GetComponent<Collider2D>().bounds.extents;
            float projection = Vector2.Dot((Vector2)cube.transform.position - plateCenter, normal);
            float side = Mathf.Abs(projection) > .001f ? Mathf.Sign(projection) :
                (Vector2.Dot(-cube.gravityDirection, normal) >= 0 ? 1 : -1);
            float cubeRadius = Mathf.Abs(normal.x) * cubeExtents.x + Mathf.Abs(normal.y) * cubeExtents.y;
            Vector2 socket = plateCenter + normal * side * (plateSize.y * .5f + cubeRadius);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            body.simulated = false;
            cube.transform.position = new Vector3(socket.x, socket.y, cube.transform.position.z);
            Latched = true;
            if (announceChanges && Application.isPlaying)
                LaboratoryAudio.Play(LaboratorySound.PressurePlate, plateVisual.transform.position);
        }
        public void Unlatch()
        {
            Latched = false;
            Open = false;
            soundReady = false;
            if (cube) cube.Body.simulated = true;
        }
        public void ResetPuzzle()
        {
            if (!cube) return;
            Unlatch();
            cube.ResetGravity();
            cube.transform.position = cubeSpawn;
            cube.Body.position = cubeSpawn;
            cube.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
    }
}
