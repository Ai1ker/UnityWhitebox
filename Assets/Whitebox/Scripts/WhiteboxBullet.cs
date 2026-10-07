using UnityEngine;

namespace VectorWhitebox
{
    public class WhiteboxBullet : MonoBehaviour
    {
        public WhiteboxTurret owner;
        public bool reflected;
        public bool gravityAltered;
        float age;
        bool ownerCollisionRestored;
        public void MarkReflected()
        {
            reflected = true;
            RestoreOwnerCollision();
        }
        public void MarkGravityAltered()
        {
            gravityAltered = true;
            RestoreOwnerCollision();
        }
        void RestoreOwnerCollision()
        {
            var projectileCollider = GetComponent<Collider2D>();
            var turretCollider = owner ? owner.GetComponent<Collider2D>() : null;
            if (projectileCollider && projectileCollider.enabled && projectileCollider.gameObject.activeInHierarchy &&
                turretCollider && turretCollider.enabled && turretCollider.gameObject.activeInHierarchy)
                Physics2D.IgnoreCollision(projectileCollider, turretCollider, false);
            ownerCollisionRestored = true;
        }
        void Update()
        {
            age += Time.deltaTime;
            if (age > 12) Destroy(gameObject);
            // Ignore only the muzzle exit; manipulated projectiles must hit their owner.
            if (!ownerCollisionRestored && owner && (owner.destroyed || Vector2.Distance(transform.position, owner.transform.position) > 1.2f))
                RestoreOwnerCollision();
        }
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (TryActivateSwitch(collision.gameObject)) { Destroy(gameObject); return; }
            if (collision.gameObject.GetComponent<WhiteboxPlayer>() && WhiteboxGame.Instance) WhiteboxGame.Instance.Damage(25);
            var launchReceiver = collision.gameObject.GetComponent<BulletLaunchReceiver>();
            if (launchReceiver)
                launchReceiver.Launch(GetComponent<Rigidbody2D>().linearVelocity, collision.GetContact(0).normal);
            var turret = collision.gameObject.GetComponent<WhiteboxTurret>();
            if (turret && turret == owner && (reflected || gravityAltered)) turret.Hit();
            Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (TryActivateSwitch(other.gameObject)) Destroy(gameObject);
        }

        static bool TryActivateSwitch(GameObject hit)
        {
            var switchDoor = hit.GetComponent<BulletSwitchDoor>();
            if (switchDoor) { switchDoor.Activate(); return true; }
            var bulletSwitch = hit.GetComponent<BulletSwitch>();
            if (bulletSwitch) { bulletSwitch.Activate(); return true; }
            return false;
        }
    }
}
