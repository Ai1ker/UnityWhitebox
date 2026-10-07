using System.Collections;
using UnityEngine;

namespace VectorWhitebox
{
    [DefaultExecutionOrder(-100), RequireComponent(typeof(Rigidbody2D))]
    public class DirectionTarget : MonoBehaviour
    {
        public bool isPlayer;
        public bool gravityEditable = true;
        public Vector2 gravityDirection = Vector2.down;
        public float gravityAcceleration = 22f;
        [Min(0)] public float lateralDamping;
        [Min(.1f), Tooltip("箱子的最大总速度（单位/秒）。限制重力、子弹冲量及橙色边界累积的速度；不影响玩家和子弹。")]
        public float maxSpeed = 30f;
        public Rigidbody2D Body => GetComponent<Rigidbody2D>();
        public Vector2 VelocityBeforePhysics { get; private set; }
        Coroutine speedLimitRoutine;
        static readonly WaitForFixedUpdate afterPhysics = new WaitForFixedUpdate();
        Vector2 originalGravityDirection;
        float originalGravityAcceleration;
        float gravityRestoreAt = -1f;
        bool forcedGravity;
        float nativeGravityScale;
        public bool GravityLocked => ForcedGravityBoundary.TryGetDirection(this, out _);
        public Vector2 EffectiveGravityDirection => ForcedGravityBoundary.TryGetDirection(this, out var direction) ? direction :
            (isPlayer ? (Physics2D.gravity.sqrMagnitude > .001f ? Physics2D.gravity.normalized : Vector2.down) : gravityDirection.normalized);
        public float EffectiveGravityAcceleration => forcedGravity ?
            (GetComponent<WhiteboxBullet>() ? 22f : Mathf.Abs(nativeGravityScale) > .001f ?
                Physics2D.gravity.magnitude * Mathf.Abs(nativeGravityScale) : gravityAcceleration) : gravityAcceleration;
        public float GravitySecondsRemaining => gravityRestoreAt < 0 ? 0 : Mathf.Max(0, gravityRestoreAt - WhiteboxGame.GameplayTime);
        void Awake()
        {
            originalGravityDirection = gravityDirection;
            originalGravityAcceleration = gravityAcceleration;
        }
        void OnEnable() { speedLimitRoutine = StartCoroutine(LimitSpeedAfterPhysics()); }
        void OnDisable()
        {
            if (forcedGravity) { Body.gravityScale = nativeGravityScale; forcedGravity = false; }
            if (speedLimitRoutine != null) StopCoroutine(speedLimitRoutine);
            speedLimitRoutine = null;
        }
        IEnumerator LimitSpeedAfterPhysics()
        {
            while (true) { yield return afterPhysics; ClampSpeed(); }
        }
        public void ClampSpeed()
        {
            var body = Body;
            if (isPlayer || GetComponent<WhiteboxBullet>() || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic) return;
            body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, Mathf.Max(.1f, maxSpeed));
        }
        void Update()
        {
            RefreshForcedGravity();
            if (gravityRestoreAt >= 0 && WhiteboxGame.GameplayTime >= gravityRestoreAt) ResetGravity();
        }
        void FixedUpdate()
        {
            RefreshForcedGravity();
            ClampSpeed();
            VelocityBeforePhysics = Body.linearVelocity;
            if (!Body.simulated || (!forcedGravity && (isPlayer || !gravityEditable))) return;
            Vector2 axis = EffectiveGravityDirection;
            if (!isPlayer && lateralDamping > 0 && axis.sqrMagnitude > .001f)
            {
                Vector2 velocity = Body.linearVelocity;
                Vector2 alongGravity = axis * Vector2.Dot(velocity, axis);
                Body.linearVelocity = alongGravity + (velocity - alongGravity) * Mathf.Exp(-lateralDamping * Time.fixedDeltaTime);
            }
            Body.AddForce(axis * EffectiveGravityAcceleration * Body.mass);
        }
        public void RefreshForcedGravity()
        {
            bool locked = ForcedGravityBoundary.TryGetDirection(this, out _);
            if (locked && !forcedGravity)
            {
                nativeGravityScale = Body.gravityScale; Body.gravityScale = 0; forcedGravity = true;
                var bullet = GetComponent<WhiteboxBullet>();
                if (bullet) bullet.MarkGravityAltered();
            }
            else if (!locked && forcedGravity)
            { Body.gravityScale = nativeGravityScale; forcedGravity = false; }
        }
        public void ApplyTemporaryGravity(Vector2 direction, float duration)
        {
            gravityDirection = direction.normalized;
            if (GetComponent<WhiteboxBullet>()) gravityAcceleration = 22f;
            gravityRestoreAt = WhiteboxGame.GameplayTime + duration;
            Body.WakeUp();
        }
        public void ResetGravity()
        {
            gravityRestoreAt = -1f;
            gravityDirection = originalGravityDirection;
            gravityAcceleration = originalGravityAcceleration;
            if (Body.simulated) Body.WakeUp();
        }
        public bool Redirect(Vector2 direction)
        {
            float speed = Body.linearVelocity.magnitude;
            if (speed < .05f || direction.sqrMagnitude < .001f) return false;
            Body.linearVelocity = direction.normalized * speed;
            ClampSpeed();
            var player = GetComponent<WhiteboxPlayer>();
            if (player) player.ReleaseMomentum();
            var bullet = GetComponent<WhiteboxBullet>();
            if (bullet) bullet.MarkReflected();
            return true;
        }
    }
}
