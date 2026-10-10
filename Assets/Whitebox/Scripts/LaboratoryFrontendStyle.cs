using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox
{
    /// <summary>Flat geometry built from editable UI elements; no texture or external art dependency.</summary>
    public static class LaboratoryFrontendStyle
    {
        public static void Apply(WhiteboxFrontend frontend)
        {
            if (!frontend || !frontend.menuRoot) return;
            var scaler=frontend.GetComponent<CanvasScaler>();
            if(scaler)
            {
                scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1280,720);
                scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            }
            var background = frontend.transform.Find("Background · Replace Art");
            if(background)
            {
                var image=background.GetComponent<Image>();
                if(image){image.sprite=null;image.color=LaboratoryUiTheme.Background;image.raycastTarget=false;}
            }
            var panel=frontend.menuRoot.GetComponent<Image>();
            if(panel){panel.sprite=null;panel.color=LaboratoryUiTheme.Surface;panel.raycastTarget=false;}
            var frame=Geometry(frontend.menuRoot.transform,"LAB UI / Frame",LaboratoryUiGraphic.Shape.Frame);
            frame.color=new Color(.32f,.43f,.46f,.45f);frame.accent=LaboratoryUiTheme.Accent;frame.cornerCut=18;
            var accent=frontend.menuRoot.transform.Find("Accent · Replace Art");
            if(accent){var image=accent.GetComponent<Image>();if(image)image.color=new Color(.19f,.32f,.35f,.7f);}
            var art=frontend.menuRoot.transform.Find("Art Area · Empty Placeholder");
            if(art)
            {
                var image=art.GetComponent<Image>();if(image){image.sprite=null;image.color=new Color(.035f,.05f,.059f,1);image.raycastTarget=false;}
                var diagram=Geometry(art,"LAB UI / Direction diagram",LaboratoryUiGraphic.Shape.Diagram);
                diagram.color=new Color(.32f,.44f,.47f,.5f);diagram.accent=new Color(.24f,.6f,.64f,.85f);
                Caption(art,"LAB UI / Diagram caption",
                    frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu?"速度 · 重力 · 方向":"实验已完成",
                    frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu?"VELOCITY · GRAVITY · DIRECTION":"TESTS COMPLETE",
                    new Vector2(.5f,0),new Vector2(0,38),new Vector2(460,30),14,TextAnchor.MiddleCenter);
            }
            var content=frontend.titleText ? frontend.titleText.transform.parent : frontend.menuRoot.transform;
            Caption(content,"LAB UI / Section label",
                frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu?"矢量实验室":"实验记录",
                frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu?"VECTOR LABORATORY":"TEST RECORD",
                new Vector2(.5f,.5f),new Vector2(0,247),new Vector2(430,24),13,TextAnchor.MiddleLeft);
            if(frontend.titleText)
            {
                if(frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu) BrandText(frontend.titleText,"Rotcev");
                frontend.titleText.color=LaboratoryUiTheme.Text;frontend.titleText.fontStyle=FontStyle.Bold;
                frontend.titleText.fontSize=frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu?64:48;
                frontend.titleText.resizeTextForBestFit=true;frontend.titleText.resizeTextMinSize=28;
                frontend.titleText.resizeTextMaxSize=frontend.titleText.fontSize;
                frontend.titleText.horizontalOverflow=HorizontalWrapMode.Wrap;
                frontend.titleText.rectTransform.sizeDelta=new Vector2(430,80);
                frontend.titleText.rectTransform.anchoredPosition=new Vector2(0,194);
            }
            if(frontend.subtitleText)
            {
                if(frontend.mode==WhiteboxFrontend.ScreenMode.MainMenu) BrandText(frontend.subtitleText,"vector lab");
                frontend.subtitleText.color=new Color(.59f,.7f,.73f);frontend.subtitleText.fontSize=21;
                frontend.subtitleText.resizeTextForBestFit=true;frontend.subtitleText.resizeTextMinSize=15;frontend.subtitleText.resizeTextMaxSize=21;
                frontend.subtitleText.rectTransform.anchoredPosition=new Vector2(0,134);
            }
            if(frontend.statusText)
            {
                frontend.statusText.color=LaboratoryUiTheme.Muted;frontend.statusText.fontSize=15;
                frontend.statusText.resizeTextForBestFit=true;frontend.statusText.resizeTextMinSize=12;frontend.statusText.resizeTextMaxSize=15;
            }
            StyleButton(frontend.startButton,true);StyleButton(frontend.continueButton,false);
            StyleButton(frontend.settingsButton,false);StyleButton(frontend.quitButton,false);
            StyleButton(frontend.returnMainButton,true);
            var marks=Geometry(frontend.menuRoot.transform,"LAB UI / Registration marks",LaboratoryUiGraphic.Shape.EdgeMarks);
            var markRect=marks.rectTransform;markRect.anchorMin=markRect.anchorMax=new Vector2(0,0);markRect.pivot=Vector2.zero;
            markRect.anchoredPosition=new Vector2(24,20);markRect.sizeDelta=new Vector2(1072,16);
            marks.color=new Color(.32f,.44f,.47f,.45f);
        }

        static void StyleButton(Button button,bool primary)
        {
            if(!button)return;
            var image=button.GetComponent<Image>();if(image){image.sprite=null;image.color=Color.white;button.targetGraphic=image;}
            var colors=button.colors;
            colors.normalColor=primary?new Color(.1f,.22f,.25f,1):new Color(.08f,.11f,.127f,1);
            colors.highlightedColor=primary?new Color(.14f,.3f,.33f,1):new Color(.13f,.2f,.22f,1);
            colors.pressedColor=new Color(.16f,.37f,.4f,1);
            colors.selectedColor=colors.highlightedColor;colors.disabledColor=new Color(.1f,.13f,.14f,.5f);colors.fadeDuration=.12f;
            button.colors=colors;
            var text=button.GetComponentInChildren<Text>(true);
            if(text)
            {
                text.color=primary?LaboratoryUiTheme.Text:new Color(.8f,.85f,.86f,1);
                text.alignment=TextAnchor.MiddleLeft;text.fontSize=21;
                text.resizeTextForBestFit=true;text.resizeTextMinSize=15;text.resizeTextMaxSize=21;
                text.rectTransform.offsetMin=new Vector2(20,0);text.rectTransform.offsetMax=new Vector2(-28,0);
            }
            var frame=Geometry(button.transform,"LAB UI / Button edge",LaboratoryUiGraphic.Shape.Frame);
            frame.color=primary?new Color(.4f,.6f,.63f,.6f):new Color(.35f,.47f,.5f,.35f);
            frame.accent=primary?LaboratoryUiTheme.Accent:new Color(.2f,.43f,.46f,.6f);frame.cornerCut=7;frame.lineWidth=1;
            var layout=button.GetComponent<LayoutElement>();if(layout)layout.preferredHeight=50;
        }

        static LaboratoryUiGraphic Geometry(Transform parent,string name,LaboratoryUiGraphic.Shape shape)
        {
            var existing=FindDecoration(parent,name);
            var go=existing?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(LaboratoryUiGraphic));
            if(!existing)go.transform.SetParent(parent,false);
            var graphic=go.GetComponent<LaboratoryUiGraphic>();graphic.shape=shape;graphic.raycastTarget=false;
            var rect=graphic.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            graphic.SetAllDirty();return graphic;
        }

        static void Caption(Transform parent,string name,string chinese,string english,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,TextAnchor alignment)
        {
            var existing=FindDecoration(parent,name);
            var go=existing?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            if(!existing)go.transform.SetParent(parent,false);
            var text=go.GetComponent<Text>();text.raycastTarget=false;text.fontSize=fontSize;text.alignment=alignment;
            text.color=new Color(.4f,.56f,.59f,1);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.resizeTextForBestFit=true;text.resizeTextMinSize=11;text.resizeTextMaxSize=fontSize;
            var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;rect.anchoredPosition=position;
            var localized=go.GetComponent<LocalizedUiText>();
            if(!localized){localized=go.AddComponent<LocalizedUiText>();localized.target=text;localized.chinese=chinese;localized.english=english;}
            text.text=WhiteboxLocalization.Text(localized.chinese,localized.english);
            localized.Refresh();
        }

        static void BrandText(Text text,string brand)
        {
            var localized=text.GetComponent<LocalizedUiText>();
            if(!localized)localized=text.gameObject.AddComponent<LocalizedUiText>();
            localized.target=text;localized.chinese=brand;localized.english=brand;
            text.text=brand;localized.Refresh();
        }

        static Transform FindDecoration(Transform parent,string name)
        {
            // Decoration names contain slashes, so match direct children instead of a path.
            Transform found=null;
            foreach(Transform child in parent)
                if(child.name==name){found=child;break;}
            for(int i=parent.childCount-1;i>=0;i--)
            {
                Transform child=parent.GetChild(i);
                if(child!=found&&child.name==name)
                {
                    if(Application.isPlaying)Object.Destroy(child.gameObject);
                    else Object.DestroyImmediate(child.gameObject);
                }
            }
            return found;
        }
    }
}
