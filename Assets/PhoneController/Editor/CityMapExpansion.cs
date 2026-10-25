using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyHigh.PhoneController.Editor
{
    /// <summary>Run explicitly from the menu. Original package assets are never edited.</summary>
    public static class CityMapExpansion
    {
        private const string RootName="City Map Expansion";
        private const string Folder="Assets/CityExpansionGenerated";
        private static readonly List<string> Created=new List<string>();
        private struct Segment { public Vector3 a,b; public float width; }

        [MenuItem("Fly High/Expand City Map")]
        public static void Expand()
        {
            if(Application.isPlaying){Debug.LogWarning("Stop Play mode before expanding the city.");return;}
            var scene=SceneManager.GetActiveScene();
            if(scene.name!="CityDrive"){Debug.LogWarning("Open CityDrive before expanding the map.");return;}
            var existing=GameObject.Find(RootName);
            if(existing!=null){Selection.activeGameObject=existing;Debug.Log("The expansion is already present; selected its root.");return;}
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Expand city map");Created.Clear();
            try
            {
                EditorUtility.DisplayProgressBar("Expand City Map","Finding a clear street connection",0.1f);
                Physics.SyncTransforms();var graph=new CityRoadGraph();graph.Build();
                if(graph.nodes.Count==0)throw new InvalidOperationException("No validated asphalt road points found. No map changes were kept.");
                Bounds city=new Bounds(graph.nodes[0].position,Vector3.zero);
                foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if(r.gameObject.scene==scene&&r.bounds.size.x<400&&r.bounds.size.z<400)city.Encapsulate(r.bounds);
                float entryX=city.max.x+36;Vector3 anchor=FindAnchor(graph,entryX);
                float level=anchor.y-0.45f+0.04f;Vector3 origin=new Vector3(entryX,level,anchor.z);
                var junctions=new List<Vector3>();var segments=new List<Segment>();
                foreach(var p in CityExpansionPlan.Junctions())junctions.Add(origin+new Vector3(p.x,0,p.z));
                foreach(var s in CityExpansionPlan.Streets())segments.Add(new Segment {a=junctions[s.a],b=junctions[s.b],width=12});
                segments.Add(new Segment {a=new Vector3(anchor.x-4,level,anchor.z),b=junctions[1],width=8});
                if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets","CityExpansionGenerated");
                var root=new GameObject(RootName);Undo.RegisterCreatedObjectUndo(root,"Create city extension");SceneManager.MoveGameObjectToScene(root,scene);
                OpenEntrance(new Rect(anchor.x-4,anchor.z-4.05f,entryX-anchor.x+5,8.1f),level,root);
                EditorUtility.DisplayProgressBar("Expand City Map","Adding streets, pavements and buildings",0.5f);
                var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/ithappy/Cartoon_City_Free/Materials/Asphalt_Dark_Gray.mat");
                if(asphalt==null)throw new InvalidOperationException("City asphalt material is missing.");
                var paint=Material("Expansion lane paint",new Color(0.92f,0.91f,0.78f));
                var pavement=Material("Expansion pavement",new Color(0.55f,0.53f,0.47f));
                var grass=Material("Expansion grass",new Color(0.29f,0.48f,0.23f));
                var road=new ExpansionMesh();var markings=new ExpansionMesh();var sidewalks=new ExpansionMesh();
                foreach(var s in segments){road.Strip(s.a,s.b,s.width);StreetDetail(s,segments,junctions,markings,sidewalks);}
                foreach(var p in junctions)road.Disc(p,7.2f);
                AddMesh(root.transform,"Road expansion asphalt",road,asphalt,true);
                AddMesh(root.transform,"Road lane markings",markings,paint,false);
                AddMesh(root.transform,"Pavement expansion",sidewalks,pavement,true);
                var ground=new ExpansionMesh();ground.Strip(origin+new Vector3(124,-0.08f,-132),origin+new Vector3(124,-0.08f,132),296);
                AddMesh(root.transform,"Neighbourhood ground",ground,grass,true);
                Decorate(root.transform,origin,segments,junctions);
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);Selection.activeGameObject=root;
                if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.Frame(new Bounds(origin+Vector3.right*124,new Vector3(296,60,264)),false);
                Debug.Log("[City expansion] Added 14 junctions, 19 district streets and an entrance from the original city. Save CityDrive, then restart Play to rebuild NPC routes. Ctrl+Z undoes the scene expansion.");
            }
            catch(Exception error)
            {
                Undo.RevertAllDownToGroup(undo);
                foreach(string path in Created)if(path.StartsWith(Folder+"/",StringComparison.Ordinal))AssetDatabase.DeleteAsset(path);
                Debug.LogError("[City expansion] "+error.Message);
            }
            finally{EditorUtility.ClearProgressBar();Created.Clear();}
        }

        private static Vector3 FindAnchor(CityRoadGraph graph,float entryX)
        {
            var candidates=new List<CityRoadGraph.Node>(graph.nodes);candidates.Sort((a,b)=>b.position.x.CompareTo(a.position.x));
            foreach(var node in candidates)
            {
                if(node.neighbours.Count==0)continue;float length=entryX-node.position.x;
                var hits=Physics.OverlapBox(node.position+new Vector3(length*0.5f,1.8f,0),new Vector3(length*0.5f,1.1f,4.2f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                bool clear=true;foreach(var hit in hits)if(hit.bounds.max.y>node.position.y+0.7f){clear=false;break;}
                if(clear)return node.position;
            }
            throw new InvalidOperationException("No clear eight-metre-wide east exit found. Move a parked car/prop out of a road exit corridor, then retry.");
        }
        private static void OpenEntrance(Rect corridor,float level,GameObject expansion)
        {
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(filter.gameObject.scene!=expansion.scene||filter.sharedMesh==null||filter.transform.IsChildOf(expansion.transform))continue;
                var r=filter.GetComponent<MeshRenderer>();if(r==null)continue;Bounds b=r.bounds;
                if(b.min.y>level+0.65f||b.max.x<corridor.xMin||b.min.x>corridor.xMax||b.max.z<corridor.yMin||b.min.z>corridor.yMax)continue;
                Mesh original=filter.sharedMesh;Mesh patch=CityEntranceCut.Cut(filter,corridor,level+0.65f);if(patch==null)continue;
                Save(patch,"Connection patch.asset");Undo.RecordObject(filter,"Open street entrance");filter.sharedMesh=patch;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                foreach(var collider in filter.GetComponents<MeshCollider>())if(collider.sharedMesh==original)
                {
                    Undo.RecordObject(collider,"Open collider entrance");collider.sharedMesh=patch.vertexCount>0?patch:null;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                }
            }
        }
        private static bool OnRoad(Vector3 p,List<Segment> roads,List<Vector3> junctions,float margin=0)
        {
            foreach(var s in roads)
            {
                Vector3 d=s.b-s.a;float t=Mathf.Clamp01(Vector3.Dot(p-s.a,d)/d.sqrMagnitude);Vector3 offset=p-Vector3.Lerp(s.a,s.b,t);offset.y=0;
                if(offset.sqrMagnitude<=Mathf.Pow(s.width*0.5f+margin,2))return true;
            }
            foreach(var j in junctions){Vector3 d=p-j;d.y=0;if(d.sqrMagnitude<=Mathf.Pow(7.2f+margin,2))return true;}
            return false;
        }
        private static void StreetDetail(Segment s,List<Segment> roads,List<Vector3> junctions,ExpansionMesh paint,ExpansionMesh pavement)
        {
            Vector3 along=(s.b-s.a).normalized,right=Vector3.Cross(Vector3.up,along);float length=Vector3.Distance(s.a,s.b);
            for(float t=9;t<length-9;t+=8)paint.Strip(s.a+along*t+Vector3.up*0.018f,s.a+along*Mathf.Min(t+3,length-8)+Vector3.up*0.018f,0.14f);
            for(float t=1.5f;t<length-1.5f;t+=3)foreach(float side in new[] {-1f,1f})
            {
                Vector3 p=s.a+along*t+right*side*(s.width*0.5f+1.6f);bool clear=true;
                foreach(float end in new[] {-1.5f,1.5f})foreach(float edge in new[] {-1.45f,1.45f})
                    if(OnRoad(p+along*end+right*edge,roads,junctions,0.02f))clear=false;
                if(clear)pavement.Box(p,along,3,2.9f,0.16f);
            }
        }
        private static void Decorate(Transform root,Vector3 origin,List<Segment> roads,List<Vector3> junctions)
        {
            var buildings=new List<GameObject>();
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[] {"Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("NightLight"))continue;
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset!=null)buildings.Add(asset);
            }
            var random=new System.Random(8137);
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)foreach(float dx in new[] {-13f,13f})foreach(float dz in new[] {-13f,13f})
            {
                if(buildings.Count==0)break;Vector3 p=origin+new Vector3(x*64+32+dx,-0.08f,z*64-32+dz);
                if(OnRoad(p,roads,junctions,12))continue;
                Place(root,buildings[random.Next(buildings.Count)],p,random.Next(4)*90,19,36);
            }
            var palm=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ithappy/Cartoon_City_Free/Prefabs/Vegetation/Palm_03.prefab");
            if(palm!=null)for(int x=0;x<2;x++)for(int z=0;z<2;z++)Place(root,palm,origin+new Vector3(x*64+32,-0.08f,z*64-32),0,5,14);
        }
        private static void Place(Transform root,GameObject prefab,Vector3 p,float yaw,float width,float height)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);Undo.RegisterCreatedObjectUndo(go,"Place neighbourhood prefab");
            go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.localScale*=Mathf.Min(width/Mathf.Max(0.1f,Mathf.Max(b.size.x,b.size.z)),height/Mathf.Max(0.1f,b.size.y));
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        private static Material Material(string name,Color color)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new InvalidOperationException("URP Lit is unavailable.");
            var material=new Material(shader){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",0.1f);Save(material,name+".mat");return material;
        }
        private static void Save(UnityEngine.Object asset,string name)
        {string path=AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name);AssetDatabase.CreateAsset(asset,path);Created.Add(path);}
        private static void AddMesh(Transform root,string name,ExpansionMesh builder,Material material,bool collision)
        {
            if(builder.indices.Count==0)return;var mesh=builder.Build(name);Save(mesh,name+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Create street geometry");go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
