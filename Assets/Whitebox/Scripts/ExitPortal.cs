using UnityEngine;

namespace VectorWhitebox
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class ExitPortal : MonoBehaviour
    {
        public string nextScene = "Level02_Blank";
        public bool requireTurretDefeated = true;
        public bool requireAllGatesOpen;
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
        void OnTriggerEnter2D(Collider2D other)
        {
            if (entered || !other.GetComponent<WhiteboxPlayer>()) return;
            var game = WhiteboxGame.Instance;
            if (requireTurretDefeated)
                foreach (var turret in FindObjectsByType<WhiteboxTurret>())
                    if (!turret.destroyed) { if (game) game.Message("先摧毁炮塔，再进入传送门"); return; }
            if (requireAllGatesOpen)
                foreach (var gate in FindObjectsByType<PressureGate>())
                    if (!gate.Open) { if (game) game.Message("先打开所有感应门，再进入传送门"); return; }
            entered = true;
            if (game) game.BeginExit();
            SceneFadeTransition.Travel(nextScene);
        }
    }
}
