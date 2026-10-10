using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public class LocalizedUiText : MonoBehaviour
    {
        [TextArea(1, 8)] public string chinese;
        [TextArea(1, 8)] public string english;
        public Text target;

        void OnEnable()
        {
            if (!target) target = GetComponent<Text>();
            WhiteboxLocalization.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            WhiteboxLocalization.LanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            // Localizing runtime UI must not rewrite a designer's serialized scene text.
            if (!Application.isPlaying) return;
            if (!target) target = GetComponent<Text>();
            if (target) target.text = WhiteboxLocalization.Text(chinese, english);
        }
    }
}
