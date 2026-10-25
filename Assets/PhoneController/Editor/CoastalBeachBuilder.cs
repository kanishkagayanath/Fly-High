using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FlyHigh.PhoneController.Editor
{
    /// <summary>Explicit, undoable authoring of one continuous island coastline.</summary>
    public static class CoastalBeachBuilder
    {
        private const string Folder = "Assets/CoastalIslandGenerated";
        private const string RootName = "Coastal Beach";
        private static readonly List<string> Created = new List<string>();
        private struct RimPoint { public Vector2 edge, outward; }

        [MenuItem("Fly High/Add Coastal Beach and Fog")]
        public static void AddBeach()
        { BuildBeach(false); }

        public static void RebuildBeach()
        { BuildBeach(true); }

        private static void BuildBeach(bool rebuild)
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "CityDrive")
                throw new InvalidOperationException("Open CityDrive outside Play mode before adding its beach.");
            var roots = scene.GetRootGameObjects();
            var existing = Array.Find(roots, g => g.name == RootName);
            if (!rebuild && existing != null && existing.transform.Find("Island shoreline") != null)
            { Selection.activeGameObject = existing.transform.Find("Island shoreline").gameObject; return; }
            var city = Array.Find(roots, g => g.name == "Demonstration");
            if (city == null) throw new InvalidOperationException("The original city root Demonstration is missing.");
            var renderers = city.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("The city has no visible geometry.");
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // Include authored districts when present, excluding the package's prefab showcase.
            foreach (var extra in roots) if (extra.name == "City Map Expansion")
                foreach (var r in extra.GetComponentsInChildren<MeshRenderer>()) bounds.Encapsulate(r.bounds);
            var waterShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/PhoneController/Shaders/CoastalWater.shader");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (waterShader == null || lit == null) throw new InvalidOperationException("Coastal water or URP Lit shader is unavailable.");

            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Surround city with beach and ocean"); Created.Clear();
            try
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "CoastalIslandGenerated");
                var root = new GameObject(RootName + " building");
                SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, "Create island coastline");
                root.transform.position = new Vector3(bounds.center.x, 0, bounds.center.z);
                Vector2 half = new Vector2(bounds.extents.x, bounds.extents.z);
                var rim = Outline(half);
                var dry = Material(lit, "Warm sand", new Color(0.88f,0.75f,0.48f));
                var damp = Material(lit, "Damp sand", new Color(0.68f,0.59f,0.39f));
                var underwater = Material(lit, "Submerged sand", new Color(0.49f,0.63f,0.49f));
                var ocean = Material(waterShader, "Island ocean", Color.white);
                ocean.SetFloat("_IslandMode", 1);
                ocean.SetVector("_IslandRect", new Vector4(bounds.center.x,bounds.center.z,half.x,half.y));
                ocean.SetColor("_HorizonColor", new Color(0.66f,0.83f,0.87f));
                MeshObject(root.transform, "Island shoreline", BeachMesh(rim), new[] {dry,damp,underwater}, true);
                MeshObject(root.transform, "Ocean - all four sides", OceanMesh(rim), new[] {ocean}, false);
                DecorateAllSides(root.transform, half, lit);

                var settings = new SerializedObject(Unsupported.GetRenderSettings());
                Undo.RecordObject(settings.targetObject, "Set coastal fog");
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.66f,0.83f,0.87f);
                RenderSettings.fogStartDistance = 95; RenderSettings.fogEndDistance = 620;
                EditorUtility.SetDirty(settings.targetObject);
                // Remove the old strip only after the complete replacement has been built.
                if (existing != null) Undo.DestroyObjectImmediate(existing);
                root.name = RootName;
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = root.transform.Find("Island shoreline").gameObject;
                SceneView.RepaintAll();
                Debug.Log("[Coast] Beach and ocean now surround all four city edges, with continuous rounded corners. Save CityDrive to keep the island coast.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var path in Created) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally { Created.Clear(); }
        }

        private static List<RimPoint> Outline(Vector2 half)
        {
            var points = new List<RimPoint>();
            var corners = new[] {new Vector2(-half.x,half.y),new Vector2(half.x,half.y),
                                 new Vector2(half.x,-half.y),new Vector2(-half.x,-half.y)};
            var normals = new[] {Vector2.up,Vector2.right,Vector2.down,Vector2.left};
            for (int side=0;side<4;side++)
            {
                var a=corners[side]; var b=corners[(side+1)%4];
                int count=Mathf.Max(16,Mathf.CeilToInt(Vector2.Distance(a,b)/4));
                for(int i=0;i<count;i++) points.Add(new RimPoint {edge=Vector2.Lerp(a,b,i/(float)count),outward=normals[side]});
                for(int i=0;i<12;i++)
                {
                    float angle=(side+i/12f)*Mathf.PI*0.5f;
                    points.Add(new RimPoint {edge=b,outward=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))});
                }
            }
            return points;
        }

        private static Mesh BeachMesh(List<RimPoint> rim)
        {
            var vertices=new List<Vector3>(); var triangles=new[] {new List<int>(),new List<int>(),new List<int>()};
            float[] offsets={-0.08f,19.8f,27.1f,39,60};
            float[] levels={-0.10f,-0.24f,-0.60f,-1.42f,-3.2f};
            for(int row=0;row<offsets.Length;row++) foreach(var point in rim)
            {
                // Rounded offset contours share exactly the same seam on every side.
                Vector2 p=point.edge+point.outward*offsets[row];
                vertices.Add(new Vector3(p.x,levels[row],p.y));
            }
            for(int row=0;row<4;row++)for(int i=0;i<rim.Count;i++)
            {
                int a=row*rim.Count+i,b=(row+1)*rim.Count+i;
                int next=(i+1)%rim.Count;
                var t=triangles[row<2?0:row==2?1:2];
                Triangle(vertices,t,a,b,row*rim.Count+next);
                Triangle(vertices,t,row*rim.Count+next,b,(row+1)*rim.Count+next);
            }
            var mesh=new Mesh {name="Continuous four-sided beach",subMeshCount=3};mesh.SetVertices(vertices);
            for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        private static Mesh OceanMesh(List<RimPoint> rim)
        {
            const int rows=88;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int row=0;row<=rows;row++) foreach(var point in rim)
            {
                float t=row/(float)rows,d=25+2600*t*t;
                Vector2 p=point.edge+point.outward*d;
                vertices.Add(new Vector3(p.x,-1.05f,p.y));
            }
            for(int row=0;row<rows;row++)for(int i=0;i<rim.Count;i++)
            {
                int a=row*rim.Count+i,b=(row+1)*rim.Count+i,next=(i+1)%rim.Count;
                Triangle(vertices,triangles,a,b,row*rim.Count+next);
                Triangle(vertices,triangles,row*rim.Count+next,b,(row+1)*rim.Count+next);
            }
            var mesh=new Mesh {name="Ocean around island",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var bounds=mesh.bounds;bounds.Expand(new Vector3(0,2,0));mesh.bounds=bounds;return mesh;
        }
        private static void Triangle(List<Vector3> vertices,List<int> target,int a,int b,int c)
        {
            if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude<0.00000001f)return;
            target.Add(a);target.Add(b);target.Add(c);
        }

        private static void DecorateAllSides(Transform root,Vector2 half,Shader lit)
        {
            var cream=Material(lit,"Umbrella cream",new Color(0.96f,0.92f,0.76f));
            var coral=Material(lit,"Umbrella coral",new Color(0.93f,0.32f,0.19f));
            var teal=Material(lit,"Umbrella teal",new Color(0.08f,0.57f,0.58f));
            var wood=Material(lit,"Beach wood",new Color(0.39f,0.25f,0.12f));
            var umbrella=UmbrellaMesh();Save(umbrella,"Striped umbrella.asset");
            var palm=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ithappy/Cartoon_City_Free/Prefabs/Vegetation/Palm_03.prefab");
            var positions=new[] {new Vector3(0,0,half.y),new Vector3(half.x,0,0),new Vector3(0,0,-half.y),new Vector3(-half.x,0,0)};
            var names=new[] {"North beach","East beach","South beach","West beach"};
            for(int side=0;side<4;side++)
            {
                var go=new GameObject(names[side]);Undo.RegisterCreatedObjectUndo(go,"Create beach side");
                var edge=go.transform;edge.SetParent(root,false);edge.localPosition=positions[side];edge.localRotation=Quaternion.Euler(0,90*side,0);
                float length=2*(side%2==0?half.x:half.y);
                for(int i=0;i<6 && palm!=null;i++)
                {
                    var p=new Vector3(Mathf.Lerp(-0.43f,0.43f,i/5f)*length,0,8+(i%2)*5);
                    var tree=(GameObject)PrefabUtility.InstantiatePrefab(palm,edge);Undo.RegisterCreatedObjectUndo(tree,"Place beach palm");
                    tree.name="Beach palm";tree.transform.localPosition=p;tree.transform.localRotation=Quaternion.Euler(0,i*73,0);
                    var rs=tree.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;
                    Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                    tree.transform.localScale*=7.5f/Mathf.Max(0.1f,b.size.y);
                    b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                    var target=edge.TransformPoint(p+Vector3.down*0.18f);
                    tree.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
                }
                for(int i=0;i<5;i++)
                {
                    float x=Mathf.Lerp(-0.32f,0.32f,i/4f)*length,z=18+(i%2)*3;
                    var shade=MeshObject(edge,"Beach umbrella "+(i+1),umbrella,new[] {cream,i%2==0?coral:teal},false,false);
                    shade.transform.localPosition=new Vector3(x,-0.20f,z);shade.transform.localRotation=Quaternion.Euler(0,i*37,0);
                    Primitive(edge,"Umbrella pole",PrimitiveType.Cylinder,new Vector3(x,1.15f,z),new Vector3(0.065f,1.35f,0.065f),wood);
                    var mat=Primitive(edge,"Beach mat",PrimitiveType.Cube,new Vector3(x+1,-0.12f,z-1.5f),new Vector3(1.2f,0.045f,2.4f),i%2==0?teal:coral);
                    mat.transform.localRotation=Quaternion.Euler(0,i*19,0);
                }
            }
        }

        private static Mesh UmbrellaMesh()
        {
            var mesh=new Mesh {name="Low poly striped beach umbrella",subMeshCount=2};
            var vertices=new List<Vector3>();var a=new List<int>();var b=new List<int>();
            for(int i=0;i<12;i++)
            {
                float p=i*Mathf.PI/6,q=(i+1)*Mathf.PI/6;int start=vertices.Count;
                vertices.Add(new Vector3(0,3,0));vertices.Add(new Vector3(Mathf.Sin(p)*2.1f,2.25f,Mathf.Cos(p)*2.1f));vertices.Add(new Vector3(Mathf.Sin(q)*2.1f,2.25f,Mathf.Cos(q)*2.1f));
                (i%2==0?a:b).AddRange(new[] {start,start+1,start+2});
            }
            mesh.SetVertices(vertices);mesh.SetTriangles(a,0);mesh.SetTriangles(b,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        private static GameObject Primitive(Transform root,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;Undo.RegisterCreatedObjectUndo(go,"Add beach decoration");
            go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            var collider=go.GetComponent<Collider>();if(collider!=null)UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }
        private static Material Material(Shader shader,string name,Color color)
        {
            var m=new Material(shader){name=name};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",0.12f);Save(m,name+".mat");return m;
        }
        private static void Save(UnityEngine.Object asset,string name)
        {
            string path=AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name);
            AssetDatabase.CreateAsset(asset,path);Created.Add(path);
        }
        private static GameObject MeshObject(Transform root,string name,Mesh mesh,Material[] materials,bool collision,bool save=true)
        {
            if(save)Save(mesh,name+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Create coast geometry");
            go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;
            if(name.StartsWith("Ocean",StringComparison.Ordinal)){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
            return go;
        }
    }
}
