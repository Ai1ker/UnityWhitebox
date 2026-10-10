using UnityEngine;

namespace VectorWhitebox
{
    [DisallowMultipleComponent]
    public class SpriteFrameAnimation : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public Sprite[] frames;
        [Min(.1f)] public float framesPerSecond = 10f;
        public bool loop = true;
        public bool destroyWhenFinished;
        public int CurrentFrame { get; private set; }
        float elapsed;
        bool finished;

        void OnEnable()
        {
            elapsed = 0; finished = false; CurrentFrame = 0;
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            ShowFrame();
        }

        void Update()
        {
            if (finished || frames == null || frames.Length == 0) return;
            elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(elapsed * Mathf.Max(.1f, framesPerSecond));
            if (!loop && frame >= frames.Length)
            {
                finished = true;
                if (destroyWhenFinished) { Destroy(gameObject); return; }
                frame = frames.Length - 1;
            }
            CurrentFrame = loop ? frame % frames.Length : frame;
            ShowFrame();
        }

        void ShowFrame()
        {
            if (spriteRenderer && frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[CurrentFrame];
        }
    }
}
