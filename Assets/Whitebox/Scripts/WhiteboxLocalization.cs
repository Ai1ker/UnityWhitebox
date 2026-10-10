using System;
using System.Globalization;
using UnityEngine;

namespace VectorWhitebox
{
    public enum WhiteboxLanguage { System, Chinese, English }

    /// <summary>Small offline language service shared by menus, hints and world screens.</summary>
    public static class WhiteboxLocalization
    {
        const string PreferenceKey = "Whitebox.Language";
        static bool initialized;
        static WhiteboxLanguage mode;

        public static event Action LanguageChanged;

        public static WhiteboxLanguage Mode
        {
            get { EnsureInitialized(); return mode; }
        }

        public static bool IsChinese
        {
            get
            {
                var selected = Mode;
                if (selected == WhiteboxLanguage.Chinese) return true;
                if (selected == WhiteboxLanguage.English) return false;
                var system = Application.systemLanguage;
                return system == SystemLanguage.Chinese
                    || system == SystemLanguage.ChineseSimplified
                    || system == SystemLanguage.ChineseTraditional;
            }
        }

        public static void SetMode(WhiteboxLanguage selected)
        {
            EnsureInitialized();
            if (!Enum.IsDefined(typeof(WhiteboxLanguage), selected)) selected = WhiteboxLanguage.System;
            if (mode == selected) return;
            mode = selected;
            PlayerPrefs.SetInt(PreferenceKey, (int)mode);
            PlayerPrefs.Save();
            var changed = LanguageChanged;
            if (changed != null) changed();
        }

        public static string Text(string chinese, string english)
        {
            var preferred = IsChinese ? chinese : english;
            var fallback = IsChinese ? english : chinese;
            return !string.IsNullOrWhiteSpace(preferred) ? preferred : (fallback ?? string.Empty);
        }

        public static string Format(string chinese, string english, params object[] args)
        {
            var template = Text(chinese, english);
            if (args == null || args.Length == 0) return template;
            try
            {
                return string.Format(CultureInfo.GetCultureInfo(IsChinese ? "zh-CN" : "en-US"), template, args);
            }
            catch (FormatException)
            {
                // A malformed designer-authored placeholder must not break the menu or screen.
                return template;
            }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            int saved = PlayerPrefs.GetInt(PreferenceKey, (int)WhiteboxLanguage.System);
            mode = saved >= (int)WhiteboxLanguage.System && saved <= (int)WhiteboxLanguage.English
                ? (WhiteboxLanguage)saved : WhiteboxLanguage.System;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            initialized = false;
            LanguageChanged = null;
        }
    }
}
