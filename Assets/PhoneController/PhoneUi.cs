using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Shared phone-only visual language; no runtime textures or preview cameras.</summary>
    internal static class PhoneUi
    {
        internal static readonly Color Ink=new Color(.025f,.045f,.075f), Surface=new Color(.06f,.10f,.16f),
            Edge=new Color(.15f,.25f,.33f), Cyan=new Color(.22f,.91f,.89f), Amber=new Color(1,.72f,.28f),
            Muted=new Color(.56f,.68f,.77f), White=new Color(.92f,.97f,1);
        internal static RectTransform Rect(Transform parent,string name,float x0,float y0,float x1,float y1)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        internal static PhonePanelGraphic Panel(Transform parent,string name,float x0,float y0,float x1,float y1,Color color,Color edge,bool raycast=false)
        {
            var graphic=Rect(parent,name,x0,y0,x1,y1).gameObject.AddComponent<PhonePanelGraphic>();
            graphic.color=color;graphic.Edge=edge;graphic.raycastTarget=raycast;return graphic;
        }
        internal static TMP_Text Text(Transform parent,string name,string value,float x0,float y0,float x1,float y1,int size,Color color,bool bold=false,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
        {
            var text=Rect(parent,name,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;
            text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=size;text.color=color;text.raycastTarget=false;
            text.alignment=align;text.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;return text;
        }
        internal static UnityEngine.UI.Button Button(Transform parent,string name,string label,float x0,float y0,float x1,float y1,UnityEngine.Events.UnityAction click,Color color,Color edge,Color? textColor=null)
        {
            var rect=Rect(parent,name,x0,y0,x1,y1);var graphic=Panel(rect,"Button surface",0,0,1,1,color,edge,true);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=graphic;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;
            colors.pressedColor=new Color(.7f,.8f,.86f);colors.disabledColor=new Color(.52f,.56f,.62f,.65f);colors.fadeDuration=.09f;button.colors=colors;
            var navigation=button.navigation;navigation.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=navigation;
            if(click!=null)button.onClick.AddListener(click);
            Text(graphic.transform,"Caption",label,.08f,.12f,.92f,.88f,24,textColor??White,true,TextAlignmentOptions.Center);
            rect.gameObject.AddComponent<MenuButtonMotion>().Initialize(graphic.rectTransform);return button;
        }
        internal static TMP_InputField Input(Transform parent,string name,string hint,float y,bool pin)
        {
            var graphic=Panel(parent,name,.07f,y,.93f,y+.14f,Ink,Edge,true);var rect=graphic.rectTransform;
            var field=rect.gameObject.AddComponent<TMP_InputField>();field.targetGraphic=graphic;
            var text=Text(rect,"Input text","",.06f,.08f,.94f,.92f,27,White);
            var placeholder=Text(rect,"Placeholder",hint,.06f,.08f,.94f,.92f,22,Muted);
            field.textViewport=rect;field.textComponent=text;field.placeholder=placeholder;field.lineType=TMP_InputField.LineType.SingleLine;
            field.contentType=pin?TMP_InputField.ContentType.IntegerNumber:TMP_InputField.ContentType.Standard;
            field.keyboardType=pin?TouchScreenKeyboardType.NumberPad:TouchScreenKeyboardType.NumbersAndPunctuation;
            field.characterLimit=pin?6:15;return field;
        }
    }
}
