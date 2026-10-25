using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FlyHigh.PhoneController.Editor
{
    /// <summary>Expands street corridors while preserving the size of buildings and vehicles.</summary>
    public static class CityRoadWidening
    {
        private const string Folder="Assets/WideRoadsGenerated";
        private const string Marker="Wide roads 1.8x";
        private const float Factor=1.8f,HalfRoad=3.75f;
        private static readonly float[] XStreets={-52.5f,-7.5f,52.5f};
        private static readonly float[] ZStreets={-67.5f,22.5f,67.5f};
        private static readonly List<string> Created=new List<string>();
        private sealed class Surface { public MeshFilter filter; public Mesh source; public Matrix4x4 original; }

        public static float MapAxis(float value,float[] streets)
        {
            float shift=0;
            foreach(float centre in streets)
                shift+=Mathf.Clamp(value-centre,-HalfRoad,HalfRoad)-Mathf.Clamp(-centre,-HalfRoad,HalfRoad);
            return value+(Factor-1)*shift;
        }
        public static Vector3 Map(Vector3 point)
        {return new Vector3(MapAxis(point.x,XStreets),point.y,MapAxis(point.z,ZStreets));}
        private static float Derivative(float value,float[] streets)
        {foreach(float centre in streets)if(Mathf.Abs(value-centre)<HalfRoad)return Factor;return 1;}

        [MenuItem("Fly High/Widen City Roads")]
        public static void Widen()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.name!="CityDrive")
                throw new InvalidOperationException("Open CityDrive outside Play mode before widening roads.");
            var city=Array.Find(scene.GetRootGameObjects(),g=>g.name=="Demonstration");
            if(city==null)throw new InvalidOperationException("The original city root is missing.");
            if(city.transform.Find(Marker)!=null){Selection.activeGameObject=city;return;}
            if((city.transform.lossyScale-Vector3.one).sqrMagnitude>0.00001f || Quaternion.Angle(city.transform.rotation,Quaternion.identity)>0.01f || city.transform.position.sqrMagnitude>0.00001f)
                throw new InvalidOperationException("The original city must retain its unscaled origin for this layout-specific widening tool.");
            if(Array.Exists(scene.GetRootGameObjects(),g=>g.name=="City Map Expansion"))
                throw new InvalidOperationException("This tool targets the original island layout. An expanded district needs its road connections authored together.");
            var surfaces=new List<Surface>();var objects=new List<Transform>();
            foreach(Transform child in city.transform)
            {
                objects.Add(child);
                if(!child.name.StartsWith("road",StringComparison.OrdinalIgnoreCase) && !child.name.StartsWith("Set_B_Tiles",StringComparison.Ordinal))continue;
                foreach(var filter in child.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)
                    surfaces.Add(new Surface {filter=filter,source=filter.sharedMesh,original=filter.transform.localToWorldMatrix});
            }
            if(surfaces.Count<70)throw new InvalidOperationException("Expected the original city road and pavement tiles. No changes were made.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Widen city roads");Created.Clear();
            try
            {
                if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets","WideRoadsGenerated");
                foreach(var t in objects)
                {
                    Undo.RecordObject(t,"Move city block and street furniture");t.position=Map(t.position);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                }
                foreach(var surface in surfaces)
                {
                    var mesh=WarpMesh(surface);
                    string path=AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+surface.filter.name+" widened.asset");
                    AssetDatabase.CreateAsset(mesh,path);Created.Add(path);
                    Undo.RecordObject(surface.filter,"Widen road surface");surface.filter.sharedMesh=mesh;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(surface.filter);
                    foreach(var collider in surface.filter.GetComponents<MeshCollider>())if(collider.sharedMesh==surface.source)
                    {
                        Undo.RecordObject(collider,"Widen road collision");collider.sharedMesh=mesh;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                    }
                }
                // Optional authored spawn markers outside the city root must move with their road.
                foreach(var spawn in UnityEngine.Object.FindObjectsByType<CityCarSpawn>(FindObjectsSortMode.None))
                    if(spawn.gameObject.scene==scene && !spawn.transform.IsChildOf(city.transform))
                    {Undo.RecordObject(spawn.transform,"Move car spawn with road");spawn.transform.position=Map(spawn.transform.position);PrefabUtility.RecordPrefabInstancePropertyModifications(spawn.transform);}
                var marker=new GameObject(Marker);Undo.RegisterCreatedObjectUndo(marker,"Mark widened road layout");marker.transform.SetParent(city.transform,false);
                CoastalBeachBuilder.RebuildBeach();
                Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);Selection.activeGameObject=city;
                Debug.Log("[Wide roads] Street corridors are now 1.8 times their original width. Junctions, road colliders and beach follow the new layout; buildings and cars keep their size. Save CityDrive and restart Play to rebuild traffic routes.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach(string path in Created)AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally{Created.Clear();}
        }

        private static Mesh WarpMesh(Surface surface)
        {
            Mesh source=surface.source;
            using(var data=MeshUtility.AcquireReadOnlyMeshData(source))
            using(var positions=new NativeArray<Vector3>(source.vertexCount,Allocator.Temp))
            using(var normals=new NativeArray<Vector3>(source.vertexCount,Allocator.Temp))
            {
                data[0].GetVertices(positions);bool hasNormals=source.HasVertexAttribute(VertexAttribute.Normal);
                if(hasNormals)data[0].GetNormals(normals);
                var vertices=new List<Vector3>(source.vertexCount);var newNormals=new List<Vector3>(source.vertexCount);
                var worldNormal=surface.original.inverse.transpose;
                for(int i=0;i<source.vertexCount;i++)
                {
                    Vector3 world=surface.original.MultiplyPoint3x4(positions[i]);
                    vertices.Add(surface.filter.transform.InverseTransformPoint(Map(world)));
                    if(hasNormals)
                    {
                        Vector3 n=worldNormal.MultiplyVector(normals[i]);
                        n=new Vector3(n.x/Derivative(world.x,XStreets),n.y,n.z/Derivative(world.z,ZStreets));
                        newNormals.Add(surface.filter.transform.localToWorldMatrix.transpose.MultiplyVector(n).normalized);
                    }
                }
                var mesh=new Mesh {name=source.name+" wide road",indexFormat=IndexFormat.UInt32,subMeshCount=source.subMeshCount};
                mesh.SetVertices(vertices);if(hasNormals)mesh.SetNormals(newNormals);
                for(int channel=0;channel<8;channel++)
                    if(source.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0+channel)))
                        using(var uv=new NativeArray<Vector4>(source.vertexCount,Allocator.Temp))
                        {data[0].GetUVs(channel,uv);mesh.SetUVs(channel,new List<Vector4>(uv.ToArray()));}
                if(source.HasVertexAttribute(VertexAttribute.Color))
                    using(var colors=new NativeArray<Color>(source.vertexCount,Allocator.Temp))
                    {data[0].GetColors(colors);mesh.SetColors(new List<Color>(colors.ToArray()));}
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    var descriptor=source.GetSubMesh(sub);
                    if(descriptor.topology!=MeshTopology.Triangles)throw new InvalidOperationException("Road surface uses non-triangle geometry.");
                    var indices=new int[descriptor.indexCount];
                    for(int i=0;i<indices.Length;i++)indices[i]=(source.indexFormat==IndexFormat.UInt16?data[0].GetIndexData<ushort>()[descriptor.indexStart+i]:data[0].GetIndexData<int>()[descriptor.indexStart+i])+descriptor.baseVertex;
                    mesh.SetTriangles(indices,sub);
                }
                if(!hasNormals)mesh.RecalculateNormals();
                if(source.HasVertexAttribute(VertexAttribute.Tangent) && mesh.HasVertexAttribute(VertexAttribute.TexCoord0))mesh.RecalculateTangents();
                mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
