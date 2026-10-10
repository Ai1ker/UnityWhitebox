using UnityEngine;

namespace VectorWhitebox
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class TurretArtVisual : MonoBehaviour
    {
        public enum TurretStyle { Automatic, PhysicsStanding, FixedMount }
        [Tooltip("自动模式：根物体有 Rigidbody2D 用立式外观，否则使用固定底座。")]
        public TurretStyle style;
        public WhiteboxTurret owner;
        public SpriteRenderer bodyRenderer, gunRenderer;
        public Sprite[] physicsIdle, physicsLock, physicsFire;
        public Sprite[] fixedIdle, fixedLock, fixedFire;
        public Sprite[] gunFrames;
        [Min(.01f)] public float physicsBodyHeight = 1f, fixedBodyHeight = 1f;
        [Min(.01f)] public float gunReferenceLength = 1f;
        [Min(.01f)] public float gunWorldLength = .9f;
        [Min(.1f)] public float idleFramesPerSecond = 5f, fireFramesPerSecond = 18f;
        public TurretStyle AppliedStyle { get; private set; }
        public string CurrentAnimation { get; private set; }
        float idleTime, fireTime = -1;

        void OnEnable() { idleTime = 0; fireTime = -1; RefreshVisual(); }
        public void PlayFire() { fireTime = 0; }

        void LateUpdate()
        {
            if (Application.IsPlaying(gameObject))
            {
                idleTime += Time.deltaTime;
                if (fireTime >= 0) fireTime += Time.deltaTime;
            }
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (!owner) owner = GetComponentInParent<WhiteboxTurret>();
            if (!owner || !bodyRenderer || !gunRenderer) return;
            AppliedStyle = style == TurretStyle.Automatic
                ? (owner.GetComponent<Rigidbody2D>() ? TurretStyle.PhysicsStanding : TurretStyle.FixedMount) : style;
            bool physics = AppliedStyle == TurretStyle.PhysicsStanding;
            var idle = physics ? physicsIdle : fixedIdle;
            var locking = physics ? physicsLock : fixedLock;
            var firing = physics ? physicsFire : fixedFire;
            if (idle == null || idle.Length == 0 || locking == null || locking.Length == 0 ||
                firing == null || firing.Length == 0 || gunFrames == null || gunFrames.Length < 4) return;

            // Compensate the whitebox root's nonuniform scale without changing its collider or rigidbody.
            var parentScale = transform.parent.lossyScale;
            var box = owner.GetComponent<BoxCollider2D>();
            float height = box ? box.size.y * Mathf.Abs(owner.transform.lossyScale.y) : 1f;
            float uniform = height / Mathf.Max(.01f, physics ? physicsBodyHeight : fixedBodyHeight);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(uniform / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                uniform / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f);

            int gunFrame = 0;
            if (Application.IsPlaying(gameObject) && fireTime >= 0 && fireTime * fireFramesPerSecond < firing.Length)
            {
                int frame = Mathf.FloorToInt(fireTime * fireFramesPerSecond);
                bodyRenderer.sprite = firing[frame]; gunFrame = frame < 2 ? 2 : 3;
                CurrentAnimation = "Fire";
            }
            else if (Application.IsPlaying(gameObject) && owner.IsLocking)
            {
                bodyRenderer.sprite = locking[Mathf.Min(locking.Length - 1, Mathf.FloorToInt(owner.LockProgress * locking.Length))];
                gunFrame = 1; CurrentAnimation = "Lock";
            }
            else
            {
                bodyRenderer.sprite = idle[Application.IsPlaying(gameObject) ? Mathf.FloorToInt(idleTime * idleFramesPerSecond) % idle.Length : 0];
                CurrentAnimation = "Idle";
            }
            bodyRenderer.enabled = gunRenderer.enabled = !owner.destroyed;
            gunRenderer.sprite = gunFrames[gunFrame];
            if (owner.visual && owner.visual != bodyRenderer) owner.visual.enabled = false;
            if (owner.barrel)
            {
                foreach (var legacy in owner.barrel.GetComponentsInChildren<SpriteRenderer>(true)) legacy.enabled = false;
                // The gun is a sibling of the body, outside the old scaled barrel hierarchy.
                Vector3 direction = gunRenderer.transform.parent.InverseTransformVector(owner.barrel.right);
                gunRenderer.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
            gunRenderer.transform.localPosition = Vector3.zero;
            var gunParentScale = gunRenderer.transform.parent.lossyScale;
            float gunScale = gunWorldLength / Mathf.Max(.01f, gunReferenceLength);
            gunRenderer.transform.localScale = new Vector3(gunScale / Mathf.Max(.001f, Mathf.Abs(gunParentScale.x)),
                gunScale / Mathf.Max(.001f, Mathf.Abs(gunParentScale.y)), 1f);
        }
    }
}
