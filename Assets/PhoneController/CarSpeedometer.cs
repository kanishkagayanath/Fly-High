using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Presentation-only speed dial using the player's horizontal Rigidbody velocity.</summary>
    public sealed class CarSpeedometer : MonoBehaviour
    {
        [SerializeField, Min(20)] private float maximumDialKph = 200;
        [SerializeField, Min(0.01f)] private float smoothingTime = 0.16f;
        private Rigidbody car;
        private RectTransform root, needle;
        private TMP_Text digits, direction;
        private float displayedKph, smoothingVelocity;
        private int lastNumber = -1;
        private bool reversing;
        private static readonly Color Mint = new Color(0.34f,1,0.81f);
        private static readonly Color Ink = new Color(0.025f,0.065f,0.09f);
        public float DisplayedKph => displayedKph;

        public void Initialize(Transform target, RectTransform hud, RectTransform minimapFrame = null)
        {
            if(root!=null || target==null || hud==null)return;
            car=target.GetComponent<Rigidbody>();
            if(car==null)return;
            root=Element(minimapFrame != null ? minimapFrame : hud,"Car speedometer",new Vector2(220,220));
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,0);
            root.anchoredPosition=new Vector2(-20,20);
            if (minimapFrame != null)
            {
                root.anchorMin = root.anchorMax = new Vector2(.5f, 0);
                root.pivot = new Vector2(.5f, 1);
                root.anchoredPosition = new Vector2(0, -24);
                root.localScale = Vector3.one * .85f;
            }
            var shadow=Disc(root,"Dial shadow",232,new Color(0,0,0,0.4f));
            shadow.rectTransform.anchoredPosition=new Vector2(2,-3);
            Disc(root,"Dial border",224,Ink);
            Disc(root,"Dial rim",218,new Color(0.36f,0.54f,0.56f));
            Disc(root,"Dial face",212,new Color(0.035f,0.09f,0.12f,0.96f));
            var scale=Element(root,"Speed scale",new Vector2(212,212)).gameObject.AddComponent<MinimapGraphic>();
            const int segments=90;
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.Lerp(-135,135,i/(float)segments), b=Mathf.Lerp(-135,135,(i+1)/(float)segments);
                Color tint=i>=segments*0.8f?new Color(0.95f,0.57f,0.29f):new Color(0.20f,0.37f,0.40f);
                scale.AddTriangle(Polar(a,94),Polar(b,94),Polar(b,91),tint);
                scale.AddTriangle(Polar(a,94),Polar(b,91),Polar(a,91),tint);
            }
            for(int i=0;i<=20;i++)
            {
                float angle=Mathf.Lerp(-135,135,i/20f);
                bool major=i%2==0;
                Vector2 a=Polar(angle,86), b=Polar(angle,major?76:81);
                Vector2 side=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),-Mathf.Sin(angle*Mathf.Deg2Rad))*(major?0.9f:0.55f);
                Color tint=i>=16?new Color(1,0.70f,0.42f):new Color(0.79f,0.90f,0.91f);
                scale.AddTriangle(a-side,a+side,b+side,tint);scale.AddTriangle(a-side,b+side,b-side,tint);
                if(i%4==0)
                    Label(root,"Scale "+i,Mathf.RoundToInt(maximumDialKph*i/20f).ToString(),Polar(angle,63),new Vector2(35,18),11,new Color(0.79f,0.90f,0.91f));
            }
            scale.Finish();
            needle=Element(root,"Speed needle",new Vector2(160,160));
            var hand=needle.gameObject.AddComponent<MinimapGraphic>();
            hand.AddTriangle(new Vector2(-2,-9),new Vector2(2,-9),new Vector2(0,73),Mint);hand.Finish();
            Disc(root,"Needle hub",15,Ink);Disc(root,"Needle centre",7,Mint);
            digits=Label(root,"Speed digits","0",new Vector2(0,-34),new Vector2(140,48),40,Color.white);
            digits.fontStyle=FontStyles.Bold;
            Label(root,"Speed unit","km/h",new Vector2(0,-62),new Vector2(80,18),11,new Color(0.63f,0.78f,0.80f));
            direction=Label(root,"Drive direction","D",new Vector2(0,-83),new Vector2(26,18),12,Mint);
            Refresh(0);
        }
        private void LateUpdate()
        {
            if(car==null || root==null || !root.gameObject.activeInHierarchy || Time.deltaTime<=0)return;
            Vector3 velocity=car.linearVelocity;
            float kph=new Vector2(velocity.x,velocity.z).magnitude*3.6f;
            if(kph<0.18f)kph=0;
            float forward=Vector3.Dot(velocity,car.rotation*Vector3.forward);
            if(Mathf.Abs(forward)>0.15f)
            {
                bool reverse=forward<0;
                if(reverse!=reversing){reversing=reverse;direction.SetText(reverse?"R":"D");}
            }
            displayedKph=Mathf.SmoothDamp(displayedKph,kph,ref smoothingVelocity,smoothingTime,Mathf.Infinity,Time.deltaTime);
            if(kph==0 && displayedKph<0.25f){displayedKph=0;smoothingVelocity=0;}
            Refresh(displayedKph);
        }
        private void Refresh(float kph)
        {
            float angle=Mathf.Lerp(-135,135,Mathf.Clamp01(kph/maximumDialKph));
            needle.localRotation=Quaternion.Euler(0,0,-angle);
            int number=Mathf.RoundToInt(kph);
            if(number!=lastNumber){digits.SetText("{0:0}",number);lastNumber=number;}
        }
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
        private static Vector2 Polar(float angle,float radius)
        {float radians=angle*Mathf.Deg2Rad;return new Vector2(Mathf.Sin(radians),Mathf.Cos(radians))*radius;}
        private static RectTransform Element(Transform parent,string name,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);rect.sizeDelta=size;return rect;
        }
        private static MinimapGraphic Disc(Transform parent,string name,float diameter,Color tint)
        {var graphic=Element(parent,name,new Vector2(diameter,diameter)).gameObject.AddComponent<MinimapGraphic>();graphic.SetCircle(tint);return graphic;}
        private static TMP_Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,float fontSize,Color tint)
        {
            var rect=Element(parent,name,size);rect.anchoredPosition=position;
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.text=value;label.fontSize=fontSize;label.color=tint;
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
    }
}
