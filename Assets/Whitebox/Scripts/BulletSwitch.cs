using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(Collider2D))]
    public class BulletSwitch : MonoBehaviour
    {
        public TimedDoor door;
        public SpriteRenderer switchVisual;
        public float activeSeconds = 5f;
        public int ActivationCount { get; private set; }

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (!switchVisual) switchVisual = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (switchVisual)
                switchVisual.color = door && door.Active
                    ? new Color(.3f, .95f, .65f)
                    : new Color(1f, .52f, .22f);
        }

        public void Activate()
        {
            if (!door)
            {
                Debug.LogWarning("BulletSwitch needs a TimedDoor reference.", this);
                return;
            }
            ActivationCount++;
            door.OpenFor(activeSeconds);
            if (Application.isPlaying) LaboratoryAudio.Play(LaboratorySound.BulletSwitch, transform.position);
        }

        public void ResetSwitch()
        {
            if (door) door.ResetDoor();
            if (switchVisual) switchVisual.color = new Color(1f, .52f, .22f);
        }
    }
}
