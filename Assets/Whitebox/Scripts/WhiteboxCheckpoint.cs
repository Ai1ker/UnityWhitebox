using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class WhiteboxCheckpoint : MonoBehaviour
    {
        public Vector2 respawnOffset = new Vector2(1, 1.2f);
        public bool resetPuzzlesOnDeath = true;
        public SpriteRenderer halo;
        public SpriteRenderer beam;
        public SpriteRenderer marker;
        public bool Activated => activated;
        bool activated;

        void Update()
        {
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * (activated ? 6f : 2.5f));
            Color tint = activated ? new Color(.35f, 1f, .58f) : new Color(.3f, .82f, 1f);
            if (halo) halo.color = new Color(tint.r, tint.g, tint.b, (activated ? .27f : .13f) + pulse * .1f);
            if (beam) beam.color = new Color(tint.r, tint.g, tint.b, (activated ? .45f : .22f) + pulse * .12f);
            if (marker) marker.color = new Color(tint.r, tint.g, tint.b, activated ? .92f : .55f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            // Also catches a player who starts inside the checkpoint trigger.
            TryActivate(other);
        }

        void TryActivate(Collider2D other)
        {
            if (activated || !other.GetComponentInParent<WhiteboxPlayer>()) return;
            var game = WhiteboxGame.Instance;
            if (!game) return;
            activated = true;
            game.SetCheckpoint((Vector2)transform.position + respawnOffset, resetPuzzlesOnDeath);
            if (Application.isPlaying) LaboratoryAudio.Play(LaboratorySound.Checkpoint, transform.position);
        }
    }
}
