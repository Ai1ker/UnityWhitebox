using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorWhitebox
{
    public class DirectionSkills : MonoBehaviour
    {
        public float slowScale = .06f;
        public float cooldownSeconds = 1f;
        [InspectorName("技能 2 持续时间（秒）"), Min(.1f)]
        [Tooltip("技能 2 重力改向的持续时间，默认 5 秒。从成功释放开始计时，慢放不延长，打开暂停菜单时计时暂停。再次释放会重新计时。")]
        public float gravityDurationSeconds = 5f;
        public int Mode { get; private set; }
        public DirectionTarget Selected { get; private set; }
        public Vector2 Aim { get; private set; }
        public LineRenderer arrow;
        float normalFixed;
        Vector2 dragStart;
        bool dragging;
        float nextSkill1, nextSkill2;
        int selfRedirectsSinceGrounded;
        bool skill1CooldownFromSelf;
        const float SelfRedirectCooldownStep = .5f;
        void Awake() { normalFixed = Time.fixedDeltaTime; }
        public float CooldownRemaining(int mode)
        {
            return Mathf.Max(0, (mode == 1 ? nextSkill1 : nextSkill2) - WhiteboxGame.GameplayTime);
        }
        public bool Begin(int mode)
        {
            if (Mode != 0 || SceneFadeTransition.IsTransitioning || WhiteboxGame.Instance.Dead || WhiteboxGame.Instance.Completed ||
                (WhiteboxGame.Instance.pauseMenu && WhiteboxGame.Instance.pauseMenu.IsOpen)) return false;
            if (mode != 1 && mode != 2) return false;
            if (mode == 2 && PlayerGravityLocked()) { WhiteboxGame.Instance.Message("红色边界内无法使用技能 2"); return false; }
            if (CooldownRemaining(mode) > 0)
            {
                WhiteboxGame.Instance.Message("技能 " + mode + " 冷却中：" + CooldownRemaining(mode).ToString("0.0") + " 秒");
                return false;
            }
            if (WhiteboxGame.Instance.Energy < (mode == 1 ? 30 : 40)) { WhiteboxGame.Instance.Message("能量不足，等待回复"); return false; }
            Mode = mode; Selected = null; dragging = false;
            Time.timeScale = slowScale; Time.fixedDeltaTime = normalFixed * slowScale;
            return true;
        }
        public bool Select(DirectionTarget target)
        {
            if (Mode == 0 || !target || (Mode == 2 && (target.isPlayer || !target.gravityEditable))) return false;
            if (Mode == 2 && (target.GravityLocked || PlayerGravityLocked())) { WhiteboxGame.Instance.Message("红色边界内无法使用技能 2 改变重力"); return false; }
            Selected = target;
            Aim = Mode == 1 ? target.Body.linearVelocity.normalized : Cardinal(target.gravityDirection);
            return true;
        }
        public void SetAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > .001f) Aim = Mode == 2 ? Cardinal(direction) : direction.normalized;
        }
        static Vector2 Cardinal(Vector2 direction)
        {
            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? (direction.x >= 0 ? Vector2.right : Vector2.left)
                : (direction.y >= 0 ? Vector2.up : Vector2.down);
        }
        public bool Commit()
        {
            if (Mode == 0 || !Selected) { WhiteboxGame.Instance.Message("先用鼠标选择一个可操作目标"); return false; }
            float cost = Mode == 1 ? 30 : 40;
            if (WhiteboxGame.Instance.Energy < cost) return false;
            bool redirectingPlayer = Mode == 1 && Selected.GetComponent<WhiteboxPlayer>();
            if (Mode == 1)
            {
                if (!Selected.Redirect(Aim)) { WhiteboxGame.Instance.Message("目标当前没有速度：技能 1 只改变已有速度的方向"); return false; }
            }
            else
            {
                if (Selected.isPlayer || !Selected.gravityEditable || Aim.sqrMagnitude < .001f) return false;
                if (Selected.GravityLocked || PlayerGravityLocked()) { WhiteboxGame.Instance.Message("红色边界内无法使用技能 2 改变重力"); return false; }
                Selected.ApplyTemporaryGravity(Cardinal(Aim), gravityDurationSeconds);
                var bullet = Selected.GetComponent<WhiteboxBullet>();
                if (bullet) bullet.MarkGravityAltered();
            }
            WhiteboxGame.Instance.Energy -= cost;
            if (Mode == 1)
            {
                if (redirectingPlayer) selfRedirectsSinceGrounded++;
                skill1CooldownFromSelf = redirectingPlayer;
                nextSkill1 = WhiteboxGame.GameplayTime + cooldownSeconds +
                    (redirectingPlayer ? selfRedirectsSinceGrounded * SelfRedirectCooldownStep : 0);
            }
            else nextSkill2 = WhiteboxGame.GameplayTime + cooldownSeconds;
            Cancel(); return true;
        }
        public void OnPlayerLanded()
        {
            if (selfRedirectsSinceGrounded == 0) return;
            selfRedirectsSinceGrounded = 0;
            if (skill1CooldownFromSelf)
                nextSkill1 = Mathf.Min(nextSkill1, WhiteboxGame.GameplayTime + cooldownSeconds);
            skill1CooldownFromSelf = false;
        }
        bool PlayerGravityLocked()
        {
            var game = WhiteboxGame.Instance;
            var target = game && game.player ? game.player.GetComponent<DirectionTarget>() : null;
            return target && target.GravityLocked;
        }
        public void Cancel()
        {
            Mode = 0; Selected = null; dragging = false;
            Time.timeScale = 1; Time.fixedDeltaTime = normalFixed > 0 ? normalFixed : .02f;
            if (arrow) arrow.enabled = false;
        }
        void OnDisable() { Cancel(); }
        void Update()
        {
            var k = Keyboard.current; var m = Mouse.current;
            if (k == null || m == null) return;
            if (WhiteboxGame.Instance && WhiteboxGame.Instance.pauseMenu && WhiteboxGame.Instance.pauseMenu.IsOpen) return;
            if (Mode == 2 && PlayerGravityLocked())
            { Cancel(); WhiteboxGame.Instance.Message("进入红色边界，已取消技能 2 瞄准"); return; }
            if (m.rightButton.wasPressedThisFrame) Cancel();
            int pressed = WhiteboxControls.Pressed(WhiteboxAction.SkillOne) ? 1 : WhiteboxControls.Pressed(WhiteboxAction.SkillTwo) ? 2 : 0;
            if (pressed != 0) { if (Mode == 0) Begin(pressed); else if (Mode == pressed) Commit(); }
            if (Mode == 0) return;
            Vector2 screen = m.position.ReadValue();
            Vector2 world = Camera.main.ScreenToWorldPoint(screen);
            if (m.leftButton.wasPressedThisFrame)
            {
                DirectionTarget best = null; float distance = float.MaxValue;
                foreach (var c in Physics2D.OverlapCircleAll(world, .38f))
                {
                    var t = c.GetComponent<DirectionTarget>();
                    if (!t || (Mode == 2 && (t.isPlayer || !t.gravityEditable || t.GravityLocked))) continue;
                    float d = Vector2.Distance(world, t.transform.position);
                    if (d < distance) { best = t; distance = d; }
                }
                if (best) Select(best);
                dragging = Selected; dragStart = screen;
            }
            if (dragging && m.leftButton.isPressed && Vector2.Distance(dragStart, screen) > 6 && Selected)
                SetAim(world - (Vector2)Selected.transform.position);
            if (m.leftButton.wasReleasedThisFrame) dragging = false;
            DrawArrow();
        }
        void DrawArrow()
        {
            arrow.enabled = Selected && Aim.sqrMagnitude > .001f;
            if (!arrow.enabled) return;
            Vector3 start = Selected.transform.position; start.z = -2;
            Vector3 dir = Aim; Vector3 side = new Vector3(-dir.y, dir.x, 0);
            Vector3 end = start + dir * 1.8f;
            arrow.positionCount = 5;
            arrow.SetPositions(new[] { start, end, end - dir * .35f + side * .22f, end, end - dir * .35f - side * .22f });
        }
    }
}
