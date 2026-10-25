using UnityEngine;

namespace FlyHigh.PhoneController
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuGradient : UnityEngine.UI.MaskableGraphic
    {
        private Color left, right;
        public void Initialize(Color from, Color to) { left=from;right=to;raycastTarget=false;SetVerticesDirty(); }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();Rect rect=rectTransform.rect;
            mesh.AddVert(new Vector2(rect.xMin,rect.yMin),left,Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax,rect.yMin),right,Vector2.zero);
            float shoulder=Mathf.Lerp(rect.yMin,rect.yMax,.78f);
            mesh.AddVert(new Vector2(rect.xMin,shoulder),left,Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax,shoulder),right,Vector2.zero);
            Color topLeft=left,topRight=right;topLeft.a=topRight.a=0;
            mesh.AddVert(new Vector2(rect.xMin,rect.yMax),topLeft,Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax,rect.yMax),topRight,Vector2.zero);
            mesh.AddTriangle(0,2,3);mesh.AddTriangle(3,1,0);
            mesh.AddTriangle(2,4,5);mesh.AddTriangle(5,3,2);
        }
    }
}
