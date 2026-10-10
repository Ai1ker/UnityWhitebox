using UnityEngine;

namespace VectorWhitebox
{
    public static class LaboratoryUiTheme
    {
        public static readonly Color Ink = new Color(.035f, .055f, .067f, 1);
        public static readonly Color Panel = new Color(.065f, .09f, .105f, .92f);
        public static readonly Color PanelSoft = new Color(.065f, .09f, .105f, .48f);
        public static readonly Color Text = new Color(.9f, .94f, .95f, 1);
        public static readonly Color Muted = new Color(.54f, .64f, .67f, 1);
        public static readonly Color Accent = new Color(.28f, .78f, .8f, 1);
        public static readonly Color Line = new Color(.34f, .43f, .46f, .65f);
        public static readonly Color Danger = new Color(.95f, .3f, .25f, 1);
        public static readonly Color Background = new Color(.025f, .038f, .045f, 1);
        public static readonly Color Surface = new Color(.045f, .063f, .074f, 1);

        public static void Fill(Rect rect, Color color)
        {
            Color saved = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = saved;
        }

        public static void DrawRule(Rect rect, Color color) { Fill(rect, color); }

        public static void DrawLine(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            // Keep the caller's scaled GUI matrix intact, including inside scroll views.
            Vector2 delta = b - a;
            if (Mathf.Abs(delta.y) < .01f)
                Fill(new Rect(Mathf.Min(a.x,b.x), a.y-width*.5f, Mathf.Abs(delta.x), width),color);
            else if (Mathf.Abs(delta.x) < .01f)
                Fill(new Rect(a.x-width*.5f, Mathf.Min(a.y,b.y), width, Mathf.Abs(delta.y)),color);
            else
            {
                int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.y)));
                for(int i=0;i<=steps;i++)
                {
                    Vector2 point=Vector2.Lerp(a,b,i/(float)steps);
                    Fill(new Rect(point.x-width*.5f,point.y-width*.5f,width,width),color);
                }
            }
        }

        public static void DrawPanel(Rect rect, Color fill, Color border, float cut = 9)
        {
            cut = Mathf.Clamp(cut, 0, Mathf.Min(rect.width, rect.height) * .25f);
            Fill(new Rect(rect.x, rect.y + cut, rect.width, rect.height - cut * 2), fill);
            int steps = Mathf.CeilToInt(cut);
            for (int i = 0; i < steps; i++)
            {
                float inset = cut - i;
                Fill(new Rect(rect.x + inset, rect.y + i, rect.width - inset * 2, 1), fill);
                Fill(new Rect(rect.x + inset, rect.yMax - i - 1, rect.width - inset * 2, 1), fill);
            }
            DrawLine(new Vector2(rect.x + cut, rect.y), new Vector2(rect.xMax - cut, rect.y), border);
            DrawLine(new Vector2(rect.x + cut, rect.yMax), new Vector2(rect.xMax - cut, rect.yMax), border);
            DrawLine(new Vector2(rect.x, rect.y + cut), new Vector2(rect.x, rect.yMax - cut), border);
            DrawLine(new Vector2(rect.xMax, rect.y + cut), new Vector2(rect.xMax, rect.yMax - cut), border);
            DrawLine(new Vector2(rect.x, rect.y + cut), new Vector2(rect.x + cut, rect.y), border);
            DrawLine(new Vector2(rect.xMax - cut, rect.y), new Vector2(rect.xMax, rect.y + cut), border);
            DrawLine(new Vector2(rect.x, rect.yMax - cut), new Vector2(rect.x + cut, rect.yMax), border);
            DrawLine(new Vector2(rect.xMax - cut, rect.yMax), new Vector2(rect.xMax, rect.yMax - cut), border);
        }

        public static void DrawBar(Rect rect, float fraction, Color fill)
        {
            Fill(rect, new Color(.16f, .22f, .24f, .8f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), fill);
        }
    }
}
