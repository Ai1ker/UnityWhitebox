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
        [Tooltip("勾选后，脱离卡死会将此炮塔恢复到关卡初始位置和朝向，清零速度并复活；默认关闭。")]
        public bool reviveOnUnstuck;
        [InspectorName("摧毁特效预制体"), Tooltip("可选。摧毁时在炮塔位置生成一次，不跟随炮塔隐藏。")]
        public GameObject explosionPrefab;
        [InspectorName("特效保留时间（秒）"), Min(.1f)]
        public float explosionLifetime = 3f;
        public float range = 15f, lockSeconds = 1.3f, projectileSpeed = 9f;
        float lockTime, cooldown;
        bool trackingPlayer;
        public bool IsLocking => trackingPlayer && cooldown <= 0 && !destroyed;
        public float LockProgress => Mathf.Clamp01(lockTime / Mathf.Max(.01f, lockSeconds));
        Color initialColor;
        Vector3 initialPosition;
        Quaternion initialRotation;
        Rigidbody2D physicsBody;
        Quaternion initialBarrelRotation;
        bool initialBarrelActive, initialColliderEnabled;
        bool initialStateCaptured;
        Collider2D shape;
        void Awake() { CaptureInitialState(); }
        void CaptureInitialState()
        {
            if (initialStateCaptured) return;
            // Capture the scene instance once, before physics can move it.
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            physicsBody = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            if (visual) initialColor = visual.color;
            if (barrel) { initialBarrelRotation = barrel.localRotation; initialBarrelActive = barrel.gameObject.activeSelf; }
            initialColliderEnabled = shape && shape.enabled;
            initialStateCaptured = true;
        }
        public void ResetLock()
        {
            lockTime = 0; cooldown = .8f; trackingPlayer = false;
            LaboratoryAudio.StopLoops(this);
        }
        void OnDisable() { LaboratoryAudio.StopLoops(this); }
        void OnDestroy() { LaboratoryAudio.StopLoops(this); }
        public void Revive()
        {
            CaptureInitialState();
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (physicsBody)
            {
                physicsBody.position = initialPosition;
                physicsBody.rotation = initialRotation.eulerAngles.z;
                if (physicsBody.bodyType != RigidbodyType2D.Static)
                {
                    physicsBody.linearVelocity = Vector2.zero;
                    physicsBody.angularVelocity = 0;
                }
            }
            var target = GetComponent<DirectionTarget>();
            destroyed = false;
            if (visual) visual.color = initialColor;
            if (barrel) { barrel.localRotation = initialBarrelRotation; barrel.gameObject.SetActive(initialBarrelActive); }
            if (shape) shape.enabled = initialColliderEnabled;
            if (laser) { laser.enabled = false; laser.startWidth = laser.endWidth = .018f; }
            ResetLock();
            gameObject.SetActive(true);
            if (target) target.ResetGravity();
            Physics2D.SyncTransforms();
            if (target) target.RefreshForcedGravity();
        }
        void Update()
        {
            var g = WhiteboxGame.Instance;
            if (!g || destroyed || g.Dead || g.Completed)
            {
                trackingPlayer = false; laser.enabled = false;
                LaboratoryAudio.StopLoops(this);
                return;
            }
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
            trackingPlayer = visible;
            LaboratoryAudio.SetLoop(this, LaboratorySound.TurretAim, IsLocking && Time.timeScale > 0, origin);
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
                GetComponentInChildren<TurretArtVisual>()?.PlayFire();
                LaboratoryAudio.Play(LaboratorySound.TurretFire, bullet.transform.position);
                lockTime = 0; cooldown = 1.3f;
                LaboratoryAudio.StopLoops(this);
            }
        }
        public void Hit()
        {
            if (destroyed) return;
            destroyed = true;
            trackingPlayer = false;
            LaboratoryAudio.StopLoops(this);
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
