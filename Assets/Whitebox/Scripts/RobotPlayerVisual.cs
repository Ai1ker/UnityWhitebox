using UnityEngine;

namespace VectorWhitebox
{
    /// <summary>Sprite animation only; the player root keeps all movement and collision behaviour.</summary>
    [DisallowMultipleComponent]
    public sealed class RobotPlayerVisual : MonoBehaviour
    {
        public WhiteboxPlayer player;
        public DirectionSkills skills;
        public SpriteRenderer spriteRenderer;
        public Sprite[] idleFrames, runFrames, jumpFrames, fallFrames, skill1Frames, skill2Frames;
        public Sprite[] deathFrames;
        [Min(.1f)] public float idleFramesPerSecond = 5f;
        [Min(.1f)] public float runFramesPerSecond = 12f;
        [Min(.1f)] public float skillFramesPerSecond = 14f;
        [Min(.1f), Tooltip("死亡动画播放速度。复活会等待动画和最后一帧停留时间结束。")]
        public float deathFramesPerSecond = 12f;
        [Min(0)] public float deathHoldSeconds = .15f;
        [Min(.01f), Tooltip("待机图中实际身体高度，单位为世界单位；不包含图集透明留白。")]
        public float referenceBodyHeight = 1.26875f;
        [Min(.01f)] public float visualSize = 1f;
        public string CurrentAnimation { get; private set; } = "Idle";
        public float DeathDuration => deathFrames != null && deathFrames.Length > 0
            ? deathFrames.Length / Mathf.Max(.1f, deathFramesPerSecond) + Mathf.Max(0, deathHoldSeconds) : 0;
        float animationTime, releaseTime;
        float deathTime;
        bool dying;
        int releaseMode;
        Vector3 lastPosition;

        void Awake() { ResolveReferences(); FitToPlayer(); ResetVisual(); }
        void OnEnable() { ResolveReferences(); lastPosition = player ? player.transform.position : transform.position; }
        void ResolveReferences()
        {
            if (!player) player = GetComponentInParent<WhiteboxPlayer>();
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            var game = WhiteboxGame.Instance;
            if (game && game.player == player) skills = game.skills;
        }
        public void BindToPlayer(WhiteboxPlayer owner)
        {
            player = owner;
            ResolveReferences(); FitToPlayer(); ResetVisual();
        }
        public void FitToPlayer()
        {
            if (!player || !player.shape) return;
            if (!spriteRenderer || idleFrames == null || idleFrames.Length == 0 || !idleFrames[0]) return;
            var box = player.shape as BoxCollider2D;
            if (!box) return;
            var scale = player.transform.lossyScale;
            float height = box.size.y * Mathf.Abs(scale.y);
            float uniform = height / Mathf.Max(.01f, referenceBodyHeight) * visualSize;
            transform.localScale = new Vector3(uniform / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                uniform / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f);
            transform.localPosition = new Vector3(box.offset.x, box.offset.y - box.size.y * .5f, 0);
            transform.localRotation = Quaternion.identity;
            var legacy = player.GetComponent<SpriteRenderer>();
            if (legacy && legacy != spriteRenderer) legacy.enabled = false;
            var visor = player.transform.Find("Visor");
            if (visor) visor.gameObject.SetActive(false);
        }
        public void ResetVisual()
        {
            dying = false; deathTime = 0;
            releaseMode = 0; releaseTime = 0; animationTime = 0;
            CurrentAnimation = "Idle";
            Show(idleFrames, 0);
            if (player) lastPosition = player.transform.position;
        }
        public void PlaySkillRelease(int mode)
        {
            if (dying || (mode != 1 && mode != 2)) return;
            releaseMode = mode; releaseTime = 0;
            CurrentAnimation = "Skill" + mode;
            animationTime = 0;
            Show(mode == 1 ? skill1Frames : skill2Frames, 0);
        }
        public void PlayDeath()
        {
            if (dying) return;
            dying = true; deathTime = 0;
            releaseMode = 0; releaseTime = 0; animationTime = 0;
            CurrentAnimation = "Death";
            Show(deathFrames, 0);
        }
        void LateUpdate()
        {
            ResolveReferences();
            if (!player || !player.body || !spriteRenderer) return;
            var game = WhiteboxGame.Instance;
            if (Time.timeScale <= 0 || (game && game.pauseMenu && game.pauseMenu.IsOpen)) return;
            if (game && game.Dead)
            {
                PlayDeath();
                deathTime += Time.unscaledDeltaTime;
                if (deathFrames != null && deathFrames.Length > 0)
                    Show(deathFrames, Mathf.Min(deathFrames.Length - 1,
                        Mathf.FloorToInt(deathTime * Mathf.Max(.1f, deathFramesPerSecond))));
                return;
            }
            if (dying) ResetVisual();
            if (game && (game.Completed || SceneFadeTransition.IsTransitioning))
            { ResetVisual(); return; }
            float dt = Time.unscaledDeltaTime;
            var position = player.transform.position;
            float expectedTravel = player.body.linearVelocity.magnitude * dt;
            if ((position - lastPosition).magnitude > Mathf.Max(4f, expectedTravel * 3f)) ResetVisual();
            lastPosition = position;
            if (Mathf.Abs(player.body.linearVelocity.x) > .1f)
                spriteRenderer.flipX = player.body.linearVelocity.x < 0;
            int aiming = skills ? skills.Mode : 0;
            if (aiming != 0)
            {
                releaseMode = 0;
                ChangeState("Aim" + aiming);
                Show(aiming == 1 ? skill1Frames : skill2Frames, 0);
                return;
            }
            if (releaseMode != 0)
            {
                var frames = releaseMode == 1 ? skill1Frames : skill2Frames;
                releaseTime += dt;
                int frame = Mathf.FloorToInt(releaseTime * skillFramesPerSecond);
                if (frames != null && frame < frames.Length)
                { Show(frames, frame); return; }
                releaseMode = 0;
            }
            if (!player.Grounded)
            {
                var target = player.GetComponent<DirectionTarget>();
                Vector2 up = target ? -target.EffectiveGravityDirection : Vector2.up;
                bool rising = Vector2.Dot(player.body.linearVelocity, up) > .1f;
                ChangeState(rising ? "Jump" : "Fall");
                Show(rising ? jumpFrames : fallFrames, 0);
            }
            else if (Mathf.Abs(player.body.linearVelocity.x) > .15f)
            {
                ChangeState("Run"); animationTime += Time.deltaTime;
                ShowLoop(runFrames, animationTime, runFramesPerSecond);
            }
            else
            {
                ChangeState("Idle"); animationTime += Time.deltaTime;
                ShowLoop(idleFrames, animationTime, idleFramesPerSecond);
            }
        }
        void ChangeState(string state)
        {
            if (CurrentAnimation == state) return;
            CurrentAnimation = state; animationTime = 0;
        }
        void ShowLoop(Sprite[] frames, float elapsed, float fps)
        {
            if (frames != null && frames.Length > 0)
                Show(frames, Mathf.FloorToInt(elapsed * fps) % frames.Length);
        }
        void Show(Sprite[] frames, int index)
        {
            if (spriteRenderer && frames != null && index >= 0 && index < frames.Length && frames[index])
                spriteRenderer.sprite = frames[index];
        }
    }
}
