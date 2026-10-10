using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox
{
    /// <summary>Also dresses players authored before the shared player prefab existed.</summary>
    public static class RobotPlayerVisualBootstrap
    {
        public const string ResourcePath = "HaloRobotVisual";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var player in root.GetComponentsInChildren<WhiteboxPlayer>(true))
                    EnsureVisual(player);
        }

        public static void EnsureVisual(WhiteboxPlayer player)
        {
            if (!player || player.GetComponentInChildren<RobotPlayerVisual>(true)) return;
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (!prefab) return;
            var animation = prefab.GetComponent<RobotPlayerVisual>();
            if (!animation || !animation.spriteRenderer || animation.idleFrames == null
                || animation.idleFrames.Length == 0 || !animation.idleFrames[0]) return;
            var visual = Object.Instantiate(prefab, player.transform, false);
            visual.name = "RobotVisual";
            var legacy = player.GetComponent<SpriteRenderer>();
            if (legacy) legacy.enabled = false;
            var visor = player.transform.Find("Visor");
            if (visor) visor.gameObject.SetActive(false);
        }
    }
}
