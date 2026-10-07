using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BulletLaunchReceiver : MonoBehaviour
    {
        [Min(0)] public float launchImpulse = 20f;

        public void Launch(Vector2 bulletVelocity, Vector2 contactNormal)
        {
            var body = GetComponent<Rigidbody2D>();
            if (!body.simulated || body.bodyType != RigidbodyType2D.Dynamic) return;
            Vector2 direction = bulletVelocity.sqrMagnitude > .01f ? bulletVelocity.normalized : -contactNormal;
            body.AddForce(direction * launchImpulse, ForceMode2D.Impulse);
            var target = GetComponent<DirectionTarget>();
            if (target) target.ClampSpeed();
        }
    }
}
