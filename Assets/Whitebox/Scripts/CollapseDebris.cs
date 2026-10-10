using System.Collections.Generic;
using UnityEngine;

namespace VectorWhitebox
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class CollapseDebris : MonoBehaviour
    {
        [Tooltip("顶板上的提示灯，坠落前从青色变为红色。")]
        public SpriteRenderer warningStrip;
        [Min(0), Tooltip("松脱前的震动幅度，单位为顶板局部坐标。")]
        public float shakeDistance = .035f;
        public bool Released { get; private set; }
        public bool Falling { get; private set; }

        Rigidbody2D body;
        BoxCollider2D shape;
        Vector3 initialLocalPosition;
        Quaternion initialLocalRotation;
        SpriteRenderer[] visuals;
        Color[] initialColors;
        bool captured;
        float fallSpeed, acceleration, speedLimit, lifetime, settledAge, releaseHeight, distanceLimit;
        readonly RaycastHit2D[] hits = new RaycastHit2D[64];
        readonly List<Collider2D> overlaps = new List<Collider2D>();

        void Awake() { CaptureInitialPose(); ResetDebris(); }

        public void CaptureInitialPose()
        {
            if (captured) return;
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            visuals = GetComponentsInChildren<SpriteRenderer>(true);
            initialColors = new Color[visuals.Length];
            for (int i = 0; i < visuals.Length; i++) initialColors[i] = visuals[i].color;
            // A kinematic body never receives DirectionTarget from ForcedGravityBoundary.
            // Trigger-only debris leaves existing floors, boxes and player movement intact.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            shape.isTrigger = true;
            captured = true;
        }

        public void AnimateWarning(float elapsed, int index)
        {
            if (Released) return;
            CaptureInitialPose();
            float phase = elapsed * 43f + index * 1.7f;
            transform.localPosition = initialLocalPosition + new Vector3(Mathf.Sin(phase) * shakeDistance,
                Mathf.Sin(phase * 1.43f) * shakeDistance * .35f, 0);
            if (warningStrip) warningStrip.color = new Color(1f, .23f, .13f, 1f);
        }

        public void Release(float downwardAcceleration, float maximumSpeed, float settledLifetime, float maximumDistance)
        {
            if (Released) return;
            CaptureInitialPose();
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            body.position = transform.position;
            body.rotation = transform.eulerAngles.z;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            shape.enabled = true;
            body.simulated = true;
            acceleration = downwardAcceleration;
            speedLimit = maximumSpeed;
            lifetime = settledLifetime;
            releaseHeight = body.position.y;
            distanceLimit = maximumDistance;
            fallSpeed = settledAge = 0;
            Released = Falling = true;
            if (warningStrip) warningStrip.color = new Color(1f, .23f, .13f, 1f);
            Physics2D.SyncTransforms();
        }

        void FixedUpdate()
        {
            if (!Falling || !body.simulated) return;
            float dt = Time.fixedDeltaTime;
            fallSpeed = Mathf.Min(speedLimit, fallSpeed + acceleration * dt);
            float travel = fallSpeed * dt;
            float allowedTravel = Mathf.Min(travel, Mathf.Max(0, distanceLimit - (releaseHeight - body.position.y)));
            var filter = new ContactFilter2D { useTriggers = false };
            int count = shape.Cast(Vector2.down, filter, hits, travel + .015f);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (!hit.collider || hit.collider.attachedRigidbody == body) continue;
                var otherBody = hit.collider.attachedRigidbody;
                // Only a solid static surface arrests the visual debris. Moving props do not.
                if (!hit.collider.GetComponentInParent<WhiteboxPlayer>() &&
                    (!otherBody || otherBody.bodyType == RigidbodyType2D.Static) && hit.normal.y > .35f &&
                    hit.collider.bounds.max.y <= shape.bounds.center.y)
                    allowedTravel = Mathf.Min(allowedTravel, Mathf.Max(0, hit.distance - .008f));
            }
            // Sweeping the collider also catches the player in fast falls without tunnelling.
            for (int i = 0; i < count; i++)
                if (hits[i].collider && hits[i].distance <= allowedTravel + .015f)
                    HitPlayer(hits[i].collider);
            shape.Overlap(new ContactFilter2D { useTriggers = false }, overlaps);
            foreach (var other in overlaps) HitPlayer(other);

            if (allowedTravel < travel)
            {
                // MovePosition leaves a kinematic velocity for this step; settle directly instead.
                body.position += Vector2.down * allowedTravel;
                Falling = false;
                body.linearVelocity = Vector2.zero;
                shape.enabled = false;
                settledAge = 0;
            }
            else body.MovePosition(body.position + Vector2.down * allowedTravel);
        }

        void OnTriggerEnter2D(Collider2D other) { if (Falling) HitPlayer(other); }
        void OnTriggerStay2D(Collider2D other) { if (Falling) HitPlayer(other); }

        void HitPlayer(Collider2D other)
        {
            if (other && other.GetComponentInParent<WhiteboxPlayer>() && WhiteboxGame.Instance)
                WhiteboxGame.Instance.Damage(100);
        }

        void Update()
        {
            if (!Released || Falling) return;
            settledAge += Time.deltaTime;
            if (settledAge <= lifetime) return;
            float alpha = 1f - Mathf.Clamp01((settledAge - lifetime) / .5f);
            for (int i = 0; i < visuals.Length; i++)
            {
                if (!visuals[i]) continue;
                Color color = visuals[i].color;
                color.a = initialColors[i].a * alpha;
                visuals[i].color = color;
            }
        }

        public void ResetDebris()
        {
            CaptureInitialPose();
            Released = Falling = false;
            fallSpeed = settledAge = 0;
            shape.enabled = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            body.position = transform.position;
            body.rotation = transform.eulerAngles.z;
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i]) visuals[i].color = initialColors[i];
        }
    }
}
