using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class PressurePlatformSwitch : MonoBehaviour
    {
        [InspectorName("松开后持续时间（秒）"), Min(.1f)]
        [Tooltip("玩家或箱子持续压住时，平台一直显示。全部离开后，平台再保持这段时间，默认 2 秒；暂停菜单会暂停计时。")]
        public float activeSeconds = 2f;
        [Header("关联单向平台（最多 3 个，可留空）")]
        [InspectorName("平台 1"), Tooltip("拖入场景中的 OneWayPlatform 根对象；可移动或替换预制体自带的平台。")]
        public GameObject platform1;
        [InspectorName("平台 2")] public GameObject platform2;
        [InspectorName("平台 3")] public GameObject platform3;
        public SpriteRenderer plateVisual;
        public bool Pressed { get; private set; }
        public bool Active { get; private set; }
        float activeUntil = -1;
        bool soundReady;
        const float ContactPadding = .06f;

        struct Box
        {
            public Vector2 center, halfX, halfY;
        }

        void OnEnable() { ResetSwitch(); }
        void OnDisable() { ResetSwitch(); }
        void Start() { RefreshState(); }
        void Update() { RefreshState(); }
        public void RefreshState() { Tick(WhiteboxGame.GameplayTime); }
        void Tick(float now)
        {
            bool pressed = IsPressed();
            if (pressed && !Pressed && soundReady && Application.isPlaying)
                LaboratoryAudio.Play(LaboratorySound.PressurePlate, transform.position);
            if (pressed || Pressed) activeUntil = now + Mathf.Max(.1f, activeSeconds);
            Pressed = pressed;
            Active = pressed || now < activeUntil;
            soundReady = true;
            ApplyState();
        }

        bool IsPressed()
        {
            var plate = GetComponent<BoxCollider2D>();
            if (!plate.enabled || !gameObject.scene.IsValid()) return false;
            Box sensor = WorldBox(plate);
            sensor.halfX += sensor.halfX.normalized * ContactPadding;
            sensor.halfY += sensor.halfY.normalized * ContactPadding;
            // Geometric contact also sees boxes latched by PressureGate with their rigidbody simulation disabled.
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var target in root.GetComponentsInChildren<DirectionTarget>())
                {
                    if (!target.gameObject.activeInHierarchy || target.GetComponent<WhiteboxBullet>()) continue;
                    foreach (var shape in target.GetComponentsInChildren<Collider2D>())
                    {
                        if (!shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy) continue;
                        var box = shape as BoxCollider2D;
                        if (box)
                        {
                            if (Overlap(sensor, WorldBox(box))) return true;
                        }
                        else
                        {
                            Bounds bounds = shape.bounds;
                            if (bounds.size.sqrMagnitude > .0001f && Overlap(sensor, new Box {
                                center = bounds.center, halfX = Vector2.right * bounds.extents.x,
                                halfY = Vector2.up * bounds.extents.y })) return true;
                        }
                    }
                }
            return false;
        }

        static Box WorldBox(BoxCollider2D collider)
        {
            return new Box {
                center = collider.transform.TransformPoint(collider.offset),
                halfX = collider.transform.TransformVector(Vector2.right * collider.size.x * .5f),
                halfY = collider.transform.TransformVector(Vector2.up * collider.size.y * .5f)
            };
        }
        static bool Overlap(Box a, Box b)
        {
            return !Separated(a, b, a.halfX) && !Separated(a, b, a.halfY) &&
                !Separated(a, b, b.halfX) && !Separated(a, b, b.halfY);
        }
        static bool Separated(Box a, Box b, Vector2 edge)
        {
            Vector2 axis = new Vector2(-edge.y, edge.x);
            float radius = Mathf.Abs(Vector2.Dot(a.halfX, axis)) + Mathf.Abs(Vector2.Dot(a.halfY, axis)) +
                Mathf.Abs(Vector2.Dot(b.halfX, axis)) + Mathf.Abs(Vector2.Dot(b.halfY, axis));
            return Mathf.Abs(Vector2.Dot(b.center - a.center, axis)) > radius;
        }

        public void ResetSwitch()
        {
            Pressed = Active = false;
            activeUntil = -1;
            soundReady = false;
            // Unassigned built-in platforms stay hidden when a designer leaves a slot empty or replaces it.
            if (gameObject.scene.IsValid())
                foreach (var platform in GetComponentsInChildren<PlatformEffector2D>(true))
                    if (platform.GetComponentInParent<PressurePlatformSwitch>(true) == this)
                        platform.gameObject.SetActive(false);
            ApplyState();
        }
        void ApplyState()
        {
            SetPlatform(platform1); SetPlatform(platform2); SetPlatform(platform3);
            if (plateVisual) plateVisual.color = Active ? new Color(.3f, .95f, .65f) : new Color(.95f, .63f, .22f);
        }
        void SetPlatform(GameObject platform)
        {
            if (!platform || platform.scene != gameObject.scene || !platform.scene.IsValid() ||
                transform.IsChildOf(platform.transform) || !platform.GetComponent<PlatformEffector2D>()) return;
            if (platform.activeSelf != Active) platform.SetActive(Active);
        }
    }
}
