using UnityEngine;

namespace FlyHigh.PhoneController
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PhonePanelGraphic : UnityEngine.UI.MaskableGraphic
    {
        public Color Edge=PhoneUi.Edge;
        private readonly Vector2[] outer=new Vector2[8],inner=new Vector2[8];
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();float cut=Mathf.Min(14,Mathf.Min(r.width,r.height)*.15f),inset=1.5f;
            Corners(r,cut,outer);Corners(new Rect(r.xMin+inset,r.yMin+inset,Mathf.Max(0,r.width-2*inset),Mathf.Max(0,r.height-2*inset)),Mathf.Max(0,cut-inset),inner);
            for(int i=0;i<8;i++){vh.AddVert(outer[i],Edge,Vector2.zero);vh.AddVert(inner[i],Color.Lerp(new Color(color.r*.72f,color.g*.72f,color.b*.72f,color.a),color,Mathf.InverseLerp(r.yMin,r.yMax,inner[i].y)),Vector2.zero);}
            vh.AddVert(r.center,new Color(color.r*.85f,color.g*.85f,color.b*.85f,color.a),Vector2.zero);
            for(int i=0;i<8;i++){int j=(i+1)%8;vh.AddTriangle(i*2,j*2,i*2+1);vh.AddTriangle(j*2,j*2+1,i*2+1);vh.AddTriangle(16,i*2+1,j*2+1);}
        }
        private static void Corners(Rect r,float c,Vector2[] v)
        {
            v[0]=new Vector2(r.xMin+c,r.yMin);v[1]=new Vector2(r.xMax-c,r.yMin);v[2]=new Vector2(r.xMax,r.yMin+c);v[3]=new Vector2(r.xMax,r.yMax-c);
            v[4]=new Vector2(r.xMax-c,r.yMax);v[5]=new Vector2(r.xMin+c,r.yMax);v[6]=new Vector2(r.xMin,r.yMax-c);v[7]=new Vector2(r.xMin,r.yMin+c);
        }
    }
}
