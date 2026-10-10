using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorWhitebox
{
    public class WhiteboxPlayer : MonoBehaviour
    {
        public float runSpeed = 7.5f, jumpSpeed = 12.5f, acceleration = 85f;
        [Min(0), Tooltip("空中按左右键时的加速度；松开不刹车，同向输入不会压低已有高速惯性。")]
        public float airAcceleration = 10f;
        public Rigidbody2D body;
        public Collider2D shape;
        public bool Grounded { get; private set; }
        public bool PreservingMomentum { get; private set; }
        [Min(0), Tooltip("头顶箱子的向下重力载荷倍率；0 关闭额外压力。")]
        public float cubePressureMultiplier = 1f;
        [Min(0), Tooltip("箱子每增加 1 单位/秒的向下速度，额外增加的每单位质量下压力。")]
        public float cubeSpeedPressureMultiplier = 6f;
        [Min(0), Tooltip("所有箱子对玩家施加的额外下压力总上限；0 关闭额外压力。")]
        public float maxCubeDownwardForce = 150f;
        public float CubeDownwardForce { get; private set; }
        struct HeadImpact { public float speed, expiresAt; }
        readonly System.Collections.Generic.Dictionary<Rigidbody2D, HeadImpact> headImpacts = new System.Collections.Generic.Dictionary<Rigidbody2D, HeadImpact>();
        readonly System.Collections.Generic.List<Rigidbody2D> expiredImpacts = new System.Collections.Generic.List<Rigidbody2D>();
        bool wasGrounded;
        float move, coyote, jumpBuffer, dropTimer;
        bool cutJump;
        Vector2 lastFootstepPosition;
        float footstepDistance, nextFootstepTime;
        bool trackingFootsteps;
        Collider2D support;
        readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        readonly ContactPoint2D[] headContacts = new ContactPoint2D[32];
        readonly Rigidbody2D[] pressingBodies = new Rigidbody2D[32];
        readonly System.Collections.Generic.List<Collider2D> ignored = new System.Collections.Generic.List<Collider2D>();
        void LateUpdate()
        {
            if (!body) { ResetFootsteps(); return; }
            Vector2 position = body.position;
            var game = WhiteboxGame.Instance;
            if (!Grounded || !support || !shape || !shape.IsTouching(support) || !body.simulated || Time.timeScale <= 0 ||
                (game && game.InputLocked) || Mathf.Abs(move) < .1f || Mathf.Abs(body.linearVelocity.x) < .15f)
            { ResetFootsteps(); return; }
            if (!trackingFootsteps)
            { lastFootstepPosition = position; trackingFootsteps = true; return; }
            Vector2 displacement = position - lastFootstepPosition;
            lastFootstepPosition = position;
            // Teleports and abrupt resets are not walking distance.
            if (displacement.sqrMagnitude > 9f) { ResetFootsteps(); return; }
            footstepDistance += Mathf.Abs(displacement.x);
            if (footstepDistance < 1.65f || Time.time < nextFootstepTime) return;
            footstepDistance %= 1.65f;
            nextFootstepTime = Time.time + .16f;
            LaboratoryAudio.Play(LaboratorySound.Footstep, transform.position,
                Mathf.Clamp(Mathf.Abs(body.linearVelocity.x) / Mathf.Max(.1f, runSpeed), .4f, 1f), Random.Range(.95f, 1.05f));
        }
        void ResetFootsteps()
        { trackingFootsteps = false; footstepDistance = 0; nextFootstepTime = 0; }
        void OnDisable() { ResetFootsteps(); }
        void OnCollisionEnter2D(Collision2D collision) { RecordHeadImpact(collision); }
        void OnCollisionStay2D(Collision2D collision) { RecordHeadImpact(collision); }
        void RecordHeadImpact(Collision2D collision)
        {
            var other = collision.rigidbody;
            if (!other || other.bodyType != RigidbodyType2D.Dynamic || other.GetComponent<WhiteboxBullet>()) return;
            var target = other.GetComponent<DirectionTarget>();
            if (!target || target.isPlayer) return;
            float speed = Mathf.Max(0, -target.VelocityBeforePhysics.y, -other.linearVelocity.y);
            if (speed <= .01f) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (contact.normal.y > -.5f || contact.point.y <= shape.bounds.center.y) continue;
                HeadImpact previous;
                if (headImpacts.TryGetValue(other, out previous) && previous.expiresAt > Time.time)
                    speed = Mathf.Max(speed, previous.speed);
                // Preserve an impact briefly: the collision solver can stop the box immediately.
                headImpacts[other] = new HeadImpact { speed = speed, expiresAt = Time.time + .15f };
                break;
            }
        }
        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (WhiteboxGame.Instance && WhiteboxGame.Instance.InputLocked) { move = 0; jumpBuffer = 0; cutJump = false; return; }
            move = (WhiteboxControls.Held(WhiteboxAction.Right) ? 1 : 0) - (WhiteboxControls.Held(WhiteboxAction.Left) ? 1 : 0);
            if (WhiteboxControls.Pressed(WhiteboxAction.Jump) || WhiteboxControls.Pressed(WhiteboxAction.AlternateJump)) jumpBuffer = .13f;
            if ((WhiteboxControls.Released(WhiteboxAction.Jump) || WhiteboxControls.Released(WhiteboxAction.AlternateJump))
                && !WhiteboxControls.Held(WhiteboxAction.Jump) && !WhiteboxControls.Held(WhiteboxAction.AlternateJump)) cutJump = true;
            if (WhiteboxControls.Pressed(WhiteboxAction.Down) && support && support.GetComponent<PlatformEffector2D>()) StartCoroutine(DropThrough(support));
        }
        void FixedUpdate()
        {
            var gravityTarget = GetComponent<DirectionTarget>();
            Vector2 gravity = gravityTarget ? gravityTarget.EffectiveGravityDirection : Vector2.down;
            Vector2 up = -gravity;
            float dt = Time.fixedDeltaTime;
            dropTimer -= dt;
            Grounded = false; support = null;
            var filter = new ContactFilter2D(); filter.useTriggers = false;
            int count = shape.Cast(gravity, filter, hits, .09f);
            for (int i = 0; i < count; i++)
                if (Vector2.Dot(hits[i].normal, up) > .6f && !ignored.Contains(hits[i].collider) && Vector2.Dot(body.linearVelocity, gravity) >= -.5f)
                { Grounded = true; support = hits[i].collider; break; }
            // The ground cast also detects a nearby floor. End skill inertia only on actual contact.
            if (PreservingMomentum && Grounded && !shape.IsTouching(support)) { Grounded = false; support = null; }
            if (Grounded && support && shape.IsTouching(support)) PreservingMomentum = false;
            bool landed = Grounded && !wasGrounded;
            wasGrounded = Grounded;
            if (landed && WhiteboxGame.Instance && WhiteboxGame.Instance.skills)
                WhiteboxGame.Instance.skills.OnPlayerLanded();
            coyote = Grounded && dropTimer <= 0 ? .1f : coyote - dt;
            jumpBuffer -= dt;
            CubeDownwardForce = MeasureCubePressure();
            if (WhiteboxGame.Instance && WhiteboxGame.Instance.InputLocked) { ApplyCubePressure(); return; }
            var v = body.linearVelocity;
            if (Grounded && !PreservingMomentum && Mathf.Abs(gravity.y) > .5f)
                v.x = Mathf.MoveTowards(v.x, move * runSpeed, acceleration * dt);
            else if (move > 0 && v.x < runSpeed)
                v.x = Mathf.Min(runSpeed, v.x + airAcceleration * dt);
            else if (move < 0 && v.x > -runSpeed)
                v.x = Mathf.Max(-runSpeed, v.x - airAcceleration * dt);
            if (jumpBuffer > 0 && coyote > 0 && !PreservingMomentum)
            {
                v += up * (jumpSpeed - Vector2.Dot(v, up)); jumpBuffer = 0; coyote = 0; Grounded = false;
                LaboratoryAudio.Play(LaboratorySound.Jump, transform.position);
            }
            float risingSpeed = Vector2.Dot(v, up);
            if (cutJump && risingSpeed > 0 && !PreservingMomentum) v -= up * risingSpeed * .52f;
            cutJump = false;
            // Use Rigidbody2D gravity for the whole arc, including skill-converted vertical motion.
            body.linearVelocity = v;
            ApplyCubePressure();
        }
        float MeasureCubePressure()
        {
            expiredImpacts.Clear();
            foreach (var impact in headImpacts)
                if (!impact.Key || impact.Value.expiresAt <= Time.time) expiredImpacts.Add(impact.Key);
            foreach (var expired in expiredImpacts) headImpacts.Remove(expired);
            if (cubePressureMultiplier <= 0 || maxCubeDownwardForce <= 0) return 0;
            var filter = new ContactFilter2D(); filter.useTriggers = false;
            int count = shape.GetContacts(filter, headContacts), bodyCount = 0;
            float force = 0;
            for (int i = 0; i < count; i++)
            {
                var contact = headContacts[i];
                if (contact.normal.y > -.5f || contact.point.y <= shape.bounds.center.y) continue;
                var other = contact.collider == shape ? contact.otherCollider : contact.collider;
                var pressingBody = other ? other.attachedRigidbody : null;
                if (!pressingBody || pressingBody == body || pressingBody.bodyType != RigidbodyType2D.Dynamic) continue;
                var target = pressingBody.GetComponent<DirectionTarget>();
                if (!target || target.isPlayer || pressingBody.GetComponent<WhiteboxBullet>()) continue;
                bool counted = false;
                for (int j = 0; j < bodyCount; j++) if (pressingBodies[j] == pressingBody) { counted = true; break; }
                if (counted) continue;
                pressingBodies[bodyCount++] = pressingBody;
                float downAcceleration = -Physics2D.gravity.y * pressingBody.gravityScale;
                if (target.gravityEditable || target.GravityLocked) downAcceleration -= target.EffectiveGravityDirection.y * target.EffectiveGravityAcceleration;
                float downwardSpeed = Mathf.Max(0, -pressingBody.linearVelocity.y);
                HeadImpact impact;
                if (headImpacts.TryGetValue(pressingBody, out impact)) downwardSpeed = Mathf.Max(downwardSpeed, impact.speed);
                force += pressingBody.mass * (Mathf.Max(0, downAcceleration) + downwardSpeed * cubeSpeedPressureMultiplier);
            }
            return Mathf.Min(force * cubePressureMultiplier, maxCubeDownwardForce);
        }
        void ApplyCubePressure()
        {
            if (CubeDownwardForce > 0) body.AddForce(Vector2.down * CubeDownwardForce);
        }
        IEnumerator DropThrough(Collider2D platform)
        {
            if (ignored.Contains(platform)) yield break;
            ignored.Add(platform); Physics2D.IgnoreCollision(shape, platform, true);
            coyote = 0; jumpBuffer = 0; dropTimer = .3f;
            body.position += Vector2.down * .12f;
            body.linearVelocity = new Vector2(body.linearVelocity.x, -3f);
            yield return new WaitForSeconds(.32f);
            if (platform) Physics2D.IgnoreCollision(shape, platform, false);
            ignored.Remove(platform);
        }
        public void ReleaseMomentum() { PreservingMomentum = true; jumpBuffer = 0; coyote = 0; cutJump = false; }
        public void ResetMotion()
        {
            ResetFootsteps();
            StopAllCoroutines();
            foreach (var c in ignored) if (c) Physics2D.IgnoreCollision(shape, c, false);
            ignored.Clear(); move = coyote = jumpBuffer = 0; body.linearVelocity = Vector2.zero;
            PreservingMomentum = false;
            Grounded = wasGrounded = false; support = null;
            CubeDownwardForce = 0;
            headImpacts.Clear(); expiredImpacts.Clear();
        }
    }
}
