using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox
{
    /// <summary>The last reached checkpoint, kept separately from input and window settings.</summary>
    public static class WhiteboxSaveGame
    {
        public const string SaveKey = "VectorWhitebox.Progress.v1";
        public const string FirstLevelScene = "Level01_Whitebox";
        public const string MainMenuScene = "MainMenu";
        public const string EndingScene = "GameEnd";

        [Serializable]
        public class SavedProgress
        {
            public int version = 1;
            public string sceneName;
            public float x, y;
            public bool resetPuzzles = true;
            public bool finished;
        }

        static SavedProgress pendingResume;
        public static bool HasSave => TryGetSavedProgress(out _);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { ClearResumeRequest(); }

        public static bool IsLevelScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return false;
            string name = Path.GetFileNameWithoutExtension(scenePath.Replace('\\', '/'));
            if (!name.StartsWith("Level", StringComparison.Ordinal)) return false;
            string digits = name.Substring(5).Split('_')[0];
            return digits.Length >= 2 && int.TryParse(digits, out int level) && level > 0;
        }

        static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        public static bool TryGetSavedProgress(out SavedProgress progress)
        {
            progress = null;
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            try { progress = JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(SaveKey)); }
            catch (Exception) { return false; }
            if (progress == null || progress.version != 1 || !IsLevelScene(progress.sceneName)
                || !IsFinite(progress.x) || !IsFinite(progress.y))
            { progress = null; return false; }
            return true;
        }

        public static void Save(string sceneName, Vector2 position, bool resetPuzzles)
        {
            if (!IsLevelScene(sceneName) || !IsFinite(position.x) || !IsFinite(position.y)) return;
            var progress = new SavedProgress
            {
                sceneName = Path.GetFileNameWithoutExtension(sceneName.Replace('\\', '/')),
                x = position.x, y = position.y, resetPuzzles = resetPuzzles
            };
            Write(progress);
        }

        static void Write(SavedProgress progress)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        public static void StartNewGame()
        {
            ClearResumeRequest();
            LevelUnlockProgress.ResetProgress();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        /// <summary>Only the Continue button arms a restore. Other level travel starts at the scene spawn.</summary>
        public static string RequestContinue()
        {
            ClearResumeRequest();
            if (!TryGetSavedProgress(out var progress) || !Application.CanStreamedLevelBeLoaded(progress.sceneName)
                || !LevelUnlockProgress.IsUnlocked(progress.sceneName)) return null;
            pendingResume = progress;
            return progress.sceneName;
        }

        public static bool TryConsumeResume(string currentScene, out Vector2 position, out bool resetPuzzles)
        {
            position = Vector2.zero;
            resetPuzzles = true;
            var request = pendingResume;
            pendingResume = null;
            if (request == null || request.sceneName != Path.GetFileNameWithoutExtension(currentScene)) return false;
            position = new Vector2(request.x, request.y);
            resetPuzzles = request.resetPuzzles;
            return true;
        }

        public static void ClearResumeRequest() { pendingResume = null; }

        public static void MarkFinished()
        {
            ClearResumeRequest();
            LevelUnlockProgress.MarkLevelCompleted(SceneManager.GetActiveScene().path);
            if (!TryGetSavedProgress(out var progress)) return;
            progress.finished = true;
            Write(progress);
        }
    }
}
