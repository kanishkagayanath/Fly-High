using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace FlyHigh.PhoneController.Editor
{
    public static class PedestrianNavigationBuilder
    {
        private const string Folder="Assets/PhoneController/Pedestrians";
        private const string SettingsPath="Assets/PhoneController/Resources/FlyHighPedestrians.asset";
        private const string NavigationPath=Folder+"/City pedestrian routes.asset";
        [MenuItem("Fly High/City/Bake pedestrian routes")]
        public static void Bake()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before baking pedestrian routes.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="CityDrive")throw new InvalidOperationException("Open CityDrive to bake pedestrian routes.");
            var generated=new List<Mesh>();var sources=new List<NavMeshBuildSource>();var floorObjects=new HashSet<GameObject>();Bounds bounds=default;bool hasBounds=false;
            NavMeshDataInstance temporary=default;
            try
            {
                foreach(var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
                {
                    if(!collider.enabled||collider.isTrigger||collider.attachedRigidbody!=null||collider.sharedMesh==null||(!PedestrianSurfaces.IsBeach(collider)&&!PedestrianSurfaces.IsPavement(collider)))continue;
                    var renderer=collider.GetComponent<MeshRenderer>();if(renderer==null)continue;
                    var original=collider.sharedMesh;var vertices=original.vertices;var materials=renderer.sharedMaterials;
                    var walk=new List<int>();var blocked=new List<int>();bool beach=PedestrianSurfaces.IsBeach(collider);
                    for(int i=0;i<vertices.Length;i++)vertices[i]=collider.transform.TransformPoint(vertices[i]);
                    for(int sub=0;sub<original.subMeshCount;sub++)
                    {
                        if(original.GetTopology(sub)!=MeshTopology.Triangles)continue;
                        bool allowed=sub<materials.Length&&materials[sub]!=null&&PedestrianSurfaces.AllowedMaterial(materials[sub].name,beach);
                        int[] indices=original.GetTriangles(sub);
                        for(int t=0;t<indices.Length;t+=3)
                        {
                            Vector3 a=vertices[indices[t]],b=vertices[indices[t+1]],c=vertices[indices[t+2]];
                            if(Vector3.Cross(b-a,c-a).normalized.y<.8f)continue;
                            bool dry=!beach||(a.y>=-.36f&&b.y>=-.36f&&c.y>=-.36f);
                            var target=allowed&&dry?walk:blocked;target.Add(indices[t]);target.Add(indices[t+1]);target.Add(indices[t+2]);
                            if(allowed&&dry){if(!hasBounds){bounds=new Bounds(a,Vector3.zero);hasBounds=true;}bounds.Encapsulate(a);bounds.Encapsulate(b);bounds.Encapsulate(c);}
                        }
                    }
                    if(walk.Count>0)AddMesh(vertices,walk,0,sources,generated);
                    if(blocked.Count>0)AddMesh(vertices,blocked,1,sources,generated);
                    floorObjects.Add(collider.gameObject);
                }
                if(!hasBounds)throw new InvalidOperationException("No pavement/beach geometry found.");
                var obstacleSources=new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources((Transform)null,~0,NavMeshCollectGeometry.RenderMeshes,1,new List<NavMeshBuildMarkup>(),obstacleSources);
                foreach(var source in obstacleSources)if(source.component!=null&&!floorObjects.Contains(source.component.gameObject))sources.Add(source);
                // Building bounds are conservative solid blockers, preventing spawns inside hollow models.
                foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(renderer.name.StartsWith("Eco_Building")||renderer.name.StartsWith("Regular_Building"))
                {
                    var b=renderer.bounds;sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(b.center,Quaternion.identity,Vector3.one),size=b.size,area=1});
                }
                bounds.Expand(new Vector3(4,6,4));
                var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.32f;settings.agentHeight=1.8f;settings.agentClimb=.22f;settings.agentSlope=35;settings.minRegionArea=2;settings.overrideVoxelSize=true;settings.voxelSize=.1f;settings.overrideTileSize=true;settings.tileSize=128;
                if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/PhoneController","Pedestrians");
                var data=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath);
                if(data==null){data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(data==null)throw new InvalidOperationException("Pedestrian NavMesh bake failed.");data.name="City pedestrian routes";AssetDatabase.CreateAsset(data,NavigationPath);}
                else if(!NavMeshBuilder.UpdateNavMeshData(data,settings,sources,bounds))throw new InvalidOperationException("Pedestrian NavMesh update failed.");
                EditorUtility.SetDirty(data);temporary=NavMesh.AddNavMeshData(data);
                var surfaces=new PedestrianSurfaces();var points=SampleWaypoints(bounds,surfaces);
                var asset=AssetDatabase.LoadAssetAtPath<PedestrianSettings>(SettingsPath);
                if(asset==null){asset=ScriptableObject.CreateInstance<PedestrianSettings>();AssetDatabase.CreateAsset(asset,SettingsPath);}
                var characters=new List<GameObject>();
                for(int i=1;i<=10;i++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Hodaart/CasualCharacterCollection3/Prefabs/Characters/Character "+i.ToString("D2")+".prefab");if(prefab!=null)characters.Add(prefab);}
                if(characters.Count==0)throw new InvalidOperationException("No imported character prefabs found.");
                asset.navigation=data;asset.characters=characters.ToArray();asset.waypoints=points.ToArray();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
                int beachPoints=0;var regions=new HashSet<int>();foreach(var point in points){if(point.beach)beachPoints++;regions.Add(point.region);}
                Debug.Log("[Pedestrians] Baked "+points.Count+" safe destinations ("+beachPoints+" beach), "+regions.Count+" disconnected walking regions. Roads excluded; no crossing links.");
            }
            finally{if(temporary.valid)temporary.Remove();foreach(var mesh in generated)Object.DestroyImmediate(mesh);}
        }
        private static void AddMesh(Vector3[] vertices,List<int> triangles,int area,List<NavMeshBuildSource> sources,List<Mesh> generated)
        {
            var mesh=new Mesh{name="Pedestrian bake source",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();generated.Add(mesh);
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh,transform=Matrix4x4.identity,area=area});
        }
        private static List<PedestrianWaypoint> SampleWaypoints(Bounds bounds,PedestrianSurfaces surfaces)
        {
            var mesh=NavMesh.CalculateTriangulation();var parent=new int[mesh.vertices.Length];for(int i=0;i<parent.Length;i++)parent[i]=i;
            var duplicates=new Dictionary<Vector3Int,int>();
            for(int i=0;i<parent.Length;i++)
            {
                Vector3 p=mesh.vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*20),Mathf.RoundToInt(p.y*20),Mathf.RoundToInt(p.z*20));
                if(duplicates.TryGetValue(key,out int earlier))Union(parent,i,earlier);else duplicates.Add(key,i);
            }
            for(int i=0;i<mesh.indices.Length;i+=3){Union(parent,mesh.indices[i],mesh.indices[i+1]);Union(parent,mesh.indices[i],mesh.indices[i+2]);}
            var points=new List<PedestrianWaypoint>();var unique=new HashSet<Vector3Int>();var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=1};
            for(float x=Mathf.Ceil(bounds.min.x/3)*3;x<=bounds.max.x;x+=3)for(float z=Mathf.Ceil(bounds.min.z/3)*3;z<=bounds.max.z;z+=3)
            {
                if(!NavMesh.SamplePosition(new Vector3(x,.1f,z),out var hit,1.2f,filter)||!surfaces.Contains(hit.position,out bool beach))continue;
                var key=new Vector3Int(Mathf.RoundToInt(hit.position.x*2),Mathf.RoundToInt(hit.position.y*2),Mathf.RoundToInt(hit.position.z*2));if(!unique.Add(key))continue;
                int region=-1;
                for(int t=0;t<mesh.indices.Length;t+=3)
                {
                    Vector3 a=mesh.vertices[mesh.indices[t]],b=mesh.vertices[mesh.indices[t+1]],c=mesh.vertices[mesh.indices[t+2]];
                    if(Inside(hit.position,a,b,c)){region=Find(parent,mesh.indices[t]);break;}
                }
                if(region>=0)points.Add(new PedestrianWaypoint{position=hit.position,region=region,beach=beach});
            }
            var sizes=new Dictionary<int,int>();var extents=new Dictionary<int,Bounds>();
            foreach(var point in points){if(!sizes.ContainsKey(point.region)){sizes.Add(point.region,0);extents.Add(point.region,new Bounds(point.position,Vector3.zero));}sizes[point.region]++;var b=extents[point.region];b.Encapsulate(point.position);extents[point.region]=b;}
            points.RemoveAll(point=>sizes[point.region]<8||extents[point.region].size.sqrMagnitude<64);
            return points;
        }
        private static bool Inside(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            float ab=(b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x),bc=(c.x-b.x)*(p.z-b.z)-(c.z-b.z)*(p.x-b.x),ca=(a.x-c.x)*(p.z-c.z)-(a.z-c.z)*(p.x-c.x);
            return (ab>=-.0001f&&bc>=-.0001f&&ca>=-.0001f)||(ab<=.0001f&&bc<=.0001f&&ca<=.0001f);
        }
        private static int Find(int[] parent,int n){while(parent[n]!=n){parent[n]=parent[parent[n]];n=parent[n];}return n;}
        private static void Union(int[] parent,int a,int b){a=Find(parent,a);b=Find(parent,b);if(a!=b)parent[b]=a;}
    }
}

