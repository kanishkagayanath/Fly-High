using UnityEngine;
using UnityEngine.UI;

namespace FlyHigh.PhoneController
{
    /// <summary>Small vector illustrations; no textures, cameras or imported assets.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GuideIllustration : MaskableGraphic
    {
        private static readonly Color Cyan=new Color(.22f,.91f,.89f),Violet=new Color(.61f,.51f,1),Gold=new Color(1,.75f,.25f),Dark=new Color(.055f,.12f,.18f),White=new Color(.87f,.96f,1);
        private int page;
        private float nextFrame,unit;
        private Vector2 centre;
        public int Page {get=>page;set{page=value;SetVerticesDirty();}}
        private void Update()
        {
            if(Time.unscaledTime<nextFrame)return;nextFrame=Time.unscaledTime+.04f;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=GetPixelAdjustedRect();unit=Mathf.Min(r.width,r.height);centre=r.center;
            if(unit<=0)return;
            switch(page)
            {
                case 0:
                    Box(vh,-.43f,-.12f,.24f,.28f,Cyan);Box(vh,-.40f,-.09f,.21f,.25f,Dark);
                    Box(vh,-.18f,-.24f,-.03f,-.12f,Cyan);Box(vh,-.31f,-.28f,.1f,-.24f,White);
                    Road(vh,new Vector2(-.095f,.08f),.22f,.29f);Car(vh,new Vector2(-.095f,.02f),.10f);
                    Phone(vh,new Vector2(.31f,-.03f),0);break;
                case 1:
                    Circle(vh,new Vector2(0,-.21f),.045f,Gold);
                    Arc(vh,new Vector2(0,-.21f),.17f,35,145,.025f,Cyan);
                    Arc(vh,new Vector2(0,-.21f),.30f,35,145,.025f,Cyan);
                    Arc(vh,new Vector2(0,-.21f),.43f,35,145,.025f,Violet);
                    float glow=.5f+.5f*Mathf.Sin(Time.unscaledTime*2.5f);Circle(vh,new Vector2(-.3f,-.31f),.055f,new Color(.22f,.91f,.89f,.5f+.5f*glow));Circle(vh,new Vector2(.3f,-.31f),.055f,Violet);
                    for(int i=0;i<7;i++)Box(vh,-.21f+i*.06f,-.32f,-.18f+i*.06f,-.30f,White);break;
                case 2:
                    Phone(vh,Vector2.zero,Mathf.PI*.5f+Mathf.Sin(Time.unscaledTime*1.4f)*.23f);
                    Arrow(vh,new Vector2(-.38f,0),false,Cyan);Arrow(vh,new Vector2(.38f,0),true,Cyan);break;
                case 3:
                    Box(vh,-.42f,-.24f,-.17f,.04f,Violet);Box(vh,-.12f,-.24f,.13f,.04f,new Color(.9f,.35f,.23f));Box(vh,.18f,-.24f,.43f,.29f,Cyan);
                    Arrow(vh,new Vector2(-.295f,-.10f),false,Dark);Box(vh,-.06f,-.15f,.07f,-.05f,White);
                    for(int i=0;i<4;i++)Box(vh,.22f,.06f+i*.045f,.39f,.077f+i*.045f,Dark);
                    Line(vh,new Vector2(.305f,-.16f),new Vector2(.305f,-.04f),.025f,Dark);Triangle(vh,new Vector2(.25f,-.06f),new Vector2(.36f,-.06f),new Vector2(.305f,.005f),Dark);break;
                default:
                    Road(vh,new Vector2(0,-.10f),.63f,.50f);
                    for(int i=0;i<4;i++){Vector2 p=new Vector2(-.19f+i*.125f,-.21f+i*.075f);Circle(vh,p,.052f,Gold);Circle(vh,p,.034f,new Color(1,.87f,.42f));}
                    Box(vh,-.18f,.20f,.18f,.41f,Violet);
                    Line(vh,new Vector2(-.10f,.24f),new Vector2(-.10f,.36f),.018f,White);Line(vh,new Vector2(-.10f,.36f),new Vector2(0,.28f),.018f,White);
                    Line(vh,new Vector2(0,.28f),new Vector2(.10f,.36f),.018f,White);Line(vh,new Vector2(.10f,.36f),new Vector2(.10f,.24f),.018f,White);break;
            }
        }
        private void Road(VertexHelper vh,Vector2 p,float width,float height)
        {
            Box(vh,p.x-width/2,p.y-height/2,p.x+width/2,p.y+height/2,new Color(.12f,.19f,.24f));
            Line(vh,p+new Vector2(-width*.44f,-height*.5f),p+new Vector2(-width*.44f,height*.5f),.008f,White);
            Line(vh,p+new Vector2(width*.44f,-height*.5f),p+new Vector2(width*.44f,height*.5f),.008f,White);
            for(int i=0;i<4;i++)Box(vh,p.x-.006f,p.y-height*.42f+i*height*.24f,p.x+.006f,p.y-height*.31f+i*height*.24f,White);
        }
        private void Phone(VertexHelper vh,Vector2 p,float angle)
        {
            Vector2 a=p+Rotate(new Vector2(-.14f,-.27f),angle),b=p+Rotate(new Vector2(-.14f,.27f),angle),c=p+Rotate(new Vector2(.14f,.27f),angle),d=p+Rotate(new Vector2(.14f,-.27f),angle);
            Line(vh,a,b,.024f,Cyan);Line(vh,b,c,.024f,Cyan);Line(vh,c,d,.024f,Cyan);Line(vh,d,a,.024f,Cyan);
            Arc(vh,p,.082f,0,360,.014f,White);Line(vh,p,p+Rotate(new Vector2(0,.073f),angle),.013f,White);
            Circle(vh,p+Rotate(new Vector2(0,-.22f),angle),.014f,Violet);
        }
        private static Vector2 Rotate(Vector2 p,float a){float s=Mathf.Sin(a),c=Mathf.Cos(a);return new Vector2(p.x*c-p.y*s,p.x*s+p.y*c);}
        private void Car(VertexHelper vh,Vector2 p,float size)
        {
            Box(vh,p.x-size*.44f,p.y-size,p.x+size*.44f,p.y+size,Gold);
            Box(vh,p.x-size*.34f,p.y,p.x+size*.34f,p.y+size*.55f,Cyan);
        }
        private void Arrow(VertexHelper vh,Vector2 p,bool right,Color c)
        {float direction=right?1:-1;Line(vh,p-new Vector2(direction*.06f,0),p+new Vector2(direction*.06f,0),.02f,c);Triangle(vh,p+new Vector2(direction*.09f,0),p+new Vector2(direction*.025f,.06f),p+new Vector2(direction*.025f,-.06f),c);}
        private void Arc(VertexHelper vh,Vector2 p,float radius,float start,float end,float width,Color c)
        {
            int steps=Mathf.CeilToInt(Mathf.Abs(end-start)/12);Vector2 last=p+new Vector2(Mathf.Cos(start*Mathf.Deg2Rad),Mathf.Sin(start*Mathf.Deg2Rad))*radius;
            for(int i=1;i<=steps;i++){float angle=Mathf.Lerp(start,end,i/(float)steps)*Mathf.Deg2Rad;Vector2 next=p+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;Line(vh,last,next,width,c);last=next;}
        }
        private void Circle(VertexHelper vh,Vector2 p,float radius,Color c)
        {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(vh,p,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,c);}}
        private void Box(VertexHelper vh,float x0,float y0,float x1,float y1,Color c)
        {Quad(vh,new Vector2(x0,y0),new Vector2(x0,y1),new Vector2(x1,y1),new Vector2(x1,y0),c);}
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {Vector2 delta=b-a;if(delta.sqrMagnitude<.000001f)return;Vector2 n=new Vector2(-delta.y,delta.x).normalized*width*.5f;Quad(vh,a-n,a+n,b+n,b-n,c);}
        private void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {int first=vh.currentVertCount;Vert(vh,a,tint);Vert(vh,b,tint);Vert(vh,c,tint);Vert(vh,d,tint);vh.AddTriangle(first,first+1,first+2);vh.AddTriangle(first,first+2,first+3);}
        private void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {int first=vh.currentVertCount;Vert(vh,a,tint);Vert(vh,b,tint);Vert(vh,c,tint);vh.AddTriangle(first,first+1,first+2);}
        private void Vert(VertexHelper vh,Vector2 p,Color tint){vh.AddVert(centre+p*unit,tint,Vector2.zero);}
    }
}
