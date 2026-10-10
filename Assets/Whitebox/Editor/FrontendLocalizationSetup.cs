using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox
{
    /// <summary>Adds bilingual label bindings to existing UI prefabs without rebuilding layouts or scenes.</summary>
    public static class FrontendLocalizationSetup
    {
        [MenuItem("Whitebox/Localization/Configure Existing Menu Prefabs")]
        public static void Setup()
        {
            Configure("Assets/Whitebox/Prefabs/UI/MainMenu.prefab");
            Configure("Assets/Whitebox/Prefabs/UI/EndingMenu.prefab");
        }

        static void Configure(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var frontend in root.GetComponentsInChildren<WhiteboxFrontend>(true))
                    frontend.ConfigureLocalization();
                // Keep extra designer labels editable too. Unknown content deliberately has no invented translation.
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                {
                    if (text.GetComponent<LocalizedUiText>() || string.IsNullOrEmpty(text.text)) continue;
                    bool dynamicStatus = false;
                    foreach (var frontend in root.GetComponentsInChildren<WhiteboxFrontend>(true))
                        if (frontend.statusText == text) dynamicStatus = true;
                    if (dynamicStatus) continue;
                    var localized = text.gameObject.AddComponent<LocalizedUiText>();
                    localized.target = text;
                    localized.chinese = text.text;
                    localized.english = text.text;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
