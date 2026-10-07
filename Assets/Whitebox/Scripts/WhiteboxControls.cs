using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorWhitebox
{
    public enum WhiteboxAction { Left, Right, Jump, AlternateJump, Down, SkillOne, SkillTwo }

    public static class WhiteboxControls
    {
        static readonly Key[] defaults = { Key.A, Key.D, Key.W, Key.Space, Key.S, Key.Digit1, Key.Digit2 };
        static readonly string[] names = { "向左移动", "向右移动", "跳跃", "备用跳跃", "向下 / 下穿", "技能 1", "技能 2" };
        static readonly Key[] bindings = new Key[defaults.Length];
        static bool loaded;
        const string PrefPrefix = "VectorWhitebox.Key.";

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            Array.Copy(defaults, bindings, defaults.Length);
            for (int i = 0; i < bindings.Length; i++)
            {
                var key = (Key)PlayerPrefs.GetInt(PrefPrefix + i, (int)defaults[i]);
                bool duplicate = false;
                for (int j = 0; j < bindings.Length; j++) if (j != i && bindings[j] == key) duplicate = true;
                if (Enum.IsDefined(typeof(Key), key) && key != Key.None && key != Key.Escape && !duplicate)
                    bindings[i] = key;
            }
        }

        public static string Name(WhiteboxAction action) => names[(int)action];
        public static Key Binding(WhiteboxAction action) { Load(); return bindings[(int)action]; }
        public static string Display(WhiteboxAction action) => Display(Binding(action));
        public static string Display(Key key)
        {
            if (key == Key.Space) return "空格";
            if (key >= Key.Digit1 && key <= Key.Digit9) return ((int)key - (int)Key.Digit1 + 1).ToString();
            if (key == Key.Digit0) return "0";
            return key.ToString();
        }
        public static bool Held(WhiteboxAction action) => Keyboard.current != null && Keyboard.current[Binding(action)].isPressed;
        public static bool Pressed(WhiteboxAction action) => Keyboard.current != null && Keyboard.current[Binding(action)].wasPressedThisFrame;
        public static bool Released(WhiteboxAction action) => Keyboard.current != null && Keyboard.current[Binding(action)].wasReleasedThisFrame;

        public static bool TryBind(WhiteboxAction action, Key key, out string error)
        {
            Load();
            if (!Enum.IsDefined(typeof(Key), key) || key == Key.None || key == Key.Escape)
            { error = "Esc 保留给暂停菜单，请选择其他键。"; return false; }
            for (int i = 0; i < bindings.Length; i++)
                if (i != (int)action && bindings[i] == key)
                { error = Display(key) + " 已用于「" + names[i] + "」。"; return false; }
            bindings[(int)action] = key;
            PlayerPrefs.SetInt(PrefPrefix + (int)action, (int)key);
            PlayerPrefs.Save();
            error = null;
            return true;
        }

        public static void ResetDefaults()
        {
            Load();
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = defaults[i];
                PlayerPrefs.DeleteKey(PrefPrefix + i);
            }
            PlayerPrefs.Save();
        }
    }
}
