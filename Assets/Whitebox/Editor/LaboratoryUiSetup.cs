using UnityEditor;
using UnityEngine;

namespace VectorWhitebox.Editor
{
    public static class LaboratoryUiSetup
    {
        [MenuItem("Whitebox/Art/Apply Flat Laboratory UI")]
        public static void Setup()
        {
            foreach(var path in new[]{"Assets/Whitebox/Prefabs/UI/MainMenu.prefab","Assets/Whitebox/Prefabs/UI/EndingMenu.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    LaboratoryFrontendStyle.Apply(root.GetComponent<WhiteboxFrontend>());
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
    }
}
