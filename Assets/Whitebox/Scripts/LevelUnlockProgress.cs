using System;
using System.IO;
using UnityEngine;

namespace VectorWhitebox
{
    /// <summary>Sequential completion is independent of checkpoint saves and the tester override.</summary>
    public static class LevelUnlockProgress
    {
        public const string ProgressKey = "VectorWhitebox.Unlocks.v1";
        public const string TesterUnlockAllKey = "VectorWhitebox.TesterUnlockAll";

        public static int HighestCompletedLevel => Mathf.Max(0, PlayerPrefs.GetInt(ProgressKey, 0));

        public static bool TesterUnlockAll
        {
            get => PlayerPrefs.GetInt(TesterUnlockAllKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TesterUnlockAllKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool IsUnlocked(string scenePath)
        {
            int level = LevelNumber(scenePath);
            return level > 0 && (TesterUnlockAll || level <= (long)HighestCompletedLevel + 1);
        }

        /// <summary>Only completing the next normal level advances progress. Tester runs never do.</summary>
        public static bool MarkLevelCompleted(string currentScene)
        {
            if (TesterUnlockAll) return false;
            int level = LevelNumber(currentScene);
            if (level <= 0 || level != (long)HighestCompletedLevel + 1) return false;
            PlayerPrefs.SetInt(ProgressKey, level);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Starting over clears completion without changing the tester setting.</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
        }

        static int LevelNumber(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return 0;
            string name = Path.GetFileNameWithoutExtension(scenePath.Replace('\\', '/'));
            if (!name.StartsWith("Level", StringComparison.Ordinal)) return 0;
            string digits = name.Substring(5).Split('_')[0];
            return digits.Length >= 2 && int.TryParse(digits, out int level) && level > 0 ? level : 0;
        }
    }
}
