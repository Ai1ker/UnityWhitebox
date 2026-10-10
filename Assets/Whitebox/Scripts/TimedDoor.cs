using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(Collider2D))]
    public class TimedDoor : MonoBehaviour
    {
        public Collider2D doorCollider;
        public SpriteRenderer doorVisual;
        public bool Active { get; private set; }
        float activeUntil;

        void Awake()
        {
            if (!doorCollider) doorCollider = GetComponent<Collider2D>();
            if (!doorVisual) doorVisual = GetComponent<SpriteRenderer>();
            ApplyState();
        }

        void Update()
        {
            if (Active && WhiteboxGame.GameplayTime >= activeUntil)
            {
                Active = false;
                if (Application.isPlaying) LaboratoryAudio.Play(LaboratorySound.DoorClose, transform.position);
            }
            ApplyState();
        }

        public void OpenFor(float seconds)
        {
            bool wasActive = Active;
            Active = true;
            activeUntil = WhiteboxGame.GameplayTime + Mathf.Max(.1f, seconds);
            ApplyState();
            if (!wasActive && Application.isPlaying) LaboratoryAudio.Play(LaboratorySound.DoorOpen, transform.position);
        }

        public void ResetDoor()
        {
            Active = false;
            activeUntil = 0;
            ApplyState();
        }

        void ApplyState()
        {
            if (doorVisual)
            {
                Color c = Active ? new Color(.3f, .95f, .65f) : new Color(1f, .52f, .22f);
                doorVisual.color = new Color(c.r, c.g, c.b, Active ? .15f : .95f);
            }
            if (!doorCollider) return;
            var game = WhiteboxGame.Instance;
            bool occupied = game && game.player && doorVisual &&
                game.player.shape.bounds.Intersects(doorVisual.bounds);
            doorCollider.enabled = !Active && !occupied;
        }
    }
}
