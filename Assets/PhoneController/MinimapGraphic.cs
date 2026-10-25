using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Small vector shapes and cached map triangles rendered by the existing uGUI Canvas.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MinimapGraphic : UnityEngine.UI.MaskableGraphic
    {
        private readonly List<Vector2> points = new List<Vector2>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private Texture mapTexture;
        private bool circle;

        public override Texture mainTexture { get { return mapTexture != null ? mapTexture : base.mainTexture; } }

        public void SetTexture(Texture texture)
        { mapTexture = texture; SetMaterialDirty(); }

        public void SetCircle(Color tint)
        { circle = true; color = tint; raycastTarget = false; SetVerticesDirty(); }

        public void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Color tint)
        { AddTriangle(a,b,c,Vector2.zero,Vector2.zero,Vector2.zero,tint); }

        public void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, Color tint)
        {
            // uGUI meshes use 16-bit indices. This map is built once and stays below that limit.
            if (points.Count > 60000) return;
            points.Add(a); points.Add(b); points.Add(c);
            uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC);
            Color32 packed = tint; colors.Add(packed); colors.Add(packed); colors.Add(packed);
        }
        public void Finish() { raycastTarget = false; SetVerticesDirty(); }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            if (circle)
            {
                Rect rect = rectTransform.rect;
                Vector2 centre = rect.center; float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
                mesh.AddVert(centre, color, Vector2.zero);
                const int segments = 96;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    mesh.AddVert(centre + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius, color, Vector2.zero);
                    if (i > 0) mesh.AddTriangle(0, i, i + 1);
                }
            }
            else
            {
                for (int i = 0; i < points.Count; i++) mesh.AddVert(points[i], colors[i], uvs[i]);
                for (int i = 0; i < points.Count; i += 3) mesh.AddTriangle(i, i + 1, i + 2);
            }
        }
    }
}
