using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>North-up map with cached scene road geometry and its original lane-marking UVs.</summary>
    public sealed class CityMinimap : MonoBehaviour
    {
        [SerializeField, Min(20)] private float viewRadius = 55;
        private const float Diameter = 212;
        private Transform player;
        private RectTransform root, content, arrow;
        private MinimapGraphic roads;
        private readonly Dictionary<Texture,MinimapGraphic> texturedRoads = new Dictionary<Texture,MinimapGraphic>();
        private float pixelsPerMetre;
        private static readonly Color Sea = new Color(0.055f,0.20f,0.25f);
        private static readonly Color Sand = new Color(0.52f,0.52f,0.36f);
        private static readonly Color Pavement = new Color(0.25f,0.32f,0.33f);
        private static readonly Color Asphalt = new Color(0.16f,0.18f,0.19f);
        private static readonly Color Building = new Color(0.09f,0.15f,0.19f);

        public void Initialize(Transform target, RectTransform hud)
        {
            if (root != null) return;
            player = target;
            pixelsPerMetre = Diameter / (2 * viewRadius);
            root = Element(hud,"Circular minimap",new Vector2(220,220));
            root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
            root.anchoredPosition = new Vector2(-20,-20);
            var shadow = Disc(root,"Map shadow",232,new Color(0,0,0,0.4f));
            shadow.rectTransform.anchoredPosition = new Vector2(2,-3);
            Disc(root,"Map border",224,new Color(0.025f,0.065f,0.09f));
            Disc(root,"Map rim",218,new Color(0.36f,0.54f,0.56f));
            var clip = Disc(root,"Round map mask",Diameter,Sea);
            clip.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            content = Element(clip.rectTransform,"Road map content",new Vector2(4096,4096));
            roads = content.gameObject.AddComponent<MinimapGraphic>();
            BuildMap(); roads.Finish();
            foreach (var graphic in texturedRoads.Values) graphic.Finish();

            Disc(clip.rectTransform,"Player marker halo",22,new Color(0,0,0,0.72f));
            arrow = Element(clip.rectTransform,"Player direction",new Vector2(20,20));
            Arrow(arrow,"Arrow outline",1.2f,new Color(0.015f,0.06f,0.08f));
            Arrow(arrow,"Arrow",1,new Color(0.34f,1,0.81f));
            var north = Disc(root,"North badge",21,new Color(0.025f,0.065f,0.09f));
            north.rectTransform.anchoredPosition = new Vector2(0,100);
            var textRect = Element(north.rectTransform,"North",new Vector2(20,20));
            var label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = "N"; label.fontSize = 13; label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.84f,0.96f,0.96f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            Follow();
        }

        private void BuildMap()
        {
            var scene = gameObject.scene;
            foreach (var obj in scene.GetRootGameObjects()) if (obj.name == "Coastal Beach")
            {
                var shoreline = obj.transform.Find("Island shoreline");
                if (shoreline != null) AddSurface(shoreline.GetComponent<MeshFilter>(),Sand,false);
            }
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.name != "Demonstration" && obj.name != "City Map Expansion") continue;
                foreach (var filter in obj.GetComponentsInChildren<MeshFilter>())
                {
                    string name = filter.name;
                    if (name.StartsWith("Set_B_Tiles",StringComparison.OrdinalIgnoreCase) ||
                        name.IndexOf("pavement",StringComparison.OrdinalIgnoreCase) >= 0 || name == "Neighbourhood ground")
                        AddSurface(filter,Pavement,false);
                }
                foreach (var filter in obj.GetComponentsInChildren<MeshFilter>())
                    if (filter.name.IndexOf("road",StringComparison.OrdinalIgnoreCase) >= 0)
                        AddSurface(filter,Pavement,true);
                foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.name.StartsWith("Eco_Building",StringComparison.Ordinal) || renderer.name.StartsWith("Regular_Building",StringComparison.Ordinal))
                    {
                        Bounds b=renderer.bounds;
                        Quad(new Vector2(b.min.x,b.min.z),new Vector2(b.max.x,b.max.z),Building);
                    }
            }
        }

        private void AddSurface(MeshFilter filter, Color tint, bool roadMaterials)
        {
            if (filter == null || filter.sharedMesh == null) return;
            var mesh = filter.sharedMesh;
            // Imported non-readable models keep a bounded footprint fallback. Authored widened
            // road meshes are readable, so their actual curves and junctions are used here.
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            if (!mesh.isReadable)
            {
                Bounds b=renderer.bounds;
                Quad(new Vector2(b.min.x,b.min.z),new Vector2(b.max.x,b.max.z),roadMaterials?Asphalt:tint);
                return;
            }
            var vertices = mesh.vertices; var materials = renderer.sharedMaterials;
            var uvs = roadMaterials ? mesh.uv : null;
            for (int sub=0;sub<mesh.subMeshCount;sub++)
            {
                if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
                var material = sub<materials.Length ? materials[sub] : null;
                bool asphalt = roadMaterials && material!=null && RoadTriangleMask.AsphaltMaterial(material.name);
                bool paint = roadMaterials && material!=null && material.name.IndexOf("lane paint",StringComparison.OrdinalIgnoreCase)>=0;
                Color surface = asphalt ? Asphalt : paint ? Color.white : tint;
                // Preserve the scene's road atlas and UVs, including curved lane lines and
                // junction markings. The UI shader stays unlit and supports the round mask.
                Texture texture = asphalt && uvs.Length==vertices.Length ? material.mainTexture : null;
                MinimapGraphic textured = texture != null ? RoadLayer(texture) : null;
                Vector2 scale = texture != null ? material.mainTextureScale : Vector2.one;
                Vector2 offset = texture != null ? material.mainTextureOffset : Vector2.zero;
                var triangles = mesh.GetTriangles(sub);
                for (int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 a=filter.transform.TransformPoint(vertices[triangles[i]]);
                    Vector3 b=filter.transform.TransformPoint(vertices[triangles[i+1]]);
                    Vector3 c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                    if (Vector3.Cross(b-a,c-a).y <= 0.00001f) continue;
                    if (textured != null)
                        textured.AddTriangle(Point(a),Point(b),Point(c),
                            Vector2.Scale(uvs[triangles[i]],scale)+offset,
                            Vector2.Scale(uvs[triangles[i+1]],scale)+offset,
                            Vector2.Scale(uvs[triangles[i+2]],scale)+offset,material.color);
                    else roads.AddTriangle(Point(a),Point(b),Point(c),surface);
                }
            }
        }
        private MinimapGraphic RoadLayer(Texture texture)
        {
            if (texturedRoads.TryGetValue(texture,out var graphic)) return graphic;
            graphic = Element(content,"Road texture - " + texture.name,content.sizeDelta).gameObject.AddComponent<MinimapGraphic>();
            graphic.SetTexture(texture);
            texturedRoads.Add(texture,graphic);
            return graphic;
        }
        private Vector2 Point(Vector3 world) { return new Vector2(world.x,world.z)*pixelsPerMetre; }
        private void Quad(Vector2 min,Vector2 max,Color tint)
        {
            min*=pixelsPerMetre;max*=pixelsPerMetre;
            var b=new Vector2(min.x,max.y);var d=new Vector2(max.x,min.y);
            roads.AddTriangle(min,b,max,tint);roads.AddTriangle(min,max,d,tint);
        }
        private void LateUpdate() { Follow(); }
        private void Follow()
        {
            if (player == null || root == null || !root.gameObject.activeInHierarchy) return;
            // Use the same interpolated car Transform as the smooth driving camera.
            content.anchoredPosition = -Point(player.position);
            arrow.localRotation = Quaternion.Euler(0,0,-player.eulerAngles.y);
        }
        private void OnDestroy() { if (root != null) Destroy(root.gameObject); }

        private static RectTransform Element(Transform parent,string name,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);rect.sizeDelta=size;rect.anchoredPosition=Vector2.zero;return rect;
        }
        private static MinimapGraphic Disc(Transform parent,string name,float diameter,Color color)
        {
            var graphic=Element(parent,name,new Vector2(diameter,diameter)).gameObject.AddComponent<MinimapGraphic>();graphic.SetCircle(color);return graphic;
        }
        private static void Arrow(Transform parent,string name,float scale,Color color)
        {
            var graphic=Element(parent,name,new Vector2(20,20)).gameObject.AddComponent<MinimapGraphic>();
            Vector2 a=new Vector2(0,9)*scale,b=new Vector2(-6,-6)*scale,c=new Vector2(0,-3)*scale,d=new Vector2(6,-6)*scale;
            graphic.AddTriangle(a,b,c,color);graphic.AddTriangle(a,c,d,color);graphic.Finish();
        }
    }
}
