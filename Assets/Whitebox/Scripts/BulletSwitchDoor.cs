using UnityEngine;

namespace VectorWhitebox
{
    // Legacy combined prefab. New levels can use separate BulletSwitch and TimedDoor prefabs.
    [RequireComponent(typeof(Collider2D))]
    public class BulletSwitchDoor : MonoBehaviour
    {
        public Collider2D doorCollider;
        public SpriteRenderer doorVisual;
        public SpriteRenderer switchVisual;
        public float activeSeconds = 5f;
        public bool Active { get; private set; }
        public int ActivationCount { get; private set; }
        float activeUntil;
        readonly Color offColor = new Color(1f, .52f, .22f);
        readonly Color onColor = new Color(.3f, .95f, .65f);

        void Awake()
        {
            // The switch detects bullets without blocking the player or movable props.
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Update()
        {
            if (Active && WhiteboxGame.GameplayTime >= activeUntil)
            {
                Active = false;
                if (Application.isPlaying)
                    LaboratoryAudio.Play(LaboratorySound.DoorClose, doorVisual ? doorVisual.transform.position : transform.position);
            }
            ApplyDoorState();
        }

        public void Activate()
        {
            bool wasActive = Active;
            Active = true;
            ActivationCount++;
            activeUntil = WhiteboxGame.GameplayTime + Mathf.Max(.1f, activeSeconds);
            ApplyDoorState();
            if (Application.isPlaying)
            {
                LaboratoryAudio.Play(LaboratorySound.BulletSwitch, switchVisual ? switchVisual.transform.position : transform.position);
                if (!wasActive)
                    LaboratoryAudio.Play(LaboratorySound.DoorOpen, doorVisual ? doorVisual.transform.position : transform.position);
            }
        }

        void ApplyDoorState()
        {
            if (switchVisual) switchVisual.color = Active ? onColor : offColor;
            if (doorVisual)
            {
                Color c = Active ? onColor : offColor;
                doorVisual.color = new Color(c.r, c.g, c.b, Active ? .15f : .95f);
            }
            if (!doorCollider) return;
            var game = WhiteboxGame.Instance;
            bool occupied = game && game.player && doorVisual && game.player.shape.bounds.Intersects(doorVisual.bounds);
            doorCollider.enabled = !Active && !occupied;
        }

        public void ResetSwitch()
        {
            Active = false;
            activeUntil = 0;
            ApplyDoorState();
        }
    }
}
