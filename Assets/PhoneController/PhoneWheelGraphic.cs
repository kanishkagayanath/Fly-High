using UnityEngine;
namespace FlyHigh.PhoneController
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PhoneWheelGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();float radius=Mathf.Min(r.width,r.height)*.47f;var centre=r.center;
            Ring(vh,centre,radius*.85f,radius,PhoneUi.Edge);Ring(vh,centre,radius*.88f,radius*.91f,PhoneUi.Cyan);
            for(int i=0;i<3;i++){float a=(i*120+30)*Mathf.Deg2Rad;Vector2 dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),side=new Vector2(-dir.y,dir.x)*radius*.07f;
                Quad(vh,centre+dir*radius*.20f-side,centre+dir*radius*.84f-side,centre+dir*radius*.84f+side,centre+dir*radius*.20f+side,PhoneUi.Edge);}
            Ring(vh,centre,0,radius*.28f,PhoneUi.Surface);Ring(vh,centre,radius*.25f,radius*.28f,PhoneUi.Cyan);
            Quad(vh,centre+new Vector2(-radius*.05f,radius*.83f),centre+new Vector2(radius*.05f,radius*.83f),centre+new Vector2(radius*.05f,radius),centre+new Vector2(-radius*.05f,radius),PhoneUi.Amber);
        }
        private static void Ring(UnityEngine.UI.VertexHelper vh,Vector2 centre,float inner,float outer,Color c)
        {for(int i=0;i<80;i++){float a=i*Mathf.PI*2/80,b=(i+1)*Mathf.PI*2/80;var x=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var y=new Vector2(Mathf.Cos(b),Mathf.Sin(b));Quad(vh,centre+x*inner,centre+x*outer,centre+y*outer,centre+y*inner,c);}}
        private static void Quad(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {int n=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddVert(d,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
