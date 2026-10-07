using UnityEngine;

namespace VectorWhitebox
{
    public class WhiteboxTurret : MonoBehaviour
    {
        public WhiteboxBullet bulletPrefab;
        public LineRenderer laser;
        public SpriteRenderer visual;
        public Transform barrel;
        public bool destroyed;
        [Tooltip("勾选后，玩家使用暂停菜单的脱离卡死会复活此炮塔；默认关闭。")]
        public bool reviveOnUnstuck;
        [InspectorName("摧毁特效预制体"), Tooltip("可选。摧毁时在炮塔位置生成一次，不跟随炮塔隐藏。")]
        public GameObject explosionPrefab;
        [InspectorName("特效保留时间（秒）"), Min(.1f)]
        public float explosionLifetime = 3f;
        public float range = 15f, lockSeconds = 1.3f, projectileSpeed = 9f;
        float lockTime, cooldown;
        Color initialColor;
        Quaternion initialBarrelRotation;
        bool initialBarrelActive, initialColliderEnabled;
        Collider2D shape;
        void Awake()
        {
            shape = GetComponent<Collider2D>();
            if (visual) initialColor = visual.color;
            if (barrel) { initialBarrelRotation = barrel.localRotation; initialBarrelActive = barrel.gameObject.activeSelf; }
            initialColliderEnabled = shape && shape.enabled;
        }
        public void ResetLock() { lockTime = 0; cooldown = .8f; }
        public void Revive()
        {
            destroyed = false;
            if (visual) visual.color = initialColor;
            if (barrel) { barrel.localRotation = initialBarrelRotation; barrel.gameObject.SetActive(initialBarrelActive); }
            if (shape) shape.enabled = initialColliderEnabled;
            if (laser) { laser.enabled = false; laser.startWidth = laser.endWidth = .018f; }
            ResetLock();
            gameObject.SetActive(true);
        }
        void Update()
        {
            var g = WhiteboxGame.Instance;
            if (!g || destroyed || g.Dead || g.Completed) { laser.enabled = false; return; }
            cooldown -= Time.deltaTime;
            Vector2 origin = transform.position;
            Vector2 delta = (Vector2)g.player.transform.position - origin;
            bool visible = delta.magnitude < range;
            if (visible)
            {
                foreach (var hit in Physics2D.RaycastAll(origin, delta.normalized, delta.magnitude))
                {
                    if (hit.collider.isTrigger || hit.collider.transform == transform || hit.collider.GetComponent<WhiteboxBullet>()) continue;
                    if (hit.collider.GetComponent<WhiteboxPlayer>()) break;
                    // Movable puzzle props do not hide the player from the turret.
                    if (hit.collider.GetComponent<DirectionTarget>()) continue;
                    visible = false; break;
                }
            }
            laser.enabled = visible;
            if (!visible) { lockTime = 0; return; }
            laser.SetPosition(0, origin); laser.SetPosition(1, g.player.transform.position);
            barrel.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            if (cooldown > 0) return;
            lockTime += Time.deltaTime;
            laser.startWidth = laser.endWidth = Mathf.Lerp(.018f, .065f, lockTime / lockSeconds);
            if (lockTime >= lockSeconds)
            {
                var bullet = Instantiate(bulletPrefab, origin + delta.normalized * .95f, Quaternion.identity);
                bullet.owner = this; bullet.GetComponent<Rigidbody2D>().linearVelocity = delta.normalized * projectileSpeed;
                Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), GetComponent<Collider2D>());
                lockTime = 0; cooldown = 1.3f;
            }
        }
        public void Hit()
        {
            if (destroyed) return;
            destroyed = true;
            if (laser) laser.enabled = false;
            if (barrel) barrel.gameObject.SetActive(false);
            var collider = GetComponent<Collider2D>();
            if (collider) collider.enabled = false;
            if (explosionPrefab)
            {
                var effect = Instantiate(explosionPrefab, transform.position, transform.rotation);
                Destroy(effect, Mathf.Max(.1f, explosionLifetime));
            }
            gameObject.SetActive(false);
        }
    }
}
