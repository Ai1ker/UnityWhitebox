using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class FinalExitPortal : MonoBehaviour
    {
        public bool requireTurretDefeated;
        public bool requireAllGatesOpen;
        [Tooltip("正常进入 GameEnd 场景；未配置该场景时使用此界面预制体作为备用。")]
        public WhiteboxFrontend endingMenuPrefab;
        public SpriteRenderer core, halo;
        public Transform innerRing, outerRing;
        bool entered;
        void Update()
        {
            float t = Time.unscaledTime;
            if (innerRing) innerRing.Rotate(0, 0, 38f * Time.unscaledDeltaTime);
            if (outerRing) outerRing.Rotate(0, 0, -23f * Time.unscaledDeltaTime);
            if (core) core.color = new Color(.53f, 1f, .83f, .48f + .18f * Mathf.Sin(t * 4f));
            if (halo) halo.color = new Color(.2f, .86f, 1f, .18f + .1f * Mathf.Sin(t * 2.7f));
        }
        void OnTriggerEnter2D(Collider2D other) { TryEnter(other); }
        public bool TryEnter(Collider2D other)
        {
            if (entered || !other || !other.GetComponentInParent<WhiteboxPlayer>() || SceneFadeTransition.IsTransitioning) return false;
            var game = WhiteboxGame.Instance;
            if (!game || game.Dead || game.Completed) return false;
            if (requireTurretDefeated)
                foreach (var turret in FindObjectsByType<WhiteboxTurret>())
                    if (!turret.destroyed) { game.Message("先摧毁炮塔，再进入终点", "Destroy the turrets before entering the final exit."); return false; }
            if (requireAllGatesOpen)
                foreach (var gate in FindObjectsByType<PressureGate>())
                    if (!gate.Open) { game.Message("先打开所有感应门，再进入终点", "Open all sensor doors before entering the final exit."); return false; }
            entered = true;
            game.BeginExit();
            WhiteboxFrontend.ShowEnding(endingMenuPrefab);
            if (Application.isPlaying && (SceneFadeTransition.IsTransitioning || WhiteboxFrontend.IsEndingOpen))
                LaboratoryAudio.Play(LaboratorySound.Portal, transform.position);
            return true;
        }
    }
}
